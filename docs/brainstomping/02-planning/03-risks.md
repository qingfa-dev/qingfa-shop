# Risks

The resys-eshop risk register, scoring each identified risk and naming its mitigation and owner.

**Links:** [Roadmap](./01-roadmap.md) · [Backlog](./02-backlog.md) · [Constraints](../01-requirements/01-business/03-constraints.md) · [Planning index](./README.md) · [Spec](../11-spec/README.md)

---

## Purpose

This register records the risks that could materially delay or degrade resys-eshop, with the
mitigation that reduces each and the role accountable for it. Risks are tracked as probabilities,
not tasks: a mitigation is planned work that lowers the likelihood or impact, and its items live
in the backlog (`02/02`).

Owners are roles, not individuals, so accountability survives staffing changes.

---

## Scoring

| Dimension | Low | Medium | High |
|---|---|---|---|
| **Likelihood** | Unlikely in the cycle; no trigger observed. | Plausible; has happened on comparable projects. | Expected unless actively managed. |
| **Impact** | Absorbed inside a phase; no milestone slip. | Slips a milestone or forces a scope trade. | Threatens a release goal or a locked constraint. |

Priority for mitigation follows likelihood × impact. Any **High** impact risk is reviewed every
cycle regardless of likelihood.

---

## Risk Register

| ID | Risk | Likelihood | Impact | Mitigation | Owner |
|---|---|---|---|---|---|
| R-01 | **Payment-gateway variance** — Stripe and PayPal differ in authorization, capture, refund, webhook, and idempotency semantics, so one adapter works while the other silently diverges. | High | High | Define one `IPaymentGateway` contract and a shared conformance test suite both adapters must pass; model payment states generically; record gateway references and reconcile via webhooks; sandbox test plan in `06/02-01`; runbook `08/03-03`. | Payments lead |
| R-02 | **Tax-rule complexity** — jurisdiction hierarchy, inclusive vs exclusive tax, categories, and post-promotion computation interact and produce wrong totals or compliance gaps. | High | High | Keep the jurisdiction > region > city model explicit; compute tax as a separate component on post-promotion amounts; make resolution pluggable via `ITaxZoneResolver`; dedicated tax test plan `06/02-03`; document limits in `01/02-06` open questions. | Tax lead |
| R-03 | **Multi-store data isolation** — a query or endpoint missing store scope leaks or mutates another store's catalog, inventory, or orders. | Medium | High | Enforce store scope at the data layer (`Resys.Core`) rather than per-endpoint; require store scope on `/api/admin`; add cross-store isolation cases to integration and E2E tests; audit-log all admin mutations; review `03/03-02`. | Platform architect |
| R-04 | **i18n scope creep** — per-locale content, fallback, currency formatting, and admin translation tooling expand beyond the supported-locale model into a full localization effort. | High | Medium | Cap scope to the `Translatable` pattern and per-store supported locales (`01/03-04`); require default-locale fallback rather than complete coverage; treat RTL and translation-admin depth as `Could`; refresh `01/03-04` open questions when scope changes. | Stores lead |
| R-05 | **Nuxt major upgrades** — Nuxt 4.5.x, `@nuxtjs/i18n`, and PrimeVue 4 move independently, and a forced major upgrade breaks SSR, i18n, or admin components. | Medium | Medium | Pin Nuxt 4.5.x and Vue 3.5.43; keep dependencies in a renovate-style review cadence (`10/02`); isolate third-party usage behind `web/shared`; verify upgrades against the E2E suite before adoption. | Frontend lead |
| R-06 | **At-least-once event duplication** — outbox relay delivers an event more than once and consumers double-send email or double-apply a side effect. | Medium | Medium | Make every consumer idempotent with a deduplication key; relay marks delivered only after consumer acknowledgement; integration test restart-after-commit; document the event catalog in `03/01-03`. | Platform architect |
| R-07 | **Promotion interaction correctness** — stacked rules and actions, per-line clamping, and post-promotion tax produce discounts that are wrong or exploitable. | High | High | Fix and document the application order (`FR-PRM-004`); clamp per line and never let a line go below zero; promotions mutate orders only via adjustments; dedicated promotion test plan `06/02-02`; admin simulator before publish. | Promotions lead |
| R-08 | **Inventory oversell under concurrency** — two checkouts reserve the last unit, or cancellation and shipment race the ledger. | Medium | High | Reserve stock transactionally on confirm; keep `on_hand` vs `available` and the `StockMovement` ledger authoritative; enforce no-negative-stock except at backorder locations; concurrency cases in the inventory test plan `06/02-04`. | Inventory lead |
| R-09 | **Scope pressure toward CMS or recommendations** — stakeholders request excluded features and dilute Spree parity. | Medium | Medium | Hold the out-of-scope list in `01/01-01` and the no-CMS/no-GraphQL ADRs (`00/01-03`); route genuine changes through a superseding ADR; track requests in `10/01`. | Product owner |
| R-10 | **.NET 10 ecosystem maturity** — a library in the stack lags .NET 10 or an SDK update breaks EF Core, Npgsql, or Identity integration. | Low | Medium | Pin the .NET 10.0 LTS bandwidth and packages; run CI on every dependency bump; keep the dependency policy and upgrade path in `10/02`; prefer first-party Microsoft packages. | Backend lead |
| R-11 | **Storefront performance regression** — SSR rendering plus EF Core queries produce N+1 reads and miss the Core Web Vitals and latency budgets. | Medium | High | Set query guardrails and SSR budgets (`01/03-01`); cache public catalog reads; add performance gates to CI (`06/03-02`); review slow queries in the database runbook `08/03-02`. | Frontend lead |
| R-12 | **Test environment flakiness** — Testcontainers and Playwright suites are slow or unstable, eroding confidence in verification. | Medium | Low | Keep integration fixtures minimal and parallel-safe; quarantine flaky E2E cases with an owner; publish coverage and metrics per `06/03-01`; run the suite on a dedicated CI lane. | QA lead |
| R-13 | **Documentation drift** — code and the canonical docs diverge as implementation proceeds, defeating the docs-first contract. | Medium | Medium | Treat the spec as canonical and require an ADR to change a decision; update the owning area in the same change as the code; re-run link and placeholder checks in CI; log gaps in `10/01`. | Tech writer |

