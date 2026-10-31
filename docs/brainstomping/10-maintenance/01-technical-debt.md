# Technical Debt

The resys-eshop debt register: every known trade-off, its impact, the remedy that retires it, and the risk it sits under.

**Links:** [Dependency policy](./02-dependency-policy.md) · [Performance tuning](./03-performance-tuning.md) · [Risks](../02-planning/03-risks.md) · [Maintenance index](./README.md) · [Spec](../11-spec/README.md)

---

## Purpose

Technical debt is a **realized trade-off**, not an uncertainty. A risk (`02/03`) is something that
might happen; a debt item is something the design already accepts — a simpler boundary, a narrower
scope, a deferred optimization — that carries a known cost. This register makes those costs explicit
so they are managed deliberately instead of discovered during an incident.

The guiding rule: **a debt item is only real if it names its impact and the remedy that closes it.**
An item without a remedy is either a requirement gap (goes to `01`) or a risk (goes to `02/03`).

This file is the sole owner of the debt identifiers `TD-01`…`TD-11`. Risks own `R-01`…`R-13` in
`02/03`; the two registers cross-reference but never duplicate.

---

## Register Conventions

- **ID** — stable `TD-NN`. An ID is never reused after an item is retired.
- **Area** — the module, concern, or cross-cutting surface the debt lives in.
- **Impact** — what the debt costs if left alone: a correctness exposure, a performance ceiling, a
  maintenance tax, or a scope constraint. Impact is written as a consequence, not a feeling.
- **Proposed remedy** — the concrete change that retires the item, with the owning document or
  mechanism cited as plain text (`AA/BB-CC`).
- **Linked risk** — the register entry in `02/03` this debt can cause or exacerbate, or `—` when the
  debt is a pure capability trade-off with no matching risk.

The register is seeded from the locked decisions in the [spec](../11-spec/README.md)
and the risks in `02/03`. Items are added from milestone reviews, incident postmortems (`08/03-01`),
and deferred findings recorded in task reports.

---

## Debt Register

| ID | Area | Description | Impact | Proposed remedy | Linked risk |
|---|---|---|---|---|---|
| TD-01 | Module boundaries | Modules are an in-process modular monolith (`03/02-01`): a missed reference can reach another module's entities or services directly instead of through its contract. | A cross-module coupling that compiles and ships, eroding the boundary until a module cannot be changed or tested in isolation. | Enforce internal visibility and add architecture tests that fail on cross-module references; keep the contract and folder rules in `03/02-01` and `05/02-02`; review new references in PR. | R-03 |
| TD-02 | Payment gateways | `IPaymentGateway` (`03/04-05`) normalizes Stripe and PayPal, but provider semantics (authorize vs capture, webhook shape, idempotency keys, refund timing) do not map one-to-one. | Adapter code accumulates provider-specific conditionals that a single contract test cannot cover, so one gateway can diverge silently. | Maintain one gateway contract plus a shared conformance suite every adapter must pass (`06/02-01`); keep provider quirks in the adapter, never in checkout; model payment states generically. | R-01 |
| TD-03 | i18n coverage | Translations cover `Translatable` catalog text only (`03/03-03`), with default-locale fallback and no full localization of every UI string, email, or admin surface. | Some locales render partially translated content, and translation depth is uneven across the storefront and admin. | Cap the promise to the supported-locale model in `01/03-04`; audit missing translations and render the fallback explicitly; treat RTL and translation-admin depth as `Could` until re-scoped. | R-04 |
| TD-04 | Tax engine | The jurisdiction model is limited to country > region > city with inclusive/exclusive handling and category interaction (`03/02-05`); finer sub-jurisdictions or compound rates are not modeled. | A jurisdiction whose rules fall outside the hierarchy computes tax through an override or not at all, which is a compliance exposure. | Keep resolution behind `ITaxZoneResolver` so a deeper hierarchy is additive; document the modeled limits in `03/02-05`; cover edge jurisdictions in the tax test plan `06/02-03`. | R-02 |
| TD-05 | Promotions | Stacked rules and actions, per-line clamping, and a documented application order (`03/02-04`, `FR-PRM-004`) make the engine the most interaction-heavy component. | Rule ordering or clamping defects produce wrong discounts or exploitable combinations that surface only in production order data. | Freeze the application order, clamp per line at zero, mutate orders only through adjustments; exercise combinations in the promotion test plan `06/02-02`; require the admin simulator before publish. | R-07 |
| TD-06 | Domain events | The outbox guarantees at-least-once delivery (`03/01-03`), which pushes deduplication and idempotency onto every consumer. | A consumer that is not idempotent double-sends email or double-applies a side effect on a relay retry. | Require a deduplication key and idempotent handling per consumer; acknowledge only after the side effect commits; keep the event catalog in `03/01-03`; add restart-after-commit integration tests. | R-06 |
| TD-07 | SSR and caching | Public catalog reads are cached at the edge with per-store keys and a 60 s invalidation window (`NFR-PERF-008`); cache correctness depends on every catalog mutation emitting the right invalidation. | A missed invalidation serves stale or cross-store content until expiry, which is a correctness and trust problem, not just a latency one. | Keep invalidation a first-class step of catalog writes; assert headers and re-read after mutation in the integration suite (`06/01-03`); keep cache termination behind `03/01-02` and sizing in `08/04-01`. | R-11 |
| TD-08 | Documentation | The docs-first contract (`00/02-01`) can drift from code once implementation outpaces the written decision. | A canonical doc that no longer matches the code misleads every later change, defeating the contract that the spec is authoritative. | Change code and the owning doc in the same pull request; require an ADR to change a locked decision; rely on the link and forbidden-token checks in `05/03-03`; log gaps here. | R-13 |
| TD-09 | Test suite | Testcontainers integration tests and the Playwright E2E suite are slower and more environment-sensitive than unit tests, and can flake. | Flaky gates erode confidence and tempt the team to ignore a red lane, weakening every other guarantee. | Keep fixtures minimal and parallel-safe; quarantine with a named owner, never silently retry; track flakiness in `06/03-01`; run on a dedicated lane. | R-12 |
| TD-10 | Scope boundaries | The platform deliberately excludes CMS, GraphQL, Blazor, and recommendations (`00/01-03`); stakeholder requests will keep arriving. | Unscheduled additions dilute Spree parity and consume the capacity the in-scope modules need. | Hold the out-of-scope list in `01/01-01`; route a genuine change through a superseding ADR; track requests here and in the backlog `02/02`. | R-09 |
| TD-11 | Store scoping | Store isolation is enforced at the data layer (`03/03-02`) rather than per endpoint, so a new query that omits the store predicate can silently cross stores. | A single missing predicate leaks or mutates another store's data, the highest-severity data-integrity failure. | Make store scope a required query filter with a guard that fails closed; add cross-store isolation cases to integration and E2E; keep the rule in `03/03-02`. | R-03 |

