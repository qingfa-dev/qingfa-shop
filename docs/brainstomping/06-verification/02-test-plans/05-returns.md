# Returns Test Plan

Case-by-case verification of the returns module: partial and full returns, refund versus store credit, exchange creating new order lines, restock movements, and the return lifecycle.

**Links:** [Test matrix](../01-test-strategy/01-matrix.md) · [Unit tests](../01-test-strategy/02-unit.md) · [Integration tests](../01-test-strategy/03-integration.md) · [Order lifecycle](../../03-architecture/02-components/03-order-lifecycle.md) · [Inventory model](../../03-architecture/02-components/06-inventory-model.md) · [Payments requirements](../../01-requirements/02-functional/05-payments.md) · [Returns requirements](../../01-requirements/02-functional/09-returns.md) · [Verification index](../README.md) · [Spec](../../11-spec/README.md)

---

## Scope

This plan proves the returns contract in `01/02-09` as designed by `03/02-03` for the lifecycle
and `03/02-06` for restock. It covers return authorization against a shipped or completed order,
return items and quantities, reasons, reception and inspection, refund versus store credit,
exchange creating new order lines, and restocking accepted items through the inventory ledger.

It assumes these rulings: a return references the original order and lines without editing them;
the refund amount is the price paid for accepted items and never exceeds the captured amount minus
prior refunds; store credit is a store-scoped ledger grant; an exchange creates new order lines
rather than mutating the original; restocking writes a positive return movement, unsellable
quantities write none; `reserved` is a counter and returns never change it; and completing a return
does not change the original order's computed totals.

Layers: quantity limits, lifecycle transitions, and reason requirements are unit tests; refund
ceiling, restock linkage, exchange settlement, and admin store scoping are integration tests; the
admin authorize → receive → inspect → restock → refund journey is an E2E spec.

---

## Preconditions

- A shipped or completed order with at least two line items, one of which has quantity greater than
  one, captured through a payment gateway.
- An inventory location that can receive restocked goods, and a variant with a known `on_hand`.
- An admin-defined set of return reasons, including one customer-fault and one merchant-fault reason.
- Store credit ledger enabled for the store and a capturing gateway reference on the order.

---

## Cases

### RET-01 — A partial return within the purchased quantity is accepted

- **Type:** unit
- **FR reference:** FR-RET-002
- **Steps:**
  1. Reference an order line with purchased quantity 3.
  2. Create a return item with quantity 2.
- **Expected:**
  - The return item is accepted with the returned quantity 2.
  - The original line item's quantity remains 3 and is not edited.

### RET-02 — A return exceeding the unreturned quantity is rejected

- **Type:** unit
- **FR reference:** FR-RET-002
- **Steps:**
  1. Create a return of 2 units against a line of 3, record it, then attempt a second return of 2 more.
  2. Attempt a return item with quantity 0.
- **Expected:**
  - The second return is rejected because the cumulative returned quantity would exceed 3.
  - A zero-quantity return item is not created; a permitted second return may take the remaining 1 unit.

### RET-03 — A full accepted return refunds through the original gateway

- **Type:** integration
- **FR reference:** FR-RET-005
- **Steps:**
  1. Receive and inspect a full return of all purchased units as restockable.
  2. Settle it as a refund.
- **Expected:**
  - The refund is routed to the original capturing gateway for the price paid for the accepted items.
  - A refund transaction and an audit entry are recorded; the original order's totals are unchanged.

### RET-04 — Refund never exceeds captured minus prior refunds

- **Type:** integration
- **FR reference:** FR-RET-005, FR-PAY-006
- **Steps:**
  1. Refund part of a captured order, then attempt a second refund that would exceed the captured remainder.
- **Expected:**
  - The second refund is rejected at the settlement ceiling.
  - The total refunded never exceeds the captured amount minus prior refunds.

### RET-05 — Store credit grants an equal ledger credit

- **Type:** integration
- **FR reference:** FR-RET-005, FR-PAY-007
- **Steps:**
  1. Receive and inspect an accepted return.
  2. Settle it as store credit instead of a gateway refund.
- **Expected:**
  - A store-scoped ledger credit equal to the refund amount is granted and no gateway refund is issued.
  - The credit is visible on the customer's store-scoped balance.

### RET-06 — An exchange creates new order lines without mutating the original

- **Type:** integration
- **FR reference:** FR-RET-006
- **Steps:**
  1. Create an exchange from a received return, naming a replacement variant and quantity.
  2. Read back the original order and the replacement lines.
