# Promotions Test Plan

Case-by-case verification of the promotion engine: order- and item-scoped discounts, fixed-price and free-shipping actions, Nth-item free, rules, usage limits, active windows, and per-line clamping.

**Links:** [Test matrix](../01-test-strategy/01-matrix.md) · [Unit tests](../01-test-strategy/02-unit.md) · [Integration tests](../01-test-strategy/03-integration.md) · [Promotion engine](../../03-architecture/02-components/04-promotion-engine.md) · [Order lifecycle](../../03-architecture/02-components/03-order-lifecycle.md) · [Promotions requirements](../../01-requirements/02-functional/07-promotions.md) · [Verification index](../README.md) · [Spec](../../11-spec/README.md)

---

## Scope

This plan proves the promotion contract in `01/02-07` as designed by `03/02-04`. It covers every
built-in action (percent off order, amount off order, percent off item, amount off item, fixed-price
item, free shipping, Nth-item free), rule AND semantics, the deterministic application order,
per-line clamping, coupon validation reasons, usage limits, active windows, and the adjustments the
engine records.

It assumes the engine rulings: every effect is a `promotion` adjustment and never a tax row; tax is
computed separately on post-promotion amounts; captured unit prices are never edited; the adjustment
sources are `promotion | shipping | manual`; and the totals formula is
`total = item total + adjustments + tax`. Free shipping is a promotion adjustment kept as a separate
row from the original shipping adjustment.

Layers: action arithmetic, application order, and clamping are unit tests with the order graph
built in memory; coupon uniqueness, atomic usage accounting, and adjustment persistence are
integration tests against real PostgreSQL; cart apply/remove is the storefront E2E journey.

---

## Preconditions

- A store with a two-line or three-line order fixture in one ISO-4217 currency, with captured unit
  prices and positive quantities.
- A promotion fixture builder that sets channels, window, priority, usage limits, rules, and action
  parameters without persisting unless the case is an integration case.
- At least one shipping method selected so a shipping adjustment exists for free-shipping cases.
- A coupon promotion with a unique-per-store code and a bounded window.

---

## Cases

### PRM-01 — Percent off order

- **Type:** unit
- **FR reference:** FR-PRM-003
- **Steps:**
  1. Build a two-line order with item total $100.00.
  2. Apply one `percent=10` order-scoped action.
- **Expected:**
  - One order-scoped negative adjustment of −$10.00 is emitted.
  - The post-promotion subtotal is $90.00, allocated across lines proportionally to their remaining amounts.

### PRM-02 — Amount off order

- **Type:** unit
- **FR reference:** FR-PRM-003
- **Steps:**
  1. Build a two-line order with item total $100.00.
  2. Apply one `amount=5` order-scoped action.
- **Expected:**
  - One order-scoped negative adjustment of −$5.00 is emitted.
  - The post-promotion subtotal is $95.00 with no line driven below zero.

### PRM-03 — Percent off item is line-scoped

- **Type:** unit
- **FR reference:** FR-PRM-003
- **Steps:**
  1. Build an order with two lines in the configured product/variant match set.
  2. Apply a `percent=20` item-scoped action.
- **Expected:**
  - One line-scoped negative adjustment per matching line, each 20% of that line's running amount.
  - Lines outside the match set receive no adjustment.

### PRM-04 — Amount off item is line-scoped and clamped

- **Type:** unit
- **FR reference:** FR-PRM-003, FR-PRM-005
- **Steps:**
  1. Build a line of $10.00 and apply an `amount=15` item-scoped action.
- **Expected:**
  - The recorded line-scoped adjustment is −$10.00, the clamped amount, not −$15.00.
  - The line's remaining amount reaches zero and never goes negative.

### PRM-05 — Fixed-price item changes the effective price only

- **Type:** unit
- **FR reference:** FR-PRM-003
- **Steps:**
  1. Build a line with a captured unit price of $30.00 and quantity 2.
  2. Apply a fixed-price action of $19.00.
- **Expected:**
  - A line-scoped negative adjustment of −$22.00 (2 × $11.00 reduction) is emitted.
  - The captured unit price on the line item remains $30.00; a configured price below zero is clamped at zero.

### PRM-06 — Free shipping is a separate promotion adjustment

- **Type:** unit
- **FR reference:** FR-PRM-003, FR-SHP-006
- **Steps:**
  1. Build an order with a shipping adjustment of $8.00.
  2. Apply the free-shipping action.
- **Expected:**
  - An order-scoped promotion adjustment of −$8.00 against shipping cost is emitted.
  - The original shipping adjustment remains on the order, so both rows are present; shipping cost is zero.

### PRM-07 — Nth-item free discounts the cheapest qualifying units

- **Type:** unit
- **FR reference:** FR-PRM-003
- **Steps:**
  1. Build a line with quantity 6 and `nth=3`, with per-unit amounts differing across the line's units.
  2. Apply the Nth-item-free action.
- **Expected:**
  - `floor(6 / 3) = 2` of the cheapest qualifying units are discounted.
  - The emitted line-scoped adjustments sum to the price of those two cheapest units and never exceed the line amount.