---

## Risk Themes

The register clusters into five themes, so a review can focus on a coherent area at a time.

- **Money movement** (R-01, R-07): every path that moves or discounts money must be proven
  against both gateways and against the promotion rules, because errors here are visible to
  customers and costly to reverse.
- **Correctness and compliance** (R-02, R-06): tax computation and event delivery are easy to
  get subtly wrong and hard to detect after the fact, so they get dedicated test plans and
  documented invariants.
- **Tenancy and data integrity** (R-03, R-08): store isolation and stock truth are the two
  places where a single missing guard corrupts data across stores or oversells inventory.
- **Front-end and localization** (R-04, R-05, R-11): the Nuxt apps carry upgrade, i18n, and
  performance risk simultaneously because they consume every public API.
- **Delivery and platform** (R-09, R-10, R-12, R-13): scope pressure, ecosystem maturity, test
  stability, and documentation drift are slower-moving risks that erode a release if ignored.

---

## Phase Mapping

Where each risk is most likely to bite, and where its mitigation is produced. Phases refer to
the roadmap (`02/01`).

| Risk | Most active phase | Mitigation produced in |
|---|---|---|
| R-01 | P3 | `03/04-05`, `06/02-01`, `08/03-03` |
| R-02 | P3 | `01/02-06`, `06/02-03` |
| R-03 | P1–P2 | `03/03-02`, integration and E2E suites |
| R-04 | P2, P6 | `01/03-04`, stores design (`03/03-03`) |
| R-05 | P6 | `10/02`, `web/shared` boundary |
| R-06 | P1 | `03/01-03`, outbox integration tests |
| R-07 | P4 | `06/02-02`, admin promotion simulator |
| R-08 | P4 | `06/02-04`, inventory ledger tests |
| R-09 | P0–P7 | `00/01-03`, `10/01` |
| R-10 | P1 | `10/02`, CI dependency checks |
| R-11 | P6 | `01/03-01`, `06/03-02` |
| R-12 | P3–P7 | `06/03-01`, dedicated CI lane |
| R-13 | P0–P7 | link and placeholder checks in CI, `10/01` |

---

## Assumptions

The register assumes the following hold. If an assumption fails, the linked risks rise in
likelihood and the owner re-scores them at the next review.

1. The team has prior Stripe and PayPal integration experience (R-01).
2. PostgreSQL 18 and .NET 10.0 LTS remain supported and stable through the cycle (R-10).
3. No external requirement forces tax filing or remittance beyond computing and recording tax
   (R-02).
4. One team owns both Nuxt front-ends, so shared-contract changes are coordinated (R-05, R-11).
5. No third-party extension is shipped in this cycle, so extension-contract stability is not a
   release blocker (R-03, R-06).

---

## Review Cadence

- The register is reviewed at every milestone boundary (M0–M7 in `02/01`).
- Any risk that becomes **High** likelihood or impact gets a named action item added to the
  backlog (`02/02`) before the next phase begins.
- A risk that materializes is recorded as an incident or debt item and its entry is re-scored;
  closed risks are removed only at a phase review.
- Owners report status in one line per risk at the review, referencing the mitigation above.

---

## Open Questions

1. The exact conformance suite proving R-01 is authored with the checkout test plan (`06/02-01`)
   and the gateway integration docs (`03/04-05`).
2. Whether R-04 warrants a hard supported-locale cap is resolved with the i18n requirements
   (`01/03-04`) and the stores module design (`03/03-03`).
3. The dependency review cadence behind R-05 and R-10 is finalized in `10/02`.
