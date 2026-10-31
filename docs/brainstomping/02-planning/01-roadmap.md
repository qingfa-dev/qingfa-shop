# Roadmap

The sequenced delivery plan from documentation through hardening, with milestones and
exit criteria per phase.

**Links:** [Backlog](./02-backlog.md) · [Risks](./03-risks.md) · [Requirements](../01-requirements/README.md) · [Spec](../11-spec/README.md)

---

## Purpose

This roadmap orders the work implied by the requirements (`01`) and realized by the
architecture (`03`) into eight phases. It answers *in what order* capabilities land, not *how*
each is built (the architecture's concern) and not the full work inventory (the backlog,
`02/02`).

Phases are sequential by dependency: each phase assumes the exit criteria of its predecessors.
Within a phase, work may proceed in parallel across modules. Durations are relative; calendar
targets are assigned once the documentation set is complete (see `## Open Questions`). Phase
scope below names the owning requirements so progress can be measured against them.

---

## Phases

| Phase | Theme | Depends on | Milestone |
|---|---|---|---|
| P0 | Documentation | — | M0 |
| P1 | Platform core | P0 | M1 |
| P2 | Catalog and orders | P1 | M2 |
| P3 | Checkout, shipping, tax, payments | P2 | M3 |
| P4 | Promotions, inventory, returns | P3 | M4 |
| P5 | Admin | P4 | M5 |
| P6 | Storefront | P3, P4 | M6 |
| P7 | Hardening | P5, P6 | M7 |

### P0 — Documentation

- **Objective:** lock the canonical spec and produce the implementation-grade `00`–`10`
  documentation set.
- **Scope:** governance (`00`), requirements (`01`), planning (`02`), architecture (`03`),
  design (`04`), development (`05`), verification (`06`), release (`07`), operations (`08`),
  user guides (`09`), maintenance (`10`).
- **Deliverables:** the numbered files for every area with per-area `README.md` indexes; the
  spec's §12 verification passes (consistency, links, zero placeholders, API parity).

### P1 — Platform core

- **Objective:** an executable skeleton plus the cross-cutting primitives every module depends
  on.
- **Scope:** `Resys.Core` (`Money`, identities, store scoping, domain events, extension
  contracts); EF Core 10 + Npgsql on PostgreSQL 18; module projects and DI contracts;
  ASP.NET Identity + JWT Bearer; outbox relay; `/health/live` and `/health/ready`; OpenAPI +
  Scalar; Serilog/OpenTelemetry; options and per-environment configuration; seed data.
- **Deliverables:** solution layout from spec §5, a first migration, an authenticated
  `/api/admin` endpoint, and the outbox dispatcher (`03/01-03`, `03/04-03`).

### P2 — Catalog and orders

- **Objective:** the product model and the order aggregate with computed totals.
- **Scope:** product masters, variants, option types/values, taxonomy, images, per-store price
  sets and currencies, public catalog API; order aggregate and cart, line items, addresses,
  state machine, adjustments, computed totals, outbox events, cancellation, and store/currency
  integrity (`01/02-01`, `01/02-02`).
- **Deliverables:** `/api/v1` catalog reads, `/api/admin` catalog CRUD, cart endpoints, and the
  order state machine with `OrderPlaced` relayed via the outbox.

### P3 — Checkout, shipping, tax, payments

- **Objective:** a complete, server-computed purchase path with money movement.
- **Scope:** three-step checkout with step gating, guest checkout, stock reservation on confirm,
  idempotency and double-charge protection, coupon entry; shipping methods, zones, pluggable
  calculators, rate quoting and shipping adjustments; tax jurisdictions, rates, inclusive vs
  exclusive, post-promotion computation; payment methods, `IPaymentGateway`, Stripe and PayPal
  adapters, payment lifecycle, refunds and voids, store credit (`01/02-03`–`01/02-06`).
- **Deliverables:** `/api/v1` checkout endpoints, gateway adapters in sandbox, and checkout
  integration tests covering failure and retry.

### P4 — Promotions, inventory, returns

- **Objective:** live commercial mechanics and stock truth.
- **Scope:** promotion definition, rules, actions, application order, per-line clamping, coupon
  codes, usage limits; stock locations, `StockItem` per (variant, location), `on_hand` vs
  `available`, `StockMovement` ledger, reservations, transfers, backorder policy; return
  authorization, return items and reasons, reception and inspection, restocking, refund vs
  store credit, exchanges (`01/02-07`–`01/02-09`).
- **Deliverables:** promotion engine, inventory ledger, and returns flow with admin and
  customer endpoints, plus their test plans from `06/02`.

### P5 — Admin

- **Objective:** a back-office able to run the business end to end.
- **Scope:** `web/admin` Nuxt 4 + PrimeVue; multi-store switcher; catalog CRUD; order
  fulfill/ship/refund/return; inventory, shipping, tax, promotion and payment-method
  configuration; store settings; users and roles; review moderation; reports; the admin audit
  log (`03/04-02`, `03/04-03`).
- **Deliverables:** the admin app calling `/api/admin` over HTTP with JWT in an httpOnly
  cookie, and audit rows written for every mutation.

