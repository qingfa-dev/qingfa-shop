# Roadmap → Repository Implementation Audit

**Audit date:** 2026-10-31
**Repository:** `QingFa.Shop`
**Scope:** the 14-week roadmap, its 11-branch taxonomy, and milestones A–H, mapped onto the code that actually exists in `src/`, `app/`, `tests/`, `scripts/`, `make/`, and `guide/`.
**Method:** every claim below is grounded in a file path that exists at audit time. No roadmap idea was credited to the repository unless production code (not a plan, not a changelog) implements it.

---

## 0. How to read this document

### Verdict legend

| Symbol | Verdict | Meaning |
|---|---|---|
| ✅ | **Implemented** | Production code plus at least one test proves the concept. |
| 🟡 | **Partial** | The mechanism exists but a named part of the concept is missing. |
| 🧱 | **Scaffold** | Structure, naming, or a placeholder exists; behavior does not. |
| ⛔ | **Absent** | Nothing implements the concept. |
| ➖ | **Not applicable yet** | Correctly deferred because a precondition (domain, deployment unit, measured need) does not exist. |

### Evidence rule used here

A concept counts as implemented only when all three hold:

1. A **contract** exists in `src/Application` (or `src/Core` for domain rules).
2. An **adapter** exists in `src/Infrastructure`.
3. A **test** exercises the contract or the adapter (`tests/Unit/**` or `tests/Integration/**`).

Anything that fails step 3 is marked 🟡 even if the code looks complete, because this repository already gates coverage at 90% line / 80% branch (`make coverage`).

### Renaming note (important)

The roadmap uses textbook vocabulary. This repository uses its own names. Section 13 is the authoritative translation table. From this point forward the **repository names** are the ones to use in code, issues, and ADRs: for example there is no "Transactional Outbox" class — there is `OutboxMessage` + `OutboxInterceptor` + `OutboxWorker`. There is no "BFF" — there is the YARP gateway plus two SPAs. Treat the roadmap term as the concept and the repository term as the implementation.

---

## 1. Executive summary

### 1.1 What this repository actually is

This is **not** a shop with a scaffold. It is the inverse: a **production-grade platform scaffold** with a small set of real modules.

- **Real, tested platform capabilities:** layered/ports-and-adapters architecture, `Result`-based functional core, CQRS pipelines, EF Core persistence + unit of work, transactional outbox, inbox/idempotency, event sourcing with snapshots and projections, Wolverine messaging (in-memory and RabbitMQ), identity/RBAC/permissions, notification channel adapters, storage adapters, observability, resilience.
- **Real, tested business modules:** `Todos` (the reference event-sourced domain) and `Payments` (the flagship module: event-sourced `Payment`/`Refund`, provider adapters, idempotency keys, webhooks, reconciliation, retention).
- **Scaffold only:** both Vue applications (storefront and admin) render a health chip and a navigation shell. There is no catalog, no cart, no checkout.
- **Absent entirely:** `Product`, `Order`, `Cart`, `Inventory`, an ML/image-similarity service, a separate worker deployment, full-text/vector search, and any contract or end-to-end test suite.

### 1.2 Headline status

| Roadmap unit | Verdict | One-line reason |
|---|---|---|
| Week 0 — Baseline | 🟡 | Deployment units and dependency rules are real; the context diagram, feature inventory, and risk register are not written down. |
| Week 1 — Design foundations | ✅ | Abstractions, DI, `Result`, and interchangeable providers are implemented and tested. |
| Week 2 — Architecture styles | 🟡 | Clean/hexagonal layering is real; architecture tests are mostly documentation stubs. |
| Week 3 — DDD | 🟡 | Strong in `Todos`/`Payments`; `Product` and `Order` do not exist. |
| Week 4 — Use cases | ✅ | MediatR commands/queries with validation, logging, exception and transaction behaviors. |
| Week 5 — Persistence/query | 🟡 | EF Core, UoW, concurrency, filtering/sorting/paging exist; keyset pagination and query benchmarks do not. |
| Week 6 — HTTP/identity/authz | 🟡 | Full identity + permission policies + edge rate limiting; no API versioning, contract tests, or authorization matrix document. |
| Week 7 — Vue feature architecture | 🧱 | Shell apps only, no feature module, no state library, no typed product client. |
| Week 8 — Testing strategy | 🟡 | Strong unit/integration suites; no contract, property-based, e2e, load, or chaos tests. |
| Week 9 — Domain events/outbox | ✅ | Outbox rows commit with state, leased claiming, retry/backoff, dead-lettering, inbox dedupe, atomicity tests. |
| Week 10 — Background jobs/notifications | 🟡 | Channels, providers, templates, Hangfire dispatch; no persisted delivery status, per-channel retry policy, or DLQ. |
| Week 11 — Resilience/observability | 🟡 | Standard resilience handler, OTel, health checks, outbox metrics; no dashboards, SLOs, queue-age metric, or failure-injection checklist. |
| Week 12 — CQRS/projections/search | 🟡 | Projections and read models are real; search is `ToLower().Contains`, no ranking, no vector search, no benchmark. |
| Week 13 — Sagas | ⛔ / ➖ | No process manager and no checkout workflow to orchestrate; payments are a single-database state machine with a reconciliation sweep. |
| Week 14 — Event sourcing | ✅ / 🟡 | Adopted in `Todos` and `Payments` with snapshots, checkpoints, rebuild; no ADR, upcasting sample, or operational runbook. |
| Phase VI — Production readiness | 🟡 | CI, migrations, health checks, secrets-as-config exist; no SLOs, load baselines, backup/restore proof, or runbook. |

### 1.3 Five strongest things in the repository

1. **Reliable messaging is genuinely solved, not referenced.** The outbox interceptor stages rows in the same `SaveChanges` as the aggregate (`src/Infrastructure/BuildingBlocks/Adapters/Persistence/EntityFramework/Outbox/Outbox.Interceptor.cs`), the worker leases and dispatches batches, and failures back off then dead-letter (`Outbox.Dispatcher.cs:133-165`). `tests/Integration/Infrastructure.Integration.Tests/BuildingBlocks/Messaging/Outbox.Atomicity.Integration.Test.cs` proves the rollback case.
2. **Consumer idempotency is real.** `EfCoreInboxStore.ExecuteOnceAsync` wraps the duplicate check, the handler, and the processed marker in one transaction, and treats a duplicate-key race as benign only when the marker is present (`src/Infrastructure/BuildingBlocks/Adapters/Persistence/EntityFramework/Inbox/Inbox.Store.cs:60-110`).
3. **Event sourcing is applied, not demonstrated.** `Todo` and `Payment` are event-sourced aggregates, with a PostgreSQL event store doing optimistic concurrency (`PostgresEventStore.cs:46-56`), interval snapshots, and a checkpointed projection runner.
4. **Payments is a complete reliability story.** Idempotency reservation + fingerprint conflict, provider registry with Fake/Stripe/PayPal, webhook dedupe store, reconciliation safety net, and retention worker — all under `src/Modules/Payments` equivalents in `src/Application/Modules/Payments` and `src/Infrastructure/Modules/Payments`.
5. **The conventions are enforced by tooling.** `make convention.check`, `make structure.check`, and `tests/Architecture.Tests` enforce monadic results, outcome shape, dotted filenames, and labeled comments. New code must fit these rules; the gaps below are therefore cheap to add consistently.

### 1.4 Five gaps that block the roadmap's intent

1. **There is no commerce domain.** `Grep "class Product|class Order|class Cart|Inventory"` over `src/` returns no business type. Milestones A–E cannot be completed as written.
2. **The frontend cannot exercise anything.** `app/Web.Storefront/src/App.vue` renders `Catalog` and `Cart` as inert `<span>` placeholders; the only API call is `useApiHealth`.
3. **Search is not search.** `QueryTranslation.EfCore.Search.cs:33-35` lowercases and calls `string.Contains`; `EfCoreQueryCapabilities.SearchModes` explicitly reports that full-text/fuzzy search is not enabled. The ML/image-search service does not exist.
4. **Notification delivery is fire-and-forget.** `NotificationService.SendAsync` enqueues a Hangfire job; there is no delivery-status table, no provider-level retry/backoff policy, no dead-letter store, and no idempotency key per notification.
5. **Architecture tests do not enforce architecture.** `LayerArchitecture.Tests.cs` and `VerticalSliceArchitecture.Tests.cs` contain `true.Should().BeTrue(because: "... documented ... pending")`. Only the building-block boundary, naming, command-marker, inline-error, and comment-label tests are real.

---

## 2. Week 00 — System context as implemented

### 2.1 Actual context diagram

Solid boxes are deployed/real. Dashed boxes are absent. Labels in parentheses are the repository's names.

