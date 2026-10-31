# Promotions — Functional Requirements

What the promotions module must do: promotion definitions, eligibility rules, actions,
application order, per-line clamping, and the adjustments they produce.

**Links:** [Orders](./02-orders.md) · [Checkout](./03-checkout.md) · [Tax](./06-tax.md) · [Shipping](./04-shipping.md) · [Requirements index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Promotions apply discounts and incentives to an order. A **Promotion** carries channels, an
active window, and usage limits; **rules** decide eligibility (coupon, user, product/variant,
order total, shipping method); and **actions** decide the effect (percent or amount off the
order or an item, fixed-price item, free shipping, Nth-item free). The engine applies eligible
promotions in a documented order with per-line clamping and produces **adjustments** (spec §6).

Adjustments are promotion-produced only. Tax is computed separately on post-promotion amounts
(FR-TAX-004) and is never written by a promotion. Captured line-item unit prices are never
edited; a promotion changes effective amounts through adjustments. The engine design is
planned in `03/02-04`; promotion test scenarios are owned by `06/02-02`.

Actors are defined in `01/01-02`.

---

## FR-PRM-001 — Promotion definition

- **Actor:** Store Admin
- **Description:** Create a promotion with channels, an active window, and usage limits, scoped
  to a store.
- **Rules:**
  - A promotion has a name, a store scope, a set of channels, a start and end datetime, and
    usage limits (total and per-user).
  - A promotion is active only within its window and while under its usage limits.
  - Channels restrict where a promotion may apply; a coupon-only promotion is not auto-applied.
  - Disabling a promotion stops new applications without mutating historical adjustments.
- **Acceptance Criteria:**
  - A promotion outside its window does not apply.
  - A promotion that has reached its total usage limit does not apply again.
  - A promotion restricted to the storefront channel is not applied by an admin manual order
    unless that channel is included.

---

## FR-PRM-002 — Promotion rules

- **Actor:** Store Admin
- **Description:** Define eligibility rules that gate whether a promotion applies: coupon, user,
  product/variant, order total, and shipping method.
- **Rules:**
  - Rules combine with AND semantics unless a documented match policy states otherwise.
  - A coupon rule requires a matching code; a user rule targets a customer segment or specific
    customer; a product/variant rule matches line items; an order-total rule sets a minimum; a
    shipping-method rule constrains the delivery selection.
  - A promotion with no rules is eligible whenever it is active in its window and channel.
  - Eligibility is always evaluated server-side against the current order.
- **Acceptance Criteria:**
  - A coupon rule applies only when the entered code matches.
  - An order below the configured minimum total does not qualify.
  - A product rule applies only to orders containing a matching variant.

---

## FR-PRM-003 — Promotion actions

- **Actor:** Store Admin
- **Description:** Define what a promotion does: percent or amount off the order or an item,
  fixed-price item, free shipping, or Nth-item free.
- **Rules:**
  - Supported actions are percent-off order, amount-off order, percent-off item, amount-off
    item, fixed-price item, free shipping, and Nth-item free.
  - An action produces promotion adjustment(s) on the order or on specific line items.
  - Free shipping reduces the shipping cost to zero through a promotion adjustment
    (FR-SHP-006).
  - A fixed-price action sets the effective unit price to the configured amount, clamped at
    zero.
- **Acceptance Criteria:**
  - A percent-off order action reduces the order by the configured percentage.
  - An Nth-item-free action discounts the cheapest qualifying unit(s) per the configured N.
  - A fixed-price action changes the effective price without editing the captured unit price.

---

## FR-PRM-004 — Application order

- **Actor:** System
- **Description:** Apply eligible promotions in a documented, deterministic order so results are
  reproducible and auditable.
- **Rules:**
  - Promotions are ordered by a documented priority (for example explicit priority, then
    creation time, then id) and applied sequentially.
  - Each action computes against the running post-discount amounts, so later promotions see
    earlier discounts.
  - Within a promotion, line-level actions apply before order-level actions unless an action
    specifies otherwise.
  - The order is stable and independent of request ordering.
- **Acceptance Criteria:**
  - Two promotions with different priorities apply in priority order.
  - Recomputing the same inputs produces the same adjustments.
  - The applied order is recorded alongside the resulting adjustments.

---

## FR-PRM-005 — Per-line clamping

- **Actor:** System
- **Description:** Clamp discounts so no line item or order subtotal is driven below zero.
- **Rules:**
  - A line-item discount never exceeds the line's current post-discount amount.
  - An order-level discount is clamped to the sum of remaining line amounts, and to the
    shipping amount for free shipping.
  - Clamping is applied per line, not only at the order level, so allocation is explicit.
  - The clamped amount is recorded on the adjustment so the discount is auditable.
- **Acceptance Criteria:**
  - A discount larger than a line total is clamped to that total.
  - No line goes negative after any promotion is applied.
  - The sum of promotion adjustments never exceeds the pre-discount order total.

---

## FR-PRM-006 — Promotions produce adjustments only

- **Actor:** System
- **Description:** Represent every promotion effect as a promotion adjustment; promotions never
  alter tax rows or captured prices.
- **Rules:**
  - Each applied promotion writes adjustment row(s) with source `promotion` and a reference to
    the promotion and action (FR-ORD-005).
  - Adjustments may be line-item-scoped or order-scoped.
  - Tax is computed separately on post-promotion amounts (FR-TAX-004); a promotion never writes
    tax.
  - Historical adjustments on placed orders are immutable.
- **Acceptance Criteria:**
  - Applying a promotion creates an adjustment referencing that promotion.
  - No promotion writes a tax adjustment.
  - Changing a promotion does not rewrite adjustments on a placed order.

---

## FR-PRM-007 — Coupon codes

- **Actor:** Guest; Customer; Store Admin
- **Description:** Support coupon codes that activate coupon-rule promotions, with per-code
  usage tracking.
- **Rules:**
  - Codes are unique per store and matched case-insensitively.
  - A code carries its own usage limit in addition to the promotion's limits.
  - Code validation is server-side; the client is never trusted for discount amounts
    (FR-CHK-008).
  - Applying a code recomputes adjustments and totals; removing it removes its adjustment.
- **Acceptance Criteria:**
  - A valid code applies its promotion and reduces the computed total.
  - An expired or usage-exhausted code is rejected with a reason.
  - Removing the code restores the pre-coupon total.

---

## FR-PRM-008 — Usage limits and eligibility accounting

- **Actor:** System
- **Description:** Enforce total and per-user usage limits, and record usage when a promotion is
  applied to a placed order.
- **Rules:**
  - Usage is counted when an order is placed, not when a code is entered.
  - A per-user limit counts distinct customers; guest usage is tracked per order email or order.
  - Concurrent application cannot exceed a limit; usage accounting is atomic with placement.
  - Canceled orders may release usage per policy; refunds do not automatically restore usage.
- **Acceptance Criteria:**
  - A promotion at its total limit is not applied to a new order.
  - A second use beyond a per-user limit is rejected.
  - Two concurrent checkouts cannot both consume the last remaining use.

---

## FR-PRM-009 — Promotion action extension

- **Actor:** Platform Admin
- **Description:** Allow new promotion actions through `IPromotionAction`, registered in DI,
  without modifying core modules.
- **Rules:**
  - `IPromotionAction` receives the order or line context and returns adjustments (spec §8).
  - Built-in actions implement the contract; custom actions register in DI with
    last-registered-wins semantics.
  - A custom action is subject to the same clamping and application-order rules.
- **Acceptance Criteria:**
  - A custom action can be registered and applied to an order.
  - Registering a custom action requires no core changes.
  - A custom action's output is clamped like that of built-in actions.

---

## FR-PRM-010 — Admin promotion management API

- **Actor:** Store Admin; Platform Admin
- **Description:** Expose CRUD for promotions, rules, actions, and coupon codes over
  `/api/admin`, scoped to the selected store.
- **Rules:**
  - Every mutation requires the `admin` role plus store scope and is written to the audit log.
  - An admin can preview a promotion's effect on a sample order without persisting adjustments.
  - Validation rejects malformed rules or actions and duplicate coupon codes.
- **Acceptance Criteria:**
  - An admin scoped to store A cannot edit store B's promotions.
  - A preview shows the computed adjustment for a sample order.
  - Creating a duplicate coupon code within the same store is rejected.

---

## Open Questions

1. The default priority values and tie-breakers for application order are finalized in the
   promotion engine `03/02-04`.
2. The promotion test scenarios and expected adjustments are owned by `06/02-02`.
3. Whether promotions can be shared across stores via a platform template is deferred to the
   multi-store model `03/03-02`.
