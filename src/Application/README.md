# Application

Use cases — command/query handlers, application services, mappers, policies. _layer · service_

## Summary

Application contains **use case orchestration**. It implements handlers for application commands/queries, coordinates domain logic (via Core), and delegates to Infrastructure through abstractions. It has no direct external dependencies — only Core and Infrastructure abstractions.

## Contains

| Path | Role | Notes |
|---|---|---|
| `Assembly.Marker.cs` | marker | Anchors assembly for scanning |
| `Common/Mediators/` | behavior | MediatR command/query interfaces and pipeline behaviors |
| `Common/Mapping/` | mapper | `EntityMapperExtensions` create/update/projection helpers |
| `Common/Persistence/` | service | Repository, initialization, and capability abstractions |
| `Common/Caching/`, `Common/Events/`, `Common/Security/`, `Common/Validation/` | service | Cross-cutting application contracts |
| `Features/Todos/` | use-case | Vertical slices for todo lists, items, and categories |
| `Application.Composition.cs` | composition | DI registration: `AddApplication()` |

## Boundaries

In scope:
- Command/query handlers (MediatR).
- Application services (multi-step orchestrations).
- `Mapster` mappers for DTO ↔ Domain mapping.
- Domain policies (pricing, discount, shipping strategies).
- Pipeline behaviors: validation, transaction, logging.
- DI composition: `services.AddApplication()`.

Out of scope:
- Domain logic → [`../Core/`](../Core/).
- EF Core, Redis, HTTP clients → [`../Infrastructure/`](../Infrastructure/).
- Aspire config → [`../ServiceDefaults/`](../ServiceDefaults/).
- HTTP endpoints → [`../../app/Web.Api/`](../../app/Web.Api/).

## Assumptions

- Handlers own their commands/queries under `Common/Mediators`; return `Result<T>` from Core.
- Domain logic lives in Core aggregates; Application only orchestrates.
- Transactions managed by `TransactionBehavior` (MediatR pipeline).
- Validation runs via `ValidationBehavior` (FluentValidation).
- Mapper configs are explicit; no implicit mapping.

## Invariants

- **No direct Infrastructure references** — only via abstractions (`IRepository`, `IEmailSender`, etc.).
- **No MediatR runtime** — uses `Mediator.Abstractions` + source generator (or MediatR if configured).
- All handlers are `internal` except those explicitly exposed for testing.
- One handler per command/query; no `Handle` overloads.

## Contracts

- `ICommandHandler<TCommand, TResult>` — handles `ICommand<TResult>`.
- `IQueryHandler<TQuery, TResult>` — handles `IQuery<TResult>`.
- `IApplicationService` — coarse-grained orchestration.
- `IMapper` — Mapster mapping contracts.
- `AddApplication(IServiceCollection)` — DI registration entry point.

## Flows

### Place Order
1. `PlaceOrderCommand` received.
2. `ValidationBehavior` validates via `PlaceOrderCommandValidator`.
3. `PlaceOrderCommandHandler` loads `Customer` aggregate.
4. `Customer.PlaceOrder()` creates `Order` aggregate (domain logic in Core).
5. `TransactionBehavior` commits UoW.
6. `OrderPlacedEvent` raised → dispatched to Infrastructure (outbox).
7. `PlaceOrderCommandResponse` returned.

## Conventions

- Handlers named `NounVerbHandler` (e.g., `PlaceOrderHandler`).
- Services named `NounService` (e.g., `OrderService`).
- Mappers: `NounMapper.cs` with `TypeAdapterConfig` registrations.
- Behaviors: `NounBehavior.cs` (e.g., `ValidationBehavior`, `TransactionBehavior`).
- Policies: `NounPolicy.cs` (e.g., `PricingPolicy`, `ShippingPolicy`).

## Reading Order

1. `Composition.cs` — DI registration.
2. `Behaviors/` — pipeline behaviors.
3. `Commands/` / `Queries/` — handlers.
4. `Services/` — orchestrations.
5. `Mappers/` — mapping configs.
6. `Policies/` — business strategies.

## Related

- Parent: [`../README.md`](../README.md)
- Upstream: [`../Core/`](../Core/)
- Downstream: [`../Infrastructure/`](../Infrastructure/) (implements abstractions), [`../../app/Web.Api/`](../../app/Web.Api/) (endpoints)
- ADR: [`../../docs/adr/0003-vertical-slices.md`](../../docs/adr/0003-vertical-slices.md)

## Ownership

- Owning team: **Platform**.
- New use cases require Platform + Domain review.
- Handler changes require public API stability guarantee.

## Notes

- `TODO-2026Q3:` Extract `IOutbox` abstraction to Core for event dispatch.
- `TEMP:` `ValidationBehavior` currently uses FluentValidation; evaluate built-in `DataAnnotations` fallback.