```text
                         ┌──────────────────────────────┐
                         │  Browser                     │
                         └──────────────┬───────────────┘
                                        │ same origin
                    ┌───────────────────▼────────────────────┐
                    │ Gateway (app/Gateway, YARP)            │
                    │  /            → shopfront              │
                    │  /admin       → admin                  │
                    │  /api/time    → api (anonymous)        │
                    │  /api/...     → api (JWT policy)       │
                    │  per-IP fixed-window rate limiter      │
                    └───────┬───────────────────────┬────────┘
                            │                       │
              ┌─────────────▼──────────┐   ┌────────▼─────────────┐
              │ Storefront SPA         │   │ Admin SPA            │
              │ (Web.Storefront)       │   │ (Web.Admin)          │
              │ health chip only       │   │ health chip only     │
              └────────────────────────┘   └──────────────────────┘
                            │
                    ┌───────▼──────────────────────────────────┐
                    │ Shop API (app/Web.Api, composition root) │
                    │  Carter endpoints: Identity, Todos,      │
                    │  Payments                                │
                    │  Hosted services: OutboxWorker,          │
                    │  ProjectionWorker, PaymentReconciliation,│
                    │  PaymentWebhookRetention, Hangfire       │
                    └───┬───────────┬──────────────┬───────────┘
                        │           │              │
              ┌─────────▼──┐  ┌─────▼─────┐  ┌─────▼──────────────┐
              │ PostgreSQL │  │  Redis    │  │ RabbitMQ           │
              │ (db)       │  │ (cache,   │  │ (Wolverine,        │
              │ state +    │  │  tokens,  │  │  publisher         │
              │ outbox +   │  │  perms)   │  │  confirms)         │
              │ inbox +    │  └───────────┘  └────────────────────┘
              │ event store│
              └────────────┘
                    ┌──────────────────────────────┐
                    │ ML / image-search service    │  ← ⛔ absent
                    └──────────────────────────────┘
```

**Correction to the roadmap diagram:** the roadmap assumes `Product`, `Order`, and an ML service. The repository has `Todo*` and `Payment*` instead. Any exercise that says "trace one product read and one order creation" cannot be completed in this codebase today; the equivalent trace is **one todo read** (event-sourced) and **one payment creation** (idempotent command → event store → outbox → projection).

### 2.2 Deployment units vs. project organization

| Unit | Exists as a deployable? | Evidence |
|---|---|---|
| Storefront SPA | ✅ | `app/Web.Storefront` (Vite, built artifact under `dist/`) |
| Admin SPA | ✅ | `app/Web.Admin` (served under `/admin`) |
| Gateway | ✅ | `app/Gateway` (separate process, YARP route table in `Gateway.Router.cs`) |
| Shop API | ✅ | `app/Web.Api` |
| Worker | 🟡 | Only as `BackgroundService`s inside the API process; no separate worker executable |
| ML service | ⛔ | No project, no integration client, no HTTP boundary |
| PostgreSQL | ✅ | `app/AppHost/AppHost.cs:7-9`, `compose.yaml` |
| Redis | ✅ | `AppHost.cs:11` |
| RabbitMQ | ✅ | `AppHost.cs:15-17`, `Messaging__Transport=RabbitMq` injected |

This matters for Week 2's "which components are already deployed separately versus merely organized as projects": **only Gateway, API, and the two SPAs are separate processes. The outbox worker, projection worker, reconciliation worker, and notification jobs all share the API's process and lifetime.**

### 2.3 Feature inventory (business surface)

| Surface | Status | Endpoints / entry points |
|---|---|---|
| Identity: register/login/refresh/logout/2FA/external | ✅ | `app/Web.Api/Endpoints/Identity/*.Endpoint.cs` |
| Users/roles/permissions administration | ✅ | `Identity.User.Endpoint.cs`, `Identity.Role.Endpoint.cs`, `Identity.Permission.Endpoint.cs` |
| Todos (event-sourced) | ✅ | `Todo.Endpoint.cs`, `TodoList.Endpoint.cs`, `TodoCategory.Endpoint.cs`, `TodoReminder.Endpoint.cs` |
| Payments (customer) | ✅ | `PaymentEndpoint.cs` (`/api/payments`) |
| Payments (admin) | ✅ | `PaymentAdminEndpoint.cs` (`/api/admin/payments`, permission-gated) |
| Payments (webhook) | ✅ | `PaymentWebhookEndpoint.cs` (anonymous at the edge, signature-verified in the module) |
| Catalog / products | ⛔ | — |
| Cart / checkout / orders | ⛔ | — |
| Inventory | ⛔ | — |
| Image search / recommendations | ⛔ | — |

### 2.4 Dependency map and where it is enforced

```text
Core (no project refs)
  ▲
Application (Core)
  ▲
Infrastructure (Application + ServiceDefaults)
  ▲
Web.Api (composition root: all layers)
ServiceDefaults (horizontal, no project refs)
Gateway (ServiceDefaults only)
AppHost (orchestration only)
```

- Project references are real in each `.csproj`; this is the primary enforcement.
- BuildingBlocks → Modules is enforced by reflection in `tests/Architecture.Tests/BuildingBlockBoundary.Tests.cs`.
- Module → module isolation is **not** enforced: `VerticalSliceArchitecture.Tests.cs` only documents the rule.
- The `Modules/{PermissionCatalog,Persistence}` folders are composition artifacts, not business modules (`AGENTS.md`).

### 2.5 Technical risk register (the missing Week-0 deliverable)

| # | Risk | Severity | Evidence | Mitigation direction |
|---|---|---|---|---|
| R1 | No commerce domain, so the roadmap's exercises have no target | High | no `Product`/`Order`/`Cart` types | Start milestone A (section 7) |
| R2 | Frontends are shells; UI risk is untested until features land | High | `App.vue` placeholders, 6 web specs total | Build feature module with tests first |
| R3 | Search is unindexed `LIKE '%x%'`; degrades linearly | High | `QueryTranslation.EfCore.Search.cs` | Add FTS adapter + capability gate |
| R4 | Notifications can be lost silently after enqueue | High | `NotificationService.SendAsync` → Hangfire only | Persist delivery status + idempotency |
| R5 | Architecture rules are prose, not tests | Medium | `LayerArchitecture.Tests.cs:67` | Adopt NetArchTest (or reflection equivalents) |
| R6 | No API versioning/contract tests; SPA and API can drift | Medium | no `Asp.Versioning`, no Pact | Add versioning + consumer contract tests |
| R7 | Worker shares API process; a worker crash takes the API down | Medium | hosted services in `Web.Api` | Extract worker executable when load justifies it |
| R8 | No load/performance baseline for any endpoint | Medium | no k6/NBomber scripts | Add baseline for catalog/payment/search |
| R9 | Event-sourcing operate burden unowned (replay, snapshots, checkpoints) | Medium | `Projection.Worker`, `RebuildProjection.Service` | Write runbook + replay drill |
| R10 | Test suite skips integration tests without Docker/node | Low | documented in `AGENTS.md` | Require Docker in CI for main/nightly |

### 2.6 Week-0 exit criterion

> "You can explain how one product read and one order creation travel from the browser to the database and back."

**Not met as written** (no product/order). **Met for the two existing verticals**:

- Todo read: `Todo.Endpoint` → `SearchTodos` query → `TodoReadModel` (projection) → `ISearchTodos` handler → EF Core → JSON.
- Payment creation: `PaymentEndpoint.CreateAsync` → `CreatePayment.UseCase` → `Payment` aggregate (event store) + idempotency reservation → outbox row → `OutboxWorker` → Wolverine → projection → `PaymentReadModel`.

---

## 3. Phase I — Foundations and architectural boundaries

### Week 1 — Software design foundations ✅

| Concept | Where it lives |
|---|---|
| Abstraction / dependency inversion | `Application/BuildingBlocks/Ports/**` (storage, notifications), `Application/Modules/Payments/Ports/IPaymentGateway.Interface.cs` |
| Composition root | `app/Web.Api/Composition/Api.Composition.cs`, `src/Infrastructure/Infrastructure.Composition.cs` |
| Strategy + Adapter | `IPaymentGateway` with `FakePaymentGateway`, `StripePaymentGateway`, `PayPalPaymentGateway`; `IStorageProvider` with `Local.StorageProvider.Service.cs`, `S3.StorageProvider.Service.cs`; `INotificationProvider` with SendGrid/SMTP/Sinch/Logging |
| Factory | `Payment.Factory.cs`, `Todo.Factory.cs`, `QueryMap.Registry.cs` |
| Decorator / pipeline | `Application/BuildingBlocks/UseCases/Mediation/Behaviors/**` (validation → logging → transaction → exception) |
| Immutability / value objects | `Core/BuildingBlocks/Domain/ValueObjects/**` (`Money`, `Quantity`, `Percentage`, `DateRange`, `EmailAddress`, `Url`, …) |
| Refactoring / code smells | Enforced culturally by analyzers, `Directory.Build.props`, and the convention gate |

**Exit criterion:** met. The cleanest example is `IPaymentGateway`: the interface isolates the provider SDK (Stripe/PayPal) and the state machine that must not change (`Core/Modules/Payments/Intents/Payment.cs`). The abstraction would stop being worth it if there were exactly one provider with no planned second, or if every provider needed a genuinely different domain model rather than a wire mapping.

### Week 2 — Architecture styles and dependency rules 🟡

| Style | Status | Evidence |
|---|---|---|
| Layered | ✅ | `src/Core` ← `src/Application` ← `src/Infrastructure` ← `app/Web.Api` |
| Clean / Onion | ✅ | Domain has zero project refs (`src/Core/Core.csproj`); EF Core only in Infrastructure |
| Hexagonal / Ports and Adapters | ✅ | `BuildingBlocks/Ports` contracts + `Adapters` implementations |
| Modular monolith | ✅ | `Modules/*` per feature, one deployable API |
| API gateway | ✅ | `app/Gateway` YARP, ordered routes, JWT edge policy, rate limit |
| BFF | ⛔ | No backend-for-frontend aggregation; the SPAs call the API through the gateway directly |
| Microservices | ➖ | Deliberately not adopted; `AGENTS.md` and `AppHost.cs` describe one API |

**Gap:** the dependency rule is guarded by compile-time references and one reflection test, but `LayerArchitecture.Tests.cs` and `VerticalSliceArchitecture.Tests.cs` are placeholders. `AGENTS.md` acknowledges this ("broader NetArchTest rules are still pending, so don't rely on it alone").

