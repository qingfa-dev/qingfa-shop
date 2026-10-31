# Roadmap → Repository Implementation Audit — Pro Edition

**Version:** 2.0 (supersedes `roadmap-implementation-audit.md` v1.0)
**Audit date:** 2026-10-31
**Repository:** `QingFa.Shop`
**Scope:** 14-week roadmap, 11-branch taxonomy, milestones A–H, Phase VI.
**Mode:** four independent clean-context subagent audits (weeks 0–5, 6–8, 9–11, 12–14) + a parent verification pass that re-read every contested file. Subagents received the roadmap concepts only; they were not given v1.0 or its conclusions.

---

## 0. Review provenance and what changed since v1.0

### 0.1 How this edition was produced

| Pass | What happened | Output |
|---|---|---|
| Independent audit A | Weeks 0–5: foundations, architecture, DDD, use cases, persistence | Per-concept verdicts + gaps |
| Independent audit B | Weeks 6–8: HTTP/identity/authz, Vue, testing | Authorization matrix + test inventory |
| Independent audit C | Weeks 9–11: outbox/inbox, workers/notifications, resilience | Reliability walkthrough + failure modes |
| Independent audit D | Weeks 12–14 + Phase VI + milestones | Reality check + gap register |
| Parent verification | Re-opened every contested claim; ran targeted greps and reads | Corrections below |

The agents disagreed on five material points. Each was resolved by reading the source, not by majority vote.

### 0.2 Corrections to v1.0 (and to the subagent reports)

| # | Claim | Verified reality | Why it matters |
|---|---|---|---|
| C1 | "Payment writes do not use the outbox." (Agent C) | **False.** `EventSourcedRepository.SaveAsync` stages every uncommitted domain event via `IOutboxStore.EnqueueAsync` after `IEventStore.AppendAsync` (`src/Infrastructure/BuildingBlocks/Adapters/Persistence/EntityFramework/EventSourcing/EventSourcedRepository.cs:92-136`). Payments uses the outbox. | The outbox is exercised by both event-sourced modules, not only Todos. |
| C2 | "Payments publishes integration events." (v1.0 implication) | **False.** No Payments command or handler injects `IIntegrationEventPublisher`; there are no `Application/Modules/Payments/Events/*.Integration.Handler.cs`. Payment outbox rows are domain events published in-process, with no registered handlers. | Payments has no cross-module event flow; projections read the event store directly. |
| C3 | "Snapshots are not used for aggregate reconstruction." (Agent D) | **False.** `EventSourcedRepository.LoadAsync` loads the snapshot, restores it, then reads events from `snapshot.Version + 1`; corrupt snapshots fall back to full replay (`EventSourcedRepository.cs:39-80`). | Snapshotting is a real read-path optimization, not a backfill-only feature. |
| C4 | "Notification providers are wired and delivery works." (v1.0 overgrade) | **False at runtime.** Only `LoggingProvider` for **SMS** is registered (`src/Infrastructure/BuildingBlocks/Adapters/Notifications/Notification.Extension.cs:50-53`). SendGrid/SMTP/Sinch extension methods exist but are called only in tests. Additionally, Hangfire consumes only `["default"]` (`Background.Extension.cs:19,88`) while templates map to `critical`/`high` queues (`Notification.Service.Mapper.cs:78-85`). | Notification delivery is a **silent-loss path**, not a working feature. This is the most important correction. |
| C5 | "Notification retry exists because `RetryCount` is configured." (Agent C partially) | **False.** `RetryCount` is validated as a range but never read by any provider or the hub. Hangfire's implicit retry is the only retry, and it cannot help jobs stranded on unconsumed queues. | Do not claim provider retry/backoff. |
| C6 | "Architecture tests enforce layer rules." (v1.0 framed this as partial) | **Confirmed partial, more precisely:** `LayerArchitecture.Tests.cs:57-67` and `VerticalSliceArchitecture.Tests.cs:19-28` are `true.Should().BeTrue(...)` documentation. `BuildingBlockBoundary.Tests.cs`, `NamingConvention.Tests.cs`, `CommandMarker.Tests.cs`, `InlineErrorUsage.Tests.cs`, `CodeCommentLabel.Tests.cs` are real. | Only the building-block boundary and convention rules are enforced. |
| C7 | "Todo resources enforce user ownership." (v1.0 left implied) | **False.** Ownership is **tenant-scoped**: `FindOwnedAsync(id, tenantId)` filters `entity.TenantId == tenantId` (`Repository.Ownership.Extension.cs:37`). There is no user-level ownership for todos. Payments is **customer-scoped** and verified by `Read_OtherCustomersPayment_ShouldReturn404` (`Payment.Api.Test.cs:60`). | Authorization is not uniform across modules. |
| C8 | "No Dockerfiles / no immutable artifact path." (Agent A/D) | **False.** All four deployables have Dockerfiles: `app/Web.Api/Dockerfile`, `app/Gateway/Dockerfile`, `app/Web.Storefront/Dockerfile`, `app/Web.Admin/Dockerfile`. | Deployment maturity is higher than v1.0 stated. |
| C9 | "OpenAPI is absent." (v1.0 omitted it) | **False.** OpenAPI + Scalar UI are wired via `AddOpenApiDocumentation()` in `Adapters.Composition.cs:40`; endpoints mapped in `OpenApi.Composition.cs:54-62`. | API documentation exists. |
| C10 | "CI runs full integration coverage on PRs." (ambiguous in v1.0) | **No.** PR runs `coverage.unit`, `api.test`, `test.infra.broker`; `coverage.api`/`coverage.infra`/`coverage.apphost`/`coverage.report` are main/nightly only (see the backend, web, apphost, infra, and coverage-report jobs in `.github/workflows/ci.yml`). | PR green does not mean full integration coverage gate. |

