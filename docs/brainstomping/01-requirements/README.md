# 01 — Requirements

Business context, functional and non-functional requirements, and user stories for
resys-eshop.

**Links:** [Goals](./01-business/01-goals.md) · [Actors](./01-business/02-actors.md) · [Constraints](./01-business/03-constraints.md) · [Spec](../11-spec/README.md)

---

## Purpose

This area captures **what resys-eshop must achieve and must do**. It is split into four
subfolders, each one concern:

- **`01-business/`** — the business context: goals, actors, and constraints.
- **`02-functional/`** — what the system must do, one file per module.
- **`03-nonfunctional/`** — quality attributes: performance, security, availability, i18n.
- **`04-user-stories/`** — feature descriptions from each actor's perspective.

Everything here traces back to the [design spec](../11-spec/README.md),
which is the single source of truth. The functional and non-functional files derive from the
locked decisions recorded in the spec's §2 and the governance ADRs (`00/01-01` …
`00/01-04`).

---

## Business Context

| File | Purpose | Identifier |
|---|---|---|
| [01-business/01-goals.md](./01-business/01-goals.md) | Why the platform exists: multi-store commerce, Spree parity minus CMS, success criteria, and explicit non-goals (CMS, GraphQL, Blazor, recommendation engine, mobile apps). | `01/01-01` |
| [01-business/02-actors.md](./01-business/02-actors.md) | The actors — Customer, Store Admin, Platform Admin, Guest, and Payment/Shipping Providers — with responsibilities and the surfaces they use. | `01/01-02` |
| [01-business/03-constraints.md](./01-business/03-constraints.md) | The locked technical versions, payment/regulatory limits (no PAN storage), operational expectations, and documentation rules. | `01/01-03` |

---

## Functional Requirements

One file per module, following the module map in the spec's Appendix A. The module numbering
is canonical: 01-catalog, 02-orders, 03-checkout, 04-shipping, 05-payments, 06-tax,
07-promotions, 08-inventory, 09-returns, 10-accounts, 11-wishlists-reviews, 12-stores-i18n.

| File | Module | Identifier |
|---|---|---|
| [01-catalog.md](./02-functional/01-catalog.md) | Catalog | `01/02-01` |
| [02-orders.md](./02-functional/02-orders.md) | Orders | `01/02-02` |
| [03-checkout.md](./02-functional/03-checkout.md) | Checkout | `01/02-03` |
| [04-shipping.md](./02-functional/04-shipping.md) | Shipping | `01/02-04` |
| [05-payments.md](./02-functional/05-payments.md) | Payments | `01/02-05` |
| [06-tax.md](./02-functional/06-tax.md) | Tax | `01/02-06` |
| [07-promotions.md](./02-functional/07-promotions.md) | Promotions | `01/02-07` |
| [08-inventory.md](./02-functional/08-inventory.md) | Inventory | `01/02-08` |
| [09-returns.md](./02-functional/09-returns.md) | Returns | `01/02-09` |
| [10-accounts.md](./02-functional/10-accounts.md) | Accounts | `01/02-10` |
| [11-wishlists-reviews.md](./02-functional/11-wishlists-reviews.md) | Wishlists, Reviews | `01/02-11` |
| [12-stores-i18n.md](./02-functional/12-stores-i18n.md) | Stores, i18n | `01/02-12` |

---

## Non-Functional Requirements

Quality attributes that apply across modules.

| File | Attribute | Identifier |
|---|---|---|
| [01-performance.md](./03-nonfunctional/01-performance.md) | Performance | `01/03-01` |
| [02-security.md](./03-nonfunctional/02-security.md) | Security | `01/03-02` |
| [03-availability.md](./03-nonfunctional/03-availability.md) | Availability | `01/03-03` |
| [04-i18n.md](./03-nonfunctional/04-i18n.md) | i18n | `01/03-04` |

---

## User Stories

Feature narratives per actor group.

| File | Narrative | Identifier |
|---|---|---|
| [01-browse-buy.md](./04-user-stories/01-browse-buy.md) | Browse and buy | `01/04-01` |
| [02-account.md](./04-user-stories/02-account.md) | Account | `01/04-02` |
| [03-admin.md](./04-user-stories/03-admin.md) | Admin | `01/04-03` |

---

## How to Use This Area

1. Start with **business context** (`01/01-01` … `01/01-03`) to understand intent and limits.
2. Open the **functional** file for the module in question to see required behavior.
3. Check the relevant **non-functional** file for the quality bar that behavior must meet.
4. See the **user stories** for concrete end-to-end flows.
5. When a requirement needs justification, cite the spec or the governing ADR (`00/01-0N`)
   rather than restating the decision.

## Related Areas

- `02/01` (roadmap) sequences the work these requirements imply.
- `03` (architecture) realizes the requirements; module contracts live in `03/02-02`.
- `06` (verification) proves the requirements were met.
- `09/03` (API reference) documents the REST contracts that deliver the functional
  requirements.

## Open Questions

None at the area level. Any unresolved detail is stated within the specific business file
that owns it, under its own `## Open Questions` section.
