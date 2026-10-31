# Promotion Engine

Promotion definitions with rules and actions, the deterministic application order, the per-line clamping algorithm, coupon validation, usage limits, and the adjustments the engine produces.

**Links:** [Order lifecycle](./03-order-lifecycle.md) · [Tax engine](./05-tax-engine.md) · [Extension architecture](./07-extension-architecture.md) · [Module contracts](./02-module-contracts.md) · [Promotions requirements](../../01-requirements/02-functional/07-promotions.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

The Promotions module decides which discounts apply to an order and represents every effect as a
**promotion adjustment**. A `Promotion` carries channels, an active window, priority, and usage
limits; **rules** gate eligibility (coupon, user, product/variant, order total, shipping method);
and **actions** decide the effect (percent or amount off the order or an item, fixed-price item,
free shipping, Nth-item free). The engine applies eligible promotions in a documented order,
clamps per line, and returns adjustments that Orders persists (`03/02-03`).

Promotions never mutate tax and never edit captured unit prices. This engine produces promotion
adjustments only; tax is computed separately on post-promotion amounts by `03/02-05`
(`FR-PRM-006`). The extension seam for new actions is `IPromotionAction` (`03/02-07`); promotion
test scenarios are owned by `06/02-02`.

---

## Promotion Definition

A `Promotion` row is store-scoped and contains:

| Field | Meaning |
|---|---|
| `Id`, `StoreId`, `Name` | Identity and scope. |
| `Channels` | Set of channels (`storefront`, `admin`, `api`) the promotion may apply in. A coupon-only promotion is not auto-applied. |
| `StartsAtUtc`, `EndsAtUtc` | Active window; outside it the promotion is inactive. |
| `Priority` | Explicit ordering weight; higher applies earlier. |
| `TotalUsageLimit` | Maximum number of placed orders the promotion may apply to; `null` means unlimited. |
| `PerUserLimit` | Maximum uses per customer; `null` means unlimited. |
| `IsDisabled` | Disabling stops new applications without mutating historical adjustments. |
| `ActionKey` | Key resolved through `IPromotionAction` (`03/02-07`). |
| `Parameters` | Action parameters (for example `percent=10`, `amount=5`, `nth=3`). |

---

## Rules

Rules combine with **AND** semantics: every rule on a promotion must pass for the promotion to be
eligible (`FR-PRM-002`). A promotion with no rules is eligible whenever it is active in its window
and channel. Eligibility is always evaluated server-side against the current order.

| Rule | Passes when |
|---|---|
| `Coupon` | A validated coupon code matches the promotion's code, case-insensitively. |
| `User` | The customer (or guest email) belongs to the configured segment or matches the specific customer. |
| `ProductVariant` | At least one qualifying line item references a configured product or variant. |
| `OrderTotal` | The order subtotal (before promotion discounts) meets the configured minimum. |
| `ShippingMethod` | The selected shipping method is among the configured methods. |

A `ProductVariant` rule also supplies the line matcher used by item-scoped actions:
`PromotionDefinition.Matches(PromotionLine)` returns true only for lines the promotion may discount
(`03/02-07`).

---

## Actions

An action receives the promotion definition, the order lines, the coupon code, and the running
subtotal, and returns either `NotApplied` or `Applied(adjustments)` (`03/02-07`).

| Action | Scope | Base | Output |
|---|---|---|---|
| Percent off order | order | Running post-discount subtotal. | One order-scoped negative adjustment. |
| Amount off order | order | Running post-discount subtotal. | One order-scoped negative adjustment. |
| Percent off item | line | Running post-discount line amount. | Line-scoped negative adjustments. |
| Amount off item | line | Running post-discount line amount. | Line-scoped negative adjustments. |
| Fixed-price item | line | Configured effective unit price. | Line-scoped negative adjustment equal to the price reduction. |
| Free shipping | order | Running post-discount shipping amount. | One order-scoped negative adjustment against shipping cost. |
| Nth-item free | line | The cheapest `floor(quantity / N)` qualifying units per line. | Line-scoped negative adjustments. |

A fixed-price action sets the effective unit price to the configured amount, clamped at zero,
without editing the captured unit price. A free-shipping action reduces the shipping cost to zero
through its own promotion adjustment, distinct from the original shipping adjustment, so both rows
remain on the order (`FR-SHP-006`).

---

## Application Order

Eligible promotions are applied sequentially in a stable, request-order-independent sequence
(`FR-PRM-004`):

1. **Build the candidate set.** Active in the store, active in the current channel, inside the
   window, not disabled, and under both usage limits.
2. **Evaluate rules.** Every rule must pass. A coupon rule requires a code that has already passed
   `ICouponValidator` validation.
3. **Sort the candidates.** By `Priority` descending, then `StartsAtUtc` ascending, then `Id`
   ascending. This tuple is total and stable, so recomputation yields identical adjustments.
4. **Order the actions inside a promotion.** Line-level actions apply before order-level actions.
   Actions of the same scope apply in their configured order.
5. **Compute against running amounts.** Each action sees the post-discount amounts produced by all
   previously applied actions, so later promotions discount the already-reduced total.
6. **Clamp and record.** Apply the clamping algorithm below, then record the resulting adjustment(s)
   in application order so the audit trail reflects the exact sequence.

The applied order is stored alongside the resulting adjustments so the outcome is reproducible and
auditable.

---

## Per-Line Clamping Algorithm

The engine tracks a running `remaining` amount per line and the running `subtotal = Σ remaining`.

```
remaining[line] = line.Amount
subtotal        = Σ remaining

for each action in application order:
    if action is line-scoped:
        raw     = action.compute(line, remaining[line])
        applied = min(raw, remaining[line])          # never below zero
        remaining[line] -= applied
        emit adjustment(-applied, scope = line)

    if action is order-scoped:
        raw     = action.compute(order, subtotal)
        applied = min(raw, subtotal)                 # never below zero
        subtotalBefore = subtotal
        subtotal -= applied
        # allocate applied across lines proportional to remaining, to
        # clamp each share and to feed the per-line tax base
        for each line:
            share = round(applied × remaining[line] / subtotalBefore)
            share = min(share, remaining[line])      # rounding guard
            remaining[line] -= share
        emit adjustment(-applied, scope = order)
```

Line-scoped discounts are recorded per line. An order-scoped discount is recorded once at order
scope with its clamped total; its proportional allocation is computed only to enforce the
no-negative-line rule and to provide the per-line tax base. Rounding uses half-up to the currency
minor units, and any rounding remainder is assigned to the line with the largest remaining amount
so the shares sum exactly to the applied total.

### Worked example — 10% off + $5 off on a two-line order

Order: line A = 3 × $20.00 = $60.00; line B = 1 × $40.00 = $40.00; item total = $100.00.
P1 = "10% off order" (order-scoped, priority 100). P2 = "$5 off order" (order-scoped, priority 50).

| Step | Line A remaining | Line B remaining | Subtotal | Action |
|---|---|---|---|---|
| start | $60.00 | $40.00 | $100.00 | — |
| apply P1 | $54.00 | $36.00 | $90.00 | raw = 10% × $100 = $10; applied = $10; allocation A = $10 × 60/100 = $6.00, B = $4.00. |
| apply P2 | $51.00 | $34.00 | $85.00 | raw = $5; applied = $5; allocation A = $5 × 54/90 = $3.00, B = $5 × 36/90 = $2.00. |

Result: two order-scoped adjustments, −$10.00 and −$5.00; adjustment total = −$15.00; post-promotion
subtotal = $85.00, with per-line bases A = $51.00 and B = $34.00 passed to the tax engine.

**Clamping.** If P2 had been "$120 off order", `raw = $120 > subtotal $90`, so `applied = $90` and
both lines reach zero; no line goes negative. The recorded adjustment is −$90.00, not −$120.00.

---

## Coupon Validation

`ICouponValidator.ValidateAsync(code, store)` is a read-only check used to preview an adjustment at
cart or checkout (`FR-PRM-007`). It normalizes the code (trim, case-insensitive comparison against
the unique-per-store code), loads the coupon-rule promotion, and returns either the promotion or a
reason:

| Reason | Condition |
|---|---|
| `NotFound` | No coupon with that code in the store. |
| `NotStarted` / `Expired` | Outside the promotion window. |
| `Exhausted` | Total or per-code usage limit reached. |
| `UserIneligible` | The user rule does not match the current customer or guest email. |
| `ChannelMismatch` | The promotion is not enabled for the current channel. |

Validation is never trusted from the client, and the client is never sent a discount amount it can
reuse (`FR-CHK-008`). Applying a valid code recomputes adjustments and totals; removing it removes
its adjustment and restores the pre-coupon total. At confirm the code is revalidated and its usage
is recorded atomically with placement.

---

## Usage Limits

Usage is counted when an order is **placed**, not when a code is entered (`FR-PRM-008`).

- `TotalUsageLimit` bounds placed orders across the store; `PerUserLimit` bounds uses per customer.
- Guest usage is tracked per order email or per order, per store policy.
- Usage accounting is atomic with placement: the increment uses a conditional update guarded by the
  limit (`UPDATE ... WHERE used < limit`) inside the confirm transaction, so two concurrent
  checkouts cannot both consume the last remaining use.
- A canceled order may release its usage per policy; a refund does not automatically restore usage.
- Reaching a limit emits `PromotionExhausted`; the promotion is then excluded from the candidate set
  (`03/02-02`).

---

## Adjustments Produced

Every applied action yields one or more `PromotionAdjustment` values, each naming a line item (or
order scope), a signed `Money` amount, a source key (the action key), and a label (`03/02-07`).
Orders persists each as an `Adjustment` with `Source = promotion` and
`SourceReference = <promotion id>/<action key>` (`03/02-03`).

- Line-scoped actions produce line-item-scoped adjustments.
- Order-scoped actions produce one order-scoped adjustment carrying the clamped total.
- Free shipping produces an order-scoped adjustment against shipping cost, kept as a separate row
  from the original shipping adjustment.
- Captured unit prices are never edited, and historical adjustments on placed orders are immutable.

## Open Questions

1. The concrete default `Priority` values shipped for built-in promotion templates are owned by the
   admin promotion API `03/04-02`; the ordering tuple here is fixed regardless.
2. Promotion test scenarios and their expected adjustment sets are owned by `06/02-02`.
3. Whether promotions can be shared across stores via a platform template is deferred to the
   multi-store model `03/03-02`.

## Links

- [Order lifecycle](./03-order-lifecycle.md)
- [Tax engine](./05-tax-engine.md)
- [Extension architecture](./07-extension-architecture.md)
- [Module contracts](./02-module-contracts.md)
- [Promotions requirements](../../01-requirements/02-functional/07-promotions.md)
- [Shipping requirements](../../01-requirements/02-functional/04-shipping.md)
- [Design spec](../../11-spec/README.md) — §6 (promotion engine),
  §8 (extension contracts).
- Entity model: `03/03-01`; promotion tests: `06/02-02`; admin promotion API: `03/04-02`.
