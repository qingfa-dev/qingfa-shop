# BuildingBlocks and Modules Refactor

**Status:** Proposed
**Date:** 2026-10-30
**Scope:** Core, Application, Infrastructure, tests
**Constraint:** No new .NET projects. One assembly per layer. Behavior-preserving.

## 1. Context

The repository currently mixes reusable platform code and feature code inside each layer:

- Core has `Common/` and `Todos/`.
- Application has `Common/` and `Features/{Identity,Todos}/`.
- Infrastructure has technical folders (`Caching`, `Events`, `Integrations`, `Persistence`, `Security`, `Services`) and feature code embedded in them (`Schemas/Todos`, Todo projections, Todo messaging handlers, `Services/Todos`, `Security/Identity`).

This makes Todos difficult to remove or move and leaves BuildingBlocks naming feature concepts. The goal is a consistent, extractable structure without adding projects.

## 2. Goals

- Split each layer into two top-level buckets:
  - `BuildingBlocks/`: reusable platform capabilities.
  - `Modules/`: business capabilities.
- Group BuildingBlocks by correct taxonomy:
  - Core: domain kernel, modeling primitives, functional primitives.
  - Application: use-case execution, outbound ports, querying, messaging, event sourcing, security, caching.
  - Infrastructure: adapters implementing application ports.
- Keep Clean Architecture dependency direction:
  - `Core.Modules → Core.BuildingBlocks`
  - `Application.Modules → Application.BuildingBlocks → Core.BuildingBlocks`
  - `Infrastructure.Modules → Infrastructure.BuildingBlocks → Application.BuildingBlocks → Core.BuildingBlocks`
- Remove Todos references from BuildingBlocks.
- Treat Identity as a platform BuildingBlock in both Application and Infrastructure.
- Mirror the same buckets in unit and integration tests.
- Add architecture tests that prevent future boundary regressions.

## 3. Non-goals

- No new `.csproj`, solution projects, or packages.
- No split of the existing EF Core `ApplicationDbContext`.
- No multi-tenant or multi-database redesign.
- No behavioral change to Todo or Identity use cases.
- No new public HTTP API surface.
- No CI redesign beyond test folder paths if required.

## 4. Decisions

| Decision | Choice |
|---|---|
| Reusable bucket name | `BuildingBlocks` |
| Feature bucket name | `Modules` |
| Identity classification | BuildingBlock in Application and Infrastructure |
| Todos classification | Module in Core, Application, Infrastructure |
| EF context | Stays in Infrastructure BuildingBlocks |
| EF migrations | Third folder: `Infrastructure/Migrations` |
| Composition | BuildingBlocks and Modules have separate composition files |
| Taxonomy | Ports and adapters |

## 5. Target structure

### 5.1 Core

```text
src/Core/
  BuildingBlocks/
    Domain/
      Aggregates/
      Entities/
      Events/
      EventSourcing/
      Policies/
      Specifications/
      ValueObjects/
    Modeling/
      Concerns/
        Content/
        Foundation/
        Lifecycle/
        Organization/
      Metadata/
    Functional/
      Errors/
      Maybes/
      Results/
  Modules/
    Todos/
      Categories/
      Items/
      Lists/
      Reminders/
```

Namespace roots:

```text
Core.BuildingBlocks.Domain.*
Core.BuildingBlocks.Modeling.*
Core.BuildingBlocks.Functional.*
Core.Modules.Todos.*
```

Core has no composition root because it has no DI registration.

### 5.2 Application

```text
src/Application/
  BuildingBlocks/
    Caching/
    EventSourcing/
    Messaging/
    Persistence/
    Ports/
      Notifications/
      Storage/
    Querying/
    Security/
      Authentication/
      Authorization/
      Identity/
    UseCases/
      Mapping/
      Mediation/
      Validation/
  Modules/
    Todos/
      Abstractions/
      Categories/
      Common/
      Events/
      Items/
      Lists/
      Reminders/
```

Namespace roots:

```text
Application.BuildingBlocks.Caching.*
Application.BuildingBlocks.EventSourcing.*
Application.BuildingBlocks.Messaging.*
Application.BuildingBlocks.Persistence.*
Application.BuildingBlocks.Ports.*
Application.BuildingBlocks.Querying.*
Application.BuildingBlocks.Security.*
Application.BuildingBlocks.UseCases.*
Application.Modules.Todos.*
```

### 5.3 Infrastructure

```text
src/Infrastructure/
  BuildingBlocks/
    Adapters/
      BackgroundJobs/
      Caching/
      Events/
      Http/
      Messaging/
      Notifications/
      Observability/
      OpenApi/
      Persistence/
        EntityFramework/
        Initialization/
      Security/
        Authentication/
        Authorization/
        Identity/
      Serialization/
      Storage/
  Modules/
    Todos/
      Integrations/
        Messaging/
      Persistence/
        EventSourcing/
        Projections/
        Schemas/
      Services/
  Migrations/
```

Namespace roots:

```text
Infrastructure.BuildingBlocks.Adapters.*
Infrastructure.Modules.Todos.*
Infrastructure.Migrations
```

`Infrastructure/Migrations` is the only folder allowed to reference both BuildingBlocks and Modules. It is a composition artifact, not a reusable BuildingBlock.

### 5.4 Composition files

```text
src/Application/
  Application.Composition.cs
  BuildingBlocks/BuildingBlock.Application.Composition.cs
  Modules/Modular.Application.Composition.cs

src/Infrastructure/
  Infrastructure.Composition.cs
  BuildingBlocks/BuildingBlock.Infra.Composition.cs
  Modules/Modular.Infra.Composition.cs
```

`Application.Composition.cs` and `Infrastructure.Composition.cs` remain the layer entry points. They call the two bucket compositions and keep the startup pipeline.

## 6. Classification inventory

### 6.1 Core BuildingBlocks

| Current | Target |
|---|---|
| `Core/Common/Concerns/**` | `Core/BuildingBlocks/Modeling/Concerns/**` |
| `Core/Common/Domain/**` | `Core/BuildingBlocks/Domain/**` |
| `Core/Common/Monad/Errors/**` | `Core/BuildingBlocks/Functional/Errors/**` |
| `Core/Common/Monad/Maybes/**` | `Core/BuildingBlocks/Functional/Maybes/**` |
| `Core/Common/Monad/Metadata/**` | `Core/BuildingBlocks/Modeling/Metadata/**` |
| `Core/Common/Monad/Results/**` | `Core/BuildingBlocks/Functional/Results/**` |

### 6.2 Core Modules

| Current | Target |
|---|---|
| `Core/Todos/**` | `Core/Modules/Todos/**` |

### 6.3 Application BuildingBlocks

| Current | Target |
|---|---|
| `Application/Common/Caching/**` | `Application/BuildingBlocks/Caching/**` |
| `Application/Common/Events/**` | `Application/BuildingBlocks/Messaging/Events/**` |
| `Application/Common/EventSourcing/**` | `Application/BuildingBlocks/EventSourcing/**` |
| `Application/Common/Integrations/Notifications/**` | `Application/BuildingBlocks/Ports/Notifications/**` |
| `Application/Common/Integrations/Storages/**` | `Application/BuildingBlocks/Ports/Storage/**` |
| `Application/Common/Mapping/**` | `Application/BuildingBlocks/UseCases/Mapping/**` |
| `Application/Common/Mediators/**` | `Application/BuildingBlocks/UseCases/Mediation/**` |
| `Application/Common/Messaging/**` | `Application/BuildingBlocks/Messaging/**` |
| `Application/Common/Persistence/**` | `Application/BuildingBlocks/Persistence/**` |
| `Application/Common/Querying/**` | `Application/BuildingBlocks/Querying/**` |
| `Application/Common/Security/**` | `Application/BuildingBlocks/Security/**` |
| `Application/Common/Validation/**` | `Application/BuildingBlocks/UseCases/Validation/**` |
| `Application/Features/Identity/**` | `Application/BuildingBlocks/Security/Identity/**` |