**Suggestion:** replace the two placeholder tests with reflection-based rules (the repo already does this in `BuildingBlockBoundary.Tests.cs`, so the pattern exists): assert `Application` types do not reference `Infrastructure`/`Microsoft.EntityFrameworkCore`, and that `Core.Modules.X` types are not referenced by `Core.Modules.Y`. Do this **before** adding `Catalog`/`Orders`, so the new modules inherit enforcement.

**Exit criterion:** met for existing use cases; the "why each dependency points inward" explanation is demonstrable from the `.csproj` graph and `Ports/`.

---

## 4. Phase II — Business modeling and the application layer

### Week 3 — Domain-Driven Design fundamentals 🟡

| Concept | Implementation | Location |
|---|---|---|
| Entity | `Todo`, `TodoList`, `TodoCategory`, `TodoReminder`, `Payment`, `Refund`, `Dispute` | `src/Core/Modules/**` |
| Value object | `Money`, `Currency`, `Percentage`, `Quantity`, `Weight`, `Dimensions`, `EmailAddress`, `PhoneNumber`, `Url`, `Address`, `PostalCode`, `Language`, `Locale`, `DateRange`, `TimeRange` | `src/Core/BuildingBlocks/Domain/ValueObjects/**` |
| Aggregate / root | `EventSourcedAggregate<TId>`, `AggregateRoot`, `Entity` | `src/Core/BuildingBlocks/Domain/{Aggregates,Entities,EventSourcing}/**` |
| Domain events | `IDomainEvent`, `DomainEvent`, per-aggregate `*.Event.cs` | `src/Core/BuildingBlocks/Domain/Events/**` |
| Domain service / policy | `DomainPolicy`, `PaymentPolicy` | `src/Core/BuildingBlocks/Domain/Policies/**`, `src/Infrastructure/Modules/Payments/PaymentPolicy.cs` |
| Specification | `Specification` | `src/Core/BuildingBlocks/Domain/Specifications/**` |
| Factory | `Payment.Factory.cs`, `Todo.Factory.cs` | module roots |
| Invariants | `*.Validator.cs`, `*.Outcome.cs`, guarded state transitions | beside each aggregate |
| Bounded contexts | Folder-level only (`Todos`, `Payments`, `Identity`), no context map | — |

**What is genuinely strong:** state transitions live in the aggregate, not the handler. `PaymentStatus` (`Pending → Processing → Authorized → Captured → PartiallyRefunded/Refunded`, plus `Failed/Cancelled/Expired`) is a closed set, and the projection only mirrors events the aggregate already accepted.

**What is missing relative to the roadmap:** `Product` (price/publication rules) and `Order` (line items, valid transitions) do not exist. The repository therefore cannot demonstrate "which rules belong inside the domain vs. the use case" for the roadmap's actual exercises; `Todo` and `Payment` are the stand-ins.

**Suggestion:** implement `Catalog` and `Orders` as new modules following the `Payments` shape exactly:

- `src/Core/Modules/Catalog/Products/Product.cs`, `Product.Event.cs`, `Product.Factory.cs`, `Product.Validator.cs`, `Product.Outcome.cs`, `Product.Constant.cs`
- `src/Core/Modules/Orders/Orders/Order.cs`, `OrderLine.cs`, `OrderStatus.cs`, `*.Event.cs`
- Use `Money` (already exists) rather than a new price type; use `Publishable`/`Activatable` concerns (already exist in `Core/BuildingBlocks/Modeling/Concerns/Lifecycle`).

**Exit criterion:** partially met. You can explain domain-vs-use-case-vs-infrastructure for a payment, but not for a product/order because they do not exist.

### Week 4 — Application use cases ✅

| Concept | Implementation | Location |
|---|---|---|
| Command / query separation | `ICommand<T>`, `IQuery<T>` | `src/Application/BuildingBlocks/UseCases/Mediation/{Commands,Queries}/` |
| Handler | MediatR handlers co-located with the use case | `src/Application/Modules/**/Commands/**`, `.../Queries/**` |
| Mediator | `Mediator.Composition.cs` | `src/Application/BuildingBlocks/UseCases/Mediation/` |
| Request pipeline | Validation → Logging → Exception → Transaction | `.../Mediation/Behaviors/**` |
| Validation | FluentValidation + `ValidationBehavior` + `Error.Mapper` | `.../Validation/FluentValidation/**` |
| Result / Either | `Result`, `Result<T>`, `Error` with severity and HTTP status | `src/Core/BuildingBlocks/Functional/**` |
| Authorization | Endpoint attributes + permission requirement handler | `Application/BuildingBlocks/Security/Authorization/**`; applied in `app/Web.Api/Endpoints/**` |
| Transaction boundary | `TransactionBehavior` wraps commands only, queries pass through | `.../Behaviors/Transactions/Transaction.Behavior.cs:32-60` |
| HTTP isolation | Endpoints translate body → use-case request and `Result` → `ApiResult` | `app/Web.Api/Endpoints/**`, `Infrastructure/.../Serialization/ApiResult*` |

**Distinguishing the four failure kinds (the exit criterion):**

| Kind | Mechanism | Example |
|---|---|---|
| Validation failure | FluentValidation → `ValidationBehavior` → `Result` with `Validation` errors | `Todo.Validation.cs` + `Validation.Behavior.cs` |
| Domain failure | Aggregate/validator returns `Outcome.Failure.*` | `PaymentsOutcome.Failure.IdempotencyConflict()` |
| Authorization denial | `HasPermissionAttribute` → `PermissionRequirementHandler` → 403 | `PaymentAdminEndpoint.cs` |
| Unexpected technical exception | `ExceptionBehavior` → `Error.InternalServerError`; `ProblemDetails` at the edge | `Exception.Behavior.cs`, `Api.Composition.cs:26,54` |

**Gap:** there is no *resource-based* authorization abstraction in the handler pipeline. Ownership is checked imperatively inside queries, e.g. `GetPayment.cs:64` (`query.CustomerId is null || row.CustomerId == query.CustomerId`). That works, but it is not reusable, and it is easy to forget in a new query.

**Suggestion:** add `IResourceAuthorization<TResource>` in `Application/BuildingBlocks/Security/Authorization/` and a pipeline behavior that runs it for commands/queries that opt in. Then cover it with an authorization matrix test (see Week 6).

### Week 5 — Persistence and query design 🟡

| Concept | Implementation | Location |
|---|---|---|
| DbContext | `ApplicationDbContext` + design-time factory | `Infrastructure/.../Contexts/ApplicationDbContext.cs` |
| Change tracking | EF Core defaults; `AsNoTracking` in read paths | repositories/read stores |
| Unit of Work | `IUnitOfWork`, `EfCoreUnitOfWork`, `EfCoreTransaction` | `Application/.../UnitOfWorks/`, `Infrastructure/.../UnitOfWorks/` |
| Optimistic concurrency | `Versionable` concern + `Versionable.Interceptor.cs` + event-store expected-version check | `Infrastructure/.../Interceptors/Versionable.Interceptor.cs`, `PostgresEventStore.cs:46-56` |
| Repository | `IRepository<T>` family + `EfCoreRepository.*` partials | `Application/.../Repositories/`, `Infrastructure/.../Repositories/` |
| Query DSL | Filter/order/page/select/group/aggregate parser + EF translator | `Application/BuildingBlocks/Querying/**`, `Infrastructure/.../QueryTranslation/**` |
| Filtering contract | `Filter.Base.cs`, `FilterCondition`, `FilterGroup` | `Application/BuildingBlocks/Querying/Filtering/` |
| Sorting | `SortField`, `SortDirection`, `SortNulls` | `.../Querying/Sorting/` |
| Pagination | `PageRequest`, `QueryPage<T>` (offset only) | `.../Querying/Pagination/` |
| Projections | `SelectField`, `TabularResult` + EF projection translator | `.../Querying/Projection/**`, `QueryTranslation.EfCore.Projection.cs` |
| Soft delete / audit | `SoftDeletable`, `Creatable`, `Modifiable`, `Lifecycle.Interceptor` | `Core/.../Modeling/Concerns/**`, `Infrastructure/.../Interceptors/Lifecycle.Interceptor.cs` |
| Caching | `ICacheService` with memory/hybrid/distributed implementations | `Application/.../Caching/`, `Infrastructure/.../Caching/**` |

**Gaps against the roadmap:**

1. **Keyset pagination absent.** `PageRequest` is offset-only (`PageRequest.Model.cs`). The roadmap explicitly asks to compare offset and keyset.
2. **No query benchmark.** Nothing measures the DSL translation or the search path.
3. **No documented transaction boundary.** The behavior is correct (`TransactionBehavior`), but there is no ADR/diagram stating where transactions start and end per module.
4. **Caching is infrastructure-only.** `ICacheService` is used by todo integration handlers and the permission cache; no catalog/query cache exists because there is no catalog.

**Suggestion:** add `CursorPageRequest`/`CursorPage<T>` beside `PageRequest` and a benchmark project (or `scripts/perf/`) comparing both on a seeded table. Document transaction boundaries in the new architecture docs folder.

**Exit criterion:** partially met. You can explain when native EF Core is enough and why the repository abstraction exists, but you cannot demonstrate a keyset-vs-offset decision without building it.

---

## 5. Phase III — API contracts, security, and frontend structure

### Week 6 — HTTP, identity, and authorization 🟡

