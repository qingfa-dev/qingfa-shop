# Tax Engine

The jurisdiction hierarchy, tax-category resolution from the catalog taxonomy, inclusive versus exclusive computation, and tax on post-promotion amounts as a separate order component.

**Links:** [Order lifecycle](./03-order-lifecycle.md) · [Promotion engine](./04-promotion-engine.md) · [Extension architecture](./07-extension-architecture.md) · [Module contracts](./02-module-contracts.md) · [Tax requirements](../../01-requirements/02-functional/06-tax.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

The Tax module computes the **tax component** of an order total. It resolves a jurisdiction for the
destination, applies a `TaxRate` bound to that jurisdiction and the line's `TaxCategory`, and honors
the store's inclusive or exclusive pricing mode. Tax is always computed on **post-promotion
amounts**, so promotion discounts reduce the taxable base (`FR-TAX-004`).

Tax is its own computed component, never an adjustment. The authoritative
formula is `total = item total + adjustments + tax`, where `adjustments` are every non-tax adjustment
source — promotion, shipping, and manual (`03/02-03`, `FR-TAX-005`). The engine exposes
`ITaxCalculationService` and delegates
jurisdiction resolution to `ITaxZoneResolver` (`03/02-07`); its output is a `TaxBreakdown` with
per-line entries and an order total.

---

## Jurisdiction Hierarchy

A jurisdiction is store-scoped and forms a hierarchy **country > region > city**:

| Field | Meaning |
|---|---|
| `Id`, `StoreId` | Identity and scope. |
| `Level` | `Country`, `Region`, or `City`. |
| `ParentId` | The next broader level (`City → Region → Country`). |
| `Code`, `Name` | ISO/administrative code and display name. |

Resolution for a destination address walks from the most specific level to the broadest:

1. Match the city inside the resolved region and country, if a city is present.
2. Otherwise match the region inside the country.
3. Otherwise match the country.
4. A level with no matching jurisdiction contributes no tax; a missing address level falls back to
   the next broader level (`FR-TAX-001`).

Each line resolves to the most specific jurisdiction that has a rate for the line's category. Two
cities in the same region may carry different rates, and a destination with no matching jurisdiction
yields no tax for that level.

---

## Tax Categories and Rates

- A `TaxCategory` is store-scoped and carries a name; each store has exactly one **default**
  category.
- Each product or variant is classified into a category. Classification is resolved from the
  catalog taxonomy through `ICatalogTaxonomyQuery.GetTaxCategoryAsync(variant)`; an unspecified
  product falls into the store's default category (`03/02-02`).
- A `TaxRate` carries a percentage, a jurisdiction, a `TaxCategory`, an effective-from date, and an
  optional effective-to date. It is store-scoped and effective-dated.
- A rate applies only when **both** the destination jurisdiction and the line's category match. A
  product in a zero-rated category yields no tax.
- Changing or deleting a rate affects new computations only; tax already placed on an order is
  immutable (`FR-TAX-002`).

---

## Computation

Tax is computed per line and then summed, so mixed categories are taxed independently and rounding
does not drift (`FR-TAX-006`).

```
base[line] = line amount after promotion adjustments, clamped at zero
rate       = effective TaxRate for (destination jurisdiction, line TaxCategory)
```

**Exclusive mode (default):** tax is added on top of the post-promotion amount.

```
tax[line]  = round(base[line] × rate)
charged    = base[line] + tax[line]
```

**Inclusive mode:** the post-promotion amount already contains tax, and tax is extracted from
within it.

```
tax[line]  = round(base[line] × rate / (100 + rate))
net[line]  = base[line] − tax[line]
```

Rounding is applied **per line** with a half-up rule (midpoint away from zero) to the currency's
minor units, and the rounded per-line values are summed to the order tax. This keeps repeated
computation of the same inputs identical. The mode is a store setting applied consistently to every
line (`FR-TAX-003`).

### Worked example — 8.25% on a $54.00 post-promotion line

The line's base is $54.00 after promotion adjustments; the applicable rate is 8.25%.

| Mode | Calculation | Tax | Net / charged |
|---|---|---|---|
| Exclusive | $54.00 × 0.0825 = $4.455 → half-up | $4.46 | charged = $54.00 + $4.46 = $58.46 |
| Inclusive | $54.00 × 8.25 / 108.25 = $4.1155 → half-up | $4.12 | net = $54.00 − $4.12 = $49.88 |

In exclusive mode the order total adds the tax on top of the item total and adjustments; in
inclusive mode the post-promotion amount is unchanged and tax is derived from within it, so the
total is `item total + adjustments` with tax reported as a component of that amount.

---

## Interaction with Post-Promotion Amounts

- The tax base per line is the line total **after promotion adjustments**, clamped at zero; a fully
  discounted line has a zero tax base (`FR-TAX-004`).
- Order-scoped promotion discounts are allocated across lines by the promotion engine precisely so
  each line has an explicit post-promotion base (`03/02-04`).
- Tax is recomputed whenever promotions, shipping, or line items change, including at every
  checkout step; the amount charged always matches the freshly computed server total
  (`FR-CHK-009`).
- Shipping tax, where configured, uses the post-promotion shipping amount. The store's shipping
  `TaxCategory` is used when one is configured; if the store configures no shipping tax category,
  shipping is untaxed. Free-shipping promotion adjustments therefore reduce or eliminate shipping
  tax.

---

## Tax as a Separate Component

Tax is returned as a `TaxBreakdown` — per-line tax entries plus an order-level total — and is
presented as its own component alongside the item total and adjustments. It is **never** stored as
an adjustment:

- No `Adjustment` row has a tax source; the `Adjustment` source enum is
  `promotion | shipping | manual` (`03/02-03`).
- Order total = item total + adjustments + tax, with tax summed separately.
- Tax on placed orders is immutable; rate changes never rewrite historical tax (`FR-TAX-005`).

---

## Pluggable Zone Resolver

Jurisdiction resolution is delegated to `ITaxZoneResolver`, keyed and registered in DI with
last-registered-wins semantics (`03/02-07`). The default resolver implements the
country > region > city hierarchy described above; a custom resolver may replace it without
modifying the Tax module. A resolver only returns rates with their jurisdictions and never mutates
order data (`FR-TAX-007`).

## Open Questions

1. The exact rounding mode (half-up versus banker's) is finalized with the money model `03/03-03`;
   this file specifies per-line half-up as the engine default.
2. Tax-exemption certificates and customer tax identifiers are not required by the spec; if added,
   they are an extension concern (`03/02-07`).
3. Whether a store may configure more than one shipping tax category is owned by the admin tax API
   `03/04-02`.

## Links

- [Order lifecycle](./03-order-lifecycle.md)
- [Promotion engine](./04-promotion-engine.md)
- [Extension architecture](./07-extension-architecture.md)
- [Module contracts](./02-module-contracts.md)
- [Tax requirements](../../01-requirements/02-functional/06-tax.md)
- [Design spec](../../11-spec/README.md) — §6 (tax engine), §8
  (`ITaxZoneResolver`).
- Money model: `03/03-03`; entity model: `03/03-01`; admin tax API: `03/04-02`.
