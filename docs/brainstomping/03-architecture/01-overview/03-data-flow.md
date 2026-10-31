# Data Flow

The end-to-end annotated sequence for a purchase — browse, add to cart, checkout, fulfillment, return — showing where tax, promotions, inventory, and the outbox fire.

**Links:** [C4 model](./01-c4.md) · [App topology](./02-app-topology.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file traces the primary commerce flow across the components from `03/01-01`, from first
browse to completed return. Each phase lists the steps in order and annotates the four
cross-cutting mechanisms at the exact points they run:

- **`[inventory]`** — `StockItem` reads/writes, reservations, and `StockMovement` ledger entries.
- **`[tax]`** — tax computation on post-promotion amounts through the jurisdiction/rate engine.
- **`[promotion]`** — promotion rule evaluation and the adjustments it produces.
- **`[outbox]`** — a domain event persisted in the same transaction as the state change it
  describes, dispatched in-process right after commit; the relay replays anything that dispatch
  missed (at-least-once).

The order state machine (`cart → awaiting_payment → awaiting_fulfillment → shipped → completed`,
plus `canceled`/`returnable`) is the backbone of the flow; its detailed contract lives in
`03/02-03`, the tax engine in `03/02-05`, promotions in `03/02-04`, inventory in `03/02-06`, and
the data model in `03/03-01`.

---

## Flow Overview

```
 Customer        Storefront        API (modules)                    PostgreSQL / providers
    │                │                  │                                    │
    │ browse ───────►│ GET /api/v1/...  │── catalog read ───────────────────►│
    │◄───────────────│◄─────────────────│◄───────────────────────────────────│
    │ add to cart ──►│ POST cart/line   │── resolve price/promo ────────────►│
    │                │                  │   [promotion] (preview)             │
    │ checkout       │                  │                                    │
    │  delivery ────►│ PUT checkout/... │── quote shipping + adjustments ───►│
    │  payment ─────►│ PUT checkout/... │── gateway authorize/capture ──────►│ Stripe/PayPal
    │  confirm ─────►│ PUT checkout/... │── [inventory] reserve              │
    │                │                  │── [tax] compute                    │
    │                │                  │── state cart→awaiting_*            │
    │                │                  │── [outbox] OrderPlaced (+others)   │
    │                │◄─────────────────│◄───────────────────────────────────│
    │◄─ confirmation │                  │   [outbox] dispatch → email         │
    │ fulfill (admin)│                  │── ship: awaiting_fulfillment→shipped│
    │                │                  │── [outbox] OrderShipped → email     │
    │ return ───────►│                  │── RMA + refund + restock            │
    │                │                  │── [inventory] restock               │
```

Notation: `[inventory]`, `[tax]`, `[promotion]`, and `[outbox]` mark where each cross-cutting
mechanism fires. All server state changes happen inside the phases below; the client never
supplies an authoritative price, tax, promotion, or stock value.

---

## Phase 1 — Browse

1. The browser requests a storefront page. Nuxt SSR resolves the store (see `03/01-02`) and calls
   the public API catalog endpoints over `/api/v1`.
2. The API resolves the store, then the **Catalog** module reads products, variations, option
   values, taxonomy, and per-locale text for that store.
3. **`[promotion]`** — applicable promotions are evaluated for display badges/labels where the
   storefront shows them. This is a read-only preview; it does not create adjustments.
4. The API returns localized, store-scoped DTOs. The storefront renders listing, search facets, or
   product detail (including variation and stock state).
5. `Money` values cross the boundary as `(amount, currency)` pairs; no raw decimals are exposed.
6. No order state is created, no inventory is reserved, and nothing is written to the outbox.

```
storefront ──GET /api/v1/products...──► API ──catalog read──► PostgreSQL
     │                                     │
     └── localized, store-scoped DTOs ◄────┘   (read-only; [promotion] preview only)
```

---

## Phase 2 — Add to Cart

1. The shopper adds a product/variation and quantity. The storefront calls `POST /api/v1/cart/...`
   (or creates a cart first). Cart state is stored server-side, not in the browser.
2. The **Catalog** module supplies the variant's per-store price for the order currency.
3. **`[inventory]`** — the Inventory module reports availability for display. Add-to-cart does
   **not** reserve stock; reservation happens at confirm (Phase 5).
4. **`[promotion]`** — the Promotions module evaluates eligible rules against the evolving cart
   and produces **preview adjustments**. Order totals are recomputed as
   `source + adjustments` (tax is added later, at checkout).
5. The order/line-item rows and adjustments are persisted. Add-to-cart does not transition the
   order out of `cart`.
6. **`[outbox]`** — no state transition occurs, so no domain event is emitted here. Cart edits are
   not business events.

**Adjustments rule.** Promotion and shipping discounts are stored as adjustment rows referencing
the originating rule/promotion. Totals are always derived from source + adjustments + tax and are
never cached in a single column (`03/03-01`).

---

## Phase 3 — Checkout: Delivery

1. The shopper enters a shipping address and advances to the delivery step
   (`PUT /api/v1/checkout/...`). Checkout is guest-capable.
2. The API validates the address and resolves the delivery zone.
3. The **Shipping** module quotes available methods through registered `IShippingCalculator`
   implementations; selecting one writes a shipping `Money` adjustment to the order.
4. **`[promotion]`** — order-level rules that depend on the selected shipping method (for example
   free-shipping actions) are re-evaluated, updating adjustments.
5. **`[tax]`** — tax is recomputed after promotions, because tax is charged on post-promotion
   amounts. The tax engine resolves the jurisdiction hierarchy and applies bound rates; inclusive
   vs. exclusive is per store.
6. Totals are refreshed from `source + adjustments + tax`. The order is still in `cart`.
7. Changing the address or items re-quotes shipping and clears a now-invalid selection; stale
   totals are invalidated.

```
PUT checkout/delivery ──► validate address ──► [promotion] method rules
                       ──► shipping methods quote ──► shipping adjustment
                       ──► [tax] on post-promotion amount ──► recompute totals
                       (state: cart)
```

---

## Phase 4 — Checkout: Payment

1. The storefront requests the payment step. Card data is captured by the provider UI (Stripe
   Elements / PayPal JS SDK); the platform never receives PAN/CVV.
2. The amount sent to the gateway is the **freshly computed server total** in the order currency.
   A client-supplied amount that differs is rejected.
3. The **Payments** module authorizes or captures through the selected `IPaymentGateway` adapter
   and records a payment attempt with the gateway reference and outcome.
4. **`[outbox]`** — a payment outcome that changes business state (for example
   `PaymentCompleted`) is recorded as a domain event, but only alongside a state transition in the
   same transaction. An isolated failed attempt is recorded against the order without advancing
   state.
5. On decline, the order stays in its current state with items and addresses intact, and the
   shopper retries with a new payment attempt (the failed attempt is never mutated). The retry is
   guarded by an idempotency key.
6. Payment does not yet reserve stock; reservation is atomic with confirm (Phase 5).

---

## Phase 5 — Checkout: Confirm (the critical transaction)

Confirm is the only step that places the order and the point where all four mechanisms meet in a
single database transaction.

1. The API verifies the delivery and payment steps are complete and that the idempotency key has
   not already placed this checkout.
2. **`[inventory]`** — the Inventory module **reserves** the ordered quantities for the order's
   location(s). Reservation reduces `available` but does not change `on_hand`. If any line cannot
   be reserved, the entire confirm fails and the order stays in `cart`.
3. **`[promotion]`** — final adjustments are recomputed and locked for the amount charged.
4. **`[tax]`** — tax is computed one final time on the post-promotion amount so the charged total
   matches.
5. **State transition** — the order moves `cart → awaiting_payment`; on a successful immediate
   capture it advances to `awaiting_fulfillment`, otherwise it remains `awaiting_payment` until
   capture.
6. **`[outbox]`** — `OrderPlaced` (and any transition events from the same step) are written to the
   outbox table **in the same transaction** as the state change, stock reservation, and payment
   record. This is what makes delivery reliable without a broker.
7. The transaction commits. Totals are never stored as a single cached column; they are derived.
8. After commit, the unit of work dispatches the events in-process. Consumers queue the
   confirmation email and the analytics stub. Delivery is at-least-once: the
   `OutboxRelayService` replays any row the direct dispatch missed, and consumers stay safe
   because the inbox dedupes handler runs per `(EventId, HandlerType)`.

```
BEGIN
  verify steps + idempotency
  [inventory] reserve → reduce available, write StockMovement
  [promotion] final adjustments
  [tax] on post-promotion amount
  state cart → awaiting_payment (→ awaiting_fulfillment on capture)
  [outbox] OrderPlaced / PaymentCompleted rows
COMMIT
──► in-process dispatch → email (confirmation/receipt), analytics stub
    (relay replays anything the dispatch missed)
```

---

## Phase 6 — Fulfillment

1. An admin opens the paid order in `web/admin` and confirms shipment after picking/packing.
2. The **Orders** module transitions `awaiting_fulfillment → shipped`; allowed transitions are
   the only status mutators.
3. **`[inventory]`** — shipped quantities are committed: `on_hand` decreases and a
   `StockMovement` ledger entry records the outflow. Reserved quantities are released against
   the shipment.
4. **`[outbox]`** — `OrderShipped` is written in the same transaction as the state change and the
   stock movement.
5. After commit, `OrderShipped` is dispatched in-process (the relay replays it if that dispatch
   is missed); the email consumer sends the shipment notification.
6. Once all shipments are delivered and the order is settled, the order advances to `completed`.

---

## Phase 7 — Return

1. A customer requests a return from the storefront account area (or an admin initiates one). The
   **Returns** module creates a return authorization (RMA) with return items referencing original
   line items.
2. The order becomes `returnable` while a return is open; the order and its in-flight state are
   otherwise unchanged.
3. On receiving the returned goods, an admin marks the return items received. The **Returns**
   module triggers a refund through the **Payments** module (`IPaymentGateway` refund path).
4. **`[inventory]`** — restock policy decides whether returned quantities increase `available`/
   `on_hand` at a location; each change writes a `StockMovement` entry.
5. **`[outbox]`** — `ReturnStatusChanged` (and any refund event) is written in the same
   transaction as the state change.
6. After commit, `ReturnStatusChanged` is dispatched in-process (the relay replays it if that
   dispatch is missed); the email consumer sends the return-status notification.
7. When all returns are closed, the order's state reflects the refund/closure outcome
   (`completed` or `canceled` per policy).

---

## Domain Events at a Glance

Every event below is persisted to the outbox in the same transaction as the state change it
records, then dispatched after commit. The catalogue is owned by `03/03-01`.

| Event | Fired in | Primary consumers |
|---|---|---|
| `OrderPlaced` | Phase 5 (confirm) | Email (confirmation), analytics stub |
| `PaymentCompleted` | Phase 4/5 | Email (receipt), Orders |
| `OrderShipped` | Phase 6 | Email (shipment) |
| `OrderCanceled` | cancellation path | Inventory (release reservation), email |
| `ReturnStatusChanged` | Phase 7 | Email (return status) |
| `StockLow` | inventory thresholds | Admin notification |

---

## Invariants

- **Server-authoritative amounts.** Prices, promotions, tax, and totals are always computed in
  the API at the moment of charge; the client is never trusted.
- **Atomic placement.** Reservation, tax, state transition, and the `OrderPlaced` outbox row
  commit together or not at all.
- **Single-writer state.** Only state transitions mutate order status; each transition emits an
  event.
- **At-least-once delivery.** Outbox consumers must be idempotent; the outbox guarantees the
  event is not lost, not that it is delivered exactly once.
- **No cached totals.** Order totals are derived from source + adjustments + tax every time.

## Open Questions

1. Reservation expiry semantics (timer vs. explicit cancellation only) are owned by the inventory
   model (`03/02-06`); this flow requires reservation at confirm either way.
2. Whether capture is immediate at payment or deferred to `awaiting_fulfillment` is a per-store
   checkout option finalized in `03/02-05`; both paths preserve the Phase 5 transaction.
3. ~~The exact outbox relay/consumer split (in-process dispatcher vs. dedicated worker)~~ —
   **resolved** (BuildingBlocks CQRS + Outbox + Event Store — Design Spec): delivery is hybrid.
   The outbox row still commits with the state change; after commit the unit of work dispatches the
   events in-process (a dispatch failure never throws — the row simply stays undelivered), the
   `OutboxRelayService` background worker replays every undelivered row (at-least-once; claim
   lease plus `ComputeRetryDelay` backoff), and `InboxAwareNotificationPublisher` dedupes handler
   runs per `(EventId, HandlerType)`. Mechanism and ownership: `03/02-03` (outbox write) and
   `03/02-08` (kit ports).

## Links

- [C4 model](./01-c4.md)
- [App topology](./02-app-topology.md)
- [Design spec](../../11-spec/README.md) — §6 (domain and data
  architecture), §10 (cross-cutting).
- [Checkout requirements](../../01-requirements/02-functional/03-checkout.md)
- [Orders requirements](../../01-requirements/02-functional/02-orders.md)
- Order lifecycle: `03/02-03`; promotion engine: `03/02-04`; tax engine: `03/02-05`; inventory
  model: `03/02-06`; data model: `03/03-01`; deployment: `08/01-01`.
