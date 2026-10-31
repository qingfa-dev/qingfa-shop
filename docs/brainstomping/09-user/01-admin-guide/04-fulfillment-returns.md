# Fulfillment, Inventory, and Returns

Fulfill and ship orders, adjust stock through the append-only ledger, and process returns, exchanges, and restocks.

**Links:** [Admin Flows](../../04-design/02-admin/01-flows.md) · [Admin Screens](../../04-design/02-admin/02-screens.md) · [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md) · [Inventory Model](../../03-architecture/02-components/06-inventory-model.md) · [Returns Requirements](../../01-requirements/02-functional/09-returns.md) · [User docs index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This guide covers the physical side of the business: shipping paid orders, keeping stock counts
accurate, and handling goods that come back. The order list, detail view, transitions, and refunds
are in `09/01-03`; here we cover fulfillment, the stock ledger, transfers, and the return lifecycle.
The journeys are `04/02-01` Journey 2 and Journey 3; the screens are `04/02-02`; the endpoints are
the orders and inventory sections of `03/04-02`; the stories are `US-008`, `US-011`, `US-012`, and
`US-013` in `01/04-03`.

All of it is store-scoped, and every mutation is written to the audit log. Stock and order mutations
send an `If-Match` row version; a stale version is `409`.

---

## Fulfilling and Shipping

A paid order in `awaiting_fulfillment` is shipped from the order detail. The single call
`POST /api/admin/orders/{id}/fulfill` records the shipment and advances the order to `shipped`.

Request body:

```json
{
  "shipment_lines": [
    { "line_item_id": "9a8b7c6d-1234-5678-9abc-def012345678", "quantity": 2 }
  ],
  "shipping_method_id": "3f2e1d0c-aaaa-bbbb-cccc-ddddeeeeffff",
  "tracking_number": "1Z999AA10123456784"
}
```

In one transaction the call:

1. Transitions the order `awaiting_fulfillment → shipped` through the state machine (`03/02-03`).
2. Writes the shipment and its lines.
3. Commits the `on_hand` outflow and **releases the reservation** by the same quantity (`03/02-06`).
4. Writes the `OrderShipped` outbox row and appends the audit entry.

After the transaction commits, the outbox relay sends the shipment email to the customer.

Rules:

- Partial fulfillment is allowed: list only the `shipment_lines` you are shipping.
- A shipped order's recorded costs and line items are never altered (`US-008`).
- If the `If-Match` version is stale or the order is not in `awaiting_fulfillment`, the call returns
  `409` and changes nothing.
- The tracking number is optional but recommended; it appears in the shipment and the customer email.
- Shipping writes a movement that reduces `on_hand` and decrements `reserved` by the same quantity.

---

## Inventory and the Stock Ledger

Open **Inventory → Stock** (`/inventory/stock`). The screen reads
`GET /api/admin/stock_items?variant_id=&location_id=&page=&per_page=` and shows, per variant and
location, the derived quantities:

| Quantity | Meaning |
|---|---|
| `on_hand` | Physical count at the location; the sum of the stock item's movement deltas. |
| `reserved` | Quantities committed to confirmed orders. |
| `available` | `on_hand − reserved`, the sellable count. |

`on_hand` is **not** an editable balance. To change it you record a movement.

### Recording a Movement

Use **Adjust stock** on a stock item, or `POST /api/admin/stock_items/{id}/movements` with
`{ "quantity", "reason", "reference"? }`.

- `quantity` is a signed delta applied to `on_hand`.
- `reason` is one of `receipt`, `shipment`, `return`, `adjustment`, or `transfer`.
- Movements are **append-only**: a correction is a new compensating movement with the opposite
  quantity, never an edit or delete (`US-012`).
- A movement that would drive a non-backorder location negative is `409`.
- `on_hand` always equals the sum of the item's movement deltas.

### History

Click **History** to open `GET /api/admin/stock_items/{id}/movements`, an append-only ledger
queryable by reason and date. The original rows are always retained, so the history explains every
count.

### Locations

**Inventory → Locations** (`/inventory/locations`) manages stock locations with
`GET`/`POST`/`PATCH`/`DELETE /api/admin/stock_locations`, including the `allow_backorders` and
`is_default` flags.

- Exactly one active location per store is the **default**, used when none is specified.
- A non-backorder location may not go negative (`FR-INV-007`).
- Deactivating a location keeps its history and stock items but removes it from new allocation.
- Deleting a location referenced by history is `409`.

### Transfers

**Inventory → Transfers** (`/inventory/transfers`) moves stock between locations of the same store
with `POST /api/admin/stock_transfers` and
`{ "variant_id", "from_location_id", "to_location_id", "quantity" }`.

- A transfer writes an outbound movement at the source and an inbound movement at the destination.
- A transfer exceeding the source `on_hand` is `409`.
- **Both locations must belong to the same store**; a cross-store transfer is `403` (`US-013`).

---

## Returns

Open **Orders → order detail → Return** to authorize a return, or **Returns** to work an existing
one. A return references the original order and its line items and never edits them (`FR-RET-001`).

### Return Lifecycle

The return states are `requested → authorized → received → inspected → completed`, plus `canceled`
and `rejected`. A return cannot move from `requested` directly to `completed` (`FR-RET-008`).

| Step | Action | Call |
|---|---|---|
| Authorize | Create the return with items, quantities, and reasons. | `POST /api/admin/orders/{id}/returns` |
| Open | Read the return detail. | `GET /api/admin/returns/{id}` |
| Approve | Authorize the return and optionally nominate a restock location. | `POST /api/admin/returns/{id}/approve` |
| Receive | Record received quantities and move to `received`. | `POST /api/admin/returns/{id}/receive` |
| Inspect | Split each received quantity into restockable and unsellable. | in the return detail |
| Restock | Write return movements for the restockable quantity. | `POST /api/admin/stock_items/{id}/movements` |
| Settle | Refund the originating payment or grant store credit. | `POST /api/admin/orders/{id}/refund` |

Rules:

- A return is created against an order in `shipped` or `completed`; an ineligible order is `409`.
- A return item carries exactly one admin-defined **reason** (damaged, wrong item, no longer wanted,
  not as described) and its quantity never exceeds the purchased quantity not already returned
  (`FR-RET-002`, `FR-RET-003`).
- Reception records what actually arrived; receiving fewer units than requested records the shortfall
  and does not block the return.
- Inspection marks received quantities restockable or unsellable; a refund before inspection
  completes requires an explicitly waived inspection, which is recorded as such (`FR-RET-004`).

### Restocking

- Only quantities marked **restockable** become positive stock, written as a return movement at the
  nominated location (`FR-RET-007`).
- **Unsellable** quantities write no positive movement and are recorded as a write-off for reporting.
- Restocking happens **once per accepted return item**; repeating the action is idempotent and does
  not double-count stock.
- A restock increases `on_hand` at the chosen location; it never edits the original shipment.

### Settlement

- The refund amount is the price paid for the accepted return items, allocated per line and never
  exceeding the captured amount minus prior refunds (`FR-RET-005`).
- Refunds route to the original capturing gateway; offline payments are refunded manually with an
  audit entry.
- **Store credit** is granted through the ledger as a store-scoped credit instead of a gateway
  refund, at the customer's choice where the store allows.
- Shipping and tax are refunded only per the store's return policy.
- Completing a return records refunds and credits separately and does **not** change the original
  order's computed totals (`FR-RET-008`).
- Settling a return queues a notification naming the refund or credit amount (`FR-RET-009`).

---

## Exchanges

There is no separate exchange aggregate. An exchange is handled as a **return plus new order
lines** (`FR-RET-006`):

1. Process the returned goods through the return lifecycle above.
2. Create new order lines (or a replacement order) for the variant the customer wants instead.
3. Settle any price difference: charge the customer when the replacement costs more, or refund or
   grant credit when it costs less.
4. Replacement lines follow the same stock reservation and pricing rules as a normal order.

The original order's line items and captured prices are never edited.

---

## Common Tasks

| Task | Where | Call |
|---|---|---|
| Ship an order | Order detail → Fulfill | `POST /api/admin/orders/{id}/fulfill` |
| Record a stock movement | Inventory → Stock → Adjust stock | `POST /api/admin/stock_items/{id}/movements` |
| View stock history | Inventory → Stock → History | `GET /api/admin/stock_items/{id}/movements` |
| Transfer stock | Inventory → Transfers | `POST /api/admin/stock_transfers` |
| Authorize a return | Order detail → Return | `POST /api/admin/orders/{id}/returns` |
| Approve / receive | Return detail | `POST /api/admin/returns/{id}/approve`, `.../receive` |
| Restock accepted items | Return detail / Stock | `POST /api/admin/stock_items/{id}/movements` |
| Settle a return | Return detail | `POST /api/admin/orders/{id}/refund` |

---

## Open Questions

1. The exact return-window and eligibility rules (for example days after delivery) are finalized in
   `03/02-03`.
2. Whether exchanges reuse the original order or create a brand-new order number is finalized with
   the order aggregate in `03/02-03`; the return-plus-new-lines behaviour is fixed by `FR-RET-006`.
3. The default restock location shown on approval is a UX detail owned by `04/02-02`; the API
   accepts an explicit `restock_location_id`.

## Links

- [Admin Flows](../../04-design/02-admin/01-flows.md)
- [Admin Screens](../../04-design/02-admin/02-screens.md)
- [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md)
- [Order Lifecycle](../../03-architecture/02-components/03-order-lifecycle.md)
- [Inventory Model](../../03-architecture/02-components/06-inventory-model.md)
- [Returns requirements](../../01-requirements/02-functional/09-returns.md)
- [Inventory requirements](../../01-requirements/02-functional/08-inventory.md)
- [Admin stories](../../01-requirements/04-user-stories/03-admin.md)
- [Design spec](../../11-spec/README.md) — §7 (admin orders and
  inventory), §9 (admin front-end).