### 0.3 Confidence levels used below

| Level | Meaning |
|---|---|
| **Verified** | Parent re-read the decisive file(s) during this edition. |
| **Corroborated** | Two independent agents agreed and the path exists; not re-read line by line. |
| **Inferred** | Follows from verified wiring but not directly exercised by a test. |

Unless marked otherwise, every table row is **Verified** or **Corroborated**.

---

## 1. Executive summary (post-review)

### 1.1 The corrected picture

The repository is a **reliability platform with two real modules** (`Todos`, `Payments`), not a shop. That conclusion survives the second review. What changes is the grading of two areas:

- **Outbox/inbox:** stronger than v1.0 implied — event-sourced saves stage outbox rows for both modules, and the store uses `FOR UPDATE SKIP LOCKED`.
- **Notifications:** far weaker than v1.0 implied — providers are not registered, priority queues have no consumer, retry settings are dead config, and nothing persists delivery status.

### 1.2 Headline status (corrected)

| Unit | Verdict | Correction vs v1.0 |
|---|---|---|
| Week 0 Baseline | 🟡 | Deployment units include 4 Dockerfiles; documentation deliverables still absent. |
| Week 1 Foundations | ✅ | Unchanged. |
| Week 2 Architecture | 🟡 | Enforcement is narrower than stated: only building-block boundary + conventions are real tests. |
| Week 3 DDD | 🟡 | Strong in Todos/Payments; no Product/Order. |
| Week 4 Use cases | ✅ | Unchanged; resource authz is not generic. |
| Week 5 Persistence | 🟡 | Keyset pagination and benchmarks absent; snapshots **are** used on reads. |
| Week 6 HTTP/identity | 🟡 | Add OpenAPI/Scalar ✅; versioning, CORS, contract tests absent; Todo ownership is tenant-scoped. |
| Week 7 Vue | 🧱 | Unchanged: shell apps, health chip only. |
| Week 8 Testing | 🟡 | Architecture tests are partly placeholders; PR does not run full coverage gates. |
| Week 9 Outbox | ✅ | Now verified for **both** event-sourced modules. |
| Week 10 Notifications | ⛔ / 🟡 | **Downgraded.** Queue/provider wiring defects make delivery unreliable. |
| Week 11 Resilience | 🟡 | No custom async traces/meters, no circuit breaker policy, no dashboards. |
| Week 12 CQRS/search | 🟡 | Projections real; search is substring-only; FullText/Fuzzy enum values are dead. |
| Week 13 Sagas | ⛔ / ➖ | Still correctly absent. |
| Week 14 Event sourcing | ✅ | Snapshots on read path confirmed; upcasting interface absent. |
| Phase VI | 🟡 | Dockerfiles + CI real; secrets, SLOs, load, backup/restore absent. |

### 1.3 The three findings that matter most

1. **Notifications can be lost without an error.** With defaults, `EnableBackgroundJobs` is `false` and only an SMS logging provider is registered → email sends fail with `NoProvidersConfigured`. If background jobs are enabled, most templates enqueue to `critical`/`high`, but Hangfire listens only to `default` → jobs sit forever. No delivery row records either outcome.
2. **The outbox is real but its cross-module value is underused.** Payments stages domain events into the outbox, but no Payments integration events exist, so nothing leaves the module. The Todo module is the only place where domain → integration bridging is exercised.
3. **The roadmap's commerce exercises remain impossible.** No `Product`, `Order`, `Cart`, `Inventory`, or ML service. The frontends have no feature code beyond a health chip.

---

## 2. Reality check

### 2.1 Modules and deployables

| Item | Reality | Evidence |
|---|---|---|
| Business modules | `Todos`, `Payments` only | `src/Core/Modules/` |
| Identity | Platform capability, not a commerce module | `src/Application/BuildingBlocks/Security/**` |
| Deployables | API, Gateway, Storefront, Admin, AppHost | 4 Dockerfiles + `app/AppHost/AppHost.cs` |
| Worker process | ❌ none; all workers are `BackgroundService` inside the API | `Outbox.Worker.cs`, `Projection.Worker.cs`, `PaymentReconciliation.Worker.cs`, `PaymentWebhookRetention.Worker.cs` |
| Product / Order / Cart / Inventory | ❌ absent | no types in `src/` |
| ML / embeddings / image search | ❌ absent | no project, package, or client |
| Frontend features | ❌ absent; health chip only | `app/Web.Storefront/src/**`, `app/Web.Admin/src/**` |

### 2.2 API surface and authorization

