# ADR 0005: Building Blocks Kit (supersedes ADR 0002 supporting-project list)

Splits the shared kit into six `BuildingBlocks.*` projects (Kernel → Core → Application → Infrastructure → Hosting, plus a Shared DTO leaf) while keeping the modular monolith, single database, and module contract rules of ADR 0002 unchanged.

**Links:** [Governance index](../README.md) · [ADR 0002](./02-modular-monolith.md) · [Building Blocks](../../03-architecture/02-components/08-building-blocks.md) · [Migration guide](../../05-development/04-migration/01-old-references-to-building-blocks.md) · [Spec](../../11-spec/README.md)

---

## Status

**Proposed — 2026-09-24.** When accepted, mark *Accepted* and update ADR 0002 “Supporting projects” with a superseded note pointing here.

## Context

ADR 0002 locked the modular monolith and listed four supporting projects: `Resys.Core`, `Resys.Infrastructure`, `Resys.Api`, `Resys.Shared`. Implementation and the legacy `old_references/Shared` codebase show that “one Core + one Infrastructure” is too coarse:

- Language-level primitives (Result, Guard, clock) must stay free of DDD vocabulary so Kernel is a pure toolkit; shared domain (Entity, AggregateRoot, ValueObject, events) and commerce concerns (Money, store scope) need a home above it.
- Application machinery (CQRS, validation, specifications) has no home.
- Infrastructure spans persistence, outbox, audit, security, observability, caching, HTTP, storage, email, and jobs — impossible to pack or test independently and easy to couple incorrectly.
- The kit must be reusable as NuGet-style packages in future without dragging the whole platform.

A fine-grained 24-project split was rejected as too much ceremony for a small team and for a codebase whose old Shared pillars were proven at six projects.

## Decision

Keep **exactly six kit libraries** under `src/BuildingBlocks/`, named `BuildingBlocks.*` (not `Resys.*` — `Resys.*` remains for modules, API, and tests):

| Project | Layer |
|---|---|
| `BuildingBlocks.Kernel` | Pure primitives only — **no DDD vocabulary, no commerce, no ASP.NET**: `Guard`, `Result`/`Error`/`PagedResult`, `IClock`, `IHasKey`/`KeyedRegistry` |
| `BuildingBlocks.Core` | **Shared domain (DDD) + event sourcing only**: `Entity`/`AggregateRoot`/`ValueObject` + VO catalog, `IDomainEvent`, `EventEnvelope`/`StreamId`, typed-id base, concerns (`Auditable`, …), `Money`, `Translatable`. **No technical ports.** |
| `BuildingBlocks.Application` | CQRS / validation / specifications / mapping **and all module-injectable contracts by area** (Identity, Notifications, Storage, Auth, Permissions, Outbox, Stores, EventSourcing, …) — no flat `Ports/` |
| `BuildingBlocks.Shared` | HTTP DTO leaf |
| `BuildingBlocks.Infrastructure` | All technical adapters (folders: Persistence, Outbox, Auditing, Security, Identity, Observability, Caching, Http, Storage, Media, Email, BackgroundJobs, Querying) |
| `BuildingBlocks.Hosting` | Composition, OpenAPI, middleware pipeline (no `Program.cs`) |

**DDD placement:** domain bases live in **Core** (`Domain/` folders), not Kernel. Kernel stays a BCL-only toolkit; technical contracts live in **Application area folders** (audit: `05/04-01` Audit log, 2026-09-24) so Core remains pure DDD/ES. ArchTests may assert Kernel contains no `AggregateRoot`/`ValueObject`/`IDomainEvent` and Core contains no `I*Service` ports.

Unchanged from ADR 0002:

- One deployable `Resys.Api`; twelve `Resys.Modules.*`; one PostgreSQL database with per-module schema/DbContext ownership; outbox without a broker; DI contracts + last-registered-wins extensions.

Modules may reference `Kernel`, `Core`, `Application`, and `Shared` only. Only `Resys.Api` references `Infrastructure` and `Hosting`.

Folders may be promoted to additional projects later via the checklist in `03/02-08` without another structural ADR unless a **new concern domain** appears (e.g. a broker).

## Consequences

**Positive**

- Clear one-way dependencies match Clean Architecture without 24 csproj files.
- Kernel purity is enforceable (no DDD types); Core is the single shared domain + commerce foundation modules already need.
- Old Shared pillars map 1:1 into folders (migration is mechanical).
- Future NuGet pack = promote folder to project when needed.
- Outbox, audit, OTel/Serilog become first-class instead of missing.

**Negative / risks**

- Diverges from ADR 0002’s project list and from docs that still say `Resys.Core` / `Resys.Infrastructure` until those files are updated.
- One large Infrastructure assembly can hide illegal folder dependencies without ArchTests.
- Extra rename churn for anyone already scripting against `Resys.Core` paths (none in production yet).

**Neutral**

- `old_references/Shared` stays until migration finishes (`05/04-01`), then is removed from the solution.

## Alternatives considered

1. **Keep ADR 0002’s two projects only** (`Resys.Core` + `Resys.Infrastructure`) — no Application layer; outbox/audit/OTel still lumped; rejected.
2. **24 fine-grained kit projects** — best packability, too much ceremony now; deferred as optional promotions.
3. **Mirror old Shared 6 pillars by name** (`Foundations`, `Governance`, …) — preserves legacy names, conflicts with Clean-Architecture vocabulary the team chose; rejected.
4. **Put DDD bases (`Entity`, `AggregateRoot`, `ValueObject`, `IDomainEvent`) in Kernel** — keeps Core thinner, but pollutes “pure primitives” with DDD vocabulary; modules need Core anyway for `Money`/ports. Rejected in favor of Core `Domain/`.

## References

- ADR 0002 — modular monolith (still binding for topology)
- `03/02-08` — Building Blocks tree and folder map
- `05/04-01` — migration from `old_references`
- Spec §5 solution layout (update in docs pass)
