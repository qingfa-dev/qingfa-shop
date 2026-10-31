# Orders — Functional Requirements

What the orders module must do: the order aggregate, its state machine, line items,
addresses, adjustments, computed totals, and domain events.

**Links:** [Catalog](./01-catalog.md) · [Checkout](./03-checkout.md) · [Requirements index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Orders is the transactional heart of the platform. An `Order` is an **aggregate root** that
owns its line items, ship/bill addresses, and adjustments, and moves through a single, locked
state machine. Every status change is a state transition and nothing else mutates status; each
transition emits a domain event through an **outbox table in the same transaction** as the
change (spec §6, `01/01-03`).

Order totals are always **computed** from source amounts + adjustments + tax; no total is
cached in a single column. Orders are priced in the store currency using `Money`. The
authoritative lifecycle and entity detail is planned in `03/02-03`; module contracts live in
`03/02-01`.

Actors are defined in `01/01-02`.

---

## FR-ORD-001 — Order aggregate and cart

- **Actor:** Guest; Customer
- **Description:** Create an order in the `cart` state to hold a shopper's selections before
  checkout, and treat the cart as the same aggregate that later becomes a placed order.
- **Rules:**
  - A new order starts in `cart` and belongs to exactly one store.
  - A guest cart is anonymous; when a customer signs in, an existing cart is associated with
    that customer.
  - The store currency is fixed when the order is created and does not change afterward.
  - Only one active cart per customer per store is expected; merging rules are explicit.
- **Acceptance Criteria:**
  - Adding a first item creates a cart in the `cart` state for the resolved store.
  - Signing in associates the anonymous cart with the customer without losing line items.
  - The order's currency matches the store's currency for the whole session.

---

## FR-ORD-002 — Line items

- **Actor:** Guest; Customer; Store Admin
- **Description:** Add, update, and remove line items that reference a buyable variant with a
  quantity and a captured unit price.
- **Rules:**
  - A line item references a variant and records the `Money` unit price at add-time.
  - Adding the same variant again increases the quantity of the existing line item rather
    than creating a duplicate.
  - Quantity must be a positive integer; zero quantity removes the line item.
  - Line item totals are computed as `unit price × quantity` and are never stored as a single
    cached total.
- **Acceptance Criteria:**
  - Adding variant V twice yields one line item with quantity 2.
  - Setting a line item quantity to 0 removes it from the order.
  - A variant not active in the store is rejected at add time.

---

## FR-ORD-003 — Ship and bill addresses

- **Actor:** Guest; Customer
- **Description:** Attach a shipping address and a billing address to the order, with the
  billing address defaulting to the shipping address unless overridden.
- **Rules:**
  - The shipping address is required before an order can leave the delivery checkout step.
  - The billing address is required before payment is attempted.
  - An address records the fields needed to ship and to bill; it is snapshotted onto the order
    so later edits to a saved address do not rewrite order history.
  - A customer may select a previously saved address or enter a new one.
- **Acceptance Criteria:**
  - Proceeding from delivery without a shipping address is blocked.
  - Setting only a shipping address makes billing default to it.
  - Editing a saved customer address does not change an already-placed order's address.

---

## FR-ORD-004 — Order state machine

- **Actor:** System; Store Admin
- **Description:** Govern the order lifecycle through exactly the locked states and
  transitions; transitions are the only way to mutate status.
- **Rules:**
  - Forward states: `cart → awaiting_payment → awaiting_fulfillment → shipped → completed`.
  - Terminal/side states: `canceled` and `returnable`.
  - `cart → awaiting_payment` occurs on checkout confirm; `awaiting_payment →
    awaiting_fulfillment` on successful payment; `awaiting_fulfillment → shipped` on
    fulfillment/shipment; `shipped → completed` on completion.
  - An order may move to `canceled` before shipment; an order may move to `returnable` after
    shipment, per the returns flow (`01/02-09`).
  - A transition not defined by the machine is rejected, and no other code path mutates
    status.
- **Acceptance Criteria:**
  - An order cannot jump from `cart` directly to `shipped`.
  - Confirming checkout moves the order to `awaiting_payment`.
  - A successful payment moves the order to `awaiting_fulfillment`.
  - Attempting an undefined transition returns a conflict error.

---

## FR-ORD-005 — Adjustments

- **Actor:** System; Store Admin
- **Description:** Record line-item and order-level adjustments (promotion discounts,
  shipping discounts, manual corrections) as rows that reference the originating rule or promotion.
- **Rules:**
  - An adjustment has an amount (`Money`), a source (promotion, shipping, manual), and a
    reference to the rule that produced it.
  - Adjustments may be line-item-scoped or order-scoped.
  - Per-line clamping ensures a discount never drives a line below zero.
  - Adjustments are additive records; changing a promotion does not mutate historical
    adjustments on placed orders.
- **Acceptance Criteria:**
  - A promotion application creates an adjustment referencing that promotion.
  - A discount larger than a line total is clamped so the line never goes negative.
  - The order's adjustments are retrievable with their source and amount.

---

## FR-ORD-006 — Computed order totals

- **Actor:** System; Store Admin; Customer
- **Description:** Compute order totals from source amounts, adjustments, and tax on demand,
  never from a single cached total column.
- **Rules:**
  - Item total = sum of line items (unit price × quantity).
  - Adjustment total = sum of line-item + order-level adjustments.
  - Tax is computed on post-promotion amounts (see `01/02-06`).
  - Order total = item total + adjustments + tax, expressed as `Money` in the store currency.
  - Totals are recomputed whenever a contributing value changes, including during checkout.
- **Acceptance Criteria:**
  - Adding a line item changes the computed total immediately.
  - A promotion adjustment is reflected in the computed total.
  - No persisted column is the authoritative total; the value is derived from its parts.

---

## FR-ORD-007 — Domain events via outbox

- **Actor:** System
- **Description:** Emit a domain event for every order state transition, written to an outbox
  table in the same transaction that performs the transition, then relayed to consumers.
- **Rules:**
  - Events include at least `OrderPlaced`, `PaymentCompleted`, `OrderShipped`, plus
    cancellation and return events.
  - The event row is committed atomically with the state change; a relay publishes it after
    commit and marks it delivered.
  - Delivery is at-least-once; consumers must be idempotent.
  - Emails (order confirmation, shipment) and analytics subscribe to these events.
- **Acceptance Criteria:**
  - Confirming checkout persists the `OrderPlaced` outbox row in the same transaction.
  - A successful payment persists `PaymentCompleted` alongside the status change.
  - If the process restarts after commit but before relay, the event is still delivered.

---

## FR-ORD-008 — Cancel an order

- **Actor:** Store Admin; Customer
- **Description:** Cancel an order that has not shipped, moving it to `canceled` and reversing
  reserved stock and any uncanceled payment appropriately.
- **Rules:**
  - Cancellation is allowed from `cart`, `awaiting_payment`, and `awaiting_fulfillment`.
  - Cancellation after shipment is not allowed; that path is a return.
  - Reserved stock is released on cancellation.
  - A captured payment is refunded or voided through the originating gateway and the outcome
    recorded.
- **Acceptance Criteria:**
  - Canceling an `awaiting_fulfillment` order moves it to `canceled`.
  - Stock reserved by the order becomes available again.
  - Attempting to cancel a `shipped` order is rejected.

---

## FR-ORD-009 — Returnable orders

- **Actor:** Store Admin; Customer
- **Description:** Mark a shipped order `returnable` and hand it to the returns flow for
  return authorization, receipt, and refund.
- **Rules:**
  - `returnable` is reachable from `shipped` and `completed` for eligible items.
  - The returns flow (`01/02-09`) owns return authorization, inspection, and restocking.
  - A return never mutates the original line items; it references them.
  - Refunds follow the gateway used for capture, per `01/01-03`.
- **Acceptance Criteria:**
  - A shipped order can be moved to `returnable`.
  - A return references the original line items without editing them.
  - A completed return records a refund against the original payment.

---

## FR-ORD-010 — Admin order operations

- **Actor:** Store Admin; Platform Admin
- **Description:** List and inspect orders, and perform fulfillment actions (capture, fulfill,
  ship, cancel, refund, start return) through `/api/admin`, scoped to the selected store.
- **Rules:**
  - Every operation requires the `admin` role plus store scope, and is written to the audit
    log.
  - The order detail returns line items, addresses, adjustments, payments, shipments, and
    state history.
  - State-changing operations go through the state machine (FR-ORD-004), never direct status
    writes.
  - List views support filtering by state, number, and date.
- **Acceptance Criteria:**
  - An admin can filter orders to `awaiting_fulfillment` for their store.
  - A ship action moves the order to `shipped` and emits `OrderShipped`.
  - An admin scoped to store A cannot view store B's orders.

---

## FR-ORD-011 — Customer order history

- **Actor:** Customer
- **Description:** Authenticated customers can list and view their own orders through
  `/api/v1`, including line items, totals, state, and shipment status.
- **Rules:**
  - A customer sees only their own orders.
  - Guest orders are not listed without an account; claim rules are defined by the accounts
    module (`01/02-10`).
  - Totals shown are computed from the same parts as the admin view.
  - Shipment tracking references are exposed when present.
- **Acceptance Criteria:**
  - A customer's order list contains only their orders.
  - Requesting another customer's order returns a not-found or forbidden response.
  - The order detail shows the computed total with its currency.

---

## FR-ORD-012 — Store scoping and currency integrity

- **Actor:** System
- **Description:** Enforce that every order is bound to one store and priced in that store's
  currency for its entire lifecycle.
- **Rules:**
  - An order cannot move between stores.
  - All line items, adjustments, tax, and payments on an order use the order's currency.
  - Attempts to attach a `Money` of a different currency are rejected.
  - A store's currency cannot be changed while it has open orders in that currency.
- **Acceptance Criteria:**
  - A payment in a currency other than the order's currency is rejected.
  - An order created in store A is never returned under store B.
  - Changing a store's supported currencies does not alter existing orders.

---

## Open Questions

1. The exact guest-order claim behavior on sign-in is finalized by the accounts module
   (`01/02-10`); this file only asserts that order history is customer-scoped.
2. The granularity of shipment records (one per order vs. partial shipments) is detailed in
   the shipping requirements (`01/02-04`) and the lifecycle doc `03/02-03`.
3. Which event names beyond the required set (`OrderPlaced`, `PaymentCompleted`,
   `OrderShipped`) are part of the public catalog is finalized in the architecture event
   catalog (`03/01-03`).
