# Tax Test Plan

Case-by-case verification of the tax engine: jurisdiction hierarchy resolution, category-to-rate mapping, inclusive versus exclusive pricing, per-line rounding, and tax on post-promotion amounts.

**Links:** [Test matrix](../01-test-strategy/01-matrix.md) · [Unit tests](../01-test-strategy/02-unit.md) · [Integration tests](../01-test-strategy/03-integration.md) · [Tax engine](../../03-architecture/02-components/05-tax-engine.md) · [Order lifecycle](../../03-architecture/02-components/03-order-lifecycle.md) · [Tax requirements](../../01-requirements/02-functional/06-tax.md) · [Verification index](../README.md) · [Spec](../../11-spec/README.md)

---

## Scope

This plan proves the tax contract in `01/02-06` as designed by `03/02-05`. It covers the
country > region > city jurisdiction hierarchy, fallback when an address level is missing,
category-to-rate resolution from the catalog taxonomy, the store inclusive/exclusive mode, per-line
half-up rounding, and tax computed on the post-promotion base as a separate order component.

It assumes the engine rulings: tax is **not** an adjustment; the valid adjustment sources are
`promotion | shipping | manual`; the totals formula is `total = item total + adjustments + tax`;
and tax on placed orders is immutable. Order-scoped promotion discounts are allocated across lines
by the promotion engine precisely so each line has an explicit post-promotion tax base.

Layers: hierarchy resolution, category mapping, mode arithmetic, and rounding are unit tests;
effective-dated rate rows, store-scoped queries, and the persisted order component are integration
tests; the checkout totals reflecting tax is the storefront E2E journey.

---

## Preconditions

- A store with one default `TaxCategory` and at least two non-default categories (a standard and a
  zero-rated category).
- Jurisdictions for one country, one region, and two cities in that region, each able to carry a
  `TaxRate`; at least one rate that differs between the two cities.
- A product fixture classified into a category, plus one product left unspecified so it falls into
  the store default.
- Both an exclusive-mode store and an inclusive-mode store fixture.

---

## Cases

### TAX-01 — Exclusive mode adds tax on top of the post-promotion amount

- **Type:** unit
- **FR reference:** FR-TAX-003
- **Steps:**
  1. In an exclusive store, use a line whose post-promotion base is $54.00 with an 8.25% rate.
  2. Compute the breakdown.
- **Expected:**
  - `tax[line] = round($54.00 × 0.0825) = $4.46`.
  - The order total adds the tax on top: `total = item total + adjustments + tax`.

### TAX-02 — Inclusive mode extracts tax from within the amount

- **Type:** unit
- **FR reference:** FR-TAX-003
- **Steps:**
  1. In an inclusive store, use the same $54.00 post-promotion base with an 8.25% rate.
  2. Compute the breakdown.
- **Expected:**
  - `tax[line] = round($54.00 × 8.25 / 108.25) = $4.12` and `net = $54.00 − $4.12 = $49.88`.
  - The line amount is unchanged; tax is reported as a component of that amount, not added to the total.

### TAX-03 — City beats region beats country

- **Type:** unit
- **FR reference:** FR-TAX-001
- **Steps:**
  1. Resolve a destination with a city that has a rate in a region that also has a rate.
  2. Remove the city rate, then the region rate, and resolve again each time.
- **Expected:**
  - With a city rate the city rate applies; without it the region rate applies; without that the country rate applies.
  - Resolution walks most-specific to broadest and picks the most specific level that has a rate for the category.

### TAX-04 — Two cities in one region carry different rates

- **Type:** integration
- **FR reference:** FR-TAX-001
- **Steps:**
  1. Configure city A at 7.00% and city B at 9.00% in the same region and country.
  2. Compute tax for otherwise-identical orders shipping to A and to B.
- **Expected:**
  - The A order uses 7.00% and the B order uses 9.00%.
  - The region or country rate is not substituted for either city.

### TAX-05 — No matching jurisdiction yields no tax

- **Type:** unit
- **FR reference:** FR-TAX-001
- **Steps:**
  1. Resolve a destination whose country has no jurisdiction configured.
- **Expected:**
  - The level contributes no tax and no rate is invented.
  - The result is a zero tax component, not an error.

### TAX-06 — A missing address level falls back to the broader level

- **Type:** unit
- **FR reference:** FR-TAX-001
- **Steps:**
  1. Resolve a destination that has a region but no city in the address.
- **Expected:**
  - The region jurisdiction is used directly.
  - The city level is skipped without error, and the fallback order is city → region → country.

### TAX-07 — Unspecified products fall into the store default category