### P6 — Storefront

- **Objective:** the public, SEO-ready shopping experience.
- **Scope:** `web/storefront` Nuxt 4 SSR with Tailwind CSS v4; home, category listing with
  facets and pagination, product detail, search, cart, three-step checkout embedding Stripe
  Elements and the PayPal JS SDK, account (orders, addresses, wishlists, profile), review
  submission, per-store locale and currency (`01/02-11`, `01/02-12`).
- **Deliverables:** the storefront app as a pure HTTP client of `/api/v1`, with i18n and
  runtime branding per store.

### P7 — Hardening

- **Objective:** release readiness for security, performance, and operations.
- **Scope:** CSRF, CORS allow-list, rate limiting, PII redaction and audit; query guardrails
  and caching; dashboards and alerts; runbooks for the database, gateway, and double-charge;
  backup/restore drills; load testing; release and rollback; dependency policy
  (`01/03-01`–`01/03-03`, `08/02`, `08/03`).
- **Deliverables:** verified non-functional budgets, exercised on-call runbooks, a rehearsed
  rollback, and the release checklist from `07`.

---

## Milestones

| ID | Name | Phase | Definition of done |
|---|---|---|---|
| M0 | Documentation baseline | P0 | Spec approved; areas `00`–`10` written; placeholder scan clean; link check clean. |
| M1 | Walking skeleton | P1 | Solution builds and migrates; Identity issues a token; outbox relay proven. |
| M2 | Sellable catalog and cart | P2 | Catalog and order APIs satisfy `01/02-01` and `01/02-02`. |
| M3 | Paid order in sandbox | P3 | Both gateways complete a payment; checkout is idempotent. |
| M4 | Promotion, inventory, returns complete | P4 | Engine scenarios, ledger, and return flow verified. |
| M5 | Operations-ready admin | P5 | Full order lifecycle runnable from the admin UI. |
| M6 | Public storefront | P6 | Customer purchase flow works and performance budgets are met. |
| M7 | Release candidate | P7 | NFRs verified; runbooks exercised; rollback rehearsed. |

---

## Exit Criteria per phase

### P0 exit

- Every area `00`–`10` has its numbered files and a resolving `README.md` index.
- The placeholder scan is clean and the link check reports no broken links.
- API parity holds between `09/03` and `03/04` (spec §12).

### P1 exit

- `dotnet build` succeeds and migrations apply to a clean PostgreSQL 18 instance.
- A token issued by Identity authorizes an `/api/admin` request with the `admin` role.
- `Money` cannot be constructed from mismatched decimal and currency inputs.
- An outbox row written in the same transaction as a test state change is relayed after
  commit, including across a simulated restart.
- `/health/live` and `/health/ready` respond as specified in `01/03-03`.

### P2 exit

- Product masters, variants, options, taxonomy, images, and per-store prices are manageable
  through `/api/admin` (`01/02-01`).
- A cart accumulates line items, snapshots addresses, and computes totals from source plus
  adjustments without a cached total column (`01/02-02`).
- The state machine rejects undefined transitions and emits events only through the outbox.
- A store cannot read another store's catalog or orders.

### P3 exit

- A guest and a signed-in customer complete `cart → awaiting_payment → awaiting_fulfillment`
  with both Stripe and PayPal in sandbox.
- Repeating a confirm does not create a second charge (`FR-CHK-010`).
- Shipping is recorded as an adjustment and tax is recorded as a separate component computed
  on post-promotion amounts.
- A refund and a void against each gateway are recorded with the returning reference.

### P4 exit

- Promotion scenarios apply in the documented order and never drive a line below zero
  (`FR-PRM-004`, `FR-PRM-005`).
- Promotions modify orders only through adjustments (`FR-PRM-006`).
- Concurrent checkout cannot oversell available stock except where a location allows
  backorders (`FR-INV-007`).
- A return restocks accepted items and refunds through the gateway that captured the payment.

### P5 exit

- An operator creates a product, prices it, publishes it, ships an order, and processes a
  return using only the admin UI.
- The store switcher scopes every list and mutation to the selected store.
- Every admin mutation writes an audit row, and an operator for store A cannot act on store B.

### P6 exit

- A guest and a customer complete browse, search, cart, and checkout on the storefront.
- Wishlists, reviews, order history, and address management work for a signed-in customer.
- Core Web Vitals and SSR budgets in `01/03-01` are met, and all supported locales render with
  default-locale fallback.

### P7 exit

- Availability and performance NFRs in `01/03-01` and `01/03-03` are verified under load.
- Runbooks for database failover, gateway outage, and double-charge are exercised.
- A rollback rehearsal restores the previous release without data loss beyond the stated RPO.
- Logs and traces contain no PAN or PII (`01/03-02`).

---

## Open Questions

1. Calendar targets per phase are assigned when the documentation set is complete; this file
   fixes order and dependencies, not dates.
2. Whether P6 starts in parallel with P4/P5 is an execution decision once the module contract
   surface (`03/02-02`) is frozen; the milestone dependency above already permits it.
3. The depth of reporting in P5 is bounded by `01/04-03` and finalized with the operations
   area (`08`).
