# AGENTS.md

## Repository map

- .NET 10 Aspire solution. `global.json` pins SDK `10.0.401`; trust `global.json` and `package.json`.
- Layers: `src/Core` (no refs) ← `src/Application` (Core) ← `src/Infrastructure` (Application + ServiceDefaults). `app/Web.Api` is the composition root.
- Each layer splits into `BuildingBlocks/` (reusable platform capabilities) and `Modules/` (business capabilities):
  - Core: `BuildingBlocks/{Domain,Modeling,Functional}`, `Modules/Todos`
  - Application: `BuildingBlocks/{Caching,EventSourcing,Messaging,Persistence,Ports,Querying,Security,UseCases}`, `Modules/Todos`, and the `Modules/{PermissionCatalog,Persistence}` composition artifacts
  - Infrastructure: `BuildingBlocks/Adapters/{BackgroundJobs,Caching,Events,Http,Messaging,Notifications,Observability,OpenApi,Persistence,Security,Serialization,Storage}`, `Modules/Todos`, `Migrations`
- BuildingBlocks must not reference Modules. Modules may reference BuildingBlocks, and cross-module communication goes through BuildingBlocks contracts or the composition root. `Modules/{PermissionCatalog,Persistence}` are composite-catalog composition artifacts, not business module folders. `Infrastructure/Migrations` is the exempt composition artifact.
- Payments: `Core/Modules/Payments`, `Application/Modules/Payments`, `Infrastructure/Modules/Payments`
  (event-sourced `Payment` + `Refund`, `IPaymentGateway` with Fake/Stripe/PayPal adapters, schema `payment`).
- `app/Gateway` is the YARP edge in front of the Api. `app/AppHost` wires Postgres, Redis, RabbitMQ, Api, Gateway, and both Vite apps. The storefront is served at `/`, admin under `/admin`.
- `app/Web.Storefront` and `app/Web.Admin` are one pnpm workspace rooted at `app/`, not the repo root.
- `make` is the public command surface; `make help` is authoritative. `make/*.mk` declares targets, `scripts/**` implements them.
- Goals are dotted (`api.test`, `coverage.api`). Bare group names (`api`, `store`, `web`, `integration`) are not targets: `make coverage api` means two goals and fails after `coverage`. Use `make coverage.api` or `make coverage api.test`.
- AppHost injects `ConnectionStrings__db`, `ConnectionStrings__redis`, `ConnectionStrings__rabbitmq`, `Messaging__Transport=RabbitMq`, and `Persistence__AutoMigrate=true`. The standalone API reads `appsettings*.json` + environment variables; `Messaging:Transport` defaults to `InMemory`, so no broker is required outside AppHost/Compose. There is no `.env` loader despite the README.

## Commands

- `make doctor` — verify dotnet/node/pnpm/Docker.
- `make install` — one .NET restore + one `app/` pnpm install.
- `make lint` — `dotnet format --verify-no-changes`, structure check, `vue-tsc`. `make format` writes fixes.
- `make build` — solution + both SPA bundles.
- `make test` — fast gate: .NET unit → Api integration → web unit → web integration.
- `make test.infra.broker` — RabbitMQ composition + transport suite; uses Testcontainers and skips without Docker.
- `make test.all` — adds AppHost/Infrastructure integration and optional e2e; needs Docker (Testcontainers) and node/pnpm.
- `make coverage` / `make coverage.all` — coverage gates (90% line / 80% branch).
- `make ci` = lint + build + .NET/web coverage + Api tests + RabbitMQ broker tests. `make ci.full` = every suite + Compose smoke (starts Docker Compose).
- Test targets build first; the underlying scripts use `--no-build`, so build before invoking `dotnet test` directly.

## Test layout and order

- .NET unit, base → higher: `tests/Unit/Core.Unit.Tests` → `Application.Unit.Tests` → `Infrastructure.Unit.Tests` → `tests/Architecture.Tests` last.
- .NET integration: `tests/Integration/Api.Integration.Tests` → `AppHost.Integration.Tests` → `Infrastructure.Integration.Tests`.
- Web: `app/Web.*/src/__tests__/unit` then `.../integration`; `.../e2e` is optional (Playwright is not installed; the target skips cleanly).
- Single xUnit test:
  `dotnet test --project tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release --filter-class Fully.Qualified.Class`
  Use `--filter-method Fully.Qualified.Class.Method` for one test. MTP exits `8` when a filter matches zero tests.
