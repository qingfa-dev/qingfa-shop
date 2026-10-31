# Checkout Test Plan

Case-by-case verification of the three-step checkout: the happy path, guest checkout, out-of-stock handling, payment failure and retry, and coupon application.

**Links:** [Test matrix](../01-test-strategy/01-matrix.md) · [Integration tests](../01-test-strategy/03-integration.md) · [End-to-end tests](../01-test-strategy/04-e2e.md) · [Order lifecycle](../../03-architecture/02-components/03-order-lifecycle.md) · [Checkout requirements](../../01-requirements/02-functional/03-checkout.md) · [Seed data](../../05-development/01-setup/04-seed-data.md) · [Verification index](../README.md) · [Spec](../../11-spec/README.md)

---

## Scope

This plan proves the checkout flow contract in `01/02-03` as designed by `03/02-03`. It covers the
delivery → payment → confirm step gating, the confirm transaction (state transition, stock
reservation, and `OrderPlaced` outbox row together), server-computed totals, guest checkout,
idempotency, out-of-stock rejection, payment decline and retry, and coupon entry.

It does not re-test promotion arithmetic (`06/02-02`), tax arithmetic (`06/02-03`), or the full
inventory model (`06/02-04`); those are invoked here only as collaborators whose observable effect
is asserted at the checkout boundary. The `reserved` counter, `available = on_hand − reserved`,
`on_hand` movement rules, the adjustment sources `promotion | shipping | manual`, and the totals
formula `total = item total + adjustments + tax` are the rulings this plan assumes.

Layers: delivery-step invariants and confirm idempotency are unit tests; the confirm transaction,
reservation atomicity, gateway amount, and coupon recomputation are integration tests against real
PostgreSQL; the shopper journeys run in the storefront Playwright project.

---

## Preconditions

- A store resolves by host with one ISO-4217 currency and one enabled payment gateway (the `stub`
  gateway for E2E, per `06/01-04`).
- At least one active stock location with backorders disabled, and one variant with `available`
  greater than the ordered quantity.
- At least one active shipping method quoting a non-zero rate for the test destination.
- A coupon promotion (`WELCOME10`-like, percent off order) with a bounded window and usage limit.
- Integration fixtures build rows through domain operations, not the demo seed; E2E consumes the
  demo dataset and keys off its stable identifiers (`05/01-04`).

---

## Cases

### CHK-01 — Payment step is gated on delivery completion

- **Type:** unit
- **FR reference:** FR-CHK-001
- **Steps:**
  1. Create a cart order with one line item, no shipping address, and no selected method.
  2. Request the payment step for that order.
- **Expected:**
  - The request is rejected with a validation error.
  - The order stays in `cart` and no payment attempt row is written.

### CHK-02 — Revisiting delivery invalidates the shipping selection

- **Type:** integration
- **FR reference:** FR-CHK-001, FR-CHK-002
- **Steps:**
  1. Complete delivery for an order, select a shipping method, and record its shipping adjustment.
  2. Reopen delivery, change the shipping country, and re-quote.
- **Expected:**
  - The previous shipping method selection is cleared as now-invalid.
  - Methods are re-quoted for the new zone; the stale shipping adjustment is not retained on the order.

### CHK-03 — Happy path places the order and reserves stock atomically

- **Type:** E2E
- **FR reference:** FR-CHK-001, FR-CHK-002, FR-CHK-003, FR-CHK-004, FR-CHK-006
- **Steps:**
  1. As a customer, browse a product, add a variant to the cart, and enter checkout.
  2. Complete delivery with a valid address and a selected method.
  3. Pay with the success token and confirm.
- **Expected:**
  - The order transitions `cart → awaiting_payment`, and on immediate capture `→ awaiting_fulfillment`.
  - `reserved` for each ordered line increases by the ordered quantity; `on_hand` is unchanged.
  - One `OrderPlaced` outbox row commits in the same transaction as the state change.
  - The confirmation page shows the order number and a queued confirmation email.

### CHK-04 — The amount charged equals the freshly computed server total

- **Type:** integration
- **FR reference:** FR-CHK-003, FR-CHK-009
- **Steps:**
  1. Build an order whose total includes a shipping adjustment and tax.
  2. Invoke the payment step and capture the amount passed to the gateway double.
- **Expected:**
  - The gateway receives the server-computed `total = item total + adjustments + tax` in the order currency.
  - A client-supplied amount that differs from the computed total is rejected before any attempt is recorded.

### CHK-05 — Confirm idempotency prevents a second order

- **Type:** integration
- **FR reference:** FR-CHK-004, FR-CHK-010
- **Steps:**
  1. Confirm a checkout session with idempotency key K and capture the result.
  2. Replay the identical confirm request with the same key K.
- **Expected:**
  - The replay returns the original result rather than placing again.
  - Exactly one order exists and exactly one reservation is applied; no second charge is recorded.

### CHK-06 — Guest checkout completes without an account

