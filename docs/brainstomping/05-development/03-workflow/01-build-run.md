# Build and Run

The exact local commands to restore, build, and test the solution, run `Resys.Api` and both Nuxt apps, apply migrations and seed data, and reach each service by URL and port.

**Links:** [Prerequisites](../01-setup/01-prerequisites.md) · [Environment](../01-setup/02-environment.md) · [Docker Compose](../01-setup/03-docker-compose.md) · [Seed data](../01-setup/04-seed-data.md) · [.NET style](../02-conventions/01-dotnet-style.md) · [Vue & Nuxt style](../02-conventions/03-vue-nuxt-style.md) · [Branching](./02-branching.md) · [CI](./03-ci.md) · [Solution layout](../../03-architecture/02-components/01-solution-layout.md) · [Migrations](../../03-architecture/03-data/04-migrations.md) · [Development index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This is the single command reference for getting resys-eshop running on a workstation and for
reproducing the same steps CI runs (`05/03-03`). It assumes the pinned toolchain is installed
(`05/01-01`), the settings are in place (`05/01-02`), and PostgreSQL 18 is started from the Compose
file (`05/01-03`). The migration workflow is owned by `03/03-04` and the demo dataset by `05/01-04`;
this file wires them into the everyday loop.

All commands run from the repository root unless a step says otherwise. They are POSIX shell; on
Windows, develop inside WSL2 as required by `05/01-01`.

---

## Solution and Projects

The build operates on `Resys.sln` at the repository root. Its projects are:

| Path | Kind | Role |
|---|---|---|
| `src/Resys.Core` | Library | Shared kernel: `Money`, identities, store scoping, extension contracts. |
| `src/Resys.Shared` | Library | Client-facing DTOs shared with the front-ends. |
| `src/Resys.Modules.<Name>` | Library | Twelve module projects (`Catalog`, `Orders`, `Shipping`, …). |
| `src/Resys.Infrastructure` | Library | EF Core/Npgsql, Identity, storage, email, gateway adapters. |
| `src/Resys.Api` | Executable | The only backend deployable; hosts `/api/v1` and `/api/admin`. |
| `tests/Resys.UnitTests` | Test | xUnit domain units and architecture-boundary tests. |
| `tests/Resys.IntegrationTests` | Test | xUnit integration tests backed by Testcontainers PostgreSQL. |
| `tests/Resys.E2E` | Test | Playwright browser journeys for storefront and admin. |
| `web/storefront`, `web/admin`, `web/shared` | Nuxt/TS | The two front-ends and their shared package. |

`Resys.Api` is the only project with a `Program.cs`; no module runs on its own (`03/02-01`).

---

## Restore and Build

```bash
# 1. restore every NuGet dependency
dotnet restore Resys.sln

# 2. build the whole solution
dotnet build Resys.sln --no-restore -c Debug

# 3. check formatting exactly as CI does (05/02-01)
dotnet format Resys.sln --verify-no-changes --severity warn
```

- Use `-c Release` to reproduce the CI configuration.
- `dotnet build src/Resys.Api` also emits the OpenAPI documents
  (`artifacts/openapi/v1.json`, `artifacts/openapi/admin.json`, `03/04-04`).
- Treat warnings as errors: the projects set `TreatWarningsAsErrors` (`05/02-01`), so a warning
  fails the build.

Install the front-end workspace once per checkout:

```bash
cd web
pnpm install
```

---

## Test the Solution

```bash
# unit tests only (xUnit)
dotnet test tests/Resys.UnitTests -c Debug

# integration tests (xUnit + Testcontainers PostgreSQL; requires Docker)
dotnet test tests/Resys.IntegrationTests -c Debug
```

These two commands are the fast loop and cover all non-E2E test projects. Running
`dotnet test Resys.sln` additionally executes `tests/Resys.E2E`, which needs the API and both
front-ends running and Playwright browsers installed; follow the pipeline order in `05/03-03` for
that. `tests/Resys.IntegrationTests` starts a throwaway PostgreSQL 18 container through
Testcontainers, so Docker must be running (`05/01-03`). The test strategy is owned by `06/01-01`.
Run a single test while iterating:

```bash
dotnet test tests/Resys.UnitTests --filter FullyQualifiedName~OrderStateMachine
```

---

## Start the Database

PostgreSQL 18 is the only required local dependency and runs in a container (`05/01-03`):

```bash
docker compose up -d
docker compose ps            # postgres must be healthy before the next step
```

---

## Apply Migrations and Seed

The schema is created by the API host's `migrate` verb and the demo dataset by its `seed` verb
(`05/01-04`). Run them in this order:

```bash
# apply every module migration
dotnet run --project src/Resys.Api -- migrate

# load the idempotent development dataset
dotnet run --project src/Resys.Api -- seed --profile demo
```

Variants:

```bash
dotnet run --project src/Resys.Api -- seed --profile demo --verify   # seed, then print counts
dotnet run --project src/Resys.Api -- seed --profile demo --dry-run  # report planned writes only
dotnet run --project src/Resys.Api -- seed --profile demo --reset-demo
```

To add a migration for a module while developing, use the per-module `dotnet ef` commands in
`03/03-04`; never hand-edit the migration snapshot.

---

## Run the API

```bash
# environment selection enables the dev seeder and local storage (05/01-02)
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Resys.Api
```

During development, use watch mode to hot-reload on source change:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet watch --project src/Resys.Api
```

The listener URLs and ports come from `launchSettings.json` / `ASPNETCORE_URLS` and match the
defaults in `05/01-02`: HTTP `8080`, HTTPS `8443`. On startup the host exposes:

| Endpoint | Path |
|---|---|
| Public API | `/api/v1/*` |
| Admin API | `/api/admin/*` |
| Scalar UI (non-production) | `/scalar` (`03/04-04`) |
| OpenAPI document (public) | `/openapi/v1.json` |
| OpenAPI document (admin) | `/openapi/admin.json` |
| Liveness | `/health/live` |
| Readiness | `/health/ready` (`03/01-02`) |

`/health/ready` verifies the database; use it to confirm migrations and seeding succeeded.

---

## Run the Storefront and Admin

Both Nuxt 4 apps live in the `web/` pnpm workspace. Run from `web/`:

```bash
cd web

# storefront (SSR) on http://localhost:3000
pnpm --filter storefront dev

# admin on http://localhost:3001
pnpm --filter admin dev
```

The apps read the API base URL from runtime config (`05/01-02`). Run type and lint checks before
committing (`05/02-03`):

```bash
pnpm --filter storefront lint
pnpm --filter storefront typecheck
pnpm --filter admin lint
pnpm --filter admin typecheck
```

To preview a production build locally:

```bash
pnpm --filter storefront build && pnpm --filter storefront preview
pnpm --filter admin build && pnpm --filter admin preview
```

---

## Ports and URLs

| Surface | URL | Port |
|---|---|---|
| API (HTTP) | `http://localhost:8080` | 8080 |
| API (HTTPS) | `https://localhost:8443` | 8443 |
| Scalar UI (dev) | `http://localhost:8080/scalar` | 8080 |
| Storefront | `http://localhost:3000` | 3000 |
| Admin | `http://localhost:3001` | 3001 |
| PostgreSQL 18 | `localhost:5432` | 5432 |
| Mailpit (tools profile) | `http://localhost:8025` | 8025 |
| pgAdmin (tools profile) | `http://localhost:5050` | 5050 |

The same table is authoritative in `03/01-02`; keep them in step if a port changes.

---

## Typical Day

1. `docker compose up -d` and wait for `postgres` to be healthy.
2. `dotnet run --project src/Resys.Api -- migrate` after pulling schema changes.
3. Run the API, then `pnpm --filter storefront dev` and `pnpm --filter admin dev` in `web/`.
4. Before pushing, run the build, the tests, `dotnet format --verify-no-changes`, and the front-end
   lint/typecheck commands above; the same gates run in `05/03-03`.

## Troubleshooting

- **API cannot reach the database.** Confirm `docker compose ps` reports `postgres` healthy and the
  connection string is the default in `05/01-02`.
- **Storefront returns connection refused.** Start `Resys.Api` first; the SSR server calls
  `http://localhost:8080` (`05/01-02`).
- **Integration tests hang.** Docker is not running or the socket is not reachable; Testcontainers
  needs a working engine.
- **Port already in use.** Another API or a second database owns the port; stop it rather than
  changing the documented defaults.

## Open Questions

1. Whether common tasks are wrapped in a Makefile or script is a scaffold decision owned by this
   file when the repository tooling lands; the raw commands above are the contract either way.
2. The exact `launchSettings.json` profiles are finalized with the host scaffold; the ports are
   fixed by `03/01-02`.
3. The E2E invocation (`tests/Resys.E2E`) and its browser install step are owned by `06/01-04`; the
   stage position is fixed by `05/03-03`.

## Links

- [Prerequisites](../01-setup/01-prerequisites.md) · [Environment](../01-setup/02-environment.md) ·
  [Docker Compose](../01-setup/03-docker-compose.md) · [Seed data](../01-setup/04-seed-data.md)
- [.NET style](../02-conventions/01-dotnet-style.md) ·
  [Vue & Nuxt style](../02-conventions/03-vue-nuxt-style.md)
- [Branching](./02-branching.md) · [CI](./03-ci.md)
- [Solution layout](../../03-architecture/02-components/01-solution-layout.md) ·
  [App topology](../../03-architecture/01-overview/02-app-topology.md) ·
  [Migrations](../../03-architecture/03-data/04-migrations.md) · [OpenAPI](../../03-architecture/04-integration/04-openapi.md)
- [Design spec](../../11-spec/README.md) — §5 (layout), §10
  (configuration).