| Route group | Auth | Permission gate | Resource scope |
|---|---|---|---|
| `/api/auth` | Mixed | — | — |
| `/api/identity/users` | Yes | `Identity.Users.*` | — |
| `/api/identity/roles` | Yes | `Identity.Roles.*` | — |
| `/api/identity/permissions` | Yes | `Identity.Permissions.*` | — |
| `/api/todos`, `/api/todo-lists`, `/api/todo-categories` | Yes | `Todo.*` | **tenant-scoped** (`FindOwnedAsync`) |
| `/api/payments` | Yes | authenticated user | **customer-scoped** (`GetPayment.cs:64`) |
| `/api/admin/payments` | Yes | `Payments.Read/Manage/Refund` | admin bypass by design |
| `/api/payments/webhooks` | Anonymous | signature verification + webhook dedupe | — |

**Authorization matrix still missing as a document.** The table above is derived; nothing in the repo enumerates endpoint → policy → ownership.

### 2.3 CI reality

| Trigger | What actually gates |
|---|---|
| PR | lint, convention, build, unit coverage, `api.test`, `test.infra.broker` |
| main / nightly / manual | adds `coverage.api`, `coverage.infra`, `coverage.apphost`, merged `coverage.report`, Compose smoke (when Compose inputs changed) |
| e2e | only main/nightly; Playwright is not installed, so `test.web.e2e` skips |

---

## 3. Corrected end-to-end traces

### 3.1 Todo read (works)

```text
GET /api/todos/{id}
 → TodoEndpoint.GetAsync
 → GetTodo.UseCaseHandler
 → ITenantContext.RequireTenant()
 → repository.FindOwnedAsync(id, tenantId)          // tenant ownership only
 → TodoReadModel (projection table)
 → Response model → ApiResult → JSON
```

### 3.2 Todo write → outbox → broker → inbox (works, and is the real reference flow)

```text
POST /api/todos
 → command handler → Todo aggregate raises events
 → TransactionBehavior opens transaction
 → EventSourcedRepository.SaveAsync
     → PostgresEventStore.AppendAsync (ambient tx, expected version)
     → IOutboxStore.EnqueueAsync(domain events)
     → IUnitOfWork.SaveChangesAsync
 → commit
 → OutboxWorker → OutboxDispatcher (claim with FOR UPDATE SKIP LOCKED)
     → IDomainEventPublisher (MediatR, in-process)
         → TodoStatusChangedIntegrationHandler publishes IIntegrationEvent
     → Wolverine → RabbitMQ (publisher confirms)
 → IntegrationEventHandler<T> → IInboxStore.ExecuteOnceAsync
     → dedupe check + handler + marker in one transaction
```

### 3.3 Payment creation (works; no integration event leaves the module)

```text
POST /api/payments  (Idempotency-Key required)
 → CreatePayment.UseCase
     → IPaymentIdempotencyStore.ReserveAsync (fingerprint conflict detection)
     → Payment aggregate → events
 → EventSourcedRepository.SaveAsync
     → event store append
     → outbox enqueue (domain events only)
     → save
 → OutboxWorker dispatches domain events in-process
     → no Payments domain-event handlers registered → no-op
 → ProjectionWorker reads the event store (not the outbox)
     → PaymentProjection → PaymentReadModel
 → GET /api/payments/{id} reads the projection
```

**Consequence:** Payments relies on the event store for its read model, and the outbox currently adds durability without producing downstream effects. That is acceptable for now but should be stated explicitly (ADR) so nobody assumes an integration contract exists.

### 3.4 Notification delivery (broken at runtime)

```text
INotificationService.SendAsync
 ├─ EnableBackgroundJobs == false (default: property default, absent from appsettings)
 │    → SendInternalAsync
 │       → template render
 │       → NotificationHub filters providers by channel
 │       → only LoggingProvider(SMS) is registered
 │       → EMAIL: NoProvidersConfigured (result, logged, no record)
 └─ EnableBackgroundJobs == true
      → jobClient.Create<INotificationService>(..., EnqueuedState(queue))
      → queue = critical | high | low | default  (from template priority)
      → Hangfire server consumes ONLY ["default"]
      → critical/high/low jobs are never processed
```

**Failure modes:** lost email (no provider), stranded job (queue mismatch), duplicate send (no dedupe key), no operator visibility (no delivery table), no retry (settings unused). This is why Week 10 is downgraded.

---

## 4. Week 00 — Baseline

| Deliverable | Status | Evidence |
|---|---|---|
| Dependencies | ✅ | `Directory.Packages.props`, `packages.lock.json` |
| Deployment units | ✅ | 4 Dockerfiles + `AppHost.cs` |
| Data ownership | 🟡 | aggregate streams + read models; no ownership document |
| Trust boundaries | ✅ | `Gateway.Router.cs` order + anonymous exceptions |
| System context diagram | ⛔ (now provided in this document) | §2 |
| Feature inventory | ⛔ (now provided) | §2.1 |
| Dependency map | 🟡 | `.csproj` graph; no diagram (now §2) |
| Technical risk list | ⛔ (now provided) | §1.3, §11 |
| Product read trace | ⛔ | no Product |
| Order creation trace | ⛔ | no Order |

**Exit criterion:** met only for Todo read and Payment creation. Not met as written.

---

