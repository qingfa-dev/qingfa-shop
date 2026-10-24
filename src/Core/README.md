# Core

Domain kernel — primitives, value objects, events, and shared abstractions. _layer · kernel_

## Summary

Core is the innermost layer with **zero external dependencies**. It contains domain primitives (value objects, entity base, aggregate root), domain events, shared interfaces (`IRepository<T>`, `IUnitOfWork`), and cross-cutting utilities (result types, guards, specifications). All other layers depend on Core; Core depends on nothing but approved NuGet packages.

## Contains

| Path | Role | Notes |
|---|---|---|
| `Assembly.Marker.cs` | marker | Anchors assembly for scanning |
| `Primitives/` | namespace | Value objects, IDs, result types |
| `Events/` | namespace | Domain events, integration events |
| `Abstractions/` | namespace | `IRepository`, `IUnitOfWork`, `IDomainEventDispatcher` |
| `Specifications/` | namespace | Specification pattern base classes |
| `Guards/` | namespace | Argument/state validation helpers |
| `Common/` | namespace | Shared constants, enums, extensions |

## Boundaries

In scope:
- Domain primitives (ULID, Money, Email, Address, etc.).
- Base classes: `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`.
- Domain events and `IDomainEventDispatcher`.
- Result/Option types, specifications, guards.

Out of scope:
- Persistence (EF Core) → [`../Infrastructure/`](../Infrastructure/).
- Application use cases → [`../Application/`](../Application/).
- External contracts (DTOs) → [`../Contract/`](../Contract/).
- Infrastructure concerns (caching, messaging, HTTP clients).

## Assumptions

- All domain IDs are ULIDs (see `Ulid.NewUlid()`).
- Money values are immutable value objects with currency.
- Domain events are raised *after* state changes, dispatched via `IDomainEventDispatcher`.
- `Result<T>` is used for fallible operations; exceptions reserved for truly exceptional cases.

## Invariants

- **No NuGet dependencies** beyond: `FluentValidation`, `Mapster`, `Mediator.Abstractions`, `Slugify.Core`.
- **No project references** to any other layer.
- All types are `public` only if intentionally part of the shared kernel; otherwise `internal`.

## Contracts

- `Entity<TId>` — base for all entities.
- `AggregateRoot<TId>` — base for aggregates, tracks domain events.
- `ValueObject` — structural equality base.
- `IDomainEvent` — marker for domain events.
- `IRepository<T>` — read/write access for aggregates.
- `Result<T>`, `Result` — success/failure with error codes.
- `Specification<T>` — composable query predicates.

## Conventions

- One aggregate per file; `Order.cs`, `OrderItem.cs` — not `Order.Aggregate.cs`.
- Domain events named `*Event.cs` (e.g., `OrderPlacedEvent.cs`).
- Value objects in `Primitives/` (e.g., `Money.cs`, `Email.cs`).
- Guard methods named `Guard.Against.Null(arg, name)`.

## Reading Order

1. `Primitives/` — value objects, IDs.
2. `Events/` — domain event definitions.
3. `Abstractions/` — repository, unit of work interfaces.
4. `Specifications/` — specification base.
5. `Guards/` — validation helpers.

## Related

- Parent: [`../README.md`](../README.md)
- Children: `Primitives/`, `Events/`, `Abstractions/`, `Specifications/`, `Guards/`
- Downstream: [`../Contract/`](../Contract/), [`../Application/`](../Application/), [`../Infrastructure/`](../Infrastructure/)
- ADR: [`../../docs/adr/0001-domain-kernel.md`](../../docs/adr/0001-domain-kernel.md)

## Ownership

- Owning team: **Platform**.
- Changes require Platform review; breaking changes require all downstream approval.

## Notes

- `TODO-2026Q3:` Add `IClock` abstraction for time-dependent domain logic.
- `TEMP:` `Slugify.Core` used for URL slugs; evaluate native implementation.