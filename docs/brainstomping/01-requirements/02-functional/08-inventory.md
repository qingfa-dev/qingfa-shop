# Inventory — Functional Requirements

What the inventory module must do: stock locations, stock items per variant and location,
`available` vs `on_hand`, the stock-movement ledger, reservation on confirm, backorders, and
transfers.

**Links:** [Catalog](./01-catalog.md) · [Orders](./02-orders.md) · [Checkout](./03-checkout.md) · [Requirements index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Inventory owns how much of each variant can be sold and from where. It defines **stock
locations** (warehouses, retail floors), holds a **`StockItem` per (variant, location)**, and
tracks every quantity change in an append-only **`StockMovement` ledger**. Two quantities are
distinct: **`on_hand`** is the physical count at a location, and **`reserved`** is the quantity
committed to confirmed orders; **`available`** is derived as `on_hand − reserved` (spec §6).

Stock is reserved when an order is **confirmed**, not when it is added to a cart (FR-CHK-006),
and released on cancellation or expiration. No location may go negative unless it is explicitly
configured to allow **backorders** (FR-INV-007). The inventory data model and movement taxonomy
are planned in `03/02-06`; inventory test scenarios are owned by `06/02-04`.

Inventory is store-scoped: locations, stock items, and movements belong to one `Store` and are
never visible to another. All quantity effects that originate in orders or returns are recorded
here so the ledger remains the single source of stock truth.

Actors are defined in `01/01-02`.

---

## FR-INV-001 — Stock locations

- **Actor:** Store Admin; Platform Admin
- **Description:** Define stock locations where inventory is held, and mark one location as the
  default for a store.
- **Rules:**
  - A location has a name, an address, and an active/inactive flag, and belongs to exactly one
    store.
  - Exactly one active location per store is the default used when no location is specified.
  - A location may be flagged to allow backorders (FR-INV-007).
  - Deactivating a location keeps its history and stock items but removes it from new
    allocation and admin pickers.
- **Acceptance Criteria:**
  - An admin can create a location and later retrieve it within the same store.
  - Newly received stock with no explicit location lands at the store's default location.
  - A location from store A is never selectable for store B's orders or transfers.

---

## FR-INV-002 — Stock item per (variant, location)

- **Actor:** Store Admin; System
- **Description:** Maintain exactly one `StockItem` for each (variant, location) pair that holds
  stock, exposing its `on_hand` and reserved quantities.
- **Rules:**
  - A `StockItem` is uniquely identified by (variant, location); creating a duplicate pair is
    rejected.
  - Creating a variant does not automatically create stock items; a stock item comes into
    existence when stock is first received at a location.
  - A variant with no stock item at any location is treated as zero stock, not as an error, and
    may still be purchasable when a location allows backorders.
  - Edits to `on_hand` go through stock movements, never a direct quantity write
    (FR-INV-004).
- **Acceptance Criteria:**
  - Receiving stock creates a stock item for the pair and no duplicate can be created.
  - Retrieving a stock item returns its `on_hand` and reserved quantities.
  - Deleting a stock item is refused while it has a non-zero reserved quantity.

---

## FR-INV-003 — `on_hand` vs `available`

- **Actor:** System; Store Admin
- **Description:** Track the physical count (`on_hand`) separately from the reserved quantity
  (`reserved`), and derive the sellable count as `available = on_hand − reserved`.
- **Rules:**
  - `on_hand` changes only through stock movements (receipts, shipments, returns, adjustments,
    transfers).
  - `reserved` is a counter for confirmed-order commitments; reservation increments it and
    release or shipment decrements it, and it is never an `on_hand` movement.
  - `available` is derived as `on_hand − reserved`, so reservations reduce what can be sold
    without changing the physical count.
  - `available` may be negative only when the location allows backorders (FR-INV-007).
  - The storefront sells against `available`, not `on_hand` (FR-INV-009).
- **Acceptance Criteria:**
  - Reserving quantity Q increments `reserved` and reduces `available` by Q while leaving
    `on_hand` unchanged.
  - Shipping a reservation writes a shipment movement that reduces `on_hand` and decrements
    `reserved` by the same quantity, so `available` stays consistent.
  - An admin view exposes `on_hand`, `reserved`, and `available` for a stock item.

---

## FR-INV-004 — Stock movement ledger

- **Actor:** System; Store Admin
- **Description:** Record every quantity change as an append-only `StockMovement` row so stock
  history is complete and auditable.
- **Rules:**
  - A movement carries a quantity (signed delta), a reason, a stock item reference, an actor,
    and a timestamp.
  - Movement reasons include receipt, shipment, return, adjustment, and transfer.
  - `on_hand` is the derived sum of a stock item's movement deltas; no mutable balance column is
    authoritative.
  - Reservation and reservation release change the `reserved` counter (FR-INV-005), not
    `on_hand`, so they are not `StockMovement` rows.
  - Movement rows are never updated or deleted; corrections are new opposite movements.
- **Acceptance Criteria:**
  - Receiving 10 units writes a +10 movement and yields `on_hand` of 10.
  - A correction is recorded as a new compensating movement, not an edit.
  - Summing a stock item's movements equals its reported `on_hand`.

---

## FR-INV-005 — Reservation on confirm

- **Actor:** System
- **Description:** Reserve ordered quantities in the confirm transaction so stock is committed
  atomically with order placement.
- **Rules:**
  - Reservation happens in the checkout confirm transaction, never on add-to-cart
    (FR-CHK-006).
  - Reservation increments the stock item's `reserved` counter, which reduces derived
    `available`; it writes no `StockMovement` and does not change `on_hand`.
  - If any line cannot be reserved under the applicable backorder policy, the whole confirm
    fails and no partial reservation persists.
  - Reserved stock is released on cancellation, payment failure per policy, or reservation
    expiry by decrementing `reserved`.
- **Acceptance Criteria:**
  - Confirming an order reserves exactly the ordered quantities for its lines.
  - A confirm that would exceed `available` at a non-backorder location is rejected atomically.
  - Canceling the order releases the reservation and restores `available`.

---

## FR-INV-006 — Shipment and return movements

- **Actor:** Store Admin; System
- **Description:** Convert a reservation into a shipment movement at fulfillment and a receipt
  movement when returned stock is restocked.
- **Rules:**
  - Shipping writes a shipment movement that decrements `on_hand` by the shipped quantity and
    decrements the `reserved` counter by the same quantity.
  - A return that is restocked increments `on_hand` at the nominated location through a return
    movement (FR-RET-007).
  - A return marked as unsellable writes no positive movement and may write an adjustment
    movement for the damaged quantity.
  - Manual admin adjustments (shrinkage, recount) write a movement with an adjustment reason
    and an audit entry.
- **Acceptance Criteria:**
  - Shipping an order writes a shipment movement and reduces `on_hand`.
  - Restocking a returned item writes a return movement and increases `on_hand`.
  - A manual recount writes an adjustment movement visible in stock history.

---

## FR-INV-007 — Backorder policy and no negative stock

- **Actor:** Store Admin; System
- **Description:** Prevent stock from going negative unless the location is explicitly
  configured to allow backorders, and expose backorder state for purchasing.
- **Rules:**
  - By default a location does **not** allow backorders; any operation that would drive
    `available` below zero is rejected.
  - When a location allows backorders, `available` may go negative and the storefront marks the
    variant as backordered with an expected availability message (FR-INV-009).
  - Backorder policy is set per location and evaluated at reservation and shipment time.
  - Changing backorder policy affects new operations, not already-reserved stock.
- **Acceptance Criteria:**
  - On a non-backorder location, a reservation beyond `available` is rejected.
  - On a backorder location, the same reservation succeeds and `available` becomes negative.
  - The storefront reports a backordered variant differently from an out-of-stock one.

---

## FR-INV-008 — Stock transfers between locations

- **Actor:** Store Admin
- **Description:** Move stock between locations of the same store through a two-sided transfer
  that records outbound and inbound movements.
- **Rules:**
  - A transfer names a source and a destination location and one or more (variant, quantity)
    lines.
  - A transfer writes an outbound movement at the source and an inbound movement at the
    destination, keeping `on_hand` consistent across locations.
  - A transfer cannot move stock from a location with insufficient `on_hand`.
  - Both locations must belong to the same store; cross-store transfers are rejected.
- **Acceptance Criteria:**
  - Transferring 5 units reduces the source `on_hand` and increases the destination `on_hand`.
  - A transfer exceeding the source `on_hand` is rejected.
  - A transfer between stores is rejected with a validation error.

---

## FR-INV-009 — Storefront availability

- **Actor:** Guest; Customer
- **Description:** Expose stock state for a variant so the storefront can show in-stock,
  backordered, or out-of-stock before and during checkout.
- **Rules:**
  - Availability is derived from `available` across the store's sellable locations.
  - A variant with positive `available` is in stock; with zero `available` and a backorder
    location it is backordered; otherwise it is out of stock.
  - Add-to-cart is blocked for a non-backorder variant with no `available`; a backorder variant
    may be added with a visible notice.
  - Availability shown to shoppers is never sourced from a client value.
- **Acceptance Criteria:**
  - A positive-stock variant shows as in stock on the product detail response.
  - A zero-stock, non-backorder variant rejects add-to-cart.
  - A backorder variant can be added and is labeled as backordered.

---

## FR-INV-010 — Admin inventory management API

- **Actor:** Store Admin; Platform Admin
- **Description:** Expose locations, stock items, movements, adjustments, and transfers over
  `/api/admin`, scoped to the selected store.
- **Rules:**
  - Every mutation requires the `admin` role plus store scope and is written to the audit log
    (`03/04-03`).
  - Stock history is queryable by variant, location, reason, and date range.
  - Validation rejects negative receipts, unknown locations, and duplicate stock-item pairs.
  - Deleting a location with stock or history is refused in favor of deactivation.
- **Acceptance Criteria:**
  - An admin scoped to store A cannot view or modify store B's stock items.
  - A stock history query returns the movements that produced the current `on_hand`.
  - A successful adjustment appears in the audit log and in stock history.

---

## Open Questions

1. Whether reservations expire on a timer or only on explicit cancellation is finalized in the
   inventory model `03/02-06`; this file requires reservation-at-confirm either way.
2. Whether stock is allocated to a specific location at reservation or only at shipment is
   finalized with the inventory model `03/02-06`.
3. Inventory test scenarios and edge cases are owned by `06/02-04`.