### 6.4 Application Modules

| Current | Target |
|---|---|
| `Application/Features/Todos/**` | `Application/Modules/Todos/**` |

### 6.5 Infrastructure BuildingBlocks

| Current | Target |
|---|---|
| `Infrastructure/Caching/**` | `Infrastructure/BuildingBlocks/Adapters/Caching/**` |
| `Infrastructure/Events/**` | `Infrastructure/BuildingBlocks/Adapters/Events/**` |
| `Infrastructure/Integrations/Backgrounds/**` | `Infrastructure/BuildingBlocks/Adapters/BackgroundJobs/**` |
| `Infrastructure/Integrations/Http/**` | `Infrastructure/BuildingBlocks/Adapters/Http/**` |
| `Infrastructure/Integrations/Messaging/**` (generic parts) | `Infrastructure/BuildingBlocks/Adapters/Messaging/**` |
| `Infrastructure/Integrations/Notifications/**` | `Infrastructure/BuildingBlocks/Adapters/Notifications/**` |
| `Infrastructure/Integrations/Observability/**` | `Infrastructure/BuildingBlocks/Adapters/Observability/**` |
| `Infrastructure/Integrations/OpenApi/**` | `Infrastructure/BuildingBlocks/Adapters/OpenApi/**` |
| `Infrastructure/Integrations/Serialization/**` | `Infrastructure/BuildingBlocks/Adapters/Serialization/**` |
| `Infrastructure/Integrations/Storages/**` | `Infrastructure/BuildingBlocks/Adapters/Storage/**` |
| `Infrastructure/Persistence/EntityFramework/**` | `Infrastructure/BuildingBlocks/Adapters/Persistence/EntityFramework/**` |
| `Infrastructure/Persistence/Initialization/**` | `Infrastructure/BuildingBlocks/Adapters/Persistence/Initialization/**` |
| `Infrastructure/Security/Authentication/**` | `Infrastructure/BuildingBlocks/Adapters/Security/Authentication/**` |
| `Infrastructure/Security/Authorization/**` | `Infrastructure/BuildingBlocks/Adapters/Security/Authorization/**` |
| `Infrastructure/Security/Identity/**` | `Infrastructure/BuildingBlocks/Adapters/Security/Identity/**` |

### 6.6 Infrastructure Modules

| Current | Target |
|---|---|
| `Infrastructure/Integrations/Messaging/Handlers/Todo*` | `Infrastructure/Modules/Todos/Integrations/Messaging/Handlers/**` |
| Todo event/queue wiring from `Messaging.Composition.cs` | `Infrastructure/Modules/Todos/Integrations/Messaging/TodoMessaging.Composition.cs` |
| `Infrastructure/Persistence/Schemas/Todos/**` | `Infrastructure/Modules/Todos/Persistence/Schemas/**` |
| `Infrastructure/Persistence/EntityFramework/Projections/Todo*` | `Infrastructure/Modules/Todos/Persistence/Projections/**` |
| Todo/Reminder registrations from `EventStore.Composition.cs` | `Infrastructure/Modules/Todos/Persistence/EventSourcing/TodoEventStore.Composition.cs` |
| `Infrastructure/Services/Todos/**` | `Infrastructure/Modules/Todos/Services/**` |

### 6.7 Infrastructure third folder

| Current | Target |
|---|---|
| `Infrastructure/Persistence/Migrations/**` | `Infrastructure/Migrations/**` |

## 7. BuildingBlocks taxonomy

### 7.1 Core

- `Domain/`: DDD tactical patterns and primitives.
- `Modeling/`: cross-cutting model concerns and metadata.
- `Functional/`: Result, Maybe, Error types.

Rationale: the domain kernel is not a generic “Common” folder. It separates tactical DDD, modeling concerns, and functional result types.

### 7.2 Application