## 5. Weeks 1–5

### Week 1 — Design foundations ✅

Unchanged from v1.0 and corroborated: ports/adapters (payments/storage/notifications), factories, strategy/registry, decorator behaviors, composition roots, value objects, `Result`. Two interchangeable payment providers are tested without real SDK calls (`FakePaymentGateway.Spec.cs`, `StripePaymentGateway.Test.cs`, `PayPalPaymentGateway.Test.cs`).

**Nuance:** notification provider extensions are unit-tested but never composed at runtime (see Week 10). The abstraction is proven; the production wiring is not.

### Week 2 — Architecture and boundaries 🟡

| Concept | Verdict | Evidence |
|---|---|---|
| Layered / Clean / Hexagonal | ✅ | project refs + `Ports`/`Adapters` |
| Modular monolith | ✅ | `Modules/*` in one API |
| API gateway | ✅ | YARP + edge policy + rate limiter |
| BFF | ⛔ | none, deliberately |
| Inward dependency enforcement | 🟡 | `.csproj` refs real; `BuildingBlockBoundary.Tests.cs` real; `LayerArchitecture.Tests.cs:57-67` placeholder |
| Module isolation test | 🟡 | `VerticalSliceArchitecture.Tests.cs:19-28` placeholder |
| Architecture tests general | 🟡 | naming, command marker, inline errors, comment labels are real |

**Exit criterion:** met by project references, not by a runtime test. The correct claim is: *"the compiler enforces referenced layers; one reflection test enforces BuildingBlocks → Modules; earlier module-isolation guarantees are documented but unenforced."*

### Week 3 — DDD 🟡

Implemented: entities, 16+ value objects, aggregates, roots, factories, specifications, policies, domain events, validators/invariants, two bounded contexts in folders. Missing: `Product`, `Order`, `ProductId`, context map, glossary, diagrams. Domain services exist only implicitly as validators/policies.

### Week 4 — Use cases ✅ (with one gap)

Commands/queries/handlers/MediatR, validation/logging/exception/transaction behaviors, `Result`, permission-based authorization, standardized HTTP mapping. Task-relevant gap: **no reusable resource-authorization abstraction**; Payments checks `CustomerId` inline (`GetPayment.cs:64`), Todos delegates to `FindOwnedAsync(tenantId)`.

### Week 5 — Persistence 🟡

Implemented: DbContext, UoW, transactions, optimistic concurrency, snapshot-aware event-sourced repository, repositories, query DSL, filtering/sorting/projection, soft delete, offset paging, integration tests. Missing: keyset pagination (`PageRequest` only), benchmarks, documented transaction-boundary ADR, payment/`outbox` atomicity proof outside a command transaction (see §9.4).

---

## 6. Weeks 6–8

### Week 6 — HTTP, identity, authorization 🟡

| Concept | Verdict | Evidence |
|---|---|---|
| REST + DTOs + stable result envelope | ✅ | Carter modules, `ApiResult`, `ResultContractFlow.Api.Test.cs` |
| OpenAPI + Scalar | ✅ | `OpenApi.Composition.cs` wired in `Adapters.Composition.cs:40` |
| JWT auth (API + Gateway) | ✅ | `Authentication.Composition.cs`, `Gateway/Program.cs` |
| OIDC/external providers | ✅ (config) | `ExternalAuthentication.Service.cs`; providers disabled by default |
| RBAC + permission policies | ✅ | `PermissionRequirement.Handler`, `HasPermission`, per-module catalogs |
| Endpoint authorization tests | ✅ | `AdminRoute_WithoutPermission_ShouldReturn403`, 8 anonymous-rejection smoke tests |
| Gateway vs API as distinct layers | ✅ | edge JWT policy vs API permissions |
| Resource-based authorization | 🟡 | customer-scoped for payments; tenant-scoped for todos; not generic |
| API versioning | ⛔ | none |
| CORS | ⛔ | none (`AddCors`/`UseCors` absent) |
| CSRF | 🟡 | storage uploads only |
| Rate limiting | ✅ edge only | per-IP fixed window 100/10 s |
| Authorization matrix document | ⛔ | derived in §2.2 |
| Consumer-driven contract tests | ⛔ | none (integration shape tests only) |

### Week 7 — Vue feature architecture 🧱

Both apps contain exactly: `App.vue`, `HomeView.vue`, `NotFoundView.vue`, one router, one composable (`useApiHealth`), one typed client (`health.ts`), and 3 specs each. No Pinia, no forms, no guards, no feature folder, no product/cart/checkout UI. E2E is a no-op (no Playwright config or dependency).

### Week 8 — Testing 🟡

| Test kind | Status | Evidence |
|---|---|---|
| Unit (core/app/infra) | ✅ | `tests/Unit/**`, coverage-gated |
| Integration (API, EF, Redis, RabbitMQ, AppHost) | ✅ | `tests/Integration/**` with Testcontainers fixtures |
| Architecture (real) | 🟡 | 5 real suites; 2 placeholders |
| Contract (consumer-driven) | ⛔ | — |
| E2E | ⛔ | Playwright absent |
| Property-based | ⛔ | — |
| Load / chaos | ⛔ | — |
| Risk-based coverage map | ⛔ | Appendix B is the first |