### PRM-08 — Application order is deterministic

- **Type:** unit
- **FR reference:** FR-PRM-004
- **Steps:**
  1. Build three eligible promotions with distinct priorities and start times.
  2. Recompute the same order twice, shuffling the input enumeration between runs.
- **Expected:**
  - Candidates sort by priority descending, then start time ascending, then id ascending.
  - Both computations produce byte-identical adjustment sets and an identical recorded applied order.

### PRM-09 — Rules combine with AND semantics

- **Type:** unit
- **FR reference:** FR-PRM-002
- **Steps:**
  1. Build a promotion with a coupon rule and an order-total minimum rule.
  2. Evaluate it with a valid code but a subtotal below the minimum.
- **Expected:**
  - The promotion is not eligible, because every rule must pass.
  - With both rules satisfied, the promotion becomes eligible and applies.

### PRM-10 — Coupon validation returns the correct reason

- **Type:** unit
- **FR reference:** FR-PRM-007
- **Steps:**
  1. Validate an unknown code, then an out-of-window code, then an exhausted code, then an ineligible user, then a channel mismatch.
- **Expected:**
  - Each returns `NotFound`, `NotStarted`/`Expired`, `Exhausted`, `UserIneligible`, and `ChannelMismatch` respectively.
  - Validation is read-only and writes no adjustment; code matching is trimmed and case-insensitive.

### PRM-11 — Total usage limit blocks further application

- **Type:** integration
- **FR reference:** FR-PRM-008
- **Steps:**
  1. Set `TotalUsageLimit` to 1 and place an order that consumes it.
  2. Apply the same promotion to a second order.
- **Expected:**
  - The promotion is excluded from the second order's candidate set.
  - A `PromotionExhausted` event is emitted when the limit is reached; usage is counted at placement, not at code entry.

### PRM-12 — Per-user limit counts distinct customers

- **Type:** integration
- **FR reference:** FR-PRM-008
- **Steps:**
  1. Set `PerUserLimit` to 1 for a customer and place one qualifying order.
  2. Attempt a second qualifying order for the same customer, then one for a different customer.
- **Expected:**
  - The same customer's second order is rejected for that promotion.
  - The different customer's order still applies the promotion.

### PRM-13 — Concurrent checkouts cannot both consume the last use

- **Type:** integration
- **FR reference:** FR-PRM-008
- **Steps:**
  1. Set a promotion to one remaining use.
  2. Confirm two orders concurrently, each with the coupon.
- **Expected:**
  - Exactly one placement increments usage through the conditional update `used < limit`.
  - The other placement does not apply the promotion and does not exceed the limit.

### PRM-14 — An expired promotion does not apply

- **Type:** unit
- **FR reference:** FR-PRM-001
- **Steps:**
  1. Set the promotion window to end before the current instant.
  2. Evaluate eligibility for an otherwise-qualifying order.
- **Expected:**
  - The promotion is inactive and produces no adjustment.
  - A disabled promotion likewise stops new applications without mutating historical adjustments.

### PRM-15 — Clamping never drives a line or subtotal negative

- **Type:** unit
- **FR reference:** FR-PRM-005
- **Steps:**
  1. Apply a `$120` order discount to an order with a $90 post-discount subtotal.
  2. Then apply a second order-scoped discount.
- **Expected:**
  - The first adjustment records the clamped −$90.00, not −$120.00.
  - Both lines reach zero; no line is negative and the second action applies nothing.

### PRM-16 — Promotion adjustments never include tax

- **Type:** integration
- **FR reference:** FR-PRM-006
- **Steps:**
  1. Apply a promotion to an order and persist the adjustments.
  2. Inspect every `Adjustment` row for the order.
- **Expected:**
  - Every promotion-produced row has source `promotion` and a promotion/action reference.
  - No row has source `tax`; tax is reported separately by the tax engine on the post-promotion amount.

### PRM-17 — Adjustments persist in application order

- **Type:** integration
- **FR reference:** FR-PRM-004
- **Steps:**
  1. Apply two promotions with different priorities and persist.
  2. Read back the order's adjustments and the recorded applied order.
- **Expected:**
  - The persisted rows reflect the same sequence as the computed application order.
  - Recomputing the same inputs reproduces identical amounts and order.

### PRM-18 — Cart coupon apply and remove journey

- **Type:** E2E
- **FR reference:** FR-PRM-007, FR-CHK-008
- **Steps:**
  1. In the storefront cart, enter a valid coupon and observe totals.
  2. Remove the coupon and observe totals again.
- **Expected:**
  - Applying the code reduces the displayed total and shows the discount adjustment.
  - Removing it restores the pre-coupon total; no client-supplied discount is ever trusted.

---

## Open Questions

1. The concrete default `Priority` values for built-in promotion templates are owned by the admin
   promotion API `03/04-02`; this plan asserts the ordering tuple, not the defaults.
2. Whether guest usage is tracked per order email or per order is a store policy owned by
   `03/02-04`; this plan asserts that a guest cannot exceed the configured per-user limit either way.
3. Whether promotions can be shared across stores via a platform template is deferred to
   `03/03-02`; every case here is store-scoped.