- `UseCases/`: command/query mediation, mapping, validation.
- `Persistence/`: repository, unit-of-work, initialization, and capability ports.
- `Querying/`: query model, parsing, projection, pagination, sorting, filtering.
- `Messaging/`: bus, inbox, outbox, event contracts, serialization ports.
- `EventSourcing/`: event store, snapshot, projection ports.
- `Security/`: authentication, authorization, and Identity platform ports/use cases.
- `Caching/`: cache ports and options.
- `Ports/`: outbound notification and storage ports.

Rationale: Application BuildingBlocks are the application-side ports and use-case infrastructure. They do not describe external technology.

### 7.3 Infrastructure

- `Adapters/`: every implementation of an Application/Core port.
- Adapter subfolders match the port capability (`Caching`, `Messaging`, `Notifications`, `Persistence`, `Security`, `Storage`).
- Cross-cutting host adapters live under `Adapters` too (`BackgroundJobs`, `Http`, `Observability`, `OpenApi`, `Serialization`).

Rationale: Infrastructure BuildingBlocks are adapters. The name makes the hexagonal boundary explicit and removes the vague `Integrations` catch-all.

## 8. Composition and extensibility

### 8.1 Bucket composition

```text
Application/BuildingBlocks/BuildingBlock.Application.Composition.cs
  AddBuildingBlocksApplication()

Application/Modules/Modular.Application.Composition.cs
  AddModularApplication()

Application/Application.Composition.cs
  AddApplication() → AddBuildingBlocksApplication() + AddModularApplication()
```

```text
Infrastructure/BuildingBlocks/BuildingBlock.Infra.Composition.cs
  AddBuildingBlocksInfrastructure()
  UseBuildingBlocksInfrastructure()

Infrastructure/Modules/Modular.Infra.Composition.cs
  AddModularInfrastructure()
  UseModularInfrastructure()

Infrastructure/Infrastructure.Composition.cs
  AddInfrastructure()
    → AddBuildingBlocksInfrastructure()
    → AddModularInfrastructure()
  UseInfrastructure()
    → UseBuildingBlocksInfrastructure()
    → UseModularInfrastructure()
    → migration/seeding pipeline
```

### 8.2 Module composition

```text
Application/Modules/Todos/Todos.Application.Composition.cs
Infrastructure/Modules/Todos/Todos.Infrastructure.Composition.cs
```

`Modular.*.Composition` calls the module composition. Adding a module later means adding one composition file and one call, not editing BuildingBlocks.

### 8.3 Contribution contracts

Where a registry must span BuildingBlocks and Modules, define the contract in BuildingBlocks and implement it in Modules:

- `IPermissionCatalogContributor`
- `IPermissionRoleContributor`
- `IPersistenceSchemaCatalog`
- `IMessagingModule`
- `IEventStoreModule`

The default BuildingBlocks registration provides platform-only behavior. `Modular.*.Composition` adds module contributions. This keeps BuildingBlocks compilable and useful without Todos.

## 9. Todos decoupling

These are the only non-mechanical code changes in the refactor.

### 9.1 Permissions

- Move `PermissionContext.Todo` to `Application/Modules/Todos/Permissions/TodoPermissions.cs`.
- Keep Identity permissions in `Application/BuildingBlocks/Security/Authorization` as the platform baseline.
- Add a composite permission catalog in `Modular.Application.Composition` that merges platform and Todos permissions.
- Infrastructure `PermissionMetadataRegistry` and Identity `RoleSeeder` consume the composite contract instead of a static Todo-aware `PermissionContext.All`.

Acceptance: removing `Application/Modules/Todos` leaves a valid platform permission catalog for Identity.

### 9.2 Persistence schemas

- Move `PersistenceConstant.Schema.Todo` to `Application/Modules/Todos/Persistence/TodoSchema.cs`.
- Keep `identity` and `messaging` in the platform schema constants.
- Add `IPersistenceSchemaCatalog`; the default is platform schemas, and `Modular.Application.Composition` adds `todo`.
- `EfCoreResetter` resolves schemas through the catalog.

Acceptance: removing Todos does not leave `todo` in BuildingBlocks.