| Concept | Status | Location |
|---|---|---|
| REST endpoints | ✅ | Carter modules under `app/Web.Api/Endpoints/**` |
| DTOs / contracts | ✅ | `Endpoints/**/Models/*.Model.cs`, `Application/Modules/**/Models/*` |
| API versioning | ⛔ | No `Asp.Versioning`, no `/v1` segment |
| Authentication (JWT) | ✅ | `Infrastructure/.../Security/Authentication/**`, `JwtToken.Service.cs` |
| Refresh tokens / rotation / blacklist | ✅ | `RefreshToken.Service.cs`, `TokenRotation.Service.cs`, `TokenBlacklist.Service.cs` |
| Claims / permission catalog | ✅ | `Permission.Metadata.cs`, `PermissionRegistry`, `ApplicationPermissionCatalog.cs`, `*PermissionCatalog.cs` per module |
| RBAC | ✅ | Users ↔ roles ↔ permission claims; `RoleManagementService`, `UserAccountService` |
| Resource-based authorization | 🟡 | Imperative ownership filter in `GetPayment.cs:64`; no reusable abstraction |
| OAuth / OIDC | ✅ | `ExternalAuthentication.Service.cs`, `External.Option.cs` (Google/Microsoft/OIDC, disabled by default) |
| CORS | ➖ / ⛔ | Same-origin through the gateway, so not needed today; no policy configured if a second origin appears |
| CSRF | 🟡 | `StorageAntiForgeryGuard.Service.cs` protects storage uploads only; cookie-based auth is not used elsewhere |
| Rate limiting | ✅ (edge only) | `app/Gateway/Program.cs:38-54` — per-IP fixed window, 100 req / 10 s |
| Gateway vs API security as distinct layers | ✅ | Gateway validates JWT and enforces route policy (`Gateway.Router.cs`); API enforces permissions per endpoint (`HasPermission`) |

**Gaps:** no authorization matrix document, no endpoint contract tests (only behavior tests in `tests/Integration/Api.Integration.Tests/**`), no API versioning.

**Suggestion:** produce `docs/architecture/authorization-matrix.md` with rows = endpoint, columns = anonymous / authenticated / permission / resource-owner rule. Then add a test that enumerates Carter endpoints and asserts each has an explicit authorization decision — the same "fail the build when someone forgets" pattern the repo already uses for conventions.

**Exit criterion:** met in substance (identity established at the API, permissions enforced per endpoint, ownership checked in payment reads), but the evidence is spread across code rather than a matrix.

### Week 7 — Vue feature architecture 🧱

| Concept | Status | Evidence |
|---|---|---|
| Feature-based organization | ⛔ | `src/views`, `src/composables`, `src/api` only; no feature folders |
| Composition API | ✅ | `script setup` in `App.vue`, `HomeView.vue` |
| Composables | 🟡 | One real composable: `useApiHealth.ts` |
| Pinia state | ⛔ | No Pinia dependency (confirmed by `AGENTS.md`: "there is no Pinia store") |
| Typed API client | 🧱 | `src/api/health.ts` only |
| Route guards | ⛔ | Router has routes, no guards |
| Form schemas / validation | ⛔ | No forms |
| Loading/error states | 🟡 | `useApiHealth` exposes a label |
| Caching / optimistic updates | ⛔ | — |
| Component tests | 🟡 | 3 unit + 3 integration specs total across both apps |

**Suggestion (milestone A/B frontend):**

```text
app/Web.Storefront/src/features/catalog/
  components/ProductCard.vue
  composables/useProductSearch.ts
  api/products.ts            # typed client, no fetch in components
  views/CatalogView.vue
app/Web.Admin/src/features/catalog/
  views/ProductEditorView.vue
  composables/useProductForm.ts
```

Keep server-owned state in the typed client/composable (as the repo already does for health) and add Pinia only when cross-view client state appears; the roadmap's "where should a state value live" answer here is: server state → query composable; UI state → local component; shared client state → Pinia.

**Exit criterion:** not met. No feature module, no typed product client, no end-to-end path.

### Week 8 — Testing strategy and architectural verification 🟡

| Test kind | Status | Evidence |
|---|---|---|
| Unit (domain) | ✅ | `tests/Unit/Core.Unit.Tests/**` (value objects, aggregates, results, events) |
| Unit (application) | ✅ | `tests/Unit/Application.Unit.Tests/**` (handlers, behaviors, validators) |
| Unit (infrastructure) | ✅ | `tests/Unit/Infrastructure.Unit.Tests/**` (adapters, outbox, inbox, projections, payments) |
| Integration (PostgreSQL/Redis/RabbitMQ) | ✅ | `tests/Integration/Infrastructure.Integration.Tests/**` (Testcontainers fixtures) |
| API integration | ✅ | `tests/Integration/Api.Integration.Tests/**` (host factory in `tests/Testing/Fixtures/ShopApi.Factory.cs`) |
| Architecture | 🟡 | real: building-block boundary, naming, command marker, inline errors, comment labels; stubs: layer + vertical slice |
| Contract | ⛔ | no Pact/OpenAPI consumer tests |
| End-to-end | ⛔ | Playwright is not installed; the target skips cleanly |
| Property-based | ⛔ | no FsCheck/PropertyBased |
| Load / performance | ⛔ | no k6/NBomber |
| Chaos | ⛔ | no failure injection harness |

**What each test proves (and does not):**

- `Outbox.Atomicity.Integration.Test.cs` proves a failed save leaves no outbox row. It does **not** prove the broker received the message; that requires the RabbitMQ suite.
- `Inbox.Store.Integration.Test.cs` proves duplicate delivery is a no-op inside one process. It does **not** prove a second process cannot process the same message concurrently (the unique index is the real guard; assert it under concurrency).
- `Api.Integration.Tests` prove routing/contract shape through the real pipeline. They do **not** prove cross-service delivery.

**Exit criterion:** partially met. The repository knows what each suite proves; it cannot yet prove contract compatibility or a browser-level critical path. `AGENTS.md` already warns that integration tests skip without Docker/node — check skip counts before claiming infrastructure coverage.

---

## 6. Phase IV — Events, asynchronous work, and reliability

### Week 9 — Domain events and transactional outbox ✅

| Concept | Implementation | Location |
|---|---|---|
| In-process domain events | `IDomainEvent`, `IDomainEventPublisher` | `Core/BuildingBlocks/Domain/Events/**`, `Application/.../Events/` |
| Integration events | `IntegrationEvent.Base.cs`, `IIntegrationEvent` | `Application/BuildingBlocks/Messaging/**` |
| Outbox table | `OutboxMessage` + EF configuration | `Application/.../Outbox/OutboxMessage.Model.cs`, `Infrastructure/.../Outbox/OutboxMessage.Configuration.cs` |
| Atomic staging | `OutboxInterceptor` writes rows in `SavingChanges`, clears only after `SavedChanges` | `Outbox.Interceptor.cs:51-104` |
| Publisher / dispatcher | `OutboxDispatcher` claims, publishes, marks processed/failed | `Outbox.Dispatcher.cs` |
| Leasing | `ClaimAsync(batch, now, leaseSeconds)`; nil lease = another worker owns it | `Outbox.Dispatcher.cs:52-73`, `Outbox.Store.cs` |
| Retry / backoff / dead-letter | attempts + exponential delay + `DeadLetterAsync` | `Outbox.Dispatcher.cs:133-165` |
| Outbox health | `OutboxHealthCheck` (max age) | `Outbox.Health.Monitor.cs`, `Outbox.Composition.cs:52-53` |
| Inbox / idempotency | `EfCoreInboxStore.ExecuteOnceAsync` + `InboxMessage` unique key | `Inbox.Store.cs`, `InboxMessage.Configuration.cs` |
| Message contracts / versioning | `EventTypeRegistry`, `IDomainEvent.EventVersion`, serializer | `Infrastructure/.../Messaging/Serialization/**` |
| Correlation / causation | outbox columns + `CorrelationMiddleware` | `OutboxMessage.Model.cs`, `Infrastructure/.../Observability/Correlation/**` |
| Broker transport | Wolverine, `RabbitMq` with publisher confirms | `Messaging.Composition.cs:193-205` |

**Design decision worth documenting (currently only a comment):** the dispatcher publishes **domain** events as well as integration events *after* commit (`Outbox.Dispatcher.cs:96-105`). That is a deliberate "no in-process dispatch during save" choice, and it means domain handlers are not transactional with the aggregate. Write it down as an ADR; it is the kind of thing a new contributor will otherwise "fix" incorrectly.

**Exit criterion:** met. The crash-before/after-publish story is demonstrable: before publish → row remains leased/available and is retried; after publish but before mark → row is published again, and the inbox makes the consumer's effect idempotent.

### Week 10 — Background jobs and notification delivery 🟡

| Concept | Status | Location |
|---|---|---|
| Worker services | ✅ (in-process) | `OutboxWorker`, `ProjectionWorker`, `PaymentReconciliationWorker`, `PaymentWebhookRetentionWorker` |
| Queue-based load leveling | ✅ | Wolverine + RabbitMQ; Hangfire queues for notifications |
| Competing consumers | ✅ | `MaxConcurrency`, `ConsumerDispatchConcurrency` (`Messaging.Composition.cs:201-204`) |
| Scheduled work | ✅ | `PeriodicTimer` workers; Hangfire server |
| Template rendering | ✅ | `NotificationStore.Template.cs`, `Notification.Message.Builder.cs`, `MapContent()` |
| Channel routing | ✅ | `NotificationHub` filters providers by channel and priority (`Notification.Hub.Service.cs:20-33`) |
| Provider adapters | ✅ | `SendGrid.Provider.cs`, `Smtp.Provider.cs`, `Sinch.Provider.cs`, `Logging.Provider.cs` |
| Retry with backoff | 🟡 | Hangfire's built-in retries only; no module-level policy, no per-channel backoff |
| Dead-letter handling | ⛔ | No notification DLQ table/queue inspection; outbox has DLQ, notifications do not |
| Delivery status persistence | ⛔ | No `NotificationDelivery` entity or status store |
| Idempotency | ⛔ | No dedupe key on notification sends; a redelivered integration event can enqueue twice |
| Separate notification module | 🟡 | Lives in `BuildingBlocks/Ports/Notifications` + `Adapters/Notifications`, not `Modules/Notifications` |