**Important nuance:** integration suites skip without Docker (`ContainerRuntime.Util.cs`), and PR coverage is unit-only. A green PR does not prove PostgreSQL/RabbitMQ behavior; main/nightly does.

---

## 7. Weeks 9–11

### Week 9 — Events and outbox ✅

| Claim | Verified evidence |
|---|---|
| Atomic staging (EF aggregates) | `OutboxInterceptor` stages in `SavingChanges`, clears after `SavedChanges`, detaches on failure (`Outbox.Interceptor.cs:51-104`) |
| Atomic staging (event-sourced aggregates) | `EventSourcedRepository.SaveAsync` appends to the event store then enqueues outbox rows, inside the command's ambient transaction (`EventSourcedRepository.cs:92-136`) |
| Claim safety | raw SQL `FOR UPDATE SKIP LOCKED` (`Outbox.Store.cs:11-23`) |
| Lease fencing | mark requires `LeaseId` + unexpired lock (`Outbox.Store.cs:110-117`) |
| Retry/backoff/dead-letter | `Outbox.Dispatcher.cs:133-165` |
| Duplicate consumer delivery | `EfCoreInboxStore.ExecuteOnceAsync` + unique marker (`Inbox.Store.cs:41-141`) |
| Tests | `Outbox.Atomicity`, `Outbox.Flow`, `Outbox.Store`, `Inbox.Store` integration suites |

**Boundaries of the guarantee:**

- Payments stages **domain** events only; no Payments integration event exists.
- Atomicity depends on an ambient transaction. When `SaveAsync` runs outside `TransactionBehavior` (a seeder/worker path), `PostgresEventStore.AppendAsync` opens and commits its own transaction, so the event append and outbox enqueue are **not** atomic. No test covers this path.
- Domain-event handlers run **after commit** through the outbox, not inside the write transaction. This should be an ADR.

### Week 10 — Background jobs and notifications ⛔ / 🟡

| Concept | Verdict | Evidence |
|---|---|---|
| Worker services | ✅ in-process | 4 `BackgroundService` workers + Hangfire server |
| Template rendering | ✅ | `Notification.Service.Mapper.cs`, `NotificationStore.Template.Enum.cs` |
| Channel routing | ✅ | `NotificationHub` filters by channel/priority |
| Provider adapters exist | ✅ | SendGrid, SMTP, Sinch, Logging |
| Provider registration at runtime | ⛔ | only `LoggingProvider(SMS)` (`Notification.Extension.cs:50-53`); other `Add*Provider` methods are test-only |
| Priority queues | ⛔ | templates use `critical`/`high`; Hangfire consumes only `["default"]` |
| Retry with backoff | ⛔ | `RetryCount` validated but never read |
| Dead-letter | ⛔ | none |
| Delivery status persistence | ⛔ | none |
| Notification idempotency | ⛔ | none |
| Provider outage blocks request | ✅ (when background enabled) | returns immediately after enqueue |
| Duplicate queue message → duplicate work | ⛔ (uncontrolled) | no dedupe key |

**Exit criterion:** not met. The failure is silent, which is worse than a loud failure.

### Week 11 — Resilience and observability 🟡

| Concept | Verdict | Evidence |
|---|---|---|
| Timeouts/retries/circuit breaker | 🟡 | `AddStandardResilienceHandler()` gives all three by default; no per-dependency policy or explicit circuit-breaker config |
| Publisher confirms | ✅ | `Messaging.Composition.cs:196-205` |
| Structured logs | ✅ | source-generated loggers |
| Metrics | 🟡 | runtime/HTTP + `PaymentMetrics`; no outbox/inbox/notification meters |
| Tracing | 🟡 | HTTP auto-instrumentation; no custom `ActivitySource` through outbox → worker → provider |
| Correlation | 🟡 | correlation columns + middleware; no W3C trace propagation through outbox |
| Health checks | ✅ | self/db/redis/rabbitmq/outbox; **two endpoint sets** (`/health`,`/alive` dev-only; `/health/live`,`/health/ready` always) |
| Graceful shutdown | ✅ | stopping tokens honored in all workers |
| Dashboards / SLO / alerts | ⛔ | none |
| Failure-injection checklist | ⛔ | none |

---

## 8. Weeks 12–14 and Phase VI

### Week 12 — CQRS, projections, search 🟡

- CQRS at handler level ✅; read models for Todos and Payments ✅; checkpointed projection worker ✅; rebuild service ✅ (both marked `[ExcludeFromCodeCoverage]`).
- Search ⛔: `SearchMode.FullText`/`Fuzzy` are declared but unreachable; EF capability reports `Contains|StartsWith|Exact`, `SupportsRanking=false`, `SupportsHighlighting=false`; translation is `ToLower().Contains`.
- No vector search, no embeddings, no ranking, no benchmark, no separate read store decision.

### Week 13 — Sagas ⛔ / ➖

No saga, process manager, compensation orchestration, reservation, or partial-completion test. Refunds are an in-aggregate operation (`Payment.cs`), and reconciliation is a drift sweep — not distributed compensation. Correctly deferred while one database owns all state.

### Week 14 — Event sourcing ✅ / 🟡

