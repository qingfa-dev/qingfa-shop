# Contract

Public API contracts — DTOs, commands, queries, events, and validation rules. _layer · api_

## Summary

Contract defines the **public surface** shared between services, applications, and external consumers. It contains request/response DTOs, MediatR commands/queries, domain events (for cross-service communication), and FluentValidation validators. It has no business logic — only data shapes and validation rules.

## Contains

| Path | Role | Notes |
|---|---|---|
| `Assembly.Marker.cs` | marker | Anchors assembly for scanning |
| `Commands/` | command | `ICommand<TResult>` implementations (e.g., `PlaceOrderCommand`) |
| `Queries/` | query | `IQuery<TResult>` implementations (e.g., `GetOrderQuery`) |
| `Events/` | event | Integration events (e.g., `OrderPlacedIntegrationEvent`) |
| `Dtos/` | dto | Request/response DTOs for HTTP APIs |
| `Validators/` | validator | FluentValidation rules for commands/queries/DTOs |
| `Common/` | type | Shared enums, constants, error codes |

## Boundaries

In scope:
- MediatR messages: `ICommand<T>`, `IQuery<T>`, `INotification`.
- DTOs for HTTP APIs (request/response).
- Integration events published to message bus.
- FluentValidation validators for messages and DTOs.
- Shared error codes, enums, constants.

Out of scope:
- Domain logic → [`../Core/`](../Core/) or [`../Application/`](../Application/).
- Persistence models → [`../Infrastructure/`](../Infrastructure/).
- Handler implementations → [`../Application/`](../Application/).
- Infrastructure adapters (EF Core, Redis, etc.).

## Assumptions

- All commands/queries implement `ICommand<TResult>` / `IQuery<TResult>` from `Mediator.Abstractions`.
- DTOs are immutable records with `init` setters.
- Integration events are versioned via namespace or suffix (`OrderPlacedV1`).
- Validators are registered via `FluentValidation.DependencyInjectionExtensions`.

## Invariants

- **No MediatR runtime dependency** — only `Mediator.Abstractions` (source-generated mediator).
- **No project references** except [`../Core/`](../Core/).
- All types are `public` and `record`/`sealed class` — no inheritance hierarchies.
- JSON serialization uses `System.Text.Json` conventions (camelCase, snake_case via `JsonNamingPolicy`).

## Contracts

- `ICommand<TResult>` — write operations returning result.
- `IQuery<TResult>` — read operations returning result.
- `INotification` — domain/integration events (fire-and-forget).
- `IValidator<T>` — validation rules (FluentValidation).
- Error codes: `Contract.Errors.*` (e.g., `Order.NotFound`, `Payment.Declined`).

## Conventions

- Commands: `VerbNounCommand` (e.g., `PlaceOrderCommand`, `CancelOrderCommand`).
- Queries: `GetNounQuery` / `ListNounQuery` (e.g., `GetOrderQuery`, `ListOrdersQuery`).
- Events: `NounVerbEvent` (e.g., `OrderPlacedEvent`, `OrderCancelledEvent`).
- DTOs: `NounDto` / `VerbNounRequest` / `VerbNounResponse`.
- Validators: `NounValidator` (e.g., `PlaceOrderCommandValidator`).

## Reading Order

1. `Common/` — error codes, shared enums.
2. `Commands/` — write operations.
3. `Queries/` — read operations.
4. `Events/` — integration events.
5. `Dtos/` — HTTP API shapes.
6. `Validators/` — validation rules.

## Related

- Parent: [`../README.md`](../README.md)
- Upstream: [`../Core/`](../Core/) (domain primitives)
- Downstream: [`../Application/`](../Application/) (handlers), [`../../app/Web.Api/`](../../app/Web.Api/) (endpoints)
- ADR: [`../../docs/adr/0002-contract-first.md`](../../docs/adr/0002-contract-first.md)

## Ownership

- Owning team: **Platform**.
- Contract changes require **API**, **Storefront**, **Admin** review.
- Breaking changes require version bump and migration plan.

## Notes

- `TODO-2026Q3:` Generate TypeScript types from Contract via `dotnet-openapi` / `ts-openapi`.
- `TEMP:` Some DTOs still use `Guid` instead of `Ulid`; align after Core ULID adoption.