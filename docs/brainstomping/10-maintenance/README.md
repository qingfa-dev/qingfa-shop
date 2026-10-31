# 10 — Maintenance

How resys-eshop stays healthy after release: the debt register, the dependency and upgrade policy, and the performance-tuning notes.

**Links:** [Technical debt](./01-technical-debt.md) · [Dependency policy](./02-dependency-policy.md) · [Performance tuning](./03-performance-tuning.md) · [Spec](../11-spec/README.md)

---

## Purpose

This area is the maintenance contract for resys-eshop. It records the trade-offs the design has
already accepted, fixes the cadence for keeping dependencies patched and supported, and names the
hot paths and the measurement sources that keep performance within budget. It is the counterpart to
the planning risks in `02` and the release discipline in `07`: planning decides what to build,
release ships it, and maintenance keeps it correct and supported afterward.

When a statement in this area conflicts with the [design spec](../11-spec/README.md),
the spec wins and this area is corrected.

---

## File Index

| Path | Concern | Read it when |
|---|---|---|
| [01-technical-debt.md](./01-technical-debt.md) | The debt register `TD-01`…`TD-11`: module boundaries, gateway abstraction, i18n coverage, tax and promotion complexity, events, caching, and more, each with impact, remedy, and linked risk. | You are planning a phase, triaging a milestone review, or asking why a boundary is shaped the way it is. |
| [02-dependency-policy.md](./02-dependency-policy.md) | Update cadence; the .NET LTS upgrade rule (support to 2028-11-14); the Nuxt/Vue rule (Nuxt 3 EOL 2026-07-31, Vue pinned 3.5 until 3.6 GA); and the security-patch SLA. | You are bumping a runtime, framework, or package; or responding to a security advisory. |
| [03-performance-tuning.md](./03-performance-tuning.md) | Known hot paths (catalog search, checkout totals, promotion application), the indexing and caching strategy, query guardrails, and the measurement pointers. | A budget is breached, a dashboard trends the wrong way, or you are adding a query to a hot path. |

---

## Maintenance at a Glance

| Concern | Policy | Owner |
|---|---|---|
| Debt register | Trade-offs `TD-01`…`TD-11`, each with impact, remedy, and linked risk in `02/03`. | `10/01` |
| .NET runtime | .NET 10.0 LTS only, supported to 2028-11-14; successor LTS lands with runway before that date. | `10/02` |
| Nuxt / Vue | Nuxt 4.5.x (Nuxt 3 EOL 2026-07-31); Vue pinned 3.5.43 until 3.6 GA. | `10/02` |
| Security patches | Critical 7 days (72 h if exploitable), High 14 days, Medium 30 days, Low ≤ 90 days. | `10/02` |
| Upgrade verification | Full CI, E2E for the front ends, and the budgets in `06/03-02` for hot paths. | `10/02`, `07/01` |
| Catalog search | Store-scoped, index-backed, capped facets, bounded page sizes. | `10/03` |
| Checkout totals | Recomputed from source plus adjustments plus tax; batched reads, no N+1. | `10/03` |
| Promotion application | Pre-filter, evaluate once per totals computation, clamp per line at zero. | `10/03` |
| Caching | `ETag`/`Cache-Control` on public catalog, per-store keys, 60 s invalidation. | `10/03` |
| Measurement | Budgets and load profiles `06/03-02`; telemetry and dashboards `08/02-01`, `08/02-02`. | `10/03` |

---

## How to Read This Area

1. **Know what is owed.** `10/01` lists the trade-offs the design carries; each names the remedy that
   closes it and the risk in `02/03` it can cause.
2. **Keep the stack supported.** `10/02` fixes when dependencies are reviewed, why the platform runs
   an LTS runtime, and how fast a security patch must land.
3. **Keep it fast.** `10/03` names the paths that matter and the budgets and telemetry that prove
   them.
4. **Cite by identifier, link what exists.** Every doc in this set exists; cite another doc as
   `AA/BB-CC` (flat areas as `AA/NN`) and link it when a jump is useful.

---

## Related Areas

- `01` (requirements) — the performance NFRs (`01/03-01`), the security NFR that demands a dependency
  cadence (`01/03-02`), and the i18n scope (`01/03-04`) that bounds the i18n debt item.
- `02` (planning) — the risk register (`02/03`) that debt items link to and the backlog (`02/02`)
  that schedules their remedies.
- `03` (architecture) — the module contracts, data model, migrations, and integration points the debt
  and tuning notes refer to (`03/02-01`, `03/03-04`, `03/04-05`).
- `05` (development) — the CI lane (`05/03-03`) that runs the placeholder, link, and dependency
  checks this area depends on.
- `06` (verification) — the performance budgets and measurement sources (`06/03-02`) and the quality
  metrics (`06/03-01`).
- `07` (release) — the version rules (`07/01`) that classify a dependency upgrade or a break.
- `08` (operations) — telemetry, dashboards, alerts, and the database and capacity runbooks
  (`08/02-01`, `08/02-02`, `08/02-03`, `08/03-02`, `08/04-01`).
- `09` (user) — the guides whose behavior must be re-checked when an upgrade or tuning change ships.

---

## Review Cadence

Maintenance runs on a fixed rhythm so nothing depends on someone remembering it:

| Cadence | Review | Output |
|---|---|---|
| Continuous | Security advisories and automated dependency PRs. | Patch or mitigation per `10/02`. |
| Weekly | Open dependency PRs. | Merged patches; superseded bumps closed. |
| Monthly | Patches, minors, and a debt-item status line. | Updated register and dependencies. |
| Quarterly | Pins, support dates, and policy ownership. | Planned triggers and the next framework minor. |
| Per milestone (M0–M7) | Full debt register re-score. | New backlog items for any P1 debt. |
| Per release candidate | Performance budgets (`06/03-02`). | Held or green release. |

A finding that cannot be fixed now becomes a debt item in `10/01` or a risk in `02/03`, never an
unrecorded note.

---

## Keeping This Area Current

- Add or retire a debt item in `10/01` with an impact and a remedy; never delete an ID after it is
  retired.
- Update `10/02` when a support date, a pin, or a security-patch window changes, and keep it aligned
  with `00/01-01`, `01/03-02`, and `07/01`.
- Update `10/03` when a hot path, an index, or a cache policy changes, and reflect the same change
  into the budgets in `06/03-02`.
- Re-run the placeholder scan and the link check over this area before merging a change.

---

## Open Questions

None at the area level. Unresolved details live in the owning file under its `## Open Questions`
section: debt prioritization and ownership in `10/01`, the dependency bot and Node LTS pin in
`10/02`, and the search backend and caching-layer choices in `10/03`.
