# 00 — Governance

The project's binding decisions and working policies: architectural decision records
(ADRs) and the policies that govern contribution and review.

**Links:** [Design spec](../11-spec/README.md)

---

## Purpose

This area is the **decision record** for resys-eshop. Every cross-cutting choice that
later documentation depends on is captured here as an ADR, so that areas `01`–`10` can
cite one stable source instead of restating or re-arguing a decision.

The area holds two concerns:

- **`01-adr/`** — Architectural Decision Records. Immutable once accepted; a reversal is
  a new ADR that supersedes the old one.
- **`02-policies/`** — Working policies for contributors, conduct, and review.

The canonical design lives in the [design spec](../11-spec/README.md).
Where an ADR and the spec disagree, the spec is corrected or the ADR is superseded — they
must never silently diverge.

## How to use this area

1. Read the index below and open the ADR that touches your concern.
2. When a doc needs to justify a decision, cite the ADR by its identifier (for example
   `00/01-01`) rather than paraphrasing it.
3. When proposing a change to a locked decision, write a **new** ADR with
   `Status: Superseded by ADR NNNN` added to the old one; do not edit accepted decisions
   in place.

## When to add an ADR

Write a new ADR when a decision is **architectural** — it affects multiple modules or
areas, constrains future work, and is expensive to reverse. Examples: a runtime or
framework choice, a topology or persistence decision, an API style, or a security
posture. Do **not** write an ADR for a local implementation detail; that belongs in the
relevant area (`03`–`10`).

Each ADR uses the same sections so they can be read and diffed uniformly:

- `Status` — `Accepted`, or `Superseded by ADR NNNN`.
- `Context` — the forces and constraints that made a decision necessary.
- `Decision` — what was chosen, stated so it can be enforced.
- `Consequences` — positive, negative/costs, and neutral outcomes.
- `Alternatives Considered` — the realistic options and why each was not chosen.
- `Open Questions` — stated explicitly when something remains undecided.
- `Links` — related files and spec sections.

## ADRs

| File | Purpose | Links |
|---|---|---|
| [01-adr/01-stack.md](./01-adr/01-stack.md) | Locked runtime, API, persistence, and front-end stack: .NET 10.0 LTS (10.0.12), Minimal API (REST), EF Core 10 / Npgsql / PostgreSQL 18, two Nuxt 4 (Vue 3.5.43) apps. | `00/01-01` |
| [01-adr/02-modular-monolith.md](./01-adr/02-modular-monolith.md) | One deployable .NET service split into internal module projects with DI contracts; explicitly not microservices. | `00/01-02` |
| [01-adr/03-no-cms-no-gql.md](./01-adr/03-no-cms-no-gql.md) | Explicit exclusions: no CMS, no GraphQL, no Blazor, no recommendation engine. | `00/01-03` |
| [01-adr/04-headless-jwt.md](./01-adr/04-headless-jwt.md) | Headless API; admin consumes `/api/admin` over HTTP; JWT Bearer with the admin token in an httpOnly cookie; roles `customer`/`admin` plus per-store assignment. | `00/01-04` |
| [01-adr/05-building-blocks.md](./01-adr/05-building-blocks.md) | **Proposed.** Six-project `BuildingBlocks.*` kit (Kernel, Core, Application, Shared, Infrastructure, Hosting); supersedes ADR 0002 supporting-project list when accepted. | `00/01-05` |

## Policies

The policy files are part of this area and are linked from this index.

| File | Purpose | Links |
|---|---|---|
| [02-policies/01-contributing.md](./02-policies/01-contributing.md) | Contribution workflow: setup, branching, commit conventions, pull requests, definition of done, documentation rules. | `00/02-01` |
| [02-policies/02-code-of-conduct.md](./02-policies/02-code-of-conduct.md) | Community standards, scope, and enforcement (Contributor Covenant). | `00/02-02` |
| [02-policies/03-review-policy.md](./02-policies/03-review-policy.md) | Reviewer responsibilities, required approvals, review checklist, verification evidence. | `00/02-03` |

## Decision summary

The locked decisions that shape every other area, each traceable to an ADR:

| # | Decision | Choice | ADR |
|---|---|---|---|
| 1 | Front-ends | Two Nuxt 4 (Vue 3) apps: `web/storefront` (SSR) + `web/admin` (PrimeVue). No Blazor. | `00/01-01` |
| 2 | API | .NET Minimal API, REST only. No GraphQL. | `00/01-01`, `00/01-03` |
| 3 | Back-end topology | Modular monolith, headless; admin consumes `/api/admin` over HTTP. | `00/01-02` |
| 4 | Runtime | .NET 10 LTS (10.0.12, supported to 2028-11-14). | `00/01-01` |
| 5 | ORM / DB | EF Core 10 + Npgsql / PostgreSQL 18. | `00/01-01` |
| 6 | Feature scope | Full Spree parity, minus CMS. | `00/01-03` |
| 7 | Authorization | Roles `customer` and `admin`; per-store admin assignment. | `00/01-04` |
| 8 | Recommendation engine | Excluded. | `00/01-03` |
| 9 | Shared kit | Six `BuildingBlocks.*` projects; **Kernel** = pure primitives (no DDD), **Core** = shared domain (DDD bases) + commerce + ports; supersedes ADR 0002 supporting-project list when accepted. | `00/01-05` (Proposed) |

## Conventions for this area

Every governance file follows the project-wide documentation format:

- Starts with a title, a one-line purpose, and a `**Links:**` block to related files and
  the spec.
- One concern per file, 100–300 lines.
- No placeholders. If a detail is genuinely undecided, it appears under an explicit
  `## Open Questions` section, never as a placeholder token.

## Related areas

- Area `01` (requirements), area `02` (planning), and area `03` (architecture) consume the
  decisions above; the spec's §11 documentation map coordinates all areas.
- The spec's §13 open questions are resolved in areas `03`, `04`, and `05`; they do not
  alter any ADR here.

## Links

- [Design spec](../11-spec/README.md) — the single source
  of truth for the system.
- [ADR 0001 — Application stack](./01-adr/01-stack.md)
- [ADR 0002 — Modular monolith](./01-adr/02-modular-monolith.md)
- [ADR 0003 — No CMS, no GraphQL](./01-adr/03-no-cms-no-gql.md)
- [ADR 0004 — Headless API and JWT auth](./01-adr/04-headless-jwt.md)
- [ADR 0005 — Building Blocks kit](./01-adr/05-building-blocks.md) (Proposed)
