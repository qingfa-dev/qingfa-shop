# Event-Driven Architecture — Master Design

- **Status:** Approved
- **Date:** 2026-10-29
- **Branch:** `feature/service-foundation`
- **Scope owner:** Platform

## 1. Context

`QingFa.Shop` already has a DDD kernel, CQRS through MediatR, domain events, an EF Core
unit of work and a vertical-slice Todo domain. What it does not have is a reliable path
from a domain decision to another process:

- Aggregate events are dispatched in-memory after `SaveChanges`; a crash or handler
  failure loses them.
- There is no transaction boundary around command handlers.
- `WolverineFx`, `WolverineFx.RabbitMQ` and `Aspire.Hosting.RabbitMQ` are declared but
  unused. No integration events, no consumers, no retry or dead-letter policy exist.
- There is no event store, rehydration, snapshot or projection infrastructure.
- Several DDD building blocks advertised by the layer READMEs are absent:
  specifications, domain policies, event versioning, enforced optimistic concurrency.

The user approved closing all of these gaps. The e-commerce domain itself
(catalog, cart, order, payment) is explicitly out of scope.

## 2. Decisions

| Decision | Choice |
|---|---|
| Event-sourcing depth | Full framework plus pilot aggregate, then migrate `Todo` |
| Reliability owner | Our own outbox and inbox tables; Wolverine is only the transport |
| Event-store backing | PostgreSQL first, behind provider-neutral Application contracts |
| Transport | RabbitMQ in AppHost/Compose, Wolverine in-memory in unit and API tests |
| Domain scope | DDD building blocks and patterns, not a new shop domain |
| Delivery | Four sequential slices, each with its own spec and plan |

## 3. Target architecture

```text
                         ┌──────────────────────────────┐
 HTTP ──► Carter ──► MediatR ──► TransactionBehavior    │
                         │                              │
                         ▼                              │
                 Command handler                        │
                         │                              │
                         ▼                              │
          Aggregate / EventSourcedAggregate             │
                         │                              │
                         ▼                              │
      SaveChanges (one transaction)                     │
        ├─ entity state                                 │
        ├─ outbox rows                                  │
        └─ event-store rows                             │
                         │                              │
                         ▼                              │
                OutboxDispatcher (worker)               │
                  ├─ Domain event ──► MediatR handlers  │
                  └─ Integration event ──► IEventBus ───┘
                                              │
                                       Wolverine / RabbitMQ
                                              │
                                              ▼
                                    Consumer + Inbox dedupe
```

### Layer ownership

| Layer | Owns |
|---|---|
| `Core` | Specifications, domain policies, event-sourced aggregate base, event contracts |
| `Application` | Ports for outbox, inbox, bus, event store, snapshots, projections; transaction behavior; integration-event contracts |
| `Infrastructure` | EF outbox/inbox/event-store adapters, dispatcher, serializer, Wolverine transport, projection worker, health checks |
| `app/Web.Api` | Composition only; no messaging types |
| `app/AppHost`, `compose.yaml` | RabbitMQ resource and connection wiring |

### Non-negotiable invariants

- `Core` has no project references and no EF, Wolverine or RabbitMQ types.
- `Application` references `Core` only; it never references `Infrastructure`.
- A domain event is written to the outbox in the same transaction as the state change
  that produced it. If the transaction rolls back, no outbox row exists.
- Delivery is at-least-once. Consumers are idempotent through the inbox.
- Handlers may be replayed. No handler may assume exactly-once execution.
- Outbox and inbox rows are never used as a business source of truth.

## 4. Delivery sequence

| Slice | Spec | Gate |
|---|---|---|
| ① Domain design closure | `2026-10-29-domain-design-closure-design.md` | Unit + Postgres concurrency tests green |
| ② Reliable events | `2026-10-29-reliable-events-design.md` | Outbox atomicity, retry and inbox dedupe tests green |
| ③ Messaging | `2026-10-29-messaging-design.md` | Testcontainers RabbitMQ publish/consume/retry/DLQ green |
| ④ Event sourcing | `2026-10-29-event-sourcing-design.md` | Pilot aggregate + Todo migration + projection rebuild green |