- **Type:** E2E
- **FR reference:** FR-CHK-005
- **Steps:**
  1. With an anonymous browser context, add an item and enter checkout.
  2. Enter a guest email, complete delivery, pay, and confirm.
- **Expected:**
  - The visitor advances through all three steps with no token.
  - The order is placed and the confirmation email is queued to the guest address.
  - Guest order lookup requires the order number plus the same email.

### CHK-07 — Account creation at confirm does not duplicate the order

- **Type:** integration
- **FR reference:** FR-CHK-005
- **Steps:**
  1. Complete a guest checkout to the confirm step with a new email.
  2. Create an account with that email during confirmation and confirm once.
- **Expected:**
  - Exactly one order is placed and linked to the newly created account.
  - Cart contents and computed totals are unchanged by the account creation.

### CHK-08 — A confirm that would oversell is rejected atomically

- **Type:** integration
- **FR reference:** FR-CHK-006, FR-INV-007
- **Steps:**
  1. Set a non-backorder location with `on_hand` of 1 for a variant.
  2. Build a two-unit order and confirm.
- **Expected:**
  - The confirm fails because `available` would fall below zero.
  - The order stays in `cart`; no partial reservation persists on any line; no `OrderPlaced` row exists.

### CHK-09 — Out-of-stock storefront path blocks purchase

- **Type:** E2E
- **FR reference:** FR-INV-009, FR-CHK-006
- **Steps:**
  1. Open a variant that has zero `available` at a non-backorder location.
  2. Attempt to add it to the cart.
- **Expected:**
  - The storefront reports the variant as out of stock and blocks add-to-cart.
  - The availability shown is server-sourced, never a client value.

### CHK-10 — A declined payment preserves the cart and addresses

- **Type:** E2E
- **FR reference:** FR-CHK-007
- **Steps:**
  1. Progress an order through delivery and attempt payment with the decline token.
- **Expected:**
  - The shopper returns to the payment step with line items, addresses, and the delivery selection intact.
  - The decline reason is surfaced and recorded against the failed attempt.
  - The order is not placed while all attempts have failed.

### CHK-11 — Retry records a new attempt and can succeed

- **Type:** integration
- **FR reference:** FR-CHK-007
- **Steps:**
  1. Record a declined payment attempt on an order.
  2. Retry payment with a method that succeeds.
- **Expected:**
  - A second, separate payment attempt row is created; the failed attempt is not mutated.
  - The successful attempt advances the order `awaiting_payment → awaiting_fulfillment`.

### CHK-12 — A failed confirm releases its reservation

- **Type:** integration
- **FR reference:** FR-CHK-007, FR-INV-005
- **Steps:**
  1. Reserve stock during a confirm, then drive the payment to terminal failure per store policy.
- **Expected:**
  - `reserved` is decremented by the released quantity and `available` is restored.
  - `on_hand` is unchanged, because reservation and release are counter operations, not movements.

### CHK-13 — A valid coupon applies its promotion before payment

- **Type:** integration
- **FR reference:** FR-CHK-008, FR-PRM-007
- **Steps:**
  1. Build an order and enter a valid coupon code.
  2. Recompute totals.
- **Expected:**
  - A `promotion` adjustment referencing the promotion and action is written.
  - The computed total decreases by the clamped discount; tax is recomputed on the post-promotion amount.

### CHK-14 — Invalid or exhausted coupons are rejected with a reason

- **Type:** integration
- **FR reference:** FR-CHK-008, FR-PRM-007
- **Steps:**
  1. Submit an unknown code, then an expired code, then a usage-exhausted code.
- **Expected:**
  - Each is rejected with a distinct reason (`NotFound`, `Expired`, `Exhausted`).
  - The order total is unchanged in every case and no adjustment is written.

### CHK-15 — Removing a coupon restores the pre-coupon total

- **Type:** integration
- **FR reference:** FR-CHK-008, FR-PRM-007
- **Steps:**
  1. Apply a valid coupon and record the reduced total.
  2. Remove the coupon and recompute.
- **Expected:**
  - The promotion adjustment is removed and the total returns to its pre-coupon value.
  - Tax recomputes on the restored base; no adjustment row with source `tax` is ever produced.

### CHK-16 — Concurrent confirms for one order place it once

- **Type:** integration
- **FR reference:** FR-CHK-010
- **Steps:**
  1. Issue two concurrent confirm requests for the same order and checkout session.
- **Expected:**
  - Exactly one confirm wins; the other returns the first result or a conflict.
  - One order and one reservation persist, and the gateway is charged once.

---

## Open Questions

1. The idempotency-key transport (header versus body field) is owned by the public API reference
   `09/03-03`; this plan asserts the replay behavior regardless of transport.
2. Whether a store may require account creation before the payment step is a checkout-option
   detail owned by `04/01-01`; both variants preserve the single confirm transaction asserted here.
3. The reservation-expiry window default is owned by `08/01-01`; this plan asserts release on
   explicit cancellation and on terminal failure, and treats timer expiry as the same counter effect.
