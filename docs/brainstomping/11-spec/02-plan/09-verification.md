# Plan — Verification

Tasks 21–23: test strategy, test plans, and quality metrics.

**Links:** [Spec index](../README.md) · [Design spec](../01-design/01-purpose-scope.md) · [← Prev](08-development.md) · [Next →](10-release.md) · [Root README](../../../README.md)

---

### Task 21: Verification — test strategy and area index


**Files:**
- Modify: `docs/06-verification/README.md`
- Create: `docs/06-verification/01-test-strategy/01-matrix.md`
- Create: `docs/06-verification/01-test-strategy/02-unit.md`
- Create: `docs/06-verification/01-test-strategy/03-integration.md`
- Create: `docs/06-verification/01-test-strategy/04-e2e.md`
- Create: `docs/06-verification/01-test-strategy/05-coverage.md`

**Interfaces:**
- Consumes: Spec §12; `03/02-*`; `04/01-*`.
- Produces: `06/01-01` … `06/01-05`, cited by `00/02-01`, `05/03-03`.

- [ ] **Step 1: Write the five strategy files and the index**

- `01-matrix.md` — Test-type vs component matrix (which module is covered by unit, integration, E2E) and the tools per type (xUnit, Testcontainers/Postgres, Playwright, Nuxt test utils).
- `02-unit.md` — Unit scope per module, naming/arrangement conventions, mocking boundaries.
- `03-integration.md` — Testcontainers setup for PostgreSQL, per-module integration suites, transaction/reset strategy, outbox assertions.
- `04-e2e.md` — Playwright projects for storefront and admin, seeded-dataset requirement, the critical journeys from `04/01-01` and `04/02-01`.
- `05-coverage.md` — Coverage targets per layer, how coverage is measured and reported in CI.

Area README becomes an index.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/06-verification && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/06-verification
git commit -m "docs(verification): add test strategy (matrix, unit, integration, e2e, coverage)"
```

### Task 22: Verification — test plans


**Files:**
- Create: `docs/06-verification/02-test-plans/01-checkout.md`
- Create: `docs/06-verification/02-test-plans/02-promotions.md`
- Create: `docs/06-verification/02-test-plans/03-tax.md`
- Create: `docs/06-verification/02-test-plans/04-inventory.md`
- Create: `docs/06-verification/02-test-plans/05-returns.md`

**Interfaces:**
- Consumes: `06/01-01`; `03/02-03` … `03/02-06`.
- Produces: `06/02-01` … `06/02-05`.

- [ ] **Step 1: Write the five test plans**

Each plan: `Scope`, `Preconditions`, then numbered cases with `ID`, `Steps`, `Expected`, `Type` (unit/integration/E2E), `FR reference`.

- `01-checkout.md` — happy path, guest checkout, out-of-stock, payment failure/retry, coupon application.
- `02-promotions.md` — percent/amount order and item discounts, fixed-price item, free shipping, Nth-item free, usage limit, expired promotion, clamped totals.
- `03-tax.md` — inclusive vs exclusive, jurisdiction nesting, tax category mapping, tax on post-promotion amount.
- `04-inventory.md` — reservation, decrement on shipment, return restock, backorder policy, no-negative-stock.
- `05-returns.md` — partial/full return, refund vs store credit, exchange creating a new line, restock movement.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/06-verification/02-test-plans && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/06-verification/02-test-plans
git commit -m "docs(verification): add test plans (checkout, promotions, tax, inventory, returns)"
```

### Task 23: Verification — quality metrics


**Files:**
- Create: `docs/06-verification/03-quality/01-metrics.md`
- Create: `docs/06-verification/03-quality/02-performance-budgets.md`

**Interfaces:**
- Consumes: `01/03-01`, `06/01-05`; Spec §12.
- Produces: `06/03-01`, `06/03-02`, cited by `08/02-03`.

- [ ] **Step 1: Write the two quality files**

- `01-metrics.md` — Metric definitions (coverage, flaky-test rate, build duration, defect escape rate), targets, and where each is reported.
- `02-performance-budgets.md` — API p95 budgets per endpoint group, storefront LCP/CLS budgets, SSR render budget, and the check that enforces them in CI.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/06-verification && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/06-verification
git commit -m "docs(verification): add quality metrics and performance budgets"
```
