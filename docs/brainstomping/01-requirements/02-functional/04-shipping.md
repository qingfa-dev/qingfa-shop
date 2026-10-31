# Shipping — Functional Requirements

What the shipping module must do: methods, categories, zones, pluggable rate calculators,
rate selection during checkout delivery, and free-shipping interaction with promotions.

**Links:** [Catalog](./01-catalog.md) · [Orders](./02-orders.md) · [Checkout](./03-checkout.md) · [Promotions](./07-promotions.md) · [Requirements index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Shipping owns how an order gets from a fulfillment location to the shopper's destination. It
defines **shipping methods** grouped into **categories**, decides availability through
**zones**, and computes cost through pluggable calculators registered behind
`IShippingCalculator` (spec §8). The checkout delivery step quotes rates for the order and
destination; the selected rate is recorded on the order as a shipping adjustment and
participates in the computed total (FR-ORD-006).

Methods and zones are store-scoped. Rates are `Money` values in the order currency, never raw
decimals (`01/01-03`). A free-shipping promotion may reduce the selected cost to zero, but it
does so through a promotion adjustment rather than by deleting the shipping charge
(FR-SHP-006). Module contracts are planned in `03/02-02`; integration context, including
carrier-backed calculators, is in `03/04-05`.

Actors are defined in `01/01-02`.

---

## FR-SHP-001 — Shipping methods and categories

- **Actor:** Store Admin
- **Description:** Define shipping methods, each a named service level, grouped under shipping
  categories and scoped to a store.
- **Rules:**
  - A shipping method belongs to exactly one store and one category.
  - A category groups methods for presentation (for example Standard, Express) and carries an
    admin-defined order.
  - A method binds a calculator and its configuration to a display name.
  - Methods can be enabled or disabled; disabled methods are never quoted.
- **Acceptance Criteria:**
  - An admin can create method "Standard" in category "Ground" for store A.
  - Disabling a method removes it from all quotes.
  - A method defined in store A is never offered in store B.

---

## FR-SHP-002 — Zones and destination matching

- **Actor:** Store Admin; System
- **Description:** Define zones as sets of destination criteria and restrict each method's
  availability to the zones it serves.
- **Rules:**
  - A zone matches on country, region/state, and optionally postal-code ranges.
  - The most specific match wins; a method is available only when the destination falls in one
    of its zones.
  - Zones are store-scoped.
  - An order whose destination matches no zone is offered no method from that zone, but
    checkout must still offer at least one method when the destination is served at all
    (FR-SHP-004).
- **Acceptance Criteria:**
  - A method limited to zone "US-CA" is offered for a California address and not for an Oregon
    address.
  - A postal-range zone matches an address inside the range and rejects one outside it.
  - Changing the shipping country re-quotes the available methods.

---

## FR-SHP-003 — Pluggable rate calculators

- **Actor:** Platform Admin; Payment/Shipping Providers
- **Description:** Compute shipping cost through calculators registered behind
  `IShippingCalculator`, with flat, per-item, per-order, and weight strategies built in.
- **Rules:**
  - `IShippingCalculator` receives the order, destination, and method configuration and returns
    zero or more rates expressed as `Money` in the order currency.
  - Built-in calculators are flat rate, per-item, per-order (a single charge), and weight-based
    (rate per weight unit).
  - Additional calculators register in DI; last-registered wins per key (spec §8).
  - A calculator never returns a negative rate.
- **Acceptance Criteria:**
  - A flat-rate method returns the configured amount for any destination in its zone.
  - A per-item method returns the configured amount multiplied by the total quantity.
  - A weight-based method returns rate × total weight using the documented rounding rule.
  - A custom calculator becomes selectable without any change to core modules.

---

## FR-SHP-004 — Rate quoting at the delivery step

- **Actor:** Guest; Customer
- **Description:** Quote available rates for the order and destination at the checkout delivery
  step and let the shopper select exactly one.
- **Rules:**
  - Quoting requires a valid shipping address; before that, no rates are returned.
  - Each available method is quoted through its calculator; a rate carries method, service, and
    cost.
  - At least one method must be offered for a served destination; otherwise advancing past
    delivery is blocked with an explicit error.
  - Changing the address or line items re-quotes methods and clears a selection that is no
    longer valid.
- **Acceptance Criteria:**
  - A valid served address returns one or more priced methods.
  - Selecting a method records that method on the order.
  - Changing the country re-quotes methods and clears the previous selection.
  - A destination with no served method blocks advancing past the delivery step.

---

## FR-SHP-005 — Shipping cost as an adjustment

- **Actor:** System
- **Description:** When a rate is selected, record its cost as a shipping adjustment on the
  order so the computed total reflects delivery.
- **Rules:**
  - The selected cost is stored as a `Money` adjustment with source `shipping` and a reference
    to the method (FR-ORD-005).
  - Re-quoting or changing the selection replaces the shipping adjustment rather than appending
    a duplicate.
  - Shipping adjustments participate in the computed order total (FR-ORD-006).
  - Shipping cost is expressed in the order currency; a currency mismatch is rejected.
- **Acceptance Criteria:**
  - Selecting a method adds exactly one shipping adjustment to the order.
  - Re-selecting a method replaces the previous adjustment and updates the total.
  - The shipping adjustment is retrievable with its method reference and amount.

---

## FR-SHP-006 — Free-shipping interaction with promotions

- **Actor:** System; Store Admin
- **Description:** Apply a free-shipping promotion action so the selected shipping cost is
  reduced to zero through a promotion adjustment rather than by removing the shipping charge.
- **Rules:**
  - The free-shipping action produces a promotion adjustment equal to the selected shipping
    cost, clamped so shipping never falls below zero (`01/02-07`).
  - The shipping adjustment and the free-shipping adjustment are distinct rows and both remain
    visible.
  - Free shipping applies only to methods and rates the promotion's rules allow.
  - Removing the promotion restores the original shipping cost.
- **Acceptance Criteria:**
  - Applying a free-shipping promotion reduces the effective shipping cost to zero via a
    promotion adjustment.
  - The original shipping adjustment remains recorded on the order.
  - Removing the promotion restores the shipping cost in the computed total.

---

## FR-SHP-007 — Admin shipping configuration API

- **Actor:** Store Admin; Platform Admin
- **Description:** Expose CRUD for methods, categories, zones, and calculator configuration over
  `/api/admin`, scoped to the selected store.
- **Rules:**
  - Every mutation requires the `admin` role plus the target store scope and is written to the
    audit log (`03/04-03`).
  - Validation failures return structured, field-level errors.
  - Deleting a method referenced by an in-flight order does not alter that order's recorded
    cost.
- **Acceptance Criteria:**
  - An admin scoped to store A cannot modify store B's methods.
  - Creating a zone and a method makes that method quotable for matching addresses.
  - A successful mutation appears in the audit log.

---

## Open Questions

1. Whether shipment records are one per order or support partial shipments is finalized in the
   order-lifecycle doc `03/02-03`; this file covers rates and selection only.
2. Carrier-backed calculators, label generation, and tracking are an integration concern
   (`03/04-05`, `03/02-07`); the built-in calculators are local rate rules only.
3. Exact weight-unit normalization (g/kg/lb) is finalized with the data model `03/03-01`.