**Exit criterion:** **not met.** An unavailable provider cannot block the request indefinitely (Hangfire hands off asynchronously — good), but duplicate messages can create duplicate work because there is no notification idempotency key and no delivery record.

**Suggestion:** create `src/Core/Modules/Notifications/Deliveries/NotificationDelivery.cs` (status: Pending/Sent/Failed/DeadLettered, attempts, lastError, channel, provider, dedupeKey) and a `INotificationDeliveryStore` in Application with an EF implementation + unique index `(DedupeKey, Channel)`. Route `NotificationService.SendInternalAsync` through it. Then add:

- Hangfire `AutomaticRetry` policy per queue (`critical`/`default`/`bulk` already exist as priorities).
- A dead-letter sweep worker + health check ("notifications dead-lettered in last hour").
- A duplicate-delivery integration test mirroring `Inbox.Store.Integration.Test.cs`.

### Week 11 — Resilience and observability 🟡

| Concern | Status | Implementation |
|---|---|---|
| Timeouts / retries / circuit breaker | ✅ | `AddStandardResilienceHandler()` global (`ServiceDefaults.Extension.cs:32-39`); custom pipeline in `Http.Extensions.cs` |
| Bulkhead / concurrency limits | ✅ | Wolverine `MaxConcurrency`; RabbitMQ consumer dispatch concurrency |
| Backpressure | 🟡 | Prefetch + concurrency cap; no explicit queue-length shedding |
| Rate limiting | ✅ (edge) | Gateway policy |
| Structured logs | ✅ | `[LoggerMessage]` partial loggers throughout; OTel logging |
| Metrics | 🟡 | ASP.NET/HttpClient/runtime instrumentation + `PaymentMetrics`; no queue-age, outbox-lag-by-age, notification metrics |
| Distributed tracing | ✅ | OTel tracing with health-check filtering (`ServiceDefaults.Extension.cs:65-77`) |
| Health checks | ✅ | self, PostgreSQL, Redis, RabbitMQ, outbox (`HealthChecks.Extension.cs:19-29`, `Outbox.Composition.cs:52-53`); two endpoint sets exist — see Phase VI risk |
| Graceful shutdown | ✅ (framework default) | `BackgroundService` stopping tokens handled in every worker |
| Failure-injection checklist | ⛔ | none |
| Dashboards | ⛔ | none committed (OTLP exporter only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set) |

**Gaps:** the roadmap's "measure queue age, processing latency, retries, failure rate, dead-letter count" is only half-implemented. Outbox retries/dead-letters are logged, and `OutboxHealthCheck` watches max age, but there are no counters/histograms for them, and nothing for notifications.

**Suggestion:** add meters next to `PaymentMetricConstant`/`PaymentMetrics` (the pattern already exists in `src/Application/Modules/Payments/`): `outbox.dispatch.duration`, `outbox.age.seconds`, `outbox.deadlettered`, `inbox.duplicate`, `notification.delivery.duration`, `notification.failed`. Then write `docs/architecture/failure-injection-checklist.md` (provider down, broker down, DB failover, worker killed mid-batch) and run it in the nightly job.

**Exit criterion:** partially met. You can see a failed outbox row in logs and health, but you cannot yet distinguish "retrying" from "dead-lettered" on a dashboard.

---

## 7. Phase V — Advanced data and distributed workflows

### Week 12 — CQRS, projections, and search 🟡

| Concept | Status | Evidence |
|---|---|---|
| Command/query separation | ✅ | `ICommand`/`IQuery` + separate handlers; `TransactionBehavior` applies to commands only |
| Read models | ✅ | `PaymentReadModel`, `RefundReadModel`, `DisputeReadModel`, `TodoReadModel`, `TodoReminderReadModel` |
| Materialized views | ✅ (table projections) | `PaymentProjection`, `Todo.Projection`, `TodoReminder.Projection` |
| Projection updates | ✅ | `ProjectionRunner` + `ProjectionCheckpointStore` + `ProjectionWorker` |
| Rebuild | ✅ | `RebuildProjection.Service.cs` |
| Full-text search | ⛔ | `EfCoreQueryCapabilities.SearchModes` excludes `FullText`; search is `ToLower().Contains` |
| Vector search | ⛔ | no pgvector package, no embedding provider |
| Ranking / highlighting | ⛔ | `SupportsRanking = false`, `SupportsHighlighting = false` |
| Caching | 🟡 | infrastructure exists; no read-model cache |
| Eventual consistency handling | 🟡 | projections are eventually consistent; no read-your-writes or staleness contract |
| Measured search benchmark | ⛔ | none |

**Exit criterion:** partially met. The "why a read model" answer is strong (payments reads never replay events), but the roadmap's search workload — *"evaluate the current image-search retrieval as a distinct search workload"* — has no subject, because the ML service does not exist.

**Suggestion:** add a search port rather than teaching the generic repository about embeddings:

```text
src/Application/Modules/Catalog/Ports/IProductSearch.Interface.cs
src/Infrastructure/Modules/Catalog/Search/PostgresFullText.ProductSearch.cs   # tsvector + GIN
src/Infrastructure/Modules/Catalog/Search/PgVector.ProductSearch.cs           # only if measured
src/Infrastructure/Modules/Catalog/Search/ExternalMl.ProductSearch.cs         # HTTP adapter to ML service
```

Gate it with `IQueryCapabilities` so callers fail early instead of silently degrading to `Contains`, and record a benchmark before and after.

### Week 13 — Sagas and long-running workflows ⛔ / ➖

| Concept | Status | Evidence |
|---|---|---|
| Local transactions | ✅ | `TransactionBehavior`, `EfCoreTransaction` |
| Choreography | 🟡 | Outbox → integration event → inbox consumer; no explicit saga |
| Orchestration / process manager | ⛔ | none |
| Compensating transactions | 🟡 | `RefundPayment` is a real compensation for a capture; `PaymentReconciliationWorker` settles drift |
| Reservation patterns | ⛔ | no inventory to reserve |
| Timeouts | 🟡 | payment expiry in aggregate; no workflow timeout |
| Eventual consistency | ✅ | projections are eventually consistent |
| Partial-completion tests | ⛔ | no multi-step workflow to test |

**Why this is currently correct:** there is no checkout spanning independent transactional systems. `Payment` + `Refund` share one database, so a saga would add state machine complexity without removing a distributed-transaction problem. The roadmap itself says "saga only if the workflow spans independent transactional systems."

**Exit criterion:** cannot be met, and should not be forced. Create the ADR that says so; revisit when `Orders` + `Inventory` exist and inventory is a separate consistency boundary.

### Week 14 — Event sourcing: investigate, prototype, decide ✅ / 🟡

| Concept | Status | Evidence |
|---|---|---|
| Event streams | ✅ | `StoredEvent`, `PostgresEventStore` |
| Aggregate replay | ✅ | `EventSourcedAggregate`, `EventSourcedRepository` |
| Snapshots | ✅ | `EfCoreSnapshotStore`, `IntervalSnapshotPolicy` (`SnapshotInterval = 50`) |
| Projections | ✅ | `IProjection`, `ProjectionRunner`, checkpoints |
| Event versioning / upcasting | 🟡 | `EventVersion` + `UpcastMissing` failure exist; no upcaster implementation/sample |
| Replay-safe handlers | 🟡 | projections are idempotent by checkpoint, not by handler design |
| Operational requirements | 🟡 | `RebuildProjection.Service` exists; no runbook/drill |
| ADR | ⛔ | no decision record in `docs/` |

**This repository has already made the decision the roadmap asks for:** event sourcing is adopted for `Todo` and `Payment`, with relational read models. What is missing is the written decision and the operational proof.

**Suggestion:** add `docs/architecture/adr/0001-event-sourcing-for-payments.md` and `0002-event-sourcing-for-todos.md` using the pattern template below, plus a replay drill in the nightly job that rebuilds a projection from position 0 into a scratch table and compares row counts/checksums.

---

## 8. Phase VI — Production readiness (ongoing)

| Concern | Status | Evidence to produce | Current evidence |
|---|---|---|---|
| Build and deployment | ✅ / 🟡 | repeatable CI, immutable artifacts, migration strategy | `make ci`, `.github/workflows/**`, `compose.yaml`, `EfCoreMigrator`, `artifacts/publish/**`; no image immutability/signing proof |
| Security | 🟡 | authz tests, secret handling, least privilege | permission tests exist; secrets are plain `appsettings.json` values (development keys) and must move to a secret store |
| Reliability | 🟡 | timeout/retry policies, graceful shutdown, recovery tests | standard resilience + outbox retries + workers; no recovery drill |
| Observability | 🟡 | logs, metrics, traces, health/readiness | OTel + health checks + payment metrics; no SLO dashboards |
| Performance | ⛔ | baseline measurements, query analysis, load tests | none |
| Data integrity | 🟡 | constraints, transaction boundaries, concurrency, backup/restore | EF constraints + UoW + optimistic concurrency; no backup/restore proof |
| Documentation | 🟡 | context/container diagrams, ADRs, runbook | this document is the first context diagram; no ADR folder, no runbook |

