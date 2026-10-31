# BuildingBlocks and Modules Refactor — 2026-10-31

## Outcome

Core, Application, and Infrastructure now use two top-level buckets:

- `BuildingBlocks/` — reusable platform capabilities.
- `Modules/` — business capabilities.

`Todos` is the only module. `Identity` is a BuildingBlock in Application and
Infrastructure. `Infrastructure/Migrations` is the only cross-module EF artifact.

## Layer layout

| Layer | BuildingBlocks | Modules |
|---|---|---|
| Core | `Domain`, `Modeling`, `Functional` | `Todos` |
| Application | `Caching`, `EventSourcing`, `Messaging`, `Persistence`, `Ports`, `Querying`, `Security`, `UseCases` | `Todos`, plus `PermissionCatalog` and `Persistence` composite-catalog composition artifacts |
| Infrastructure | `Adapters/{BackgroundJobs,Caching,Events,Http,Messaging,Notifications,Observability,OpenApi,Persistence,Security,Serialization,Storage}` | `Todos` |

## Composition

- `Application/BuildingBlocks/BuildingBlock.Application.Composition.cs`
- `Application/Modules/Modular.Application.Composition.cs`
- `Application/Application.Composition.cs`
- `Infrastructure/BuildingBlocks/BuildingBlock.Infra.Composition.cs`
- `Infrastructure/Modules/Modular.Infra.Composition.cs`
- `Infrastructure/Infrastructure.Composition.cs`

## Todos decoupling

- Todos permissions moved to `Application/Modules/Todos/Permissions`.
- Todos schema moved to `Application/Modules/Todos/Persistence`.
- Todo messaging queues/handlers moved to `Infrastructure/Modules/Todos/Integrations/Messaging`.
- Todo event-store registrations moved to `Infrastructure/Modules/Todos/Persistence/EventSourcing`.
- Todo reference cleaner moved to `Infrastructure/Modules/Todos/Services`.
- Migration history moved to `Infrastructure/Migrations`; a generic pre-migrate guard moves legacy history tables into the default schema.

## Verification

`make lint`, `make build`, `make test`, `make coverage`, `make test.infra.broker`,
`make coverage.infra`, `make coverage.api`, `make host.test`, and `make ci` all
passed. `dotnet ef migrations has-pending-model-changes` reported no changes.

## Notes

- The work was staged only; no commit was created.
- The previous design/plan/release/spec files are preserved under
  `docs/changelogs/archive/`.