- Single web test, from `app/`:
  `pnpm --filter @qingfa/storefront exec vitest run src/__tests__/unit/App.spec.ts -t "test name"`
- Never use `pnpm test:unit` in scripts/CI; it starts Vitest in watch mode.
- `global.json` forces Microsoft.Testing.Platform. Use `dotnet test --project …`. Do not add coverlet/VSTest collectors; coverage uses the pinned `dotnet-coverage` tool.
- Integration tests **skip** when Docker/Podman or node/pnpm is missing. Check skip counts before claiming a suite exercised infrastructure.
- Helper projects under `tests/` without `[Fact]`/`[Theory]` set `QingFaRequireTestSdk=false`; `lib.test` skips projects with no test sources.

## Coverage

- Floors: 90% line / 80% branch per assembly and per web app.
- Gated: `Core`, `Application`, `Infrastructure`, `Web.Api`, both SPAs. Report-only: `ServiceDefaults`, `Gateway`, `AppHost`, test assemblies.
- Reports land in `artifacts/coverage/` (git-ignored). PR CI runs unit + web coverage; main/nightly adds Api/AppHost/Infra raw reports and `coverage.report` merges them.
- Override locally with `COVERAGE_LINES=… COVERAGE_BRANCHES=… make coverage`.

## C# conventions that fail the build

- `net10.0`, nullable enabled, analyzers on, warnings as errors. Test projects add `NoWarn` for `CS1573;CA1707;xUnit1051;CA1861`.
- Central Package Management: versions live only in `Directory.Packages.props`; `PackageReference` must not carry `Version`. Commit the updated `packages.lock.json` files — CI restores in locked mode.
- `make structure.check` enforces namespace == folder path and dotted role suffixes on C# filenames (`*.Service.cs`, `*.Handler.cs`, `*.Test.cs`, `*.Spec.cs`, …). Unrecognized roles fail. See `guide/dotted-file-naming-guide.md`.
- Comments use the labeled vocabulary (`Validate:`, `Initialize:`, `Enforce:`, `Handle:`, `Fallback:`, …), not freeform narration. See `guide/code-comment-style-guide.md`.
- Layer boundaries are real project references in `.csproj`; `Architecture.Tests` enforces BuildingBlocks → Modules via reflection, while broader NetArchTest rules are still pending, so don't rely on it alone to catch violations.
- Gateway route order is load-bearing: `/api/time` must precede `/api`, and the storefront catch-all must be last.
  The payments routes also precede `/api`: `/api/payments/webhooks/{**remainder}` is anonymous and both it and
  `/api/payments/{**remainder}` / `/api/admin/payments/{**remainder}` preserve the `/api` prefix (the catch-all strips it).
- AppHost sets auto-migration. Add EF migrations with `dotnet ef migrations add <Name> -p src/Infrastructure -s app/Web.Api`; they live in `src/Infrastructure/Migrations`.

## Web conventions

- Install once via `make store.install` / `make install` (frozen lockfile). Run pnpm commands from `app/` or through make.
- Vue 3 + TypeScript + Vite + StyleX. `API_BASE_URL` is injected by AppHost and proxied by Vite to the Gateway; there is no Pinia store.
- Vitest uses the v8 provider (`@vitest/coverage-v8`) and writes reports under `artifacts/coverage/web-*`. `src/**/__tests__/**` is excluded from coverage.
- Keep unit specs under `__tests__/unit` and router/API-flow specs under `__tests__/integration`; `test.web.sh` filters by those directories.

## CI

- PR: `backend` job runs lint/build/unit coverage + Api tests + the RabbitMQ broker suite; `web` job runs web coverage.
- `main`, nightly, and manual runs add AppHost, Infrastructure, merged coverage, and optional e2e. Compose smoke runs only when Compose/images inputs change.
- `cd.yml` runs `make ci` before building/tagging images; tags and `main` push it.
- Docs caveat: older folder READMEs can lag the implementation; trust the `.csproj` files and `make help`.

## Read next

`README.md`, `make/README.md`, `scripts/README.md`, `guide/README.md`, `guide/dotted-file-naming-guide.md`, `guide/code-comment-style-guide.md`.