**Top production-readiness risks:** plaintext development JWT key in `app/Gateway/appsettings.json` and `app/Web.Api/appsettings.json`; two overlapping health surfaces (`/health`, `/alive` are Development-only in `ServiceDefaults.Extension.cs:116`, while `/health/live`, `/health/ready` are always mapped by `HealthChecks.Extension.cs:38-55`, so an operator must know which one the platform probes); no backup/restore exercise; workers in the API process.

---

## 9. Taxonomy tree, annotated

Legend: ✅ implemented · 🟡 partial · 🧱 scaffold · ⛔ absent · ➖ deferred.

```text
SOFTWARE ENGINEERING
│
├── 1. FOUNDATIONS
│   ├── Programming
│   │   ├── Types, immutability, encapsulation, composition      ✅ Core/BuildingBlocks/{Domain,Modeling,Functional}
│   │   ├── Exceptions, Result/Either, error handling            ✅ Core/BuildingBlocks/Functional/{Results,Errors}
│   │   └── Functional and object-oriented techniques            ✅ Result extensions, Maybe, expression builders
│   ├── Design principles
│   │   ├── SOLID, DRY, KISS, YAGNI                              🟡 conventions + analyzers; YAGNI not testable
│   │   ├── Cohesion and coupling                                🟡 real project refs; module isolation unenforced
│   │   ├── Separation of concerns                               ✅ Core/Application/Infrastructure/API
│   │   └── Dependency inversion                                 ✅ Application/BuildingBlocks/Ports + Adapters
│   ├── Refactoring and code smells                              ✅ enforced by analyzers + make lint
│   └── Testing fundamentals                                     ✅ tests/Unit/** + tests/Integration/**
│
├── 2. ARCHITECTURE AND BOUNDARIES
│   ├── Architecture styles
│   │   ├── Layered                                              ✅ src/Core → src/Application → src/Infrastructure
│   │   ├── Clean / Onion / Hexagonal                            ✅ ports/adapters, zero-ref Core
│   │   ├── Modular monolith                                     ✅ Modules/* in one API
│   │   ├── Microservices / SOA                                  ➖ not adopted
│   │   └── Event-driven architecture                            ✅ outbox + Wolverine + inbox
│   ├── Dependency management
│   │   ├── Dependency injection                                 ✅ Infrastructure.Composition.cs
│   │   ├── Ports and adapters                                   ✅ Storage, Notifications, Payments
│   │   ├── Composition root                                     ✅ app/Web.Api/Composition/Api.Composition.cs
│   │   └── Architecture tests                                   🟡 BuildingBlockBoundary real; Layer/VerticalSlice stubs
│   ├── Boundaries
│   │   ├── Domain and application                               ✅
│   │   ├── Modules and bounded contexts                         🟡 folders only, no context map
│   │   ├── Process and deployment boundaries                    🟡 Gateway/API/SPAs separate; workers in-process
│   │   └── Trust and data ownership                             🟡 JWT + permissions; no data-ownership doc
│   └── API Gateway / BFF / API composition                      🟡 YARP gateway ✅; BFF ⛔
│
├── 3. DOMAIN MODELING
│   ├── DDD strategic design
│   │   ├── Domain and subdomains                                🟡 implicit in folders
│   │   ├── Bounded contexts                                     🟡 implicit; no map
│   │   ├── Context maps                                         ⛔
│   │   └── Shared / published language                          🟡 Core.BuildingBlocks
│   ├── DDD tactical design
│   │   ├── Entity and value object                              ✅ Core/Modules/** + Domain/ValueObjects/**
│   │   ├── Aggregate and aggregate root                         ✅ Aggregates/** + EventSourcedAggregate
│   │   ├── Domain service and factory                           ✅ Policies/**, *.Factory.cs
│   │   ├── Specification and policy                             ✅ Specifications/**, PaymentPolicy
│   │   └── Domain events and invariants                         ✅ *.Event.cs + *.Validator.cs
│   └── Domain modeling alternatives                             ✅ rich model chosen; transaction script not used
│
├── 4. APPLICATION AND USE CASES
│   ├── Command and query                                       ✅ ICommand/IQuery
│   ├── Handler and mediator                                    ✅ MediatR handlers
│   ├── Validation and authorization                            ✅ FluentValidation + permission policies
│   ├── Pipeline / decorator / chain of responsibility          ✅ Behaviors/**
│   ├── Result / Either / error mapping                         ✅ Functional/** + ApiResult
│   ├── Transaction and Unit of Work                            ✅ TransactionBehavior + EfCoreUnitOfWork
│   └── Orchestration and application services                  ✅ handlers orchestrate; no separate app-service layer
│
├── 5. DATA AND PERSISTENCE
│   ├── EF Core / Data Mapper                                   ✅ ApplicationDbContext + configurations
│   ├── Repository and query objects                            ✅ IRepository + Query DSL
│   ├── Transactions and concurrency                             ✅ EfCoreTransaction + Versionable + event-store version
│   ├── Filtering / sorting / pagination                        ✅ offset; ⛔ keyset
│   ├── Projections / read models                               ✅ PaymentProjection, TodoProjection
│   ├── Audit / soft delete / temporal history                  🟡 SoftDeletable + Lifecycle.Interceptor; no temporal history
│   ├── Cache / materialized view                               🟡 cache adapters; tables as read models
│   └── Search indexes and vector search                        ⛔
│
├── 6. INTERFACES AND SECURITY
│   ├── REST / RPC / gRPC / GraphQL                             ✅ REST only
│   ├── DTOs, API contracts, versioning                         🟡 DTOs ✅; versioning ⛔; contract tests ⛔
│   ├── Authentication / authorization                          ✅ JWT + permission policies
│   ├── RBAC / claims / policies / resource checks              ✅ RBAC/policies; 🟡 resource checks imperative
│   ├── OAuth / OIDC / session handling                         ✅ external auth; refresh/rotation/blacklist
│   ├── Gateway routing and rate limiting                       ✅ YARP + per-IP limiter
│   └── Vue components, composables, stores, API clients        🧱 health chip only
│
├── 7. ASYNCHRONOUS COMMUNICATION
│   ├── Command messages / domain events                        ✅ IDomainEvent + outbox
│   ├── Integration events / pub-sub                            ✅ IntegrationEvent + Wolverine
│   ├── Queue / broker / event bus                              ✅ RabbitMQ transport; InMemory default
│   ├── Transactional outbox                                    ✅ OutboxInterceptor + OutboxWorker
│   ├── Inbox / deduplication / idempotency                     ✅ EfCoreInboxStore; ✅ payment idempotency keys
│   ├── Background worker / scheduled job                       ✅ hosted services + Hangfire
│   └── Message contracts and schema evolution                  🟡 EventTypeRegistry + EventVersion; no upcaster
│
├── 8. DISTRIBUTED WORKFLOWS
│   ├── Eventual consistency                                    ✅ projections
│   ├── Saga choreography / orchestration                       ⛔
│   ├── Process manager                                         ⛔
│   ├── Compensating transaction                                🟡 refund + reconciliation
│   ├── Reservations and concurrency                            ⛔ no inventory
│   └── Distributed transaction trade-offs                      🟡 documented only in comments
│
├── 9. RESILIENCE AND OPERATIONS
│   ├── Timeout / retry / circuit breaker                       ✅ standard resilience + outbox + Wolverine
│   ├── Bulkhead / rate limit / backpressure                    ✅ edge limiter + concurrency caps; 🟡 no shedding
│   ├── Cache / fallback / graceful degradation                 🟡 null cache, provider fallback; no circuit fallback
│   ├── Logs / metrics / distributed tracing                    ✅ logs/traces; 🟡 metrics gaps
│   ├── Health / readiness / liveness                           ✅ self/db/redis/rabbitmq/outbox
│   ├── SLI / SLO / alerts                                      ⛔
│   └── CI/CD / deployment / migrations                         ✅ make ci + compose + EfCoreMigrator
│
├── 10. ADVANCED DATA ARCHITECTURE
│   ├── CQRS                                                    ✅ writes event-sourced, reads projected
│   ├── Projections and replay                                  ✅ ProjectionRunner + RebuildProjection
│   ├── Event sourcing                                          ✅ Todos + Payments
│   ├── Snapshots and event versioning                          ✅ snapshots; 🟡 versioning/upcasting
│   ├── CDC / stream processing                                 ⛔
│   ├── Sharding and replication                                ⛔ (not justified)
│   └── Data migration and synchronization                      🟡 EF migrations + backfill seeder (TodoBackfill.Seeder)
│
└── 11. VERIFICATION AND GOVERNANCE
    ├── Unit / integration / API / E2E tests                    ✅ unit/integration/API; ⛔ E2E
    ├── Contract and architecture tests                         🟡 architecture partial; ⛔ contracts
    ├── Load / performance / chaos tests                        ⛔
    ├── ADRs and technical-debt records                         ⛔ (this doc is the first)
    ├── Threat models and failure analysis                      ⛔
    └── Runbooks and recovery exercises                         ⛔
```

---

## 10. Milestones A–H

| Milestone | Roadmap target | Status | What exists | What to build next |
|---|---|---|---|---|
| A | Product CRUD | ⛔ | Nothing (Payments shows the target shape) | `Core/Modules/Catalog`, `Application/Modules/Catalog/{Products/Commands,Queries}`, `Infrastructure/Modules/Catalog/Persistence`, `Web.Api/Endpoints/Catalog` |
| B | Product filtering + image search | ⛔ | Query DSL ✅, search ❌ | Filter contract on `Product`, `IProductSearch` port, pgvector/FTS adapter, benchmark |
| C | Cart + order creation | ⛔ | Money VO, UoW, concurrency patterns | `Core/Modules/Orders`, aggregate invariants, optimistic concurrency test |
| D | Identity + admin permissions | ✅ | Users/roles/permissions/policies/endpoints | Authorization matrix + endpoint coverage test |
| E | Order confirmation notification | 🟡 | Outbox → integration event → inbox ✅; notification hub ✅ | Order events, notification delivery record, idempotent consumer |
| F | Reliable provider delivery | 🟡 | Adapter/Strategy/fallback ✅; retries partial | Delivery status store, per-channel retry, DLQ, metrics |
| G | Multi-step checkout | ⛔ / ➖ | No distributed workflow | Saga/process manager only when inventory is a separate transactional system |
| H | History + analytics | ✅ / 🟡 | Event store + projections ✅ | Audit projections, replay drill, EDR |

