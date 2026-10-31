# 11 — Specification

The canonical design specification for resys-eshop and the implementation plan that produced the
numbered `docs/00`–`docs/10` documentation set.

- **Date:** 2026-09-23
- **Status:** Approved (brainstorming complete)
- **Path:** Architectural — new project, docs-first deliverable

**Links:** [Repository README](../../README.md) · [Documentation map](../../README.md#documentation-map)

---

## Design specification (`01-design/`)

The single source of truth for the platform. Cite a section as `11/01-NN`, and the original spec
section numbers (§1–§13, Appendix A) map to the files below.

| File | Section | Purpose |
|---|---|---|
| [01-purpose-scope.md](./01-design/01-purpose-scope.md) | §1 | What resys-eshop is, and the scope of the design spec and the documentation deliverable. |
| [02-decisions.md](./01-design/02-decisions.md) | §2 | The locked architectural and scope decisions. |
| [03-system-overview.md](./01-design/03-system-overview.md) | §3 | How the modular-monolith service and the two Nuxt front-ends fit together. |
| [04-tech-stack.md](./01-design/04-tech-stack.md) | §4 | The researched backend, frontend, storefront, and admin technology choices. |
| [05-solution-layout.md](./01-design/05-solution-layout.md) | §5 | The repository and solution project layout. |
| [06-domain-data.md](./01-design/06-domain-data.md) | §6 | Core domain concepts and where the data model is specified. |
| [07-api-surface.md](./01-design/07-api-surface.md) | §7 | The public and admin REST surfaces, authentication, and security. |
| [08-extension-architecture.md](./01-design/08-extension-architecture.md) | §8 | How third parties extend modules through DI contracts. |
| [09-frontends.md](./01-design/09-frontends.md) | §9 | The storefront, admin, and shared Nuxt applications. |
| [10-cross-cutting.md](./01-design/10-cross-cutting.md) | §10 | Events, files, email, observability, security, and configuration. |
| [11-documentation-map.md](./01-design/11-documentation-map.md) | §11 | The deliverable structure and the conventions for every doc file. |
| [12-verification.md](./01-design/12-verification.md) | §12 | How the docs-only deliverable is verified. |
| [13-open-questions.md](./01-design/13-open-questions.md) | §13 | Explicit open questions resolved during implementation planning. |
| [14-module-map.md](./01-design/14-module-map.md) | Appendix A | Maps Spree concepts to resys-eshop modules. |

## Implementation plan (`02-plan/`)

The task-by-task plan that sequenced the numbered areas. Cite a file as `11/02-NN`.

| File | Scope | Purpose |
|---|---|---|
| [01-global-constraints.md](./02-plan/01-global-constraints.md) | Overview | The plan's goal plus the project-wide constraints every task inherits. |
| [02-verification-helpers.md](./02-plan/02-verification-helpers.md) | Checks | The placeholder-scan and link-check commands used to verify the docs deliverable. |
| [03-governance.md](./02-plan/03-governance.md) | Tasks 1–2 | The ADRs and contribution policies. |
| [04-requirements.md](./02-plan/04-requirements.md) | Tasks 3–8 | Business context, functional and non-functional requirements, and user stories. |
| [05-planning.md](./02-plan/05-planning.md) | Task 9 | The roadmap, backlog, and risk register. |
| [06-architecture.md](./02-plan/06-architecture.md) | Tasks 10–15 | System overview, component engines, data, and integration. |
| [07-design.md](./02-plan/07-design.md) | Tasks 16–17 | Storefront and admin design. |
| [08-development.md](./02-plan/08-development.md) | Tasks 18–20 | Setup, conventions, and workflow. |
| [09-verification.md](./02-plan/09-verification.md) | Tasks 21–23 | Test strategy, test plans, and quality metrics. |
| [10-release.md](./02-plan/10-release.md) | Task 24 | Versioning, release notes, deployment, and rollback. |
| [11-operations.md](./02-plan/11-operations.md) | Tasks 25–27 | Infrastructure, monitoring, and incident response. |
| [12-user-docs.md](./02-plan/12-user-docs.md) | Tasks 28–30 | Admin guide, customer guide, and API reference. |
| [13-maintenance.md](./02-plan/13-maintenance.md) | Task 31 | Technical debt, dependency policy, and performance tuning. |
| [14-root-readme-audit.md](./02-plan/14-root-readme-audit.md) | Task 32 | The root README, the cross-area audit, and the author self-review notes. |

## Scan policy

`docs/11-spec/` is the spec and plan of record, not part of the deliverable. It records its own
placeholder-scan commands (which contain the literal scan tokens) and illustrative path examples,
so the deliverable placeholder and link scans cover `docs/00`–`docs/10` only and skip this area
(see `05/03-03`).
