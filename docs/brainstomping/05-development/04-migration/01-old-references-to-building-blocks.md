# Migrate old_references → BuildingBlocks

A complete, executable guide to porting the legacy `old_references/Shared.*` pillars into the six `BuildingBlocks.*` projects: what to copy where, what to rewrite, what to drop, how to keep the build green after every phase, and how to retire `old_references` when done.

**Links:** [Building Blocks](../../03-architecture/02-components/08-building-blocks.md) · [ADR 0005](../../00-governance/01-adr/05-building-blocks.md) · [Module conventions](../02-conventions/02-module-conventions.md) · [.NET style](../02-conventions/01-dotnet-style.md) · [Build and run](../03-workflow/01-build-run.md) · [Development index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

`old_references/` is **read-only source** for the six legacy projects:

| Legacy project | Approx. prod `.cs` | Pillar theme |
|---|---:|---|
| `Shared.Foundations` | 96 | Kernel + application + identity/permission ports |
| `Shared.Governance` | 10 | OpenAPI, JSON conventions, validation registration |
| `Shared.Platform` | 34 | Correlation, health, logging, caching |
| `Shared.Security` | 140 | JWT, authorization, Identity entities, middleware |
| `Shared.Persistence` | 127 | EF base, interceptors, specs, seeders, identity EF |
| `Shared.Integration` | 158 | Http resilience, notifications, storage, backgrounds |
| `Shared.UnitTests` | 300 | Monolithic tests (broken ProjectReferences) |

The destination is the **six** `BuildingBlocks.*` projects under `src/BuildingBlocks/` (`03/02-08`). This guide is the checklist so nothing is lost and nothing illegal is copied.

**Rules for the whole migration**

1. **Do not edit** files under `old_references/` (reference only).
2. Legacy projects live in **`old_references/old_references.slnx` only** — they are **not** in the root `resys-eshop.slnx` because they fail restore under the current central package versions and analyzer rules. Open the legacy solution separately when you need to read/build a pillar mid-port; the root solution must stay green on the six kit projects.
3. **Do not port** module-owned leakage (cart middleware, feature catalogs, `OrderFulfillmentState`, dead package refs).
4. **Namespaces:** replace `Shared.*` with the target project namespace (table below) as you copy; keep folder names under the new root.
5. **Packages:** never copy `PackageReference` blindly — declare only packages pinned in root `Directory.Packages.props` (add missing pins first: Serilog, PayPal when needed, SES when needed).
6. **After every phase:** root `dotnet build resys-eshop.slnx` and `dotnet test` (once tests exist) must pass; commit with a conventional message.
7. **Migrations/god DbContext:** old `ApplicationDbContext` is **not** ported as-is — ADR 0002 requires per-module DbContexts. Port interceptors/converters/seeder **primitives** only.

---

## Target Map (old folder → new home)

### Kernel ← Foundations (pure primitives only)

| Old path (under `old_references/Shared/Shared.Foundations/`) | New path under `BuildingBlocks.Kernel/` |
|---|---|
| `Application/Models/Results/*` (Result, ValueResult, PagedResult) | `Results/` |
| `Application/Models/Errors/*` (core Error primitives only) | `Results/` or `Errors/` |
| `Application/Systems/*` (`ISystemDateTime` → `IClock`) | `Time/` |
| — | **Add** `Extensions/` ← `IHasKey` / `KeyedRegistry` (from `03/02-07`; pure toolkit, not domain) |
| — | **Add** `Guard/` |
| `Shared.Marker.cs` | **drop** |

**Do not put in Kernel:** `Entity`/`AggregateRoot`/`ValueObject`, `IDomainEvent`, typed ids, `Auditable`/`SoftDeletable` — those are **Domain** → Core.

**Namespace:** `BuildingBlocks.Kernel` (+ subnamespaces).

### Core ← shared domain (DDD) + event sourcing ONLY

| Old path | New path under `BuildingBlocks.Core/` |
|---|---|
| `Application/Domain/Models/{Entity,AggregateRoot,ValueObject}` | `Domain/Entities/`, `Domain/ValueObjects/ValueObject.cs` |
| `Application/Domain/Concerns/Entities/Entity.cs` (`IEntity*`) | `Domain/Concerns/Entities/` |
| `Application/Domain/Concerns/{Auditable,SoftDeletable,…}` | `Domain/Concerns/<Name>/` |
| `Application/Domain/Money/SystemMoney` | `Domain/ValueObjects/Money/` (rename `Money`) |
| `Application/Domain/Currencies/SystemCurrency*` | `Domain/ValueObjects/Currency/` |
| — **Add** VO-C catalog | `Domain/ValueObjects/{Quantity,Percentage,Slug,MoneyRange,DateRange,EmailAddress,PhoneNumber}/` |
| — **Add** ES-3 | `Domain/Events/` (`IDomainEvent`, `DomainEvent`, `EventEnvelope`, `StreamId`), `Domain/Aggregates/ISnapshotable` |
| — **Add** typed id base if needed | `Domain/Ids/` |
| (new) Translatable / locale shape per `03/03-03` | `Localization/` |
| `Application/Domain/Orders/OrderFulfillmentState.cs` | **DO NOT PORT** → Orders module |

**Not in Core (all technical contracts → Application):** identity ports, `PermissionMetadata`, `IEncryptionService`, `ICacheService`, `IOutboxWriter`, `IEventDispatcher`, `IFileStorage`, `IStoreContext`, `IEventStore`, notification interfaces, storage/media/auth/persistence interfaces.

**Namespace:** `BuildingBlocks.Core.*` / `BuildingBlocks.Core.Localization`.

### Application ← ALL remaining interfaces/abstractions + use-case machinery

**Rule:** every module-injectable interface from `old_references` lives here in an **area folder matching its old concern** — no `Ports/`, no flat dump.

| Old path (source of interfaces) | New path under `BuildingBlocks.Application/` |
|---|---|
| `Identity/IUserContext`, `IUserQuery`, `IRoleAssignment`, `IIdentitySchemaProvider` | `Identity/` |
| `Operational/Security/Encryption/IEncryptionService` | `Encryption/` |
| `Systems/SystemInfos` (`ISystemInfo`) | `Systems/` |
| `Permissions/PermissionMetadata.*` | `Permissions/PermissionMetadata/` |
| `Platform/…/ICacheService` | `Caching/` |
| `Platform/…/ICorrelationContext` | `Observability/` |
| `Integration/Notifications/{Services,Hubs,Providers,Options}` interfaces + models needed by them | `Notifications/` |
| `Integration/Storages/Services|Providers` (`IStorageService`, `IStorageProvider`) | `Storage/` |
| `Integration/Storages/Processing|Security` interfaces (`IImageProcessor`, scanner, enforcer, guard) | `Media/` |
| `Security/Authentication/**` interfaces (`ICurrentUser`, tokens, external) | `Authentication/` |
| `Security/Authorization/Permissions` (`IPermission*`) + attribute | `Permissions/` |
| `Foundations/Mappings` — wait: Mediators → | `Cqrs/` |
| `Foundations/Mappings/*` | `Mapping/` |
| `Governance/Validation/*` | `Validation/` |
| `Foundations/Models/Descriptors` | `Descriptors/` |
| `Foundations/Models/Parameters` | `Parameters/` |
| `Foundations/Models/Errors` (HTTP-agnostic) | `Errors/` (skip if redundant with Kernel `Error`) |
| `Foundations/Extensions/Exceptions` (non-HTTP) | `Exceptions/` |
| `Persistence/Specifications` pure model + parsers + `I*Parameters` | `Specifications/` |
| `Persistence/{Transactions,Initializers,Health,Seeders,Data wrapper}` interfaces | `Persistence/` (prefer Specs+UoW for module surface) |
| — **NEW** Spec + thin repo + UoW | `Specifications/IRepository.cs`, `ISpecificationExecutor.cs`, `IUnitOfWork.cs` |
| — **NEW** docs contracts | `Outbox/{IOutboxWriter,IEventDispatcher}`, `Stores/IStoreContext`, `Storage/IFileStorage`, `EventSourcing/IEventStore` |

**ASP.NET rewrites:** `WebApplicationBuilder.Add*` → `IServiceCollection.Add*`; HTTP handler / Result→HTTP / Carter → Hosting.

**Namespace:** `BuildingBlocks.Application.<Area>.*`.

**Packages:** Mediator.Abstractions (MediatR was replaced by Mediator — CPM pins 3.0.2), FluentValidation + DI extensions, Mapster (+ DI), Slugify if still needed.

**Dropped:** `ISharedMarker`; **no `IEmailSender`** (use `INotificationService`; email is a Notifications channel per ReSys.Shop `Shared.Integration`).

### Shared ← Governance conventions + future DTOs

| Old path | New path under `BuildingBlocks.Shared/` |
|---|---|
| `Governance/Conventions/*` JSON converters | `Conventions/` |
| — | DTO records as API areas appear (not in old Shared) |

**Namespace:** `BuildingBlocks.Shared` / `BuildingBlocks.Shared.Conventions`.

**Package:** none beyond BCL (converters are plain `System.Text.Json`).

### Infrastructure ← Persistence + Platform + Security + Integration

| Old path (abbrev.) | New folder under `BuildingBlocks.Infrastructure/` |
|---|---|
| `Shared.Persistence/Persistence/Data/*` | **do not port god DbContext**; extract base helpers → `Persistence/` |
| `Persistence/Interceptors/*` | `Persistence/Interceptors/` |
| `Persistence/Configurations/*` | `Persistence/Configurations/` |
| `Persistence/Transactions/*` | `Persistence/Transactions/` |
| `Persistence/Seeders/*`, `Initializers/*` | `Persistence/Seeders/`, `Persistence/Initializers/` |
| `Persistence/Health/*` | `Persistence/Health/` (+ map to Observability/Health) |
| `Persistence/Specifications/**/*.EfCore*` / apply extensions | `Querying/` |
| `Persistence/Identity/**` | `Identity/` |
| `Persistence/Authorization/Permissions/Store/*` | `Security/` or `Persistence/Security/` |
| `Persistence/Authentication/Tokens/Store/*` | `Security/` |
| — | **NEW** `Outbox/` (entity, writer, relay) |
| — | **NEW** `Auditing/` (AuditLog interceptor) |
| `Shared.Security/Security/Authentication/**` (JWT, contexts, external, guest) | `Security/Authentication/` |
| `Security/Authorization/**` except `Features/` | `Security/Authorization/` |
| `Security/Authorization/Features/*` | **DO NOT PORT** → modules |
| `Security/Identity/Domain/**` | `Identity/` |
| `Security/AntiForgery/**` | `Security/` (+ endpoints → Hosting later) |
| `Security/Cart/**` | **DO NOT PORT** → module |
| `Security/Cors`, `Headers`, `RateLimiting` | `Hosting/Pipeline/` (middleware) **or** keep settings in `Infrastructure/Security` and register from Hosting |
| `Shared.Platform/Observability/Correlation/**` | `Observability/Correlation/` |
| `Observability/Logging/*` | `Observability/Telemetry/` |
| `Observability/HealthChecks/*` | `Observability/Health/` |
| `Performance/Caching/**` | `Caching/` |
| `Shared.Integration/Operational/Http/**` | `Http/` |
| `Operational/Storages/**` except Processing/Security | `Storage/` |
| `Storages/Processing`, `Storages/Security` | `Media/` |
| `Storages/Security` scanners (nClam) | `Media/` |
| `Operational/Notifications/**` (full 73: hub, templates, store, email+SMS channels) | `Notifications/` + `Channels/Emails` under same tree (**full port — decision B**) |
| (optional mirror of email-only) | no separate `Email/` folder; email providers live under `Notifications/Channels/Emails` |
| `Operational/Backgrounds/**` | `BackgroundJobs/` |
| `Operational/Security/Encryption/AesEncryptionService.cs` | `Security/` |

**Namespaces:** `BuildingBlocks.Infrastructure.<Area>.*` (e.g. `BuildingBlocks.Infrastructure.Caching`).

### Hosting ← split composition / OpenAPI / pipeline

| Old path | New path under `BuildingBlocks.Hosting/` |
|---|---|
| `Governance/OpenApi/**` | `OpenApi/` |
| `Governance/Governance.Extension.cs` | split across `Composition/` + `OpenApi/` |
| `Foundations/Application/Endpoints/*` (Carter) | `Composition/` — **or drop Carter** (Minimal API only per ADR 0001) |
| `Foundations/Application/Extensions/Results/Result.Http.Extensions` | `Pipeline/` |
| `Foundations/Application/Exceptions/Handlers` (HTTP) | `Pipeline/` |
| `Security/Cors`, `Headers`, `RateLimiting` extensions | `Pipeline/` |
| `Security/Security.Extension.cs`, `Integration/Operational.Extension.cs`, `Platform/*Extension.cs` | `Composition/` (single `AddBuildingBlocks` / `UseBuildingBlocks` surface) |
| `AntiForgery/Endpoints` | `Pipeline/` or first module endpoints |

**Namespace:** `BuildingBlocks.Hosting.*`.

---

## Target csproj package references

Declare **only** after the pin exists in `Directory.Packages.props`.

| Project | Typical packages |
|---|---|
| Kernel | *(none)* |
| Core | *(none)* or BCL only |
| Application | `Mediator.Abstractions` (MediatR was replaced by Mediator), `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`, `Mapster`, `Mapster.DependencyInjection` |
| Shared | *(none)* |
| Infrastructure | `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.Extensions.Caching.Hybrid`, `Microsoft.Extensions.Caching.StackExchangeRedis`, `Microsoft.Extensions.Http.Resilience`, `Microsoft.Extensions.Resilience`, `Hangfire.AspNetCore`, `Hangfire.InMemory`, `AWSSDK.S3`, `SkiaSharp`, `SkiaSharp.NativeAssets.Linux`, `nClam`, `FluentEmail.*`, `OpenTelemetry.*`, **Serilog.*** (add), `AspNetCore.HealthChecks.*` |
| Hosting | `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore`, `Microsoft.AspNetCore.App` (FrameworkReference), optional `Carter` |

**Do not reference:** `Stripe.net` from Infrastructure (Payments module owns adapters). Old `Shared.Integration` had dead `Stripe.net` + `MessagePack` refs — do not copy.

---

## Phase Checklist

Work phase by phase. Each phase ends with a green build and a commit.

### Phase 0 — Scaffold (root solution)

- [x] Six csproj under `src/BuildingBlocks/` with `net10.0`, Nullable, ImplicitUsings.
- [x] ProjectReferences:
  - Core → Kernel
  - Application → Kernel, Core
  - Infrastructure → Kernel, Core, Application
  - Hosting → Kernel, Core, Application, Infrastructure
  - Shared → *(none)*
- [x] All six projects in root `resys-eshop.slnx` under `/src/BuildingBlocks/`.
- [x] Legacy pillars moved to `old_references/old_references.slnx` (not root).
- [x] Placeholder `Class1.cs` removed.
- [x] `dotnet restore resys-eshop.slnx && dotnet build resys-eshop.slnx` green.

```bash
# verify Phase 0
dotnet restore resys-eshop.slnx
dotnet build resys-eshop.slnx --no-restore
# optional: open legacy pillars read-only
dotnet build old_references/old_references.slnx   # known to fail under current CPM — expected until ported
```

### Phase 1 — Kernel (pure primitives only)

- [ ] Create folder layout per `03/02-08`: `Guard/`, `Results/`, `Extensions/`, `Time/` — **no** `Entities/`, `Events/`, `Ids/`, `Concerns/`.
- [ ] Copy Result/Error/ValueResult/PagedResult from Foundations; fix namespaces.
- [ ] Copy clock abstraction (`ISystemDateTime` → `IClock`).
- [ ] Add `IHasKey` / `KeyedRegistry` (toolkit; not domain).
- [ ] Tests: use existing `BuildingBlocks.Kernel.Tests` (already in solution); port matching tests from `Shared.UnitTests/Application/Models` and `Systems`.
- [ ] Assert (optional ArchTest): Kernel assembly has no `AggregateRoot`/`ValueObject`/`IDomainEvent`.
- [ ] `dotnet test` green; commit `feat(bb): port Kernel primitives`.

### Phase 2 — Core (shared domain + commerce)

- [ ] **Domain bases:** copy `Application/Domain/Models/*` → `Domain/Entities/`; `Application/Domain/Concerns/*` → `Domain/Concerns/`; add `Domain/Events/IDomainEvent` + `OccurredAtUtc`; add `Domain/Ids/` if needed.
- [ ] Money/Currency from Foundations; add Translatable stub if needed.
- [ ] Add ports: `IOutboxWriter`, `IDomainEventDispatcher` (today's dispatcher port — the old `IEventDispatcher` name no longer exists; it lives in `Application/Cqrs`, not `Application/Outbox`), `IFileStorage`, `IEmailSender`, `ICacheService`, `IStoreContext` (from `03/02-02` table — write if absent in old code).
- [ ] Permission metadata + identity ports.
- [ ] Tests: Money arithmetic, currency mismatch, domain-base invariants (entity id equality, VO equality).
- [ ] Commit `feat(bb): port Core domain and kernel`.

### Phase 3 — Application

- [ ] Copy Mediators → `Cqrs/`; wire optional `AddCqrs`.
- [ ] Copy validation registration → `Validation/`.
- [ ] Copy Mapster mappers → `Mapping/`.
- [ ] Split Specifications: pure model/parsers here; leave EF apply for Phase 5.
- [ ] Port `Shared.UnitTests/Application/**` and Governance validation tests.
- [ ] Commit `feat(bb): port Application`.

### Phase 4 — Shared

- [ ] JSON converters → `Conventions/`.
- [ ] Port `Shared.UnitTests/Governance/Conventions` tests.
- [ ] Commit `feat(bb): port Shared JSON conventions`.

### Phase 5 — Infrastructure (order inside the phase)

Build in dependency-friendly order; re-run build after each bullet.

1. [ ] **Persistence** — interceptors, converters, transactions, seeders, initializers (no god DbContext).  
2. [ ] **Querying** — EF apply for Specifications.  
3. [ ] **Outbox** — new: table model + writer + relay host logic (hosted service class lives here; registration in Hosting).  
4. [ ] **Auditing** — new: AuditLog + interceptor.  
5. [ ] **Security** — JWT, permission engine, AES impl; **exclude** Features catalogs and Cart.  
6. [ ] **Identity** — Identity entities + EF + seeders.  
7. [ ] **Observability** — correlation, logging, health; add **Serilog + OTel** packages (new code if absent).  
8. [ ] **Caching** — Hybrid/Redis/InMemory wrappers.  
9. [ ] **Http** — clients + `AddResilienceHandler`.  
10. [ ] **Storage / Media / Email / BackgroundJobs**.  
11. [ ] Port unit tests folder-by-folder from `Shared.UnitTests/{Operational,Security,Performance,Observability,Persistence}`.  
12. [ ] Commit per bullet: `feat(bb): port Infrastructure.<Area>`.

**Fixes while porting (do not blindly copy):**

| Old problem | Action |
|---|---|
| `Persistence.Extensions` registers Security services | register those from Hosting/Security composition only |
| Rate limit policies defined but never wired | call `AddRateLimiting` from `Hosting/Pipeline` |
| S3 provider is a stub | implement real S3 or keep Local only until Phase 5.10 |
| Notifications ~90 files | **port full tree** (hub + email + SMS) — decision B 2026-09-24 |
| Stripe/MessagePack package refs unused | drop |
| Facebook/Microsoft external logins | defer; Google only if needed |

### Phase 6 — Hosting

- [ ] `OpenApi/` from Governance OpenAPI + Scalar.
- [ ] `Pipeline/` — Result→HTTP, exception handler, CORS, headers, rate limit **wiring**, locale/store-resolution stubs.
- [ ] `Composition/` — one `AddBuildingBlocks(this IServiceCollection, IConfiguration)` and `UseBuildingBlocks(this WebApplication)`.
- [ ] Smoke: empty `Resys.Api` calls `AddBuildingBlocks` + `/health/live`.
- [ ] Commit `feat(bb): port Hosting composition`.

### Phase 7 — Architecture tests

- [ ] Create `tests/BuildingBlocks/BuildingBlocks.ArchTests` with `NetArchTest.Rules` (already pinned).
- [ ] Rules: Kernel has no deps; Core → Kernel only; no module → Infrastructure; no module → module; Infrastructure ↛ Hosting; only Api has `Program.cs`.
- [ ] Wire into CI test stage when `05/03-03` is updated.
- [ ] Commit `test(bb): architecture rules`.

### Phase 8 — Solution hygiene and remove old_references

- [ ] Confirm no project references `old_references/**`.
- [ ] Delete `old_references/old_references.slnx` when pillars are fully ported.
- [ ] Optionally delete `old_references/` or move to archive branch/tag `legacy-shared`.
- [ ] Update `03/02-01` tree, ADR 0002 note, naming table, build-run project table (docs pass with ADR 0005 acceptance).
- [ ] Final: `dotnet build && dotnet test` green; commit `chore: retire old_references`.

---

## Verification Commands

```bash
# from repo root
dotnet restore resys-eshop.slnx
dotnet build resys-eshop.slnx --no-restore
dotnet test resys-eshop.slnx --no-build          # once test projects exist

# ensure nothing new references legacy projects
rg -n "old_references|Shared\\.Foundations|Shared\\.Persistence" src/ tests/ --glob '!**/obj/**' --glob '!**/bin/**'

# ensure kit projects are in the solution
rg -n "BuildingBlocks" resys-eshop.slnx

# legacy pillars are isolated (optional read-only solution)
test -f old_references/old_references.slnx && echo "legacy slnx present"
# root solution must NOT list legacy projects
! rg -n "old_references/" resys-eshop.slnx && echo "root slnx clean"
```

Expected: empty `rg` hits under `src/`/`tests/` for legacy names; slnx lists all six kit projects (+ tests when added).

---

## Effort Guide (rough)

| Phase | Size | Notes |
|---|---|---|
| 0 Scaffold | XS | minutes |
| 1 Kernel | S | mostly move + rename |
| 2 Core | S | some **new** ports |
| 3 Application | M | specs split is the fiddly part |
| 4 Shared | XS | |
| 5 Infrastructure | **L** | largest; split into many commits |
| 6 Hosting | M | |
| 7 ArchTests | S | |
| 8 Retire old_references | XS | |

---

## Audit log — placement decisions (2026-09-24)

Record of every Target Map change locked in chat. Reason codes: **chat** = user decision; **ref** = ReSys.Shop `SHARED_KERNEL.md` / worktree; **docs** = architecture docs.

| Item | From (old map / docs) | To (locked) | Reason |
|---|---|---|---|
| Core purity | Core had ports + identity + permissions | Core = **Domain DDD + ES only** | chat — “make the core ddd/event sourcing focusing” |
| Technical contracts home | docs `Core/Ports` or draft `Application/Ports/` | **Application, one folder per area** (no `Ports/`) | chat — reject Ports dump; area folders |
| `IOutboxWriter`, `IEventDispatcher` | Core/Ports | `Application/Outbox/` | option 3 + area rule |
| `IFileStorage` | Core/Ports | `Application/Storage/` (with `IStorageService`) | area rule |
| `IStoreContext` | Core/Stores | `Application/Stores/` | option 3 |
| `ICacheService` | Core/Ports | `Application/Caching/` | from Platform interface file |
| `IEncryptionService` | Core/Ports | `Application/Encryption/` | from Foundations file |
| `IEventStore` | Core (draft) | `Application/EventSourcing/` | ES-3 + option 3 |
| Identity ports | Core/Ports or Identity | `Application/Identity/` | option 3 |
| `PermissionMetadata` | Core/Permissions | `Application/Permissions/PermissionMetadata/` | option 3 |
| Notification contracts | not in docs table / defer SMS | `Application/Notifications/` + full Infra tree | chat option B + promote contracts |
| `IEmailSender` | docs Core/Ports + `06-email-files` | **not ported** — use `INotificationService`; email = Notifications channel | ref — absent in old refs; chat — “Email pattern out” |
| Concrete Identity entities | ambiguous | **Infrastructure/Identity only**; modules use abstractions | chat — “using via abstractions” |
| Persistence module surface | raw `IDbContext` risk | **Specification + `IRepository` + `IUnitOfWork`** (Application), EF in Infra | chat — UoW/Spec/repo combo |
| Notifications Infra | “defer SMS” | **full port** 73 files | chat option B |
| Flat Core scaffold dirs | `Core/{Entities,ValueObjects,…}` | `Core/Domain/{Entities,ValueObjects,…}` | chat — Domain/ValueObjects |
| Carter endpoints | optional port | drop (ADR 0001 Minimal API) | docs default retained |
| Facebook/Microsoft external | port | defer; Google only if needed | migration fix table |
| Features/*, Cart, OrderFulfillmentState | port | **do not port** → modules | migration rules |
| God AppDbContext | port as-is | not public kit surface | ADR 0002 |

## Open Questions

1. Carter: port endpoints extension or stay Minimal-API only? (ADR 0001 says Minimal API — **default: no Carter** — locked drop.)
2. Spec DSL: Application model + Infrastructure apply (**locked**) with thin `IRepository`/`IUnitOfWork` on top.
3. When to delete `old_references` from disk vs keep for archaeology (Phase 8).
4. `IEmailSender` doc rows in `02-module-contracts` / `06-email-files` — rewrite to notification service in same docs pass as Audit log.

## Links

- [Building Blocks](../../03-architecture/02-components/08-building-blocks.md)
- [ADR 0005](../../00-governance/01-adr/05-building-blocks.md)
- [Module conventions](../02-conventions/02-module-conventions.md)
- [Build and run](../03-workflow/01-build-run.md)
