# Inventory Test Plan

Case-by-case verification of the inventory model: reservation and release, shipment decrement, return restock, the movement ledger, backorder policy, and the no-negative-stock rule.

**Links:** [Test matrix](../01-test-strategy/01-matrix.md) · [Unit tests](../01-test-strategy/02-unit.md) · [Integration tests](../01-test-strategy/03-integration.md) · [Inventory model](../../03-architecture/02-components/06-inventory-model.md) · [Checkout requirements](../../01-requirements/02-functional/03-checkout.md) · [Inventory requirements](../../01-requirements/02-functional/08-inventory.md) · [Seed data](../../05-development/01-setup/04-seed-data.md) · [Verification index](../README.md) · [Spec](../../11-spec/README.md)

---

## Scope

This plan proves the inventory contract in `01/02-08` as designed by `03/02-06`. It covers the
`on_hand` / `reserved` / `available` quantities, the append-only `StockMovement` ledger,
reservation in the confirm transaction, release on cancellation or payment failure, shipment
decrement, return restock, transfer, and the backorder policy.

It assumes the inventory rulings: `reserved` is a **counter**, not a movement;
`available = on_hand − reserved`; `on_hand` changes only through real movements (receipt, shipment,
return, adjustment, transfer); reservation and release change only `reserved`; no location may go
negative unless it is explicitly configured to allow backorders; and movements are never updated or
deleted.

Layers: quantity arithmetic, movement deltas, and backorder decisions are unit tests; the ledger,
concurrent last-unit reservation, the no-negative constraint, and restock linkage are integration
tests against real PostgreSQL; the out-of-stock storefront path and admin stock operations are
storefront and admin E2E journeys.

---

## Preconditions

- A store with one default active location and a second active location for transfers; both belong
  to the same store.
- One non-backorder location (the default) and one backorder-enabled location fixture.
- A variant with a known `on_hand`, either through a receipt movement or a built fixture that sums
  movement deltas.
- An order fixture that can be driven through confirm, cancel, and ship without a browser.

---

## Cases

### INV-01 — Reservation increments reserved and leaves on_hand unchanged

- **Type:** unit
- **FR reference:** FR-INV-005, FR-INV-003
- **Steps:**
  1. Set a stock item with `on_hand` of 10 and `reserved` of 0.
  2. Reserve quantity 3 at confirm.
- **Expected:**
  - `reserved` becomes 3 and derived `available` becomes 7.
  - `on_hand` remains 10 because reservation is a counter operation.

### INV-02 — Reservation writes no stock movement

- **Type:** unit
- **FR reference:** FR-INV-004, FR-INV-005
- **Steps:**
  1. Reserve quantity Q for an order line.
  2. Sum the stock item's movement deltas before and after.
- **Expected:**
  - No `StockMovement` row is written and the movement-delta sum is unchanged.
  - Reservation and release are counter operations, not ledger entries.

### INV-03 — available is derived as on_hand minus reserved

- **Type:** unit
- **FR reference:** FR-INV-003
- **Steps:**
  1. Build a stock item with receipts summing to `on_hand` of 8 and a `reserved` counter of 3.
  2. Read the exposed quantities.
- **Expected:**
  - The admin view exposes `on_hand = 8`, `reserved = 3`, and `available = 5`.
  - `available` is computed, never stored as an authoritative balance.

### INV-04 — Shipment decrements both on_hand and reserved

- **Type:** integration
- **FR reference:** FR-INV-006, FR-INV-003
- **Steps:**
  1. Reserve quantity 2 for a confirmed order.
  2. Fulfill and ship that quantity.
- **Expected:**
  - A negative `Shipment` movement reduces `on_hand` by 2.
  - `reserved` is decremented by 2, so `available` stays consistent; the shipment write is idempotent per shipment.

### INV-05 — on_hand equals the sum of movement deltas

- **Type:** integration
- **FR reference:** FR-INV-004
- **Steps:**
  1. Apply a receipt of +10, a shipment of −2, a return of +1, and an adjustment of −1.
  2. Sum the stock item's movements and compare to reported `on_hand`.
- **Expected:**
  - The delta sum equals `on_hand` of 8.
  - No mutable balance column contradicts the derived value.

### INV-06 — Return restock writes a positive return movement

- **Type:** integration
- **FR reference:** FR-INV-006, FR-RET-007
- **Steps:**
  1. Receive and inspect a return with 2 restockable units at the nominated location.
  2. Apply the restock.
- **Expected:**
  - A `Return` movement with a +2 delta increases `on_hand` at that location.
  - `reserved` is unchanged by the return flow.

### INV-07 — Unsellable return writes no positive movement