| Concept | Verdict | Evidence |
|---|---|---|
| Streams + append with expected version | ✅ | `PostgresEventStore.cs` |
| Replay | ✅ | `TryLoadFromHistory` + `TodoBackfill.Integration.Test.cs` |
| Snapshots | ✅ | loaded and saved by `EventSourcedRepository`; interval policy 50 |
| Projections/checkpoints/rebuild | ✅ | `ProjectionRunner`, `ProjectionCheckpointStore`, `RebuildProjection.Service.cs` |
| Upcasting | ⛔ | `EventSourcingOutcome.Failure.UpcastMissing` exists; **no `IUpcaster` interface or implementation** |
| ADR | ⛔ | none |

### Phase VI — Production readiness 🟡

| Concern | Verdict | Notes |
|---|---|---|
| Build/deploy | ✅ / 🟡 | 4 Dockerfiles, Compose, CI, EF migrations; no image signing/immutability proof |
| Secrets | ⛔ | dev JWT key committed in `app/Web.Api/appsettings.json:10` and Gateway; provider secrets empty |
| Authz tests | ✅ | 401/403/ownership tests exist |
| Reliability | 🟡 | outbox retries + standard handler; no recovery drill |
| Observability | 🟡 | OTel + health; no SLO/dashboards |
| Performance | ⛔ | no baseline/load test |
| Data integrity | 🟡 | constraints/UoW/concurrency; no backup/restore proof |
| Documentation | 🟡 | this document + OpenAPI; no ADR/runbook |

---

## 9. Pattern records (corrected)

### 9.1 Transactional Outbox

- **Scope:** EF-tracked aggregates (interceptor) **and** event-sourced aggregates (repository). Verified.
- **Mechanism:** shared-save staging → `FOR UPDATE SKIP LOCKED` claim → leasing → publish → mark; exponential backoff → dead-letter.
- **Guarantee:** at-least-once, no lost committed event, duplicate delivery possible.
- **Non-guarantee:** atomicity when a save runs outside an ambient transaction; exactly-once; cross-module event contracts (none exist).
- **Verification:** `Outbox.Atomicity.Integration.Test.cs`, `Outbox.Flow.Integration.Test.cs`, `Outbox.Store.Integration.Test.cs`, `Outbox.Dispatcher.Test.cs`.

### 9.2 Inbox / idempotent consumer

- Duplicate check + handler + marker in one transaction; duplicate-key race tolerated only when the marker is present.
- Applied by exactly two Todo consumers (`TodoCompletedIntegration.Handler`, `TodoReminderChangedIntegration.Handler`).
- Payments uses a different mechanism: `IWebhookEventStore.TryRecordAsync` (`ProcessPaymentWebhook.cs:86`) — provider-webhook dedupe, not inbox dedupe.

### 9.3 Event sourcing

- Adopted for `Todo` and `Payment`; snapshots used on read; projections rebuildable.
- Upcasting is a failure code with no implementation.
- Operational risk: `[ExcludeFromCodeCoverage]` on projections/rebuild and no replay drill.

### 9.4 Notification delivery — anti-pattern record (new)

- **Category:** Background processing / integration.
- **Observed design:** template → priority queue → Hangfire → hub → provider.
- **Defects:** queue consumer mismatch; providers unregistered; retry config unused; no delivery record; no dedupe.
- **Decision needed:** either make notifications a real module (persisted delivery + idempotency + queue list + retry/DLQ) or explicitly mark them experimental and remove from production claims.
- **Acceptance:** duplicate enqueue → one provider call; provider down → retry with backoff → dead-letter row; email channel has a registered provider; `critical`/`high` queues drained.

### 9.5 Saga — rejected; BFF — absent; Circuit breaker — default only

Unchanged and still correct. Revisit triggers: inventory as a separate consistency boundary; a second browser origin; provider-specific timeout/fallback needs.

---

## 10. Contested claims — resolution table

| # | Contested claim | Source | Resolution | Evidence |
|---|---|---|---|---|
| 1 | Payments uses the outbox | Agent C said no; v1 said yes | **Yes, for domain events** | `EventSourcedRepository.cs:92-136` |
| 2 | Payments publishes integration events | v1 implied | **No** | no `IIntegrationEventPublisher` in Payments; no `Events/` folder |
| 3 | Snapshots used on read | Agent D said no | **Yes** | `EventSourcedRepository.cs:39-80,125-136` |
| 4 | Notification providers wired | v1 said yes | **No** | only LoggingProvider registered |
| 5 | Notification retry exists | Agent C partial | **No** | `RetryCount` never read |
| 6 | Todo user ownership | Agent B said none | **Tenant ownership only** | `Repository.Ownership.Extension.cs:37` |
| 7 | Dockerfiles exist | Agent A/D implied none | **Yes, 4** | `app/*/Dockerfile` |
| 8 | OpenAPI exists | v1 omitted | **Yes** | `OpenApi.Composition.cs` |
| 9 | Layer tests enforce rules | v1 partial | **Placeholders** | `LayerArchitecture.Tests.cs:57-67` |
| 10 | E2E runs in CI | v1 said skips | **Skips; main/nightly only** | no Playwright config; `ci.yml` |

---

## 11. Gap register (Pro)

