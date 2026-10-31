# Tax — Functional Requirements

What the tax module must do: jurisdictions, tax rates and categories, inclusive/exclusive
pricing, and tax computed on post-promotion amounts as its own order component.

**Links:** [Orders](./02-orders.md) · [Checkout](./03-checkout.md) · [Promotions](./07-promotions.md) · [Requirements index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Tax computes the tax component of an order total. It resolves a **jurisdiction** for the
destination using the hierarchy country > region > city, applies a **`TaxRate`** bound to that
jurisdiction and the line's **`TaxCategory`**, and honors the store's **inclusive or exclusive**
pricing mode. Tax is always computed on **post-promotion amounts**, so discounts reduce the
taxable base (FR-TAX-004).

Tax is its own computed component of the total, not a promotion or shipping adjustment: the
authoritative formula is `total = item total + adjustments + tax`, where `adjustments` are
promotion and shipping discounts only (FR-ORD-006, FR-TAX-005). Rates are store-scoped and
effective-dated. The engine design is planned in `03/02-05`.

Actors are defined in `01/01-02`.

---

## FR-TAX-001 — Tax jurisdictions and hierarchy

- **Actor:** Store Admin
- **Description:** Define jurisdictions as country > region > city and resolve the applicable
  jurisdiction for an order destination.
- **Rules:**
  - Jurisdictions form a hierarchy; the most specific matching level wins (city over region
    over country).
  - A destination resolves to one effective jurisdiction per level that has a match.
  - A missing address level falls back to the next broader level.
  - Jurisdictions are store-scoped.
- **Acceptance Criteria:**
  - A city address uses its city rate when present, otherwise its region rate, otherwise its
    country rate.
  - Two cities in the same region can carry different rates.
  - A destination with no matching jurisdiction yields no tax for that level.

---

## FR-TAX-002 — Tax rates and tax categories

- **Actor:** Store Admin
- **Description:** Bind `TaxRate` values to a jurisdiction and a `TaxCategory`, and classify
  products and variants into categories.
- **Rules:**
  - A `TaxRate` carries a percentage, a jurisdiction, and a `TaxCategory`.
  - Each product or variant belongs to a `TaxCategory`; an unspecified product falls into the
    store's default category.
  - A rate applies only when both the destination jurisdiction and the line's category match.
  - Rates are store-scoped and effective-dated.
- **Acceptance Criteria:**
  - A product in category "Standard" is taxed at the standard rate for its destination.
  - A product in a zero-rated category yields no tax.
  - Changing a rate does not rewrite tax on placed orders.

---

## FR-TAX-003 — Inclusive vs exclusive tax per store

- **Actor:** Store Admin
- **Description:** Configure, per store, whether displayed prices include tax, and compute tax
  accordingly.
- **Rules:**
  - Exclusive mode (default): tax is added on top of the post-promotion amount.
  - Inclusive mode: the post-promotion amount already contains tax, and tax is extracted from
    within it.
  - The mode is a store setting and applies consistently to every line in the order.
  - Changing the mode affects new computations, not placed orders.
- **Acceptance Criteria:**
  - In exclusive mode, total = post-promotion amount + tax.
  - In inclusive mode, the line amount is unchanged and tax is derived from within it.
  - The store's mode is applied to every line of the order.

---

## FR-TAX-004 — Tax computed on post-promotion amounts

- **Actor:** System
- **Description:** Compute tax on the amount remaining after promotion discounts are applied, so
  discounts correctly reduce tax.
- **Rules:**
  - The tax base per line is the line total after promotion adjustments, clamped at zero.
  - Shipping tax, where configured, uses the post-promotion shipping amount.
  - Tax is recomputed whenever promotions, shipping, or line items change (FR-CHK-009).
  - The tax base never falls below zero.
- **Acceptance Criteria:**
  - A 10% order discount reduces the taxable base before tax is computed.
  - Adding a promotion recomputes tax.
  - A fully discounted line has a zero tax base.

---

## FR-TAX-005 — Tax as a separate computed component

- **Actor:** System
- **Description:** Present tax as its own component of the order total, never as a promotion or
  shipping adjustment row.
- **Rules:**
  - Order total = item total + adjustments + tax, where `adjustments` are promotion and
    shipping discounts only (FR-ORD-006).
  - Tax is computed on demand and is never stored as a promotion or shipping adjustment.
  - Tax amounts are retrievable per line item and per order for display and reporting.
  - Tax on placed orders is immutable.
- **Acceptance Criteria:**
  - The order total formula sums tax separately from adjustments.
  - No adjustment row has source `tax`.
  - The order detail shows a distinct tax amount alongside item total and adjustments.

---

## FR-TAX-006 — Per-line and order-level tax computation

- **Actor:** System
- **Description:** Compute tax per line from its category and destination, then sum the lines to
  the order's tax component.
- **Rules:**
  - Each line's tax = tax base × the applicable rate for its category and destination.
  - Rounding is applied per line with a documented rule and then summed, to avoid drift.
  - Mixed categories in one order are taxed independently.
  - Tax is expressed as `Money` in the order currency.
- **Acceptance Criteria:**
  - An order with two categories shows per-line tax and a summed total.
  - Repeated computation of the same inputs yields identical per-line tax.
  - The tax currency matches the order currency.

---

## FR-TAX-007 — Pluggable zone resolver

- **Actor:** Platform Admin
- **Description:** Resolve jurisdictions from a destination through `ITaxZoneResolver`, allowing
  custom jurisdiction logic without modifying core modules.
- **Rules:**
  - `ITaxZoneResolver` maps an address to the applicable jurisdictions and rates (spec §8).
  - The default resolver implements the country > region > city hierarchy.
  - Custom resolvers register in DI; last-registered wins per key.
  - A resolver only returns rates; it never mutates order data.
- **Acceptance Criteria:**
  - A custom resolver can replace the default without core changes.
  - Registering a resolver changes computed tax for new computations.
  - Resolver output is expressed as rates with their jurisdictions.

---

## FR-TAX-008 — Admin tax configuration API

- **Actor:** Store Admin; Platform Admin
- **Description:** Expose CRUD for jurisdictions, rates, categories, and the inclusive/exclusive
  mode over `/api/admin`, scoped to the selected store.
- **Rules:**
  - Every mutation requires the `admin` role plus store scope and is written to the audit log.
  - Validation rejects negative rates and conflicting rates at the same specificity and
    category.
  - Deleting a rate does not alter tax on placed orders.
- **Acceptance Criteria:**
  - An admin scoped to store A cannot edit store B's rates.
  - Setting a store to inclusive mode changes new order computations.
  - A negative rate is rejected with a validation error.

---

## Open Questions

1. Whether shipping is taxed, and at which rate, is confirmed in the tax engine `03/02-05`.
2. Tax-exemption certificates and customer tax identifiers are not required by the spec; if
   added, they are an extension concern (`03/02-07`).
3. The rounding mode (half-up vs. banker's) is finalized with the money model `03/03-03`.
