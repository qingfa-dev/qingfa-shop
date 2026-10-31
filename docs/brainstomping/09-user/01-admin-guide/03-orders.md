# Admin Orders

Find and filter orders, read the detail view, move an order through its state machine, and issue refunds.

**Links:** [Admin Flows](../../04-design/02-admin/01-flows.md) · [Admin Screens](../../04-design/02-admin/02-screens.md) · [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md) · [Order Lifecycle](../../03-architecture/02-components/03-order-lifecycle.md) · [Orders Requirements](../../01-requirements/02-functional/02-orders.md) · [User docs index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

The orders area is the day-to-day record of what customers bought and how it is being settled. This
guide covers the order list, the detail view, the locked status transitions, and refunds. The
physical act of fulfilling, shipping, and processing returns is in `09/01-04`. The journey is
`04/02-01` Journey 2; the screens are `04/02-02`; the endpoints are the orders section of
`03/04-02`; the stories are `US-007` … `US-010` in `01/04-03`.

Orders are store-scoped. An admin scoped to store A cannot view store B's orders (`US-007`). Every
status change goes through the order state machine — there is no direct status write — and an
illegal transition is `409` and changes nothing (`03/02-03`).

---

## Finding Orders

Open **Orders** (`/orders`). The list reads
`GET /api/admin/orders?state=&email=&number=&from=&to=&page=&per_page=` and shows the order number,
customer, total, state, and placed date.

| Filter | Parameter | Notes |
|---|---|---|
| State | `state` | One of the state-machine values below. |
| Customer email | `email` | Partial match on the order's email. |
| Order number | `number` | The human order number, for example `R100000123`. |
| Placed from | `from` | Inclusive lower bound. |
| Placed to | `to` | Inclusive upper bound. |
| Page | `page` / `per_page` | Server-paged; default 20, max 100. |

State is rendered as a coloured tag. Clicking a row opens the order detail. Results are always
limited to the selected store, so switch stores to look at another store's orders.

---

## The Detail View

`GET /api/admin/orders/{id}` returns everything needed to understand and act on one order:

- **State timeline / history** — every transition with actor and timestamp.
- **Customer and addresses** — the ship address and the bill address snapshot taken at purchase.
- **Line items** — variant, name, quantity, unit price, and line total.
- **Adjustments** — promotion and shipping discounts, plus any manual adjustments.
- **Payments** — state, amount, and gateway reference (never a PAN).
- **Shipments** — method, tracking number, shipped date, and the lines in each shipment.
- **Totals** — item total, adjustment total, tax total, and order total.

Amounts are `{ "amount", "currency" }` values in the store currency. The admin formats them but
never converts them (`03/03-03`).

---

## The State Machine

Order status is forward `cart → awaiting_payment → awaiting_fulfillment → shipped → completed`, with
side states `canceled` and `returnable`. Only the transitions below are legal; anything else is
`409`.

| From | To | Trigger in the admin |
|---|---|---|
| `awaiting_fulfillment` | `shipped` | Fulfill and ship (see `09/01-04`). |
| `awaiting_fulfillment` | `canceled` | Cancel before shipment. |
| `awaiting_payment` | `canceled` | Cancel or terminal payment failure. |
| `cart` | `canceled` | Cancel a cart/order before payment. |
| `shipped` | `completed` | All shipments delivered and the order settled. |
| `shipped` | `returnable` | A return is opened. |
| `completed` | `returnable` | A return is opened within the return window. |
| `returnable` | `completed` | All open returns are closed. |

Notes:

- `completed` is **not terminal**: it can still open a return window and move to `returnable`.
- `canceled` is terminal.
- Canceling a `shipped` order is rejected — that path is a return, not a cancellation (`US-010`).
- Cancellation releases reserved stock and refunds or voids a captured payment through the
  originating gateway.

Fulfill and cancel send an `If-Match` row version. A stale version is `409`; reload the order and
retry. Because these calls mutate the order, they also carry the CSRF header.

---

## Refunds

Refund a captured payment from the order detail. The call is
`POST /api/admin/orders/{id}/refund` with `{ "amount", "payment_id", "reason"? }`.

Rules:

- The refund is routed to the gateway that captured the payment (`03/04-05`).
- It never exceeds the captured amount minus prior refunds; an over-refund is `422`.
- The refund records a transaction row and an audit entry; no PAN is ever exposed.
- A refund can be partial; repeat it while the remainder allows.
- Offline payments are refunded manually with an audit entry.

A refund does not, by itself, move the order state. Returns settle separately and are covered in
`09/01-04`.

---

## Manual Adjustments

A manual adjustment corrects an order total without editing a captured price. Use
`POST /api/admin/orders/{id}/adjustments` with `{ "line_item_id"?, "amount", "label" }`.

- A manual adjustment is a first-class, total-affecting `Adjustment` with `source = manual`.
- A manual adjustment is **never** tax.
- The adjustment appears in the order's adjustment total and is written to the audit log.
- An adjustment on a `cart`/`awaiting_payment` order is `409`; adjustments are for placed orders.

---

## Cancelling an Order

Cancel from the order detail with `POST /api/admin/orders/{id}/cancel` and an optional
`{ "reason" }`.

- Allowed from `cart`, `awaiting_payment`, and `awaiting_fulfillment` (`US-010`).
- Reserved stock is released so the quantity becomes available again.
- A captured payment is refunded or voided through the originating gateway.
- Cancelling a `shipped` order returns `409`; open a return instead.
- The call sends `If-Match`; a stale version is `409`.

---

## Reading the Numbers

| Field | Meaning |
|---|---|
| `item_total` | Sum of line totals (`unit price × quantity`). |
| `adjustment_total` | Promotion, shipping, and manual adjustments. |
| `tax_total` | Tax computed on the post-promotion amount. |
| `total` | `item_total + adjustment_total + tax_total`. |

Line totals are always computed, never cached columns (`03/02-03`). Tax and totals are server-owned;
the admin never posts a computed amount.

---

## Common Tasks

| Task | Call |
|---|---|
| List and filter orders | `GET /api/admin/orders?state=&email=&number=&from=&to=&page=&per_page=` |
| Open an order | `GET /api/admin/orders/{id}` |
| Refund a payment | `POST /api/admin/orders/{id}/refund` |
| Add a manual adjustment | `POST /api/admin/orders/{id}/adjustments` |
| Cancel an unshipped order | `POST /api/admin/orders/{id}/cancel` |

---

## Open Questions

1. The concrete report set and KPI definitions are finalized with the dashboard design in `04/02-02`
   and `09/01-05`; the order list and detail are unaffected.
2. Whether shipping and tax are refundable by default is a store-policy detail confirmed with the
   returns test plan `06/02-05`.
3. The exact order-number format is owned by `03/02-03`; the examples here (`R100000123`) follow the
   admin contract `03/04-02`.

## Links

- [Admin Flows](../../04-design/02-admin/01-flows.md)
- [Admin Screens](../../04-design/02-admin/02-screens.md)
- [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md)
- [Order Lifecycle](../../03-architecture/02-components/03-order-lifecycle.md)
- [Payment gateways](../../03-architecture/04-integration/05-payment-gateways.md)
- [Orders requirements](../../01-requirements/02-functional/02-orders.md)
- [Admin stories](../../01-requirements/04-user-stories/03-admin.md)
- [Design spec](../../11-spec/README.md) — §7 (admin orders),
  §9 (admin front-end).