- **Expected:**
  - New order lines reference the replacement variant and are subject to normal reservation and pricing rules.
  - The original order's line items, captured prices, and totals are unchanged; the exchange carries its own settlement reference.

### RET-07 — Exchange price differences settle through normal paths

- **Type:** integration
- **FR reference:** FR-RET-006
- **Steps:**
  1. Exchange for a higher-priced replacement, then for a lower-priced one.
- **Expected:**
  - The higher-priced replacement records an amount due charged through payment.
  - The lower-priced replacement records a refund or store credit; neither edits the original captured prices.

### RET-08 — Restocking writes a return movement at the chosen location

- **Type:** integration
- **FR reference:** FR-RET-007, FR-INV-006
- **Steps:**
  1. Inspect a return item as 2 restockable units and restock at the nominated location.
  2. Read the stock item and movements.
- **Expected:**
  - A `Return` movement with a +2 delta increases `on_hand` at that location.
  - `reserved` is unchanged, because returns never change the reservation counter.

### RET-09 — Unsellable quantities are written off, not stocked

- **Type:** integration
- **FR reference:** FR-RET-007
- **Steps:**
  1. Inspect a return item as unsellable and settle the return.
- **Expected:**
  - No positive movement is written and `on_hand` does not increase.
  - The unsellable quantity is recorded as a write-off for reporting.

### RET-10 — Restocking is idempotent per accepted item

- **Type:** integration
- **FR reference:** FR-RET-007
- **Steps:**
  1. Restock an accepted return item once and record `on_hand`.
  2. Repeat the restock action for the same return item.
- **Expected:**
  - The repeated action writes no second positive movement.
  - `on_hand` is not double-counted and the return item remains settled.

### RET-11 — The return state machine rejects illegal transitions

- **Type:** unit
- **FR reference:** FR-RET-008
- **Steps:**
  1. Attempt `requested → completed` directly.
  2. Drive `requested → authorized → received → inspected → completed` through the legal path.
- **Expected:**
  - The direct skip is rejected; each transition is recorded with actor and timestamp.
  - A return cannot move to reception before authorization.

### RET-12 — Completing a return leaves order totals unchanged

- **Type:** integration
- **FR reference:** FR-RET-008
- **Steps:**
  1. Record the original order's computed total.
  2. Complete a return with a refund and a store credit.
- **Expected:**
  - The original order's item total, adjustments, and tax remain unchanged.
  - Refunds and credits are recorded as separate transactions on the order's returns summary.

### RET-13 — Return eligibility is limited to shipped or completed orders

- **Type:** integration
- **FR reference:** FR-RET-001
- **Steps:**
  1. Request a return on a `cart` order, then on a `shipped` order owned by the customer.
  2. Attempt a return on another customer's order.
- **Expected:**
  - The `cart` order and the foreign order are rejected.
  - The owned `shipped` order accepts the return and may move the order `→ returnable`.

### RET-14 — Every return item carries exactly one reason

- **Type:** integration
- **FR reference:** FR-RET-003
- **Steps:**
  1. Attempt to save a return item without a reason, then with an admin-defined reason.
- **Expected:**
  - The item without a reason is rejected with a validation error.
  - The item with a reason is accepted; deactivating that reason later does not alter the saved return.

### RET-15 — Admin settles a return end to end

- **Type:** E2E
- **FR reference:** FR-RET-004, FR-RET-010
- **Steps:**
  1. As an admin, authorize a requested return, receive it, inspect it, restock it, and issue a refund.
  2. Observe the return detail and the order's returns summary.
- **Expected:**
  - The return advances through the locked states and every operation is written to the audit log.
  - The detail exposes items, reasons, inspection results, refunds, credits, and state history.

### RET-16 — Reception shortfalls and waived inspection are recorded

- **Type:** integration
- **FR reference:** FR-RET-004
- **Steps:**
  1. Receive fewer units than requested and inspect a split of restockable and unsellable quantities.
  2. Waive inspection on another return and refund it.
- **Expected:**
  - The shortfall is recorded and does not block the return; the received quantity moves the return to `received`.
  - A waived inspection moves the return directly to `inspected` and the waiver is recorded in the audit log.

---

## Open Questions

1. The exact return-window and eligibility rules (for example days after delivery) are owned by the
   order lifecycle `03/02-03`; this plan asserts the allowed entry states, not the window length.
2. Whether shipping and tax are refundable by default is a store-policy detail confirmed with this
   plan; the refund ceiling asserted here always caps at captured minus prior refunds.
3. Whether an exchange reuses the original order or creates a brand-new order number is resolved by
   the order aggregate `03/02-03`; either shape must create new lines and leave the original untouched.