Priorities: **P0** blocks the roadmap or causes silent data loss; **P1** required before production claims; **P2** only with a measured need.

### P0

| ID | Gap | Why it is P0 | Suggested change | Acceptance evidence |
|---|---|---|---|---|
| G1 | No Catalog/Product | Milestones A/B impossible | `src/Core/Modules/Catalog/Products/Product.cs` + events/factory/validator/outcome; `Application/Modules/Catalog/Products/{Commands,Queries}`; `Infrastructure/Modules/Catalog/Persistence`; `Web.Api/Endpoints/Catalog/ProductEndpoint.cs` | domain tests for price/publication; API integration test |
| G2 | No Cart/Order | Milestones C/E/G impossible | `src/Core/Modules/Orders/Orders/Order.cs`, `OrderLine`, `OrderStatus`; `CreateOrder`/`Checkout` use cases | transition + concurrency tests |
| G3 | Frontends are shells | Week 7 exit criterion impossible | `features/catalog`, `features/cart` with typed client + composable + view; Pinia only for shared client state | component + API-flow specs |
| G4 | Search is substring-only | Week 12 deliverable impossible | `IProductSearch` port + PostgreSQL FTS adapter; capability gate | `FullText` supported + benchmark |
| G5 | No ML/image-search boundary | Week 12 workload absent | `IEmbeddingProvider` + HTTP adapter with timeout/resilience; catalog degrades gracefully | stub-server contract test + ML-down test |
| G6 | Notification runtime wiring broken | **Silent message loss** | register providers in `Adapters.Composition`; set Hangfire `Queues = NotificationSettingConstant.BackgroundJobs.Queues.All`; remove or implement `RetryCount` | email send test; queue-drain test; config test |
| G7 | Notification delivery not durable/idempotent | **Duplicate/lost sends, no operator visibility** | `NotificationDelivery` entity (status/attempts/lastError/dedupeKey) + store + unique index; DLQ sweep; health check | duplicate-enqueue test; provider-failure → dead-letter test |
| G8 | Architecture rules unenforced | New modules inherit no isolation guarantee | replace placeholders with reflection rules; extend `BuildingBlockBoundary.Tests` to cross-module | test fails on a deliberate violation |

### P1

| ID | Gap | Suggested change |
|---|---|---|
| G9 | No keyset pagination | `CursorPageRequest`/`CursorPage<T>` + benchmark vs offset |
| G10 | No API versioning | `Asp.Versioning` + `/api/v1` or header strategy; OpenAPI version metadata |
| G11 | No CORS policy | `AddCors` in `Api.Composition`/Gateway when a second origin appears |
| G12 | No contract tests | OpenAPI diff in CI or Pact consumer tests |
| G13 | No authorization matrix doc | `docs/architecture/authorization-matrix.md` + endpoint coverage test |
| G14 | No outbox/inbox/notification metrics | meters next to `PaymentMetrics` pattern; outbox age/dead-letter, inbox duplicates, notification outcomes |
| G15 | No async trace propagation | `ActivitySource` in dispatcher/consumer/hub; propagate traceparent through outbox metadata |
| G16 | No replay/upcasting proof | `IUpcaster` + one version-bump sample + nightly rebuild checksum drill |
| G17 | Snapshots/rebuild excluded from coverage | remove `[ExcludeFromCodeCoverage]` or add dedicated integration tests |
| G18 | Secrets in appsettings | move to env/user-secrets; fail startup on scaffold key outside Development |
| G19 | No worker process | extract `app/Web.Worker` when scaling needs justify it |
| G20 | No e2e/load/backup | Playwright critical path; k6 baseline; scripted `pg_dump`/restore drill |
| G21 | Atomicity outside command transactions | either require an ambient transaction in `EventSourcedRepository.SaveAsync` or document/assert the fallback |

### P2

| ID | Gap | Trigger |
|---|---|---|
| G22 | No saga/process manager | checkout spans independent transactional systems |
| G23 | No CDC | outbox polling latency/volume becomes a cost |
| G24 | No sharding/stream processing | data volume/availability exceeds one Postgres |
| G25 | No BFF | storefront needs cross-service aggregation |
| G26 | Per-provider circuit breaker | one provider needs a different timeout/fallback |

---

## 12. Milestones A–H (corrected)

| Milestone | Status | Next step |
|---|---|---|
| A Product CRUD | ⛔ | G1 |
| B Filtering + image search | ⛔ | G1 + G4 + G5 |
| C Cart + order creation | ⛔ | G2 |
| D Identity + admin permissions | ✅ | authorization matrix doc |
| E Order confirmation notification | ⛔ | G1/G2 + G6/G7 |
| F Reliable provider delivery | ⛔ (webhooks partially) | G6/G7 |
| G Multi-step checkout | ⛔ / ➖ | only after G2 + inventory boundary |
| H History + analytics | 🟡 | event store is the history; add projections/drill |

---

## 13. Taxonomy tree (corrected annotations)

Only deltas from v1.0 are annotated; everything else stands.