- **Type:** unit
- **FR reference:** FR-TAX-002
- **Steps:**
  1. Classify a variant with no explicit category via the catalog taxonomy query.
  2. Classify a variant explicitly assigned to a standard category.
- **Expected:**
  - The unspecified variant resolves to the store's default category.
  - The explicit variant resolves to its assigned category; classification is server-side.

### TAX-08 — A zero-rated category yields no tax

- **Type:** unit
- **FR reference:** FR-TAX-002
- **Steps:**
  1. Build an order with one standard-category line and one zero-rated line shipping to a taxed jurisdiction.
- **Expected:**
  - The standard line is taxed and the zero-rated line yields `$0.00`.
  - Per-line entries preserve both outcomes and are summed to the order tax.

### TAX-09 — A rate requires both jurisdiction and category to match

- **Type:** unit
- **FR reference:** FR-TAX-002
- **Steps:**
  1. Configure a rate for category X at the destination jurisdiction.
  2. Compute tax for a line in category X, then for a line in category Y.
- **Expected:**
  - Category X is taxed at the configured rate; category Y has no matching rate and yields no tax for that rate.
  - A rate only applies when the destination jurisdiction and the line category both match.

### TAX-10 — Tax is computed on the post-promotion amount

- **Type:** unit
- **FR reference:** FR-TAX-004
- **Steps:**
  1. Build a line of $100.00 with an applicable 10% rate.
  2. Apply a 10% order discount, then compute tax on the allocated post-promotion base.
- **Expected:**
  - The tax base is the post-discount line amount, so tax drops from $10.00 to $9.00.
  - Adding or changing the promotion triggers a tax recomputation before payment.

### TAX-11 — A fully discounted line has a zero tax base

- **Type:** unit
- **FR reference:** FR-TAX-004
- **Steps:**
  1. Apply a promotion that clamps one line to zero.
  2. Compute tax for that line.
- **Expected:**
  - The tax base is clamped at zero and the line tax is `$0.00`.
  - No negative base or negative tax is produced.

### TAX-12 — Per-line half-up rounding then sum avoids drift

- **Type:** unit
- **FR reference:** FR-TAX-006
- **Steps:**
  1. Build several lines whose tax individually lands on a half-minor-unit midpoint.
  2. Compute per-line tax, then the order tax.
- **Expected:**
  - Each line rounds half-up to the currency minor units before summing.
  - Repeated computation of identical inputs yields identical per-line and total tax.

### TAX-13 — Tax is a separate component, never an adjustment

- **Type:** integration
- **FR reference:** FR-TAX-005
- **Steps:**
  1. Place an order with tax and read back the order detail.
  2. Inspect every `Adjustment` row for the order.
- **Expected:**
  - The order exposes a distinct tax amount alongside item total and adjustments.
  - No adjustment has source `tax`; only `promotion`, `shipping`, and `manual` rows exist.

### TAX-14 — Changing a rate does not rewrite placed-order tax

- **Type:** integration
- **FR reference:** FR-TAX-002
- **Steps:**
  1. Place an order at a given rate, then edit the stored rate or delete it.
  2. Re-read the placed order's tax component.
- **Expected:**
  - The placed order's tax is unchanged; rate changes affect new computations only.
  - Deleting the rate likewise leaves historical tax intact.

### TAX-15 — Effective-dated rates select by instant

- **Type:** integration
- **FR reference:** FR-TAX-002
- **Steps:**
  1. Persist a rate effective from date D and a successor rate effective from date D+30.
  2. Compute tax for a computation instant before and after D+30.
- **Expected:**
  - The earlier instant uses the first rate and the later instant uses the successor.
  - Only the rate whose effective window contains the instant applies.

### TAX-16 — Checkout totals reflect tax at every step

- **Type:** E2E
- **FR reference:** FR-TAX-004, FR-CHK-009
- **Steps:**
  1. Complete checkout with a taxed destination and a coupon applied.
  2. Observe the totals display and the charged amount.
- **Expected:**
  - The displayed tax reflects the post-promotion base, computed server-side.
  - The amount sent to the gateway equals the freshly computed server total including tax.

---

## Open Questions

1. The final rounding mode (half-up versus banker's) is owned by the money model `03/03-03`; this
   plan asserts per-line half-up as the engine default.
2. Whether shipping is taxed and at which category is confirmed by the store's shipping tax
   category; free-shipping adjustments reduce or eliminate shipping tax, owned by `03/02-05`.
3. Whether a store may configure more than one shipping tax category is owned by the admin tax API
   `03/04-02`; the shipping-tax base rule asserted here is independent of that count.
