# Prerequisites and Toolchain

The exact, pinned tools and versions required to build and run resys-eshop locally.

**Links:** [Environment](./02-environment.md) · [Docker Compose](./03-docker-compose.md) · [Seed data](./04-seed-data.md) · [ADR 0001 — Stack](../../00-governance/01-adr/01-stack.md) · [Solution layout](../../03-architecture/02-components/01-solution-layout.md) · [Development index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the single list of what a contributor installs before touching resys-eshop. The
versions are not suggestions: the backend runtime is locked by
[ADR 0001](../../00-governance/01-adr/01-stack.md) and §4 of the
[design spec](../../11-spec/README.md), and the front-end targets
Nuxt 4.5.x on a compatible Node.js LTS. Do not substitute a version, and do not upgrade a pinned
line as part of an unrelated change; a stack change is an ADR (`00/01-01`).

The environment variables these tools consume are in `05/01-02`; the database container is started
from the Compose file in `05/01-03`; the demo dataset is applied per `05/01-04`.

---

## Pinned Versions

| Tool | Version | Why this version | Used by |
|---|---|---|---|
| .NET SDK | **10.0.x** — install 10.0.12 or a later patch of the `10.0` feature band | .NET 10 LTS, supported to 2028-11-14; .NET 11 is RC/STS and out of scope. | `src/Resys.Api`, all `src/Resys.Modules.*`, `tests/*` |
| EF Core CLI (`dotnet-ef`) | **10.0.x** (match the SDK band) | Migrations are generated against EF Core 10. | Schema workflow in `03/03-04` |
| Node.js | **22.x LTS** (minimum `22.12.0`; a newer LTS such as 24.x is also compatible) | Nuxt 4.5 requires Node 20.19+/22.12+; pin one LTS line per team and record it in `web/.nvmrc`. | `web/storefront`, `web/admin`, `web/shared` |
| Package manager | **pnpm 10.x** (Corepack-managed) | The `web/` folder is a workspace of three packages; pnpm is the pinned manager. `npm` (bundled with Node) is the fallback only. | `web/*` |
| Vue | **3.5.43** | Pinned stable; 3.6 is RC. Installed transitively by Nuxt. | `web/*` |
| Docker Engine | **27+** (Docker Desktop 4.3x on macOS/Windows) | Runs the PostgreSQL 18 container and any tooling containers. | `05/01-03` |
| Docker Compose | **v2** — invoked as `docker compose` | Compose v1 (`docker-compose`) is not supported. | `05/01-03` |
| PostgreSQL | **18** (`postgres:18` image; no host install required) | Current stable, per the stack ADR. Runs in a container, not on the host. | API database |
| Git | **2.45+** | Branching, Conventional Commits, and the pre-commit checks in `05/03-03`. | Whole repository |

Optional local tooling:

| Tool | Version | Purpose |
|---|---|---|
| pgAdmin 4 (`dpage/pgadmin4`) | current stable | Inspecting the local database; started from the optional profile in `05/01-03`. |
| Mailpit (or any SMTP catcher) | current stable | Captures transactional email on port `1025` when `Email:Provider=smtp` (`05/01-02`). |
| Scalar / any OpenAPI viewer | current stable | Browses the generated OpenAPI document; the document itself is served by `Resys.Api` (`03/04-04`). |

PostgreSQL is **never installed on the host**. The container in `05/01-03` is the only supported
local database, so every contributor runs PostgreSQL 18 and nothing else. This keeps the
Testcontainers integration tests (`06/01-03`) and the local database on the same major version.

---

## Supported Operating Systems

- **Linux** — any current distribution with a 64-bit toolchain; the primary target.
- **macOS** — Apple silicon or Intel; Docker Desktop supplies the container runtime.
- **Windows** — develop inside **WSL2** (Ubuntu/Debian); run the Docker engine in WSL2 integration
  mode. Native PowerShell development is not a supported path because the shell commands in this
  area and in `05/03-01` are POSIX.

---

## Install

1. **.NET SDK 10.0.x.** Install from the official channel and confirm the band:

   ```bash
   dotnet --list-sdks        # must show a 10.0.x entry
   dotnet --version          # must print 10.0.x
   ```

2. **EF Core CLI.** It is a global tool, installed once per machine:

   ```bash
   dotnet tool install --global dotnet-ef --version 10.0.*
   dotnet ef --version       # must match the installed SDK band
   ```

3. **Node.js 22.x LTS.** Install via `nvm` (Linux/macOS) or `fnm`, then pin the repository version:

   ```bash
   node --version            # v22.12.0 or later LTS
   corepack enable
   corepack prepare pnpm@10 --activate
   pnpm --version            # 10.x
   ```

   `corepack` is the supported way to obtain the pinned pnpm; it is bundled with Node and avoids a
   global install that drifts per machine.

4. **Docker + Compose.** Install Docker Engine (or Docker Desktop) and verify the Compose v2 plugin:

   ```bash
   docker --version
   docker compose version    # must print "Docker Compose version v2.x"
   ```

5. **Git.** Confirm the version and your identity:

   ```bash
   git --version             # 2.45 or later
   git config --get user.name
   git config --get user.email
   ```

Linux users should confirm that the current account can run `docker` without `sudo` (add it to the
`docker` group) so the Compose commands in `05/01-03` work as written.

---

## Repository Bootstrap

After the tools are installed, a first-time setup follows the order in `00/02-01`:

1. `git clone` the repository and check out `main`.
2. Restore the .NET solution: `dotnet restore` from the repository root.
3. Install front-end dependencies once: `pnpm install` from `web/`.
4. Start the database with `05/01-03` and apply the seed dataset with `05/01-04`.
5. Build and run the API and both front-ends per `05/03-01`.

---

## Verifying the Toolchain

Run the whole set before opening a pull request; a mismatch here is the most common cause of a
green-on-my-machine failure in CI (`05/03-03`).

| Check | Command | Expected |
|---|---|---|
| SDK band | `dotnet --version` | `10.0.x` |
| EF tool | `dotnet ef --version` | `10.0.x` |
| Node version | `node --version` | `v22.x` LTS or newer LTS |
| Package manager | `pnpm --version` | `10.x` |
| Container runtime | `docker compose version` | `v2.x` |
| Database image | `docker compose config --images` | includes `postgres:18` |

---

## Version Drift Rules

- **One version line per category.** The team installs the same .NET band, the same Node LTS, and
  the same pnpm major; a version bump is its own change, not a side effect.
- **The lockfile wins.** `pnpm-lock.yaml` and the .NET lockfiles are committed; never resolve a
  dependency by editing the lockfile by hand.
- **A stack change is an ADR.** Moving off .NET 10, Node LTS, PostgreSQL 18, or pnpm requires a new
  ADR in `00/01-*` that supersedes `00/01-01`, because those choices are binding on later areas.
- **CI matches local.** The pipeline in `05/03-03` installs the same pinned versions, so a local
  pass predicts a CI pass.

## Open Questions

1. The exact Node LTS minor and the pnpm minor are confirmed when `web/` is scaffolded; the
   constraint (Nuxt 4.5-compatible Node LTS, pnpm 10.x) is fixed here and in the stack ADR.
2. Whether the repository pins tool versions through `.nvmrc` plus `global.json` or through a
   container-based dev image is owned by `05/03-01`; either way the versions above are the source
   of truth.