```text
2. ARCHITECTURE
   ├── Architecture tests              🟡 BuildingBlockBoundary + conventions real;
   │                                      Layer/VerticalSlice are placeholders
   └── BFF                             ⛔

3. DOMAIN MODELING
   ├── Bounded contexts                🟡 folders only (Todos, Payments); no map
   └── Product / Order                 ⛔

5. DATA
   ├── Pagination                      🟡 offset only
   └── Search indexes / vector         ⛔ (FullText/Fuzzy enums are dead)

6. INTERFACES
   ├── OpenAPI/Scalar                  ✅
   ├── API versioning                  ⛔
   ├── CORS                            ⛔
   ├── Resource checks                 🟡 customer-scoped (payments), tenant-scoped (todos)
   └── Vue features                    🧱 health chip only

7. ASYNC
   ├── Outbox                          ✅ both event-sourced modules (domain events)
   ├── Integration events              ✅ Todo only
   ├── Inbox                           🟡 2 Todo consumers only
   └── Notification delivery           ⛔ runtime wiring/status/retry/DLQ

9. RESILIENCE
   ├── Circuit breaker                 🟡 default standard handler only
   ├── Metrics                         🟡 payments only
   └── Dashboards/SLO/alerts           ⛔

10. ADVANCED DATA
    ├── Snapshots                      ✅ used on read path
    └── Upcasting                      ⛔ (failure code only)
```

---

## 14. Renaming map (unchanged, still authoritative)

| Roadmap term | Repository term |
|---|---|
| Transactional outbox | `OutboxMessage` + `OutboxInterceptor` + `EventSourcedRepository.SaveAsync` + `OutboxWorker` |
| Inbox / idempotent consumer | `InboxMessage` + `EfCoreInboxStore.ExecuteOnceAsync` |
| Event store | `StoredEvent` + `PostgresEventStore` |
| Snapshot | `Snapshot` + `EfCoreSnapshotStore` + `IntervalSnapshotPolicy` |
| Projection/read model | `IProjection` + `ProjectionRunner` + `*ReadModel` |
| Unit of Work | `IUnitOfWork` / `EfCoreUnitOfWork` |
| Query object/specification | `Query` DSL + `Filter`/`SortField`/`PageRequest` |
| API gateway | `Gateway` (YARP) |
| RBAC/policy | `PermissionMetadata` + `HasPermission` + `PermissionRequirementHandler` |
| Resource ownership | `FindOwnedAsync(tenantId)` (Todos) / `CustomerId` check (Payments) |
| Background worker | `BackgroundService` workers + Hangfire |
| Adapter/Strategy | `IPaymentGateway`, `IStorageProvider`, `INotificationProvider` |
| Notification delivery status | **absent** — proposed `NotificationDelivery` |
| Saga/process manager | **absent** |
| ML service | **absent** |

Naming rules for new code: role suffixes (`.Service.cs`, `.Handler.cs`, `.Outcome.cs`, `.Constant.cs`, `.Configuration.cs`), Result-returning public members, labeled comments, magic values in constants, one outcome file per capability.

---

## 15. Verification appendix

### 15.1 Commands used to resolve contested claims

```bash
# C1/C2 payments + outbox
rg -n "IOutboxStore|IIntegrationEventPublisher" src/Infrastructure/.../EventSourcing src/**/Payments
# C3 snapshots
sed -n '30,140p' src/Infrastructure/.../EventSourcing/EventSourcedRepository.cs
# C4/C5 notifications
rg -n "AddEmailSendGridProvider|AddSmsSinchProvider|EnableBackgroundJobs" src app
rg -n "DefaultQueues|Queues =" src/Infrastructure/.../BackgroundJobs
# C6 architecture tests
rg -n "pending API compatibility|true.Should\(\).BeTrue" tests/Architecture.Tests
# C7 ownership
rg -n "FindOwnedAsync|TenantId ==" src/Application/BuildingBlocks/Persistence
# C8 deployables
find app -maxdepth 2 -iname 'Dockerfile*'
# C9 OpenAPI
rg -n "MapScalar|MapOpenApi" src app
# C10 CI tiering
sed -n '80,230p' .github/workflows/ci.yml
```

### 15.2 Test coverage map by risk (corrected)

| Risk | Proven | Not proven |
|---|---|---|
| Domain invariants | Core unit tests | cross-aggregate invariants |
| Handler behavior | Application unit tests | real DB semantics |
| EF/query/pagination | Infrastructure integration | production volume |
| Outbox atomicity | Atomicity test | non-command save path; crash-before-dispatch |
| Outbox delivery | Flow + RabbitMQ tests | broker ACK before mark (unit only) |
| Inbox dedupe | Inbox integration | multi-process race |
| Event store concurrency | EventStore integration | snapshot under load |
| Payments | API + sandbox tests | real Stripe/PayPal |
| Identity/authz | API tests | full authorization matrix |
| Notifications | provider unit tests | **end-to-end delivery; queue consumption; retry** |
| Gateway routing | AppHost specs | rate-limit behavior under load |
| Web UI | 6 specs | any real user path |

---

## 16. Bottom line

The platform is stronger than a scaffold and weaker than a shop. The second review confirmed the reliability core, corrected the outbox scope upward, and corrected notification maturity downward. The roadmap becomes executable only after **G1–G8**; the most urgent production defect is **G6/G7**, because notifications currently fail silently.
