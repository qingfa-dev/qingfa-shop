# Business Goals

Why resys-eshop exists, what success means, and what is deliberately out of scope.

**Links:** [Actors](./02-actors.md) · [Constraints](./03-constraints.md) · [Requirements index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

resys-eshop is a **Spree-aspired multi-store e-commerce platform** built on the .NET stack.
It gives merchants a storefront they can brand per store, a back-office to run commerce
operations, and a headless REST API that both front-ends consume over HTTP.

This document states the business intent behind the platform. It is the root of the
requirements area: functional requirements (planned `01/02-01` … `01/02-12`), non-functional
requirements (planned `01/03-01` … `01/03-04`), and user stories (planned `01/04-01` …
`01/04-03`) must trace back to a goal or success criterion stated here.

The platform is delivered **docs-first**. The immediate deliverable is an
implementation-grade documentation set; production code follows the plans derived from the
[design spec](../../11-spec/README.md).

---

## Business Goals

### G1 — Multi-store commerce from one deployment

Run many storefronts from a single deployable service. Every catalog, inventory, pricing,
and order record is scoped to a `Store`; the storefront resolves the store from the request
domain and the admin provides a store switcher. A merchant can open a second store without
standing up a second backend or database.

### G2 — Spree parity, minus CMS

Match the functional surface a team already knows from Spree so the domain mapping is 1:1:
catalog with master/variation products and option types, taxons, orders with a state
machine, shipping, payments, tax, promotions, inventory, returns, wishlists, reviews,
accounts, and i18n/multi-currency. The full module map is the spec's Appendix A.

The parity is explicit about its edge: **CMS is excluded**. Pages, page builders, blogs,
and content blocks are not part of the product. Merchants use the storefront's rendered
pages; they do not author content inside resys-eshop.

### G3 — Headless, contract-first API

The commerce domain is exposed only through a REST API — public `/api/v1` and admin
`/api/admin` — generated as OpenAPI and browsable in Scalar. Both front-ends are fully
decoupled HTTP clients, so a merchant can replace or extend a front-end without touching the
backend.

### G4 — One codebase, clean module boundaries

Deliver a modular monolith: one deployable .NET service internally split into module
projects (Catalog, Orders, Inventory, Shipping, Tax, Promotions, Payments, Returns,
Wishlists, Reviews, Accounts, Stores) with contracts registered in DI. Third parties add an
extension by implementing a contract — no source modification. This keeps operational
complexity low while preserving the seams to extract a module later if scale demands it.

### G5 — Documentation as a first-class asset

Produce an implementation-grade documentation set across `docs/00`–`docs/11` — small,
numbered, per-concern files with an index per area. A reader or agent loads one file at a
time; one spec remains the single source of truth.

---

## Success Criteria

The platform is successful when:

| # | Criterion | Evidence |
|---|---|---|
| S1 | A shopper can browse a store, add to cart, and complete a 3-step checkout (delivery → payment → confirm) with a real gateway. | Storefront E2E checkout (planned `06/02-01`). |
| S2 | An admin can create a product with variations, options, images, and taxons, and see it live on the storefront. | Admin E2E catalog flow (planned `06/01-04`). |
| S3 | An admin can process an order end to end: capture payment, fulfill, ship, refund, and accept a return. | Order lifecycle spec (planned `03/02-03`). |
| S4 | Two stores share one deployment and remain fully isolated in catalog, inventory, and orders. | Multi-store data doc (planned `03/03-02`). |
| S5 | Promotions and tax compute correctly and consistently, with totals always derived from source plus adjustments. | Promotion/tax engines (planned `03/02-04`, `03/02-05`). |
| S6 | Stock cannot go negative unless the location explicitly allows backorders; movements are ledgered. | Inventory model (planned `03/02-06`). |
| S7 | A store serves more than one locale and currency without forking the catalog. | i18n/money doc (planned `03/03-03`). |
| S8 | The documentation set passes its own checks: no placeholders, all relative links resolve, no contradictions across areas. | Spec §12 verification. |

---

## Non-Goals

The following are **explicitly excluded**. They are recorded here so no functional
requirement re-introduces them, and so an ADR is required to reverse them (see
`00/01-03`).

- **CMS** — no pages, page builder, blog, or content blocks.
- **GraphQL** — REST only; `/api/v1` and `/api/admin`.
- **Blazor** — front-ends are Nuxt 4 (Vue 3) only.
- **Recommendation / personalization engine** — "resys" is only the project name; there is
  no recommender.
- **Native mobile apps** — the storefront is responsive web; no iOS/Android clients.
- **Microservices** — the backend is a modular monolith, not a service mesh.
- **Production code in this phase** — code follows the implementation plans; this effort
  produces documentation.

---

## Related Goals in Other Areas

- Planning and sequencing: `02/01` (roadmap), `02/02` (backlog), `02/03` (risks).
- Functional decomposition of goals G1–G5: `01/02-01` … `01/02-12`.
- Quality attributes backing the success criteria: `01/03-01` … `01/03-04`.
- Cross-cutting decisions: `00/01-01` (`ADR 0001 — stack`), `00/01-02`
  (`ADR 0002 — modular monolith`), `00/01-03` (`ADR 0003 — exclusions`), `00/01-04`
  (`ADR 0004 — headless + JWT`).

## Open Questions

1. Which store is the canonical default for a fresh deployment is decided with the seed
   dataset in `05/01-04`; it does not affect the goals above.
2. The exact audit-log retention window is a policy detail owned by `08/03-01`; it is
   recorded here only as an operational expectation.
