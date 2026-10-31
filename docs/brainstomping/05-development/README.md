# 05 — Development

How to set up, configure, and run resys-eshop locally, plus the conventions and workflow the team follows while building it.

**Links:** [Prerequisites](./01-setup/01-prerequisites.md) · [Environment](./01-setup/02-environment.md) · [Docker Compose](./01-setup/03-docker-compose.md) · [Seed data](./01-setup/04-seed-data.md) · [Migration runbooks](./04-migration/README.md) · [Spec](../11-spec/README.md)

---

## Purpose

This area is the working guide for a contributor: the toolchain to install, the configuration to
provide, the containers to start, the data to load, and the conventions and workflow that keep the
codebase consistent. It turns the locked stack in the
[design spec](../11-spec/README.md) and
[ADR 0001](../00-governance/01-adr/01-stack.md) into day-to-day commands.

The area is split into four concern subfolders, each with numbered files (~100–300 lines). Start
with `01-setup` to get running, then follow `02-conventions` and `03-workflow` as you write and
merge changes; `04-migration` holds one-time porting runbooks (for example legacy `old_references`
into `BuildingBlocks.*`). When a statement here conflicts with the spec, the spec wins and this
area is corrected.

---

## Subfolder Index

| Subfolder | Purpose | Files |
|---|---|---|
| `01-setup` | Install the pinned toolchain, configure the API and front-ends, start PostgreSQL 18, and load the demo dataset. | [01-prerequisites.md](./01-setup/01-prerequisites.md) · [02-environment.md](./01-setup/02-environment.md) · [03-docker-compose.md](./01-setup/03-docker-compose.md) · [04-seed-data.md](./01-setup/04-seed-data.md) |
| `02-conventions` | The style and structure rules per stack: C# and .NET, module construction, Vue/Nuxt, and naming. | [01-dotnet-style.md](./02-conventions/01-dotnet-style.md) · [02-module-conventions.md](./02-conventions/02-module-conventions.md) · [03-vue-nuxt-style.md](./02-conventions/03-vue-nuxt-style.md) · [04-naming.md](./02-conventions/04-naming.md) |
| `03-workflow` | Building and running, branching and commits, and the CI pipeline that gates a merge. | [01-build-run.md](./03-workflow/01-build-run.md) · [02-branching.md](./03-workflow/02-branching.md) · [03-ci.md](./03-workflow/03-ci.md) |
| `04-migration` | One-time runbooks: porting legacy code into the current architecture. | [01-old-references-to-building-blocks.md](./04-migration/01-old-references-to-building-blocks.md) · [README](./04-migration/README.md) |

Every file in this area is linked from the subfolder index above; prose elsewhere cites it by its
identifier (for example `05/02-01`, `05/03-01`) and links it when a jump is useful.

---

## How to Read This Area

1. **Get running** with `01-setup`: install tools (`05/01-01`), configure the API, Nuxt runtime,
   and the storage default (`05/01-02`), start PostgreSQL 18 and the optional tools (`05/01-03`),
   and load the demo dataset (`05/01-04`).
2. **Write consistent code** with `02-conventions`: C# style (`05/02-01`), how to add and wire a
   module (`05/02-02`), Vue/Nuxt style (`05/02-03`), and naming across projects, routes, tables,
   and docs (`05/02-04`).
3. **Build, branch, and merge** with `03-workflow`: local build/run commands (`05/03-01`), the
   branching and commit rules (`05/03-02`), and the CI pipeline (`05/03-03`).
4. **Port legacy code** with `04-migration` when moving `old_references/Shared` into
   `BuildingBlocks.*` (`05/04-01`).
5. Cite a file by its identifier (`AA/BB-CC`) in prose and link it from the index above when a
   reader should jump to it.

---

## Setup at a Glance

A first-time setup is the five steps in
[`00/02-01`](../00-governance/02-policies/01-contributing.md), detailed across `01-setup`:

| Step | Action | File |
|---|---|---|
| 1 | Install .NET SDK 10.0.x, Node.js LTS, pnpm, Docker + Compose, Git. | `05/01-01` |
| 2 | Provide API settings and Nuxt runtime config; note the storage default. | `05/01-02` |
| 3 | Start PostgreSQL 18 (and optional pgAdmin/Mailpit) with Compose. | `05/01-03` |
| 4 | Apply migrations and load the development seed dataset. | `05/01-04` |
| 5 | Build and run the API, storefront, and admin. | `05/03-01` |

The stack is **.NET 10 LTS + EF Core 10 + PostgreSQL 18** on the back end and **Nuxt 4.5.x (Vue
3.5.43, TypeScript strict)** on the front end; the project tree these commands operate on is
`03/02-01` ([solution layout](../03-architecture/02-components/01-solution-layout.md)).

---

## Configuration Rule

One resolved decision belongs at this level because it spans setup and architecture:

- **Object storage default is `local` disk in development and S3-compatible in production.** Spec
  §13 Open Question 2 is resolved in `05/01-02`; the `IFileStorage` contract it backs is
  [`03/04-06`](../03-architecture/04-integration/06-email-files.md).

Two more open questions from the spec are owned by sibling areas and are not re-decided here: the
admin rendering mode (`04/02-01`) and the payment-gateway default order (`03/04-05`).

---

## Related Areas

- `00` (governance) — contributing, review policy, and the ADRs that lock this area's stack.
- `01` (requirements) — the behavior the code being developed must satisfy.
- `03` (architecture) — the solution layout, module contracts, and data model behind the commands.
- `04` (design) — the UI specifications the front-end work implements.
- `06` (verification) — the test strategy and coverage gates the workflow runs in CI.
- `08` (operations) — the deployment topology this local environment mirrors.
- `10` (maintenance) — the dependency policy that governs tool and library upgrades.

---

## Keeping This Area Current

- Update `05/01-01` when a pinned tool version changes, and record a stack change as an ADR first.
- Update `05/01-02` when a setting is added, renamed, or its default changes, and keep the Nuxt
  runtime-config table in step with the API's options.
- Update `05/01-03` when a dependency service, port, or volume changes; keep the ports here aligned
  with `03/01-02`.
- Update `05/01-04` when the seed dataset or the seed commands change; the layer split is fixed by
  `03/03-04`.
- Keep the `02-conventions` and `03-workflow` links above current; this index is the single place a
  reader discovers them.

## Open Questions

None at the area level. Unresolved details live in the owning file under its `## Open Questions`
section. The one spec-level question this area owns — the local object-storage default — is
resolved in `05/01-02`.
