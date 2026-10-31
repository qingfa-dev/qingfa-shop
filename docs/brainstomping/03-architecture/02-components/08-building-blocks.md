# Building Blocks

The six-project shared kit every module and the API host compose from: responsibilities per project, folder layout inside each, and the one-way dependency rules.

**Links:** [Solution layout](./01-solution-layout.md) · [Module contracts](./02-module-contracts.md) · [Extension architecture](./07-extension-architecture.md) · [Migration guide](../../05-development/04-migration/01-old-references-to-building-blocks.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

`BuildingBlocks.*` is the **shared kit** (Clean-Architecture-style layers) reused by every `Resys.Modules.*` project and by `Resys.Api`. It is **not** a module: it owns no commerce tables, no `/api` routes of its own, and no store-specific rules.

This file is the physical map of the kit: which of the six projects holds which concern, what folders each project contains, and which project may reference which. The logical contract tiers modules publish stay in `03/02-02`; extension seams stay in `03/02-07`. Migration from the legacy `old_references/Shared` pillars is `05/04-01`.

**Status of other layout docs:** ADR 0002 still names `Resys.Core` / `Resys.Infrastructure` as supporting projects. Until superseding **ADR 0005** lands, treat this file + ADR 0005 (draft in `00/01`) as the **authoritative kit tree** for new work; `03/02-01` is updated in the same pass as that ADR.

---

## Repository Tree (kit only)

```
src/BuildingBlocks/
├─ BuildingBlocks.Kernel/           # pure primitives (no DDD) — depends on nothing
├─ BuildingBlocks.Core/             # shared domain (DDD) + commerce kernel — → Kernel
├─ BuildingBlocks.Application/      # CQRS, validation, query models — → Kernel, Core
├─ BuildingBlocks.Shared/           # client DTOs + JSON conventions — leaf
├─ BuildingBlocks.Infrastructure/   # technical adapters (many folders) — → Kernel, Core, Application
└─ BuildingBlocks.Hosting/          # composition helpers (no Program.cs) — → all of the above

src/Modules/Resys.Modules.<Name>/   # twelve modules (unchanged ownership rules)
src/Api/Resys.Api/                  # only executable
old_references/                     # read-only legacy Shared.* source until migration completes
```

All six kit projects are `net10.0`, central package management (`Directory.Packages.props`), and default `IsPackable=false` until a future feed strategy opts in.

---

## Project Responsibilities

| Project | Kind | One-line responsibility | May reference |
|---|---|---|---|
| `BuildingBlocks.Kernel` | Library | Pure language-level toolkit — **zero DDD, zero commerce, zero ASP.NET**: `Guard`, `Result`/`Error`/`PagedResult`, `IClock`, `IHasKey`/`KeyedRegistry`. | nothing (BCL only) |
| `BuildingBlocks.Core` | Library | **Shared domain (DDD) + event sourcing only**: `Domain/Entities`, `Domain/ValueObjects` (full VO catalog), `Domain/Concerns`, `Domain/Events` + `StreamId`/`EventEnvelope`, `Domain/Aggregates`, `Localization/`. **No technical ports.** | `Kernel` |
| `BuildingBlocks.Application` | Library | Use-case machinery **and every module-injectable contract** by area: `Cqrs/`, `Validation/`, `Mapping/`, `Specifications/` (+ `IRepository`/`IUnitOfWork`), `Identity/`, `Encryption/`, `Caching/`, `Notifications/`, `Storage/`, `Media/`, `Authentication/`, `Permissions/`, `Persistence/`, `Outbox/`, `Stores/`, `EventSourcing/`. | `Kernel`, `Core` |
| `BuildingBlocks.Shared` | Library | Client-facing HTTP DTO records and JSON convention converters. Leaf. | nothing |
| `BuildingBlocks.Infrastructure` | Library | Technical adapters in subfolders: Persistence, Querying, Outbox, Auditing, Security, Identity, Observability, Caching, Http, Storage, Media, Email, BackgroundJobs. | `Kernel`, `Core`, `Application` |
| `BuildingBlocks.Hosting` | Library | Composition-root helpers: `AddBuildingBlocks()` (buses, behaviors once in order Logging → Validation → ExceptionMapping, in-memory durability defaults), module scan conventions, OpenAPI/Scalar, middleware pipeline (CORS, rate limit, store resolution, locale, Result→HTTP), background/outbox relay registration. | all kit projects + modules (at runtime wiring only) |
| `Resys.Modules.<Name>` ×12 | Library | One bounded commerce slice each (unchanged from `03/02-02`). | `Kernel`, `Core`, `Application`, `Shared` |
| `Resys.Api` | Executable | Sole `Program.cs`; composes modules + infrastructure + hosting. | modules, infrastructure, hosting, core, shared, kernel, application |

---

## Folder Layout Inside Each Project

Filenames below are **conventions**, not mandatory single files. Promote a folder to its own `.csproj` only when packability or team ownership demands it (see “Splitting later”).

### `BuildingBlocks.Kernel/`

Pure primitives only — no DDD types, no commerce, no ASP.NET. ArchTests may assert absence of `AggregateRoot`/`ValueObject`/`IDomainEvent`.

```
Kernel/
├─ Guard/
├─ Results/                 # Result, ValueResult, PagedResult, Error
├─ Extensions/              # IHasKey, IKeyedRegistry, KeyedRegistry
└─ Time/                    # IClock (+ SystemClock impl lives in Infra)
```

### `BuildingBlocks.Core/`

Shared **domain** foundation (DDD bases) plus the commerce kernel and ports. Module entities stay in each module’s `Domain/` folder; only the **base types** live here.

```
Core/
└─ Domain/
   ├─ Entities/             Entity, AggregateRoot, IEntity concern
   ├─ ValueObjects/         ValueObject base + Money, Currency, Quantity, …
   ├─ Events/               IDomainEvent, DomainEvent, EventEnvelope, StreamId
   ├─ Aggregates/           ISnapshotable
   ├─ Ids/                  strongly-typed id base
   └─ Concerns/             Auditable, SoftDeletable, Versionable, …
└─ Localization/            Translatable, locale fallback shape
```

Module entities stay in each module’s `Domain/`; only base types live here. **Technical contracts are not in Core** — they live in Application area folders below.

### `BuildingBlocks.Application/`

```
Application/
├─ Cqrs/                    # ICommand/IQuery + buses, IDomainEventDispatcher, behaviors
│                            # (Logging → Validation → ExceptionMapping), inbox-aware publisher
├─ Validation/              # FluentValidation DI scan, options validators
├─ Specifications/          # typed ISpecification<T> / ISpecificationExecutor<T> + parsers
│                            # + IRepository/IUnitOfWork
├─ Mapping/                 # Mapster + Identity/Validation → Error mappers
├─ Descriptors/             # query descriptors / parameters
├─ Parameters/               # sort/seo/named/activatable parameter interfaces
├─ Errors/  Exceptions/     # error catalog + non-HTTP exception mapping
├─ Identity/                # IUserContext, IUserQuery, IRoleAssignment, IIdentitySchemaProvider
├─ Encryption/              # IEncryptionService
├─ Systems/                 # ISystemInfo
├─ Caching/                 # ICacheService
├─ Observability/           # ICorrelationContext
├─ Notifications/           # INotificationService, INotificationHub, INotificationProvider, settings
├─ Storage/                 # IStorageService, IStorageProvider, IFileStorage
├─ Media/                   # IImageProcessor, malware scanner/enforcer/guard
├─ Authentication/          # ICurrentUser, token/refresh/blacklist/theft, external login
├─ Permissions/             # IPermissionService/Store/Cache + PermissionMetadata
├─ Persistence/             # IDatabaseTransaction, IDatabaseInitializer, IDataSeeder, …
├─ Outbox/                  # IOutboxWriter, IOutboxReader, IInboxStore
├─ Stores/                  # IStoreContext
├─ EventSourcing/           # IEventStore, IAggregateStore, IEventSerializer,
│                            # IEventTypeNameRegistry, IEventUpcaster
```

No flat `Ports/` folder: contracts are split by area to match `old_references` concern paths.

### `BuildingBlocks.Shared/`

```
Shared/
├─ Conventions/             # Base64, case, dictionary, enum JSON converters
└─ <Area>/                  # DTO records per API area (Catalog, Orders, …) as they appear
```

### `BuildingBlocks.Infrastructure/`

```
Infrastructure/
├─ Persistence/             # EF base/conventions, interceptors, converters, seeders,
│                            # initializers, transactions, DB health state
├─ Querying/                # Specification → IQueryable apply
├─ Outbox/                  # EfOutboxWriter / EfOutboxReader / EfInboxStore + OutboxRelayService
├─ EventSourcing/           # EfEventStore (expected-version conflict, 0-based ReadAll)
├─ InMemory/                # in-memory outbox/inbox/event store/repository/UoW defaults
├─ Time/                    # SystemClock (IClock implementation)
├─ Auditing/                # AuditLog + same-tx interceptor                   [NEW vs old]
├─ Security/                # JWT, permission policy engine, antiforgery service, AES
├─ Identity/                # Identity stores, User/Role/RefreshToken EF + seeders
├─ Observability/
│  ├─ Correlation/
│  ├─ Telemetry/            # OpenTelemetry + Serilog + redaction              [NEW vs old]
│  └─ Health/
├─ Caching/                 # Hybrid / Redis / InMemory ICacheService
├─ Http/                    # typed clients, resilience, correlation propagation
├─ Storage/                 # IFileStorage local + S3, key scheme, signed URLs
├─ Media/                   # SkiaSharp derivatives + nClam scanners
├─ Email/                   # legacy name — prefer Notifications/Channels/Emails
├─ Notifications/           # full multi-channel hub (email, SMS, logging) — ported 2026-09-24
└─ BackgroundJobs/          # Hangfire registration
```

### `BuildingBlocks.Hosting/`

```
Hosting/
├─ Composition/             # AddBuildingBlocks() (buses, behaviors, in-memory defaults),
│                            # Add* / Map* conventions, hosted-service registration
├─ OpenApi/                 # dual documents, Scalar, transformers
└─ Pipeline/                # CORS, headers, rate limit (must be wired), CSRF, auth,
                             # store resolution, locale, Result→HTTP, problem details
```

**Edge composition recipe** — the host executable owns `AddMediator` (it references
`Mediator.SourceGenerator`; kit libraries reference `Mediator.Abstractions` only) and calls it
**before** `AddBuildingBlocks()`. Both options are load-bearing:

```csharp
builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
    options.NotificationPublisherType = typeof(InboxAwareNotificationPublisher);
});
builder.Services.AddBuildingBlocks();
```

- `ServiceLifetime.Scoped` — Mediator's default is `Singleton`, which would capture the kit's
  **Scoped** buses, behaviors and unit of work (captive dependencies under `ValidateScopes`).
- `NotificationPublisherType = typeof(InboxAwareNotificationPublisher)` — routes domain events
  through the inbox gate (`IInboxStore` dedupe) before the handlers run. Mediator's default
  (`ForeachAwaitPublisher`) has no inbox, so omitting the option silently reintroduces
  double-handling on relay replay.

---

## Dependency Direction

```
                         Resys.Api
                    ┌─────────┼──────────────┐
                    ▼         ▼              ▼
             Modules.*   Hosting.*    Infrastructure.*
                    │         │              │
                    │         └──────┬───────┘
                    │                ▼
                    │         Application.*  ──► Core ──► Kernel
                    │                │            │
                    └────────────────┼────────────┘
                                     ▼
                              Shared (leaf)
```

**Rules (enforced by compile-time ProjectReference + `Resys.ArchTests`):**

1. `Kernel` references nothing and contains **no DDD types** (`Entity`, `AggregateRoot`, `ValueObject`, `IDomainEvent`).
2. `Core` references only `Kernel` and owns shared domain bases + commerce + ports.
3. `Application` references only `Kernel` and `Core`.
4. `Shared` references nothing.
5. `Infrastructure` references `Kernel`, `Core`, `Application` — never `Hosting` or a module.
6. Inside `Infrastructure`, folders reference only ports in `Core`/`Application`; `Querying` may use `Persistence` types; `Identity` may use `Persistence`; `Media` may use `Storage`.
7. `Hosting` may reference every kit project (composition).
8. Modules reference `Kernel`, `Core`, `Application`, `Shared` only — **never** `Infrastructure`, `Hosting`, or another module.
9. Only `Resys.Api` has `Program.cs` and references `Infrastructure` + `Hosting` + all modules.

---

## What Does Not Live in the Kit

| Concern | Owner |
|---|---|
| Stripe / PayPal / offline gateway adapters | `Resys.Modules.Payments` |
| Per-module feature permission catalogs | each module (`Authorization/Features`) |
| Cart token middleware, order state machine | module (Orders / Cart) |
| Domain entities for catalog, orders, … | each module |
| Migrations and module `DbContext` implementations | each module (`Persistence/`) |
| Broker / message bus | forbidden (ADR 0002) |

---

| Cross-cutting port | Contract home | Implementation home |
|---|---|---|
| `IOutboxWriter`, `IOutboxReader`, `IInboxStore` | `Application/Outbox` | `Infrastructure/Outbox` (EF) + `Infrastructure/InMemory` |
| `IDomainEventDispatcher` | `Application/Cqrs` | `Application/Cqrs` (`MediatorDomainEventDispatcher`) — replaces the deleted `IEventDispatcher` |
| `ISpecification<T>`, `ISpecificationExecutor<T>`, `IRepository<T>`, `IUnitOfWork` | `Application/Specifications` | `Infrastructure/Querying` + `Infrastructure/Persistence` (EF) + `Infrastructure/InMemory` |
| `IAggregateStore` | `Application/EventSourcing` | `Application/EventSourcing` (`AggregateStore` over `IEventStore`) |
| `IEventStore` | `Application/EventSourcing` | `Infrastructure/EventSourcing` (EF) + `Infrastructure/InMemory` |
| `IEventTypeNameRegistry`, `IEventSerializer`, `IEventUpcaster` | `Application/EventSourcing` | registry + serializer: `Application/EventSourcing`; upcaster: extension point (no default impl) |
| `IFileStorage` | `Application/Storage` | `Infrastructure/Storage` |
| Transactional email | `Application/Notifications` (`INotificationService` — no `IEmailSender`) | `Infrastructure/Notifications` (Channels/Emails) |
| `ICacheService` | `Application/Caching` | `Infrastructure/Caching` |
| `IClock` | `Kernel/Time` | `Infrastructure/Time` (`SystemClock`) |
| `IStoreContext` | `Application/Stores` | request middleware in `Hosting/Pipeline` |
| Audit write | `Application` (optional port) | `Infrastructure/Auditing` |

The outbox/inbox/event-store/specification/repository/unit-of-work rows exist **twice each**: an
in-memory default registered by `AddBuildingBlocks()`, and an EF implementation that
`AddBuildingBlocksEfCore()` swaps in (plus the `OutboxRelayService` hosted worker).

---

## Splitting Later (folder → csproj)

Keep **six** projects until a folder needs its own NuGet identity or team ownership. Promote in this order when justified:

1. `Infrastructure/Outbox` → `BuildingBlocks.Outbox`
2. `Infrastructure/Security` + `Identity` → `BuildingBlocks.Security`
3. `Infrastructure/Observability` → `BuildingBlocks.Observability`
4. `Application/Specifications` + `Infrastructure/Querying` → `BuildingBlocks.Querying`
5. `Infrastructure/Storage` + `Media` → `BuildingBlocks.Storage`

Promotion step: create csproj, move folder, fix ProjectReferences, update ArchTests, update this file.

---

## Open Questions

1. Whether module-facing contract namespaces remain `Resys.Core.*` or move to `BuildingBlocks.Core.*` (ADR 0005).
2. ~~Whether MediatR stays optional forever or becomes required for new handlers~~ — **resolved:**
   MediatR is gone; the kit dispatches through **Mediator** (`Mediator.Abstractions` 3.0.2), with
   the source generator and the `AddMediator(...)` call owned by the edge project only (kit
   libraries reference the abstractions and never register the mediator). `AddBuildingBlocks()`
   registers the pipeline behaviors exactly once, in the order Logging → Validation →
   ExceptionMapping (`05/02-02`).
3. ~~Whether `Infrastructure/Notifications` (SMS hub) is ported~~ — **resolved:** full port (Audit log 2026-09-24).

## Links

- [Solution layout](./01-solution-layout.md) — full repo tree including modules
- [Module contracts](./02-module-contracts.md) — tiers modules publish
- [Extension architecture](./07-extension-architecture.md) — `KeyedRegistry` usage
- [Migration guide](../../05-development/04-migration/01-old-references-to-building-blocks.md) — porting `old_references`
- [ADR 0005 (draft)](../../00-governance/01-adr/05-building-blocks.md)