### 9.3 Messaging

- `BuildingBlocks/Adapters/Messaging` keeps Wolverine setup, settings, serialization, retry, health, and the generic bus.
- `Modules/Todos/Integrations/Messaging` owns Todo event names, queues, handlers, publish/listen bindings, and event-type registration.
- `IMessagingModule` is the extension contract.

Acceptance: queue names, exchange name, routing keys, and handler bindings are unchanged.

### 9.4 Event sourcing

- BuildingBlocks keeps the generic event store, snapshot store, checkpoint store, projection runner, and projection worker.
- Todos owns `TodoFactory`, `TodoReminderFactory`, `TodoProjection`, `TodoReminderProjection`, and their registration.
- `IEventStoreModule` is the extension contract.

Acceptance: the same aggregates and projections are registered at startup.

### 9.5 Services

- BuildingBlocks keeps generic service composition hooks.
- Todos owns `ITodoReferenceCleaner → TodoReferenceCleaner` registration.

### 9.6 Persistence context and migrations

- `ApplicationDbContext` stays in `Infrastructure/BuildingBlocks/Adapters/Persistence/EntityFramework/Contexts`.
- It references Identity BuildingBlock types only.
- Migrations move to `Infrastructure/Migrations`.
- Migration snapshot/designer CLR type strings must be updated after namespace moves.

## 10. Tests

Tests mirror the same taxonomy.

| Test project | BuildingBlocks | Modules |
|---|---|---|
| `Core.Unit.Tests` | `BuildingBlocks/{Domain,Modeling,Functional}` | `Modules/Todos` |
| `Application.Unit.Tests` | `BuildingBlocks/{Caching,EventSourcing,Messaging,Persistence,Ports,Querying,Security,UseCases}` | `Modules/Todos` |
| `Infrastructure.Unit.Tests` | `BuildingBlocks/Adapters/{Caching,Events,Http,Messaging,Notifications,Observability,OpenApi,Persistence,Security,Serialization,Storage}` | `Modules/Todos` |
| `Infrastructure.Integration.Tests` | `BuildingBlocks/{Caching,Messaging,Persistence,Security}` | `Modules/Todos` |
| `Api.Integration.Tests` | `BuildingBlocks/{Api,Serialization,Security/Identity}` | `Modules/Todos` |
| `AppHost.Integration.Tests` | root (host composition) | root |

Identity tests stay under `BuildingBlocks/Security/Identity`.

Test namespaces follow the folder path, including project root, e.g.:

```text
Application.Unit.Tests.BuildingBlocks.Security.Identity.Auth.LoginTest
Infrastructure.Unit.Tests.Modules.Todos.Persistence.Schemas.TodoSeederTest
```

## 11. Architecture guardrails

Add reflection-based architecture tests:

1. `Core.BuildingBlocks.*` must not reference `Core.Modules.*`.
2. `Application.BuildingBlocks.*` must not reference `Application.Modules.Todos.*`.
3. `Infrastructure.BuildingBlocks.*` must not reference `Infrastructure.Modules.Todos.*` or `Application.Modules.Todos.*`.
4. `*.Modules.Todos.*` may reference `*.BuildingBlocks.*`.
5. Modules must not reference another module directly; cross-module communication goes through BuildingBlocks contracts or the composition root.
6. `Infrastructure.Migrations` is exempt from rule 3.

These tests use reflection over type namespaces and referenced assemblies, not NetArchTest, so they work with the current test stack.

## 12. Migration plan

### Phase 0: Baseline

- Record `make build`, `make test`, `make coverage`, and `make structure.check` results.
- Confirm working tree state and do not mix unrelated edits.

### Phase 1: Core

- Move `Common` → `BuildingBlocks/{Domain,Modeling,Functional}`.
- Move `Todos` → `Modules/Todos`.
- Update namespaces and global usings.
- Move Core unit tests to the mirrored buckets.
- Verify `make lib.build` and `make test.dotnet.unit`.

### Phase 2: Application

