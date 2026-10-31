# 06 — Verification

The test strategy that proves resys-eshop: the layer matrix, unit, integration, and end-to-end rules, the coverage gates, the detailed test plans, and the quality metrics.

**Links:** [Matrix](./01-test-strategy/01-matrix.md) · [Unit tests](./01-test-strategy/02-unit.md) · [Integration tests](./01-test-strategy/03-integration.md) · [End-to-end tests](./01-test-strategy/04-e2e.md) · [Coverage](./01-test-strategy/05-coverage.md) · [Spec](../11-spec/README.md)

---

## Purpose

This area is how the team proves that resys-eshop behaves as specified. It turns the verification
obligations in §12 of the [design spec](../11-spec/README.md) — internal
consistency, working links, zero placeholders, and API parity — plus the functional requirements in
`01/02-*` and the journeys in `04/01-01` and `04/02-01` into a layered test strategy and a set of
concrete test plans.

The area is split into three concern subfolders, each with numbered files (~100–300 lines). Start with
`01-test-strategy` to understand what is tested where and with which tool, then follow `02-test-plans`
for the case-by-case detail and `03-quality` for the metrics and budgets. When a statement here
conflicts with the spec, the spec wins and this area is corrected.

The test layers are the ones the CI pipeline gates in `05/03-03`: unit (xUnit), integration
(Testcontainers PostgreSQL 18), front-end component (Nuxt test utils), and end-to-end (Playwright).
The tools and the module-by-module assignment are owned by `06/01-01`; the pipeline stages are owned by
`05/03-03`.

---

## Subfolder Index

| Subfolder | Purpose | Files |
|---|---|---|
| `01-test-strategy` | The layer matrix, the scope and conventions per layer, the Testcontainers and outbox strategy, the E2E journeys, and the coverage model. | [01-matrix.md](./01-test-strategy/01-matrix.md) · [02-unit.md](./01-test-strategy/02-unit.md) · [03-integration.md](./01-test-strategy/03-integration.md) · [04-e2e.md](./01-test-strategy/04-e2e.md) · [05-coverage.md](./01-test-strategy/05-coverage.md) |
| `02-test-plans` | The case-by-case test plans for the highest-risk domains: checkout, promotions, tax, inventory, and returns. | [01-checkout.md](./02-test-plans/01-checkout.md) · [02-promotions.md](./02-test-plans/02-promotions.md) · [03-tax.md](./02-test-plans/03-tax.md) · [04-inventory.md](./02-test-plans/04-inventory.md) · [05-returns.md](./02-test-plans/05-returns.md) |
| `03-quality` | The quality metrics and the performance budgets that the pipeline reports and enforces. | [01-metrics.md](./03-quality/01-metrics.md) · [02-performance-budgets.md](./03-quality/02-performance-budgets.md) |

Every file in this area is linked from the subfolder index above; prose elsewhere cites it by its
identifier (for example `06/02-01` for the checkout plan or `06/03-02` for the performance budgets).

---

## How to Read This Area

1. **Start with the matrix.** `06/01-01` names the four layers, the tool each runs on, the boundary
   between them, and the module-by-module assignment of coverage.
2. **Write fast tests first.** `06/01-02` fixes the unit scope per module, the `Method_State_Expected`
   naming and Arrange–Act–Assert arrangement, and the hand-written test-double boundary.
3. **Prove persistence and contracts against real PostgreSQL.** `06/01-03` defines the Testcontainers
   18 fixture, the template-and-clone reset strategy, the per-module integration suites, and the outbox
   assertions.
4. **Prove the journeys in a browser.** `06/01-04` defines the storefront and admin Playwright
   projects, the seeded-dataset requirement, the critical journeys from `04/01-01` and `04/02-01`, and
   the browser matrix.
5. **Hold the line on coverage.** `06/01-05` sets the target per layer, how Coverlet and Vitest produce
   the number, and how the pipeline reports and gates it.
6. Cite a file by its identifier (`AA/BB-CC`) in prose and link it from the index above when a
   reader should jump to it.

---

## Layer Summary

| Layer | Project / location | Tool | Owner |
|---|---|---|---|
| Unit | `tests/Resys.UnitTests` | xUnit | `06/01-02` |
| Integration | `tests/Resys.IntegrationTests` | xUnit + Testcontainers (PostgreSQL 18) | `06/01-03` |
| Front-end component | `web/storefront`, `web/admin` | Nuxt test utils (Vitest) | `06/01-02` |
| End-to-end | `tests/Resys.E2E` | Playwright | `06/01-04` |

Unit and integration require no browser; integration requires Docker. The full stack plus the demo seed
dataset is required only for E2E (`05/01-04`). The commands are in `05/03-01`; the pipeline order is in
`05/03-03`.

---

## Verification Obligations

Spec §12 defines four obligations for this documentation-first effort, and each maps to a concrete
check:

| Obligation | Where it is proven |
|---|---|
| Internal consistency — no contradiction between areas | Architecture tests in the unit layer (`06/01-02`) plus review (`00/02-03`). |
| Link check — every relative doc link resolves | CI stage 6 in `05/03-03`. |
| Zero placeholders — no forbidden token | CI stage 6 in `05/03-03`. |
| API parity — `09/03-03` matches `03/04-01` and `03/04-02` | OpenAPI parity assertions in the integration layer (`06/01-03`). |

No code is produced by the documentation effort itself; the layers above describe how the future code
will be verified and are the contract the implementation plans build against.

---

## Related Areas

- `01` (requirements) — the functional requirements `01/02-*` and non-functional requirements
  `01/03-*` the plans assert.
- `02` (planning) — the risk register `02/03`, whose mitigations name several test plans in this
  area.
- `03` (architecture) — the module contracts, order lifecycle, and engine designs the layers exercise.
- `04` (design) — the storefront and admin journeys the E2E layer realizes as Playwright specs.
- `05` (development) — the build/run commands the tests use and the CI pipeline that gates them.
- `00` (governance) — the review policy that consumes the test-strategy layer map.

---

## Keeping This Area Current

- Update `06/01-01` when a layer, a tool, or a module's coverage assignment changes, and keep the CI
  mapping aligned with `05/03-03`.
- Update `06/01-02` when a module's unit scope or the test-double boundary changes.
- Update `06/01-03` when the Testcontainers fixture, the reset strategy, or an integration suite
  changes; keep it aligned with the migrations in `03/03-04`.
- Update `06/01-04` when a journey, route, or seeded identifier changes; keep it aligned with
  `04/01-01`, `04/02-01`, and `05/01-04`.
- Update `06/01-05` when a target or an exclusion changes, and record the reason.
- Keep the `02-test-plans` and `03-quality` links above current; this index is the single place a
  reader discovers them.

## Open Questions

None at the area level. Unresolved details live in the owning file under its `## Open Questions`
section. The three spec-level questions relevant to verification are owned elsewhere: the integration
migration strategy is resolved in `06/01-01` and `06/01-03`, and the performance-budget thresholds are
owned by `06/03-02`.