- **Type:** integration
- **FR reference:** FR-INV-006, FR-RET-007
- **Steps:**
  1. Inspect a return item as unsellable.
  2. Apply the return settlement.
- **Expected:**
  - No positive `Return` movement is written and `on_hand` is unchanged.
  - The damaged quantity may be recorded as an `Adjustment` movement or a write-off for reporting.

### INV-08 — Cancellation releases the reservation

- **Type:** integration
- **FR reference:** FR-INV-005
- **Steps:**
  1. Confirm an order, then cancel it before shipment.
  2. Read the stock item.
- **Expected:**
  - `reserved` is decremented by the ordered quantity and `available` is restored.
  - `on_hand` is unchanged and no movement is written by the release.

### INV-09 — A non-backorder location rejects an oversell atomically

- **Type:** integration
- **FR reference:** FR-INV-007, FR-CHK-006
- **Steps:**
  1. Set `available` of 1 at a location with backorders disabled.
  2. Confirm an order for 2 units.
- **Expected:**
  - The whole confirm fails and no partial reservation persists on any line.
  - `reserved` is unchanged and the order stays in `cart`.

### INV-10 — A backorder location permits negative available

- **Type:** unit
- **FR reference:** FR-INV-007
- **Steps:**
  1. Flag a location as allowing backorders with `on_hand` of 1 and `reserved` of 0.
  2. Reserve quantity 3.
- **Expected:**
  - The reservation succeeds and `available` becomes −2.
  - The storefront marks the variant as backordered with an expected-availability message, distinct from out of stock.

### INV-11 — on_hand never goes negative outside backorder

- **Type:** integration
- **FR reference:** FR-INV-007, FR-INV-008
- **Steps:**
  1. Attempt a shipment or transfer that exceeds the source `on_hand` at a non-backorder location.
- **Expected:**
  - The operation is rejected and `on_hand` is unchanged.
  - Negative `on_hand` is impossible; only `available` may go negative, and only on a backorder location.

### INV-12 — Movements are append-only; corrections are compensating rows

- **Type:** integration
- **FR reference:** FR-INV-004
- **Steps:**
  1. Record a receipt of +10, then correct an overcount by recording −2 as a new movement.
  2. Attempt to update or delete an existing movement.
- **Expected:**
  - The correction is a new opposite movement, and the delta sum becomes 8.
  - Movement rows are never updated or deleted; an edit attempt is refused.

### INV-13 — Transfers are two-sided and same-store only

- **Type:** integration
- **FR reference:** FR-INV-008
- **Steps:**
  1. Transfer 5 units from the source to the destination location of the same store.
  2. Attempt a transfer that exceeds the source `on_hand`, then a cross-store transfer.
- **Expected:**
  - The valid transfer writes a `Transfer` outbound at the source and inbound at the destination, netting source `on_hand` −5 and destination `on_hand` +5.
  - The over-transfer is rejected and a cross-store transfer is rejected with a validation error.

### INV-14 — Storefront availability uses available, not on_hand

- **Type:** E2E
- **FR reference:** FR-INV-009
- **Steps:**
  1. Set a variant with positive `on_hand` fully reserved so `available` is zero at a non-backorder location.
  2. Open the product detail page.
- **Expected:**
  - The variant is reported out of stock and add-to-cart is blocked.
  - Availability is derived from server-side `available`, never a client value.

### INV-15 — Concurrent last-unit reservation commits once

- **Type:** integration
- **FR reference:** FR-INV-005, FR-INV-002
- **Steps:**
  1. Set `available` of 1 under the stock item's concurrency token.
  2. Confirm two orders for one unit concurrently.
- **Expected:**
  - Exactly one reservation commits; the other fails under the row-version guard.
  - `reserved` never exceeds the reservable quantity and no oversell persists.

### INV-16 — Admin adjustment is recorded in history and audit

- **Type:** integration
- **FR reference:** FR-INV-004, FR-INV-010
- **Steps:**
  1. As an admin, record a recount adjustment of −1 with an audit entry.
  2. Query stock history by variant, location, reason, and date range.
- **Expected:**
  - An `Adjustment` movement with a −1 delta is visible in stock history.
  - The audit entry is written; a store-A admin cannot view or modify store-B stock.

---

## Open Questions

1. The reservation-expiry window default and whether the reaper runs in the outbox relay or a
   dedicated worker are owned by `08/01-01`; this plan treats timer expiry as the same counter
   release as explicit cancellation.
2. Whether stock history is exposed through a dedicated admin report in addition to the stock-item
   detail is owned by `03/04-02`; the movement ledger asserted here is the same source either way.
3. Whether stock is allocated to a specific location at reservation or only at shipment is resolved
   by `03/02-06` in favor of allocation at reservation; the shipment draws down the same `StockItem`.