- Move `Common` → `BuildingBlocks/*`.
- Move `Features/Identity` → `BuildingBlocks/Security/Identity`.
- Move `Features/Todos` → `Modules/Todos`.
- Rename `Mediators` → `UseCases/Mediation`, `Mapping` → `UseCases/Mapping`, `Validation` → `UseCases/Validation`, `Integrations/Notifications` → `Ports/Notifications`, `Integrations/Storages` → `Ports/Storage`.
- Add `BuildingBlock.Application.Composition` and `Modular.Application.Composition`.
- Move Application unit tests.
- Verify `make build` and `make test.dotnet.unit`.

### Phase 3: Infrastructure

- Move technical folders under `BuildingBlocks/Adapters/*`.
- Move Identity baseline under `Adapters/Security/Identity`.
- Move Todos-specific persistence, projects, messaging, event sourcing, and services under `Modules/Todos`.
- Add `BuildingBlock.Infra.Composition` and `Modular.Infra.Composition`.
- Split `Messaging.Composition`, `EventStore.Composition`, and `Service.Composition`.
- Move Infrastructure unit tests.
- Verify `make build`, `make test.dotnet.unit`, and `make test.infra.broker`.

### Phase 4: Migrations

- Move migrations to `Infrastructure/Migrations`.
- Update `ApplicationDbContext` references and snapshot/designer CLR type names.
- Verify `dotnet ef migrations has-pending-model-changes` reports no changes.
- Verify `make test.dotnet.integration` if Docker is available.

### Phase 5: Tests and guardrails

- Move integration test folders.
- Add architecture boundary tests.
- Update test namespaces and global usings.
- Verify `make test`, `make coverage`, and `make coverage.infra`.

### Phase 6: Documentation

- Update `README.md`, `src/README.md`, layer READMEs, `tests/README.md`, `AGENTS.md`, and `scripts/README.md`.
- Document the BuildingBlocks/Modules taxonomy and composition seams.

### Phase 7: Full gate

- `make build`
- `make test`
- `make coverage`
- `make test.infra.broker`
- `make coverage.infra`
- `make structure.check`
- `make ci`

## 13. Risks

| Risk | Mitigation |
|---|---|
| EF snapshot/designer type strings drift | Update every moved CLR type name and run `has-pending-model-changes` |
| `PersistenceConstant.Schema.Default` currently equals `todo` | Replace with a neutral platform default and verify migrations produce no model changes |
| Static `PermissionContext` used across tests | Preserve exact permission identifiers and role defaults through composite contracts |
| Messaging queue/exchange names drift | Keep constants unchanged and assert startup composition in tests |
| Namespace moves break global usings | Update each layer's `Using.cs` and run `make build` after every phase |
| Large mechanical diff hides behavioral change | Separate folder moves from Todos decoupling commits/phases; run tests at each phase |
| Architecture tests are reflection-only | Add explicit namespace dependency checks and keep manual review for composition roots |

## 14. Acceptance criteria

- Core, Application, and Infrastructure each expose `BuildingBlocks/` and `Modules/`.
- `Modules/` contains only Todos in Core, Application, and Infrastructure.
- Identity is a BuildingBlock in Application and Infrastructure.
- No BuildingBlocks folder references `Todos`.
- `Infrastructure/Migrations` is the only cross-module EF exception.
- `ApplicationDbContext` remains single and lives in Infrastructure BuildingBlocks.
- BuildingBlocks and Modules each have explicit composition files.
- Tests mirror the same buckets.
- Architecture tests enforce the rules.
- No new projects or packages.
- All existing tests pass with unchanged behavior.

## 15. Resolved during planning

- The Infrastructure third folder is `Infrastructure/Migrations`.
- The implementation plan is `docs/superpowers/plans/2026-10-30-building-blocks-modules-refactor.md`.
- Folder moves and Todos decoupling are sequenced as separate tasks, but all changes stay uncommitted per the human partner's instruction.
- The Todos permission and schema contributor refactor is included in the plan, not deferred.