---

## Priority

Priority orders the register for planning; it is not a severity of the impact column. It is re-scored
at each milestone review.

| Priority | Meaning | Action |
|---|---|---|
| **P1** | The debt can cause a correctness, money, or data-isolation failure. | Schedule in the current or next phase; do not let it cross a release without a mitigation. |
| **P2** | The debt caps performance, maintainability, or test confidence. | Plan into a phase with a named owner; track in the backlog `02/02`. |
| **P3** | The debt is a deliberate scope boundary or a low-cost tax. | Revisit only when the scope or the assumption behind it changes. |

Current reading: TD-01, TD-05, TD-07, and TD-11 are **P1** because they touch boundaries, money, and
store data; TD-02, TD-04, TD-06, and TD-09 are **P2**; TD-03, TD-08, and TD-10 are **P3** while the
locked scope holds.

---

## Lifetime

1. **Intake.** A milestone review, an incident postmortem (`08/03-01`), or a deferred task finding
   adds an item with an ID, an impact, and a remedy. An item with no remedy is redirected to `01` or
   `02/03`.
2. **Triage.** The owner sets priority and links the risk it sits under. A P1 item that is not
   scheduled is written as an open question until it is.
3. **Remedy.** The change lands with the test or evidence that proves the cost is gone — an
   architecture test, a conformance suite, a budget assertion, or an isolation case.
4. **Retirement.** A retired item is marked closed with the commit or document that closed it and is
   removed at a phase review; the ID is not reused.

Debt is never closed by editing this file alone. A register entry that says "fixed" without the
proving change is reopened.

---

## Relationship to Risks and Backlog

- A **risk** (`02/03`) is an uncertainty scored by likelihood and impact; a **debt item** is a
  decision already made. TD-01, TD-03, TD-04, TD-05, TD-06, TD-07, TD-09, and TD-11 are the realized
  costs behind R-03, R-04, R-02, R-07, R-06, R-11, R-12, and R-03 respectively.
- A debt item whose remedy is scheduled becomes a backlog item (`02/02`); the register keeps the
  traceability, the backlog keeps the work.
- A debt item that grows into a new uncertainty is promoted to a risk entry rather than duplicated.

---

## Review Cadence

- The register is reviewed at every milestone boundary (M0–M7 in `02/01`), with priority re-scored.
- Any new **P1** item gets a backlog entry before the next phase begins.
- Security-relevant debt is reviewed with the dependency cadence in `10/02`.
- Performance-relevant debt is cross-checked against the budgets in `06/03-02` and the tuning notes
  in `10/03`.

---

## Open Questions

1. Whether a debt-aging metric belongs in the quality report (`06/03-01`) or stays a review artifact
   is a reporting decision; the register format and the review cadence above are fixed.
2. The owner accountable for cross-cutting items such as TD-08 and TD-10 is confirmed with the
   governance policies (`00/02-03`); the items and remedies are fixed.