---

## 11. Pattern discovery records

Use these as the template for new patterns. Records below are the ones the repository has actually earned.

### Pattern: Transactional Outbox

- **Category:** Messaging / reliability.
- **Problem:** the database change and the broker publish can succeed or fail independently (dual-write).
- **Context:** a committed aggregate event must reach consumers even if the process dies after commit.
- **Implementation:** `OutboxInterceptor` stages `OutboxMessage` in the same `SaveChanges`; `OutboxWorker` polls; `OutboxDispatcher` leases, publishes, marks, retries, dead-letters.
- **Forces:** atomicity, no lost events, at-least-once delivery, eventual publication.
- **Alternatives rejected:** direct publish (loses events); CDC (operational weight not justified yet).
- **Costs:** extra table, polling latency (2 s default), duplicate delivery handled by the inbox.
- **Verification:** `Outbox.Atomicity.Integration.Test.cs`, `Outbox.Flow.Integration.Test.cs`, `Outbox.Store.Integration.Test.cs`, `Outbox.Dispatcher.Test.cs`.
- **Revisit when:** publish latency becomes user-visible or message volume makes polling wasteful → CDC/stream.

### Pattern: Inbox / Idempotent Consumer

- **Category:** Messaging / reliability.
- **Problem:** at-least-once delivery means the same message may run twice.
- **Implementation:** `EfCoreInboxStore.ExecuteOnceAsync` — duplicate check + handler + marker in one transaction; duplicate-key race is benign only if the marker exists.
- **Costs:** one row per (message, consumer); requires a dedicated scoped `DbContext`.
- **Verification:** `Inbox.Store.Test.cs`, `Inbox.Store.Integration.Test.cs`, `IntegrationEventHandler.Test.cs`.
- **Revisit when:** a consumer needs external side effects that cannot join the database transaction (then use an outbox on the consumer side too).

### Pattern: Event Sourcing

- **Category:** Data architecture.
- **Problem:** the business needs a reconstructable history and audit of state changes.
- **Implementation:** `EventSourcedAggregate`, `PostgresEventStore` (expected-version concurrency), `EfCoreSnapshotStore`, `ProjectionRunner`.
- **Adopted for:** `Todo`, `Payment`/`Refund`.
- **Costs:** eventual consistency in reads, replay/upcasting burden, snapshot tuning (`SnapshotInterval = 50`).
- **Verification:** `EventSourcedAggregate.Test.cs`, `EventStore.Integration.Test.cs`, `PaymentProjection.Test.cs`, `Todo.Projection.Test.cs`.
- **Revisit when:** a module needs only current state and an audit log; use relational state + history table instead.

### Pattern: CQRS with Projections

- **Category:** Data architecture.
- **Problem:** write invariants and read shapes conflict.
- **Implementation:** commands append events; `PaymentProjection`/`TodoProjection` maintain `*ReadModel`; queries hit read models only.
- **Costs:** projection lag, checkpoint management, rebuild tooling.
- **Verification:** `Projection.Runner.Test.cs`, `RebuildProjection.Service.cs`, integration tests.
- **Revisit when:** the read model can be a simple EF query over the write table with no invariant conflict.

### Pattern: Adapter + Strategy (providers)

- **Category:** Integration / design pattern.
- **Problem:** vendor SDKs and wire formats must not enter the domain.
- **Implementation:** `IPaymentGateway` + registry; `IStorageProvider` (Local/S3); `INotificationProvider` (SendGrid/SMTP/Sinch/Logging) + priority fallback in `NotificationHub`.
- **Verification:** provider behavior tests under `tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/**` and `Modules/Payments/Adapters/**`.
- **Revisit when:** providers share no contract or only one is ever needed.

### Pattern: Saga — deliberately rejected

- **Category:** Distributed workflow.
- **Decision:** do not implement a saga/process manager now.
- **Reason:** all payment state transitions commit in one PostgreSQL database; `PaymentReconciliationWorker` covers provider drift; there is no inventory reservation.
- **Revisit when:** checkout spans order + payment + inventory with separate commit points, or a provider becomes the system of record for a step that cannot be compensated locally.

### Pattern: Circuit Breaker / Standard Resilience

- **Category:** Reliability.
- **Implementation:** `AddStandardResilienceHandler()` on every `HttpClient` (`ServiceDefaults.Extension.cs:32-39`); custom pipeline in `Http.Extensions.cs`; publisher confirms in `Messaging.Composition.cs:196-205`.
- **Gap:** no per-dependency tuning or fallback behavior; default policy applies to all clients.
- **Revisit when:** a slow provider needs a shorter timeout than the global default, or a fallback response is required.

### Pattern: BFF — deliberately absent

- **Category:** API composition.
- **Decision:** the SPAs call the API through the gateway; no BFF.
- **Reason:** one client shape, same origin, no aggregation across services. A BFF would add a deployable with no distinct requirement.
- **Revisit when:** the storefront needs to aggregate multiple backends or the admin needs a materially different contract.

---

## 12. Gap register and prioritized backlog

Priorities: **P0** blocks the roadmap as written; **P1** needed before production claims; **P2** only when a measured need appears.

### P0 — required to make the roadmap executable

| ID | Gap | Suggested implementation | Acceptance evidence |
|---|---|---|---|
| G1 | No `Product`/`Catalog` | `src/Core/Modules/Catalog/Products/Product.cs` + events/factory/validator/outcome; `Application/Modules/Catalog/Products/{Commands,Queries}`; `Infrastructure/Modules/Catalog/Persistence`; `Web.Api/Endpoints/Catalog/ProductEndpoint.cs` | Domain unit tests for price/publication invariants; API integration test; projection test |
| G2 | No `Cart`/`Order` | `src/Core/Modules/Orders/Orders/Order.cs` + `OrderLine` + status; `CreateOrder`/`AddLine`/`Checkout` use cases | Aggregate transition tests; concurrency test on `Versionable` |
| G3 | Frontend features absent | `features/catalog` and `features/cart` per section 5 (Week 7); typed client, composables, route guards | Component tests + one API-flow integration spec per feature |
| G4 | Search is substring-only | `IProductSearch` port + PostgreSQL FTS adapter (`tsvector` + GIN); capability-gated | Benchmark before/after; capability test asserting `FullText` support |
| G5 | No ML/image-search boundary | `Application/.../Ports/IEmbeddingProvider` + `Infrastructure/.../Search/ExternalMl` HTTP adapter with timeout/resilience | Contract test with a stub HTTP server; failure-mode test (ML down → catalog still serves) |
| G6 | Notification delivery not durable | `NotificationDelivery` entity + store + unique dedupe key; status transitions; DLQ sweep | Duplicate-delivery test; provider-failure test; DLQ health check |
| G7 | Architecture rules unenforced | Replace placeholder tests with reflection rules (layer refs, module isolation, EF-outside-Infrastructure) | Tests fail when a violating reference is added (verify by temporary commit) |

### P1 — production-readiness

| ID | Gap | Suggestion | Evidence |
|---|---|---|---|
| G8 | No keyset pagination | `CursorPageRequest`/`CursorPage<T>` + repository support | Offset vs keyset benchmark |
| G9 | No contract/versioning | `Asp.Versioning` + OpenAPI diff check or Pact consumer tests | CI step that fails on breaking change |
| G10 | No authorization matrix | `docs/architecture/authorization-matrix.md` + endpoint coverage test | Every Carter endpoint has an explicit decision |
| G11 | Observability gaps | Outbox/inbox/notification meters; OTLP dashboards; SLO doc | Dashboard screenshot/JSON + alert rules |
| G12 | No load baseline | k6/NBomber scripts for catalog list, search, payment create | Committed baseline + CI perf smoke |
| G13 | No backup/restore proof | Scripted restore drill against Compose Postgres | Drill log in `docs/architecture/runbooks/` |
| G14 | Secrets in appsettings | Move JWT/provider keys to environment/user-secrets; fail startup on dev keys in non-dev | Startup guard test |
| G15 | Event-sourcing ops unowned | ADRs + replay drill + upcaster sample | ADR files + nightly rebuild comparison |
| G16 | Worker in API process | Extract `app/Web.Worker` hosting outbox/projection/notification workers | Compose/AppHost wiring + integration test |
| G17 | Missing e2e | Install Playwright and add a critical-path suite under `app/Web.Storefront/src/__tests__/e2e/`; the `make test.web.e2e` target already exists and skips cleanly when no suite is present | `make test.web.e2e` runs a browse → cart → pay path |

### P2 — only with measured justification

| ID | Gap | Trigger to revisit |
|---|---|---|
| G18 | No saga/process manager | Checkout spans independent transactional systems |
| G19 | No CDC/stream processing | Outbox polling latency or volume becomes a cost |
| G20 | No sharding/replication | Data volume or availability target exceeds one Postgres |
| G21 | No BFF | Storefront needs cross-service aggregation |
| G22 | No CORS policy | A second browser origin is introduced |

---

