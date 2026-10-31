# Inventory Model

Stock items per variant and location, the `on_hand`/`reserved`/`available` quantities, the append-only stock-movement ledger, reservation/shipment/return flows, and backorder rules.

**Links:** [Order lifecycle](./03-order-lifecycle.md) · [Module contracts](./02-module-contracts.md) · [Extension architecture](./07-extension-architecture.md) · [Inventory requirements](../../01-requirements/02-functional/08-inventory.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Inventory owns how much of each variant can be sold and from where. It defines stock **locations**,
holds exactly one `StockItem` per (variant, location), and records every physical quantity change in
an append-only `StockMovement` ledger. Two quantities are distinct: **`on_hand`** is the physical
count at a location and changes only through real movements, while **`reserved`** is a counter for
confirmed-order commitments and is never an `on_hand` movement. The sellable count is derived as
`available = on_hand − reserved` (`FR-INV-003`).

Stock is reserved when an order is **confirmed**, not when it is added to a cart (`03/02-03`), and
released on cancellation or reservation expiry. No location may go negative unless it is explicitly
configured to allow backorders (`FR-INV-007`). Inventory is store-scoped: locations, stock items,
and movements belong to one `Store` and are never visible to another. The end-to-end flow is
`03/01-03`; inventory test scenarios are owned by `06/02-04`.

---

## Stock Locations

- A location has a name, an address, an active/inactive flag, a backorder flag, and belongs to
  exactly one store.
- Exactly one active location per store is the **default**, used when no location is specified.
- Deactivating a location keeps its history and stock items but removes it from new allocation and
  admin pickers.
- A location from one store is never selectable for another store's orders or transfers
  (`FR-INV-001`).

---

## StockItem — Variant × Location

A `StockItem` exists for each (variant, location) pair that holds stock, and is unique on that pair.

| Field | Meaning |
|---|---|
| `Id`, `StoreId` | Identity and scope. |
| `VariantId` | The sellable variant. |
| `LocationId` | The holding location. |
| `Reserved` | Counter of quantities committed to confirmed orders. |
| `RowVersion` | Concurrency token guarding reservation and shipment. |

`on_hand` is **not** a stored mutable balance: it is derived as the sum of the stock item's
movement deltas. `available = on_hand − reserved` is likewise derived. A variant with no stock item
at any location is treated as zero stock, not as an error, and may still be purchasable when a
location allows backorders. Creating a variant does not automatically create stock items; the item
comes into existence when stock is first received (`FR-INV-002`).

| Quantity | Source | Changes on |
|---|---|---|
| `on_hand` | Σ `StockMovement.Quantity` | receipt +, shipment −, return restock +, adjustment ±, transfer out − / in +. |
| `reserved` | counter column | reservation +, release −, shipment −. |
| `available` | derived `on_hand − reserved` | any of the above; may be negative only on a backorder location. |

---

## Stock Movement Ledger

Every physical quantity change is an append-only `StockMovement` row (`FR-INV-004`):

| Field | Meaning |
|---|---|
| `Id`, `StockItemId` | Identity and owning stock item. |
| `Quantity` | Signed delta applied to `on_hand`. |
| `Reason` | `Receipt`, `Shipment`, `Return`, `Adjustment`, or `Transfer`. |
| `ReferenceType`, `ReferenceId` | Originating order, return, transfer, or adjustment. |
| `ActorId`, `OccurredAtUtc` | Who caused it and when (via `IClock`). |

Rules:

1. `on_hand` is the derived sum of a stock item's movement deltas; no mutable balance column is
   authoritative.
2. Movement reasons are exactly receipt, shipment, return, adjustment, and transfer.
3. Reservation and reservation release change the `reserved` counter, **not** `on_hand`, so they
   are **not** `StockMovement` rows.
4. Movement rows are never updated or deleted; a correction is a new compensating movement.

---

## Reservation Flow

Reservation happens in the checkout confirm transaction, never on add-to-cart (`FR-INV-005`).

1. For each order line, resolve the location (the order's selected location, otherwise the store's
   default) and load the `StockItem` under its concurrency token.
2. Check the backorder policy: if the location disallows backorders and `available` would fall below
   zero, the line fails.
3. Increment `reserved` by the ordered quantity. No `StockMovement` is written and `on_hand` is
   unchanged; `available` decreases.
4. If any line cannot be reserved, the whole confirm fails and no partial reservation persists.
5. Release decrements `reserved` on cancellation, terminal payment failure per policy, or
   reservation expiry, restoring `available`.

Allocation to a location happens at reservation: `reserved` is per `StockItem`, so the shipment
later draws down the same location. Reservations are released on explicit cancellation or payment
failure; a store may configure a reservation expiry window, in which case a reaper releases
reservations older than the window. When no window is configured, reservations do not expire on a
timer.

---

## Shipment and Return Flows

**Shipment** converts a reservation into physical outflow at fulfillment:

1. Write a `Shipment` movement with a negative delta, reducing `on_hand` by the shipped quantity.
2. Decrement `reserved` by the same quantity, so `available` stays consistent.
3. The write is idempotent per shipment; a partial shipment decrements by the shipped quantity only.

**Return** restocks returned goods at a nominated location (`FR-INV-006`):

1. A restocked return writes a `Return` movement with a positive delta, increasing `on_hand`.
2. A return marked unsellable writes no positive movement; it may write an `Adjustment` movement for
   the damaged quantity.
3. Returns never change `reserved`.

**Adjustment** covers manual recounts and shrinkage: an admin writes an `Adjustment` movement with
the signed delta and an audit entry. **Transfer** moves stock between two locations of the same
store through a `Transfer` outbound movement at the source and a `Transfer` inbound movement at the
destination; a transfer exceeding the source `on_hand`, or crossing stores, is rejected
(`FR-INV-008`).

---

## Backorder Rules

- By default a location does **not** allow backorders; any operation that would drive `available`
  below zero is rejected (`FR-INV-007`).
- When a location allows backorders, `available` may go negative and the storefront marks the
  variant as backordered with an expected-availability message.
- Backorder policy is evaluated at reservation and shipment time and is set per location.
- Changing backorder policy affects new operations, not already-reserved stock.
- The storefront sells against `available` across the store's sellable locations, never against
  `on_hand`: positive `available` is in stock; zero `available` at a backorder location is
  backordered; otherwise the variant is out of stock. Add-to-cart is blocked for a non-backorder
  variant with no `available`, and availability is never sourced from a client value
  (`FR-INV-009`).

## Open Questions

1. The reservation-expiry window default (and whether the reaper runs in the outbox relay or a
   dedicated worker) is owned by the operations topology `08/01-01`; the release semantics here are
   fixed.
2. Whether stock history is exposed through a dedicated admin report in addition to the stock-item
   detail is owned by the admin inventory API `03/04-02`.
3. Inventory test scenarios and edge cases are owned by `06/02-04`.

## Links

- [Order lifecycle](./03-order-lifecycle.md)
- [Module contracts](./02-module-contracts.md)
- [Extension architecture](./07-extension-architecture.md)
- [Inventory requirements](../../01-requirements/02-functional/08-inventory.md)
- [Returns requirements](../../01-requirements/02-functional/09-returns.md)
- [Design spec](../../11-spec/README.md) — §6 (inventory).
- Entity model: `03/03-01`; deployment topology: `08/01-01`; inventory tests: `06/02-04`.