Each slice ends with `make ci`. Slice ④ ends with `make ci.full`. No slice starts before
the previous one is merged or explicitly waived.

## 5. Cross-cutting conventions

### Names and files

New files follow `guide/dotted-file-naming-guide.md`. The role is the final dotted
segment or a sub-role whose chain contains a role:

- `Specification.Interface.cs`, `Specification.Base.cs`, `Specification.Extension.cs`
- `Outbox.Store.Interface.cs`, `Outbox.Store.cs`, `Outbox.Dispatcher.cs`
- `OutboxMessage.Model.cs`, `OutboxMessage.Configuration.cs`
- `EventStore.Interface.cs`, `PostgresEventStore.cs`
- `Projection.Worker.cs`, `TodoReminder.Projection.cs`

### Results and errors

All flow-level failures follow `docs/specs/2026-10-30-monadic-error-handling-design.md`:
`TryXxx` methods return `Result`, validators own input rules, and throws are limited to
DI guards, startup composition, adapter boundaries, cancellation, and documented final
unwraps. Ports return `Result` / `Result<T>`. New failures live in a dedicated `*.Outcome.cs`
file with stable codes, following the existing `DomainEventOutcome` shape. Infrastructure
translates provider exceptions to those outcomes; it never leaks `NpgsqlException`.

### Comments

Comments use the labeled vocabulary (`Validate:`, `Enforce:`, `Publish:`, `Retry:`,
`Compensate:`). No freeform narration.

### Configuration

Every new subsystem has a bound settings object with a `SectionName`, defaults,
a FluentValidation validator and `ValidateOnStart`. Secrets come from configuration or
Aspire, never from code.

## 6. Testing strategy

| Layer | Unit | Integration |
|---|---|---|
| Core | specifications, policies, event versioning, aggregate rehydration | — |
| Application | transaction behavior, message routing, serializer contracts | — |
| Infrastructure | stores with EF InMemory where valid | PostgreSQL through Testcontainers for every SQL behavior |
| Messaging | bus adapter, topology, consumer base | RabbitMQ through Testcontainers |
| API | — | full command → outbox → handler → integration event flow |

Rules:

- InMemory EF is not accepted as proof of concurrency, JSONB, `SKIP LOCKED`, indexes or
  transaction behavior. Those tests use PostgreSQL.
- Every dispatcher/worker exposes a callable `RunOnceAsync`/`DispatchBatchAsync` so tests
  never depend on timers.
- Coverage floors remain 90% line / 80% branch per assembly.

## 7. Rollout and compatibility

- New tables arrive through EF migrations committed with the slice.
- Outbox and inbox are additive; existing tables keep their shape.
- The Todo migration in slice ④ keeps all public API contracts. Existing API integration
  tests are the regression gate.
- Feature flags default to safe values:
  - `Outbox:Enabled` true in the Api; false in migration-only hosts.
  - `Messaging:Enabled` true only when a broker connection exists.
  - `EventStore:Enabled` true when the event-store migration is applied.

## 8. Risks

| Risk | Mitigation |
|---|---|
| Outbox poller changes domain-event timing from immediate to eventual | Accept and document; handlers must tolerate delay; API responses do not depend on handler completion |
| Two reliability models if Wolverine's outbox is enabled by accident | Explicitly disable Wolverine durable outbox; ownership test asserts only our outbox is registered |
| Event-sourcing `Todo` rewrites tested behavior | Pilot proves the framework first; Todo migration is a separate wave gated by the existing API suite |
| Polling load on PostgreSQL | Batch claim with `FOR UPDATE SKIP LOCKED`, configurable interval, retention cleanup, outbox lag health check |
| JSON event payloads drift | Stable `EventName` + `EventVersion`; upcaster chain; round-trip tests per event |

## 9. Out of scope

- E-commerce domain model.
- Exactly-once delivery.
- Marten or a dedicated event-store database.
- Distributed sagas/process managers (candidate fifth slice).
- Cross-region or multi-broker topology.

## 10. Slice documents

1. [Domain design closure](2026-10-29-domain-design-closure-design.md)
2. [Reliable events](2026-10-29-reliable-events-design.md)
3. [Messaging](2026-10-29-messaging-design.md)
4. [Event sourcing](2026-10-29-event-sourcing-design.md)