## 13. Renaming map: roadmap term → repository term

| Roadmap term | Repository term | Location |
|---|---|---|
| Layered / Clean / Hexagonal | Core → Application → Infrastructure → API | `src/*`, `app/Web.Api` |
| Ports and Adapters | `BuildingBlocks/Ports` + `BuildingBlocks/Adapters` | `src/Application`, `src/Infrastructure` |
| Composition root | `Api.Composition` + `Infrastructure.Composition` | `app/Web.Api/Composition`, `src/Infrastructure` |
| Command/query handler | `ICommand`/`IQuery` + MediatR handler | `Application/BuildingBlocks/UseCases/Mediation` |
| Pipeline behavior | `IPipelineBehavior` behaviors | `.../Behaviors/**` |
| Result/Either | `Result`, `Result<T>`, `Error` | `Core/BuildingBlocks/Functional` |
| Aggregate root | `EventSourcedAggregate<TId>`, `AggregateRoot` | `Core/BuildingBlocks/Domain/Aggregates` |
| Domain event | `IDomainEvent` / `*.Event.cs` | `Core/BuildingBlocks/Domain/Events`, module roots |
| Integration event | `IntegrationEvent` | `Application/BuildingBlocks/Messaging/Events` |
| Transactional outbox | `OutboxMessage` + `OutboxInterceptor` + `OutboxWorker` | `Infrastructure/.../Outbox/**` |
| Inbox / idempotent consumer | `InboxMessage` + `EfCoreInboxStore` | `Infrastructure/.../Inbox/**` |
| Event store | `StoredEvent` + `PostgresEventStore` | `Infrastructure/.../EventSourcing/**` |
| Snapshot | `Snapshot` + `EfCoreSnapshotStore` | same |
| Projection / read model | `IProjection` + `*ReadModel` | `Application/.../EventSourcing`, `Infrastructure/Modules/*/Persistence` |
| Unit of Work | `IUnitOfWork` / `EfCoreUnitOfWork` | `Application/.../Persistence/UnitOfWorks` |
| Repository | `IRepository<T>` family | `Application/.../Persistence/Repositories` |
| Query object / specification | `Query` DSL + `Filter`/`SortField`/`PageRequest` | `Application/BuildingBlocks/Querying` |
| API gateway | `Gateway` (YARP) | `app/Gateway` |
| BFF | *(none — deliberately absent)* | — |
| RBAC / policy | `PermissionMetadata` + `HasPermission` + `PermissionRequirementHandler` | `Application/.../Security/Authorization` |
| Background worker | `BackgroundService` implementations + Hangfire | `Infrastructure/.../Outbox`, `.../Projections`, `Modules/Payments/**` |
| Adapter / Strategy | `IPaymentGateway`, `IStorageProvider`, `INotificationProvider` | `Application/.../Ports`, `Infrastructure/.../Adapters` |
| Health/readiness | `/health`, `/alive`, `/health/ready`, `/health/live` | `ServiceDefaults.Extension.cs`, `HealthChecks.Extension.cs` |
| ML service | *(none)* | — |
| Saga / process manager | *(none)* | — |

### Naming conventions for the missing pieces

Follow the existing rules (`guide/dotted-file-naming-guide.md`, `guide/convention-gate-guide.md`):

- Role suffixes are mandatory: `.Service.cs`, `.Handler.cs`, `.Interface.cs`, `.Abstract.cs`, `.Test.cs`, `.Spec.cs`, `.Outcome.cs`, `.Constant.cs`, `.Validator.cs`, `.Configuration.cs`, `.Model.cs`, `.Extension.cs`.
- One `.Outcome.cs` per capability with nested `Codes` and `Failure`/`Success`; no `Error.*` construction outside it.
- Magic numbers ≥ 10 go in `*Constant.cs`; wire-shaped strings go in `*Constant.cs`.
- Public members of `*.Service.cs`/`*.Interface.cs`/`*.Abstract.cs` return `Result`/`Result<T>`.
- New module folders: `src/Core/Modules/Catalog`, `src/Application/Modules/Catalog`, `src/Infrastructure/Modules/Catalog`, `app/Web.Api/Endpoints/Catalog`.

---

## 14. How to verify and maintain this audit

```bash
make help                 # authoritative command surface
make lint                 # format, structure, convention gate, vue-tsc
make build                # solution + both SPAs
make test                 # fast gate: .NET unit → Api integration → web
make test.infra.broker    # RabbitMQ composition/transport (needs Docker)
make coverage             # 90% line / 80% branch gates
```

Re-run the evidence checks used here:

```bash
# Domain modules that exist
find src/Core/Modules -maxdepth 2 -type d

# Outbox/inbox anchors
rg -n "class Outbox|class Inbox|IOutboxStore|IInboxStore" src

# Worker inventory
rg -n "BackgroundService" src

# Search capability truth
sed -n '1,60p' src/Infrastructure/BuildingBlocks/Adapters/Persistence/EntityFramework/Capabilities/EfCoreQueryCapabilities.cs

# Architecture-test gaps
rg -n "pending API compatibility|true.Should\(\).BeTrue" tests/Architecture.Tests
```

Maintenance rules:

1. **This document is an audit, not a plan.** When a gap closes, update the row and add the proving path; do not delete the history.
2. **Every new module must update sections 2.3, 3–7, and 13 in the same PR.** That is the price of a map that stays true.
3. **Prefer ADRs for decisions.** Create `docs/architecture/adr/README.md` and use the pattern-discovery template in section 11.
4. **Re-audit on every minor release.** Roadmaps rot faster than code.

---

## Appendix A — Concept → file index (quick lookup)

| Concept | Primary file(s) |
|---|---|
| Result / Error | `src/Core/BuildingBlocks/Functional/Results/Result*.cs`, `Errors/Error*.cs` |
| Value objects (Money etc.) | `src/Core/BuildingBlocks/Domain/ValueObjects/**` |
| Aggregate base | `src/Core/BuildingBlocks/Domain/Aggregates/**`, `src/Core/BuildingBlocks/Domain/EventSourcing/EventSourcedAggregate*.cs` |
| Todo aggregate | `src/Core/Modules/Todos/Items/Todo.cs` |
| Payment aggregate | `src/Core/Modules/Payments/Intents/Payment.cs` |
| Payment providers | `src/Infrastructure/Modules/Payments/Adapters/**` |
| Payment idempotency | `src/Infrastructure/Modules/Payments/Persistence/PaymentIdempotencyStore.cs` |
| Payment reconciliation | `src/Infrastructure/Modules/Payments/Reconciliation/PaymentReconciliation.Worker.cs` |
| MediatR pipeline | `src/Application/BuildingBlocks/UseCases/Mediation/**` |
| Query DSL | `src/Application/BuildingBlocks/Querying/**` |
| EF query translator | `src/Infrastructure/.../QueryTranslation/**` |
| Repository | `src/Infrastructure/.../Repositories/**` |
| Unit of work / transaction | `src/Infrastructure/.../UnitOfWorks/**` |
| Outbox | `src/Infrastructure/.../Outbox/**` |
| Inbox | `src/Infrastructure/.../Inbox/**` |
| Event store | `src/Infrastructure/.../EventSourcing/**` |
| Projections | `src/Infrastructure/.../Projections/**` |
| Messaging (Wolverine) | `src/Infrastructure/BuildingBlocks/Adapters/Messaging/**` |
| Notifications | `src/Application/BuildingBlocks/Ports/Notifications/**`, `src/Infrastructure/.../Notifications/**` |
| Storage | `src/Application/BuildingBlocks/Ports/Storage/**`, `src/Infrastructure/.../Storage/**` |
| Identity / permissions | `src/Application/BuildingBlocks/Security/**`, `src/Infrastructure/.../Security/**` |
| Observability | `src/ServiceDefaults/**`, `src/Infrastructure/.../Observability/**` |
| Gateway | `app/Gateway/**` |
| Composition root | `app/Web.Api/Composition/**`, `src/Infrastructure/Infrastructure.Composition.cs` |
| Architecture rules | `tests/Architecture.Tests/**` |
| Conventions | `guide/**`, `scripts/common/convention-baseline.tsv` |

---

## Appendix B — Test coverage map by risk

| Risk | Proven by | Not proven by |
|---|---|---|
| Domain invariants | `tests/Unit/Core.Unit.Tests/Modules/**` | Cross-aggregate invariants |
| Handler behavior | `tests/Unit/Application.Unit.Tests/Modules/**` | Real database semantics |
| EF mapping/queries | `tests/Integration/Infrastructure.Integration.Tests/**` | Production data volume |
| Outbox atomicity | `Outbox.Atomicity.Integration.Test.cs` | Broker acknowledgement |
| Outbox delivery | `Outbox.Flow.Integration.Test.cs`, `RabbitMq.Messaging.Test.cs` | Multi-node failover |
| Inbox dedupe | `Inbox.Store.Integration.Test.cs` | Concurrent multi-process race |
| Event store concurrency | `EventStore.Integration.Test.cs` | Snapshot under sustained load |
| Payments end-to-end | `Payment.Api.Test.cs`, `Payments.Sandbox.Test.cs` | Real Stripe/PayPal |
| Identity/authz | `Identity*.Api.Test.cs` | Resource-based matrix coverage |
| Gateway routing | `tests/Integration/AppHost.Integration.Tests/GatewayRouting.Spec.cs` | Load/rate-limit behavior |
| Web UI | `app/Web.*/src/__tests__/**` (6 specs) | Real browser path |

---

*End of audit. The repository's strength is its platform; its weakness is that the shop it is named for has not been built. The roadmap is executable only after G1–G7 in section 12.*
