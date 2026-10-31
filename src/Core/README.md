# Core

Domain kernel — primitives, value objects, events, and shared abstractions. _layer · kernel_

## Summary

Core is the innermost layer with **no project references**. It contains the domain kernel (`BuildingBlocks/Domain`), reusable modeling concerns (`BuildingBlocks/Modeling`), functional result and error types (`BuildingBlocks/Functional`), and the Todos business module (`Modules/Todos`). All other layers depend on Core; Core depends only on approved NuGet packages.

## Contains

| Path | Role | Notes |
|---|---|---|
| `Assembly.Marker.cs` | marker | Anchors assembly for scanning |
| `BuildingBlocks/Domain/` | kernel | Aggregates, entities, events, event sourcing, policies, specifications, value objects |
| `BuildingBlocks/Modeling/` | kernel | Reusable model concerns, metadata, and their base types |
| `BuildingBlocks/Functional/` | kernel | `Result`, `Maybe`, `Error`, and their extension/factory surfaces |
| `Modules/Todos/` | module | Todo, list, category, and reminder aggregates |

## Boundaries

In scope:
- Domain primitives (ULID, Money, Email, Address, etc.).
- Base classes: `Entity<TKey>`, `AggregateRoot<TKey>`, `EventSourcedAggregate`.
- Domain events, event replay, and event-sourced aggregate contracts.
- Result/Maybe/Error types, specifications, policies, and modeling concerns.
- The Todos business module.

Out of scope:
- Persistence (EF Core) → [`../Infrastructure/`](../Infrastructure/).
- Application use cases → [`../Application/`](../Application/).
- Infrastructure concerns (caching, messaging, HTTP clients).

## Assumptions

- All domain IDs are ULIDs (see `Ulid.NewUlid()`).
- Money values are immutable value objects with currency.
- Domain events are raised *after* state changes.
- `Result<T>` is used for fallible operations; exceptions reserved for truly exceptional cases.

## Invariants

- **No NuGet dependencies** beyond: `FluentValidation`, `Mapster`, `MediatR`, `Slugify.Core`.
- **No project references** to any other layer.
- `BuildingBlocks` never reference `Modules`; the Todos module may reference BuildingBlocks.
- All types are `public` only if intentionally part of the shared kernel; otherwise `internal`.

## Contracts

- `Entity<TKey>` — base for all entities.
- `AggregateRoot<TKey>` — base for aggregates, tracks domain events.
- `EventSourcedAggregate` — base for replayable aggregates; `ISnapshotable` marks snapshot support.
- `IDomainEvent` / `DomainEvent` — domain event contract and base record.
- `Result<T>`, `Result` — success/failure with error codes.
- `Maybe<T>`, `Error` — optional values and structured failures.
- `Specification<T>` — composable query predicates.

## Conventions

- One aggregate per file; `Todo.cs`, `TodoCategory.cs` — not `Todo.Aggregate.cs`.
- Domain events are records in `*.Event.cs` files (e.g., `Todo.Event.cs`).
- Value objects live under `BuildingBlocks/Domain/ValueObjects/` (e.g., `Money/`, `Address/`).
- Error codes follow `Subject.Field.Rule`; guards return structured errors.

## Reading Order

1. `BuildingBlocks/Domain/` — aggregates, entities, events, value objects.
2. `BuildingBlocks/Modeling/` — concerns and metadata.
3. `BuildingBlocks/Functional/` — result, maybe, and error types.
4. `Modules/Todos/` — the Todo business module.

## Related

- Parent: [`../README.md`](../README.md)
- Children: `BuildingBlocks/Domain/`, `BuildingBlocks/Modeling/`, `BuildingBlocks/Functional/`, `Modules/Todos/`
- Downstream: [`../Application/`](../Application/), [`../Infrastructure/`](../Infrastructure/)
- ADR: [`../../docs/adr/0001-domain-kernel.md`](../../docs/adr/0001-domain-kernel.md)

## Ownership

- Owning team: **Platform**.
- Changes require Platform review; breaking changes require all downstream approval.

## Notes

- `TODO-2026Q3:` Add `IClock` abstraction for time-dependent domain logic.
- `TEMP:` `Slugify.Core` used for URL slugs; evaluate native implementation.