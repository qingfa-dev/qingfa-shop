# Returns — Functional Requirements

What the returns module must do: return authorization, return items and reasons, reception and
inspection, refunds vs store credit, exchanges, and restocking.

**Links:** [Orders](./02-orders.md) · [Payments](./05-payments.md) · [Inventory](./08-inventory.md) · [Requirements index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Returns handles the money and stock consequences of sending purchased goods back. A **Return**
references the original order and its line items without editing them (FR-ORD-009), carries a
lifecycle state, and holds one or more **return items** each with a quantity and a **reason**.
Money leaves through the originating payment as a refund, or as **store credit** when the
customer prefers it (spec §6, FR-PAY-007).

An **exchange** does not mutate the returned order: it creates **new order lines** for the
replacement variants and settles any price difference. Accepted stock is **restocked** through
the inventory ledger, while unsellable stock is written off (FR-INV-006). The return flow and
state machine are planned in `03/02-03`; return test scenarios are owned by `06/02-05`.

Returns are store-scoped and always reference items from a shipped order. Refunds follow the
gateway that captured the payment and never exceed the captured amount minus prior refunds
(FR-PAY-006).

Actors are defined in `01/01-02`.

---

## FR-RET-001 — Return authorization

- **Actor:** Customer; Store Admin
- **Description:** Create a return against an eligible order, capturing the items and quantities
  the customer intends to send back, and issue a return authorization.
- **Rules:**
  - A return is created against an order in `shipped` or `completed`; a return may move the
    order to `returnable` (FR-ORD-009).
  - A return starts in a `requested` state and is authorized explicitly by an admin before
    goods are accepted.
  - A customer may request a return only for their own orders; an admin may create one on the
    customer's behalf.
  - The return references the original order and line items and never edits them.
- **Acceptance Criteria:**
  - A customer can request a return on a shipped order they own.
  - A return for another customer's order is rejected.
  - An unauthorized return cannot advance to reception.

---

## FR-RET-002 — Return items and quantities

- **Actor:** Customer; Store Admin
- **Description:** Track one return item per returned order line, with a quantity that never
  exceeds the quantity originally purchased and not already returned.
- **Rules:**
  - A return item references an original line item and carries a returned quantity.
  - The sum of returned quantities across all returns of a line never exceeds the purchased
    quantity.
  - A return item with quantity zero is not created.
  - Return items are immutable once the return is received; later corrections create a new
    adjustment rather than editing history.
- **Acceptance Criteria:**
  - Requesting 2 of 3 purchased units is accepted; requesting 4 is rejected.
  - A second return on the same line cannot exceed the remaining unreturned quantity.
  - The original line item's quantity is unchanged by the return.

---

## FR-RET-003 — Return reasons

- **Actor:** Store Admin; Customer
- **Description:** Classify each return item with an admin-defined reason so returns can be
  reported and routed.
- **Rules:**
  - Reasons are store-scoped and admin-managed; examples include damaged, wrong item, no longer
    wanted, and not as described.
  - Every return item carries exactly one reason.
  - A reason may be flagged as customer-fault or merchant-fault, which can influence restocking
    and fees.
  - Reasons are used in reporting and are never used to edit the original order.
- **Acceptance Criteria:**
  - A return item cannot be saved without a reason.
  - Admin reports can group returns by reason.
  - Deactivating a reason does not change returns that already reference it.

---

## FR-RET-004 — Reception and inspection

- **Actor:** Store Admin
- **Description:** Receive a returned shipment, inspect the goods, and record accepted, rejected,
  or damaged quantities per return item.
- **Rules:**
  - Reception records the received quantity per return item and moves the return to
    `received`.
  - Inspection marks each received quantity as restockable or unsellable, and completing
    inspection moves the return from `received` to `inspected`.
  - A return cannot be refunded before inspection completes unless an admin explicitly waives
    inspection; a waived inspection moves the return directly to `inspected`.
  - Reception and inspection are written to the audit log (`03/04-03`).
- **Acceptance Criteria:**
  - Receiving fewer units than requested records the shortfall and does not block the return.
  - Inspection can split a received quantity into restockable and unsellable portions.
  - A waived-inspection refund is recorded as such in the audit log.

---

## FR-RET-005 — Refund vs store credit

- **Actor:** Store Admin; Customer
- **Description:** Settle an accepted return by refunding the originating payment or by granting
  store credit, at the customer's choice where the store allows.
- **Rules:**
  - The refund amount is the price paid for the accepted return items, allocated per line and
    never exceeding the captured amount minus prior refunds (FR-PAY-006).
  - Refunds are routed to the original capturing gateway; offline payments are refunded manually
    with an audit entry.
  - Store credit is granted through the ledger and is store-scoped (FR-PAY-007).
  - Shipping and tax are refunded only per the store's return policy.
- **Acceptance Criteria:**
  - A full accepted return refunds the line amounts through the original gateway.
  - Choosing store credit grants an equal ledger credit instead of a gateway refund.
  - A refund exceeding the captured remainder is rejected.

---

## FR-RET-006 — Exchanges create new order lines

- **Actor:** Store Admin; Customer
- **Description:** Handle an exchange by creating new order lines for the replacement variants
  and settling any price difference, without mutating the original order.
- **Rules:**
  - An exchange creates a new order (or new lines on a replacement order) referencing the
    replacement variant and quantity.
  - Any price difference is charged or refunded through the normal payment and store-credit
    paths; the original order's captured prices are never edited.
  - Replacement lines are subject to the same stock reservation and pricing rules as a normal
    order (FR-CHK-009, FR-INV-005).
  - An exchange may be created from a received return and carries its own settlement reference.
- **Acceptance Criteria:**
  - An exchange produces new order lines for the replacement variant.
  - A higher-priced replacement records the amount due; a lower-priced one records a refund or
    credit.
  - The original order's line items and totals are unchanged.

---

## FR-RET-007 — Restocking accepted items

- **Actor:** System; Store Admin
- **Description:** Return restockable quantities to inventory through the stock ledger and write
  off unsellable quantities.
- **Rules:**
  - Restocking writes a return movement at the nominated location (FR-INV-006).
  - Only quantities marked restockable during inspection become positive stock.
  - Unsellable quantities write no positive movement and are recorded as a write-off for
    reporting.
  - Restocking happens once per accepted return item; a repeated action is idempotent.
- **Acceptance Criteria:**
  - A restockable quantity increases `on_hand` at the chosen location.
  - An unsellable quantity does not increase stock and is reported as a write-off.
  - Repeating restocking for the same return item does not double-count stock.

---

## FR-RET-008 — Return lifecycle and order interaction

- **Actor:** System; Store Admin
- **Description:** Move a return through its locked states and reflect accepted returns in the
  order's returns summary without changing order totals.
- **Rules:**
  - Return states are `requested → authorized → received → inspected → completed`, plus
    `canceled` and `rejected`.
  - Completing a return does not change the original order's computed totals; it records refund
    transactions and credits separately.
  - Canceling a return before reception releases nothing that was not already held.
  - Each state transition is recorded with actor and timestamp.
- **Acceptance Criteria:**
  - A return cannot move from `requested` directly to `completed`.
  - A completed return leaves the original order's computed total unchanged.
  - The order detail shows the returns and their settlement amounts.

---

## FR-RET-009 — Return notifications

- **Actor:** System
- **Description:** Notify the customer when a return is authorized, received, and settled.
- **Rules:**
  - Return events drive transactional email through `IEmailSender`
    (`03/04-03`, Payment/Shipping Providers in `01/01-02`).
  - Notifications include the return reference, status, and, on settlement, the refund or credit
    amount.
  - Notification failures do not roll back the underlying state change; delivery is retried.
- **Acceptance Criteria:**
  - Authorizing a return queues a notification to the customer.
  - Settling a return queues a notification naming the refund or credit amount.
  - A failed send does not undo the return state transition.

---

## FR-RET-010 — Admin returns API

- **Actor:** Store Admin; Platform Admin
- **Description:** List, inspect, authorize, receive, inspect, restock, and settle returns over
  `/api/admin`, scoped to the selected store.
- **Rules:**
  - Every operation requires the `admin` role plus store scope and is written to the audit log.
  - The return detail exposes items, reasons, inspection results, refunds, credits, and state
    history.
  - State changes go through the return state machine, never through direct writes.
  - Customers view their own returns through `/api/v1` account endpoints (FR-ORD-011).
- **Acceptance Criteria:**
  - An admin can authorize and receive a requested return.
  - An admin scoped to store A cannot view store B's returns.
  - A settlement action records both a transaction and an audit entry.

---

## Open Questions

1. The exact return-window and eligibility rules (for example days after delivery) are finalized
   in the order-lifecycle doc `03/02-03`.
2. Whether shipping and tax are refundable by default is a store-policy detail confirmed with
   the returns test plan `06/02-05`.
3. Whether exchanges reuse the original order or create a brand-new order number is finalized
   with the order aggregate in `03/02-03`.
