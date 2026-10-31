# Checkout — Functional Requirements

What the checkout module must do: the three-step delivery → payment → confirm flow, guest
checkout, stock reservation, failure/retry, and coupon entry.

**Links:** [Catalog](./01-catalog.md) · [Orders](./02-orders.md) · [Requirements index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Checkout turns a cart into a placed order. It is a **three-step flow** — delivery → payment →
confirm — operating on the order aggregate defined in `02-orders.md`. Each step is gated so a
shopper cannot skip ahead, and the confirm step is the only trigger for the state transition
`cart → awaiting_payment` and the `OrderPlaced` event.

Checkout is guest-capable: a visitor can complete a purchase without an account. Stock is
reserved at confirm, not on add-to-cart. Payment failures return the shopper to the payment
step with the cart intact. Pricing, tax, and totals are always computed server-side and never
trusted from the client (`01/01-03`). The server-side flow is detailed in `03/02-04` and the
public API in `09/03-03`.

Actors are defined in `01/01-02`.

---

## FR-CHK-001 — Three-step flow and step gating

- **Actor:** Guest; Customer
- **Description:** Guide checkout through exactly three steps — delivery, payment, confirm —
  in that order, and reject attempts to advance out of order.
- **Rules:**
  - Step order is delivery → payment → confirm; the shopper may move back to a completed step
    to revise it.
  - Advancing from delivery requires a valid shipping address and a selected shipping method.
  - Advancing from payment requires a successful payment authorization/capture outcome.
  - Confirm is the only step that places the order; revisiting a step invalidates any later
    step's result until re-completed.
- **Acceptance Criteria:**
  - Requesting the payment step before delivery is completed returns a validation error.
  - Returning to delivery and changing the address invalidates a prior shipping selection.
  - Confirm is rejected while the payment step is incomplete.

---

## FR-CHK-002 — Delivery step

- **Actor:** Guest; Customer
- **Description:** Collect the shipping address, resolve available shipping methods for the
  order, and let the shopper select one; the selected cost becomes an order adjustment.
- **Rules:**
  - The shipping address is validated before methods are quoted.
  - Available methods come from shipping calculators registered behind
    `IShippingCalculator` (`03/02-07`); at least one method must be offered.
  - Selecting a method writes a `Money` shipping adjustment to the order (FR-ORD-005).
  - Changing the address or items re-quotes methods and clears a now-invalid selection.
- **Acceptance Criteria:**
  - An address in a served zone returns one or more shipping methods with prices.
  - Selecting a method adds a shipping adjustment to the order.
  - Changing the country re-quotes methods and clears the previous selection.

---

## FR-CHK-003 — Payment step

- **Actor:** Guest; Customer
- **Description:** Collect payment through the selected gateway, authorize/capture the amount,
  and record the payment against the order.
- **Rules:**
  - Card data is captured by the provider's UI (Stripe Elements / PayPal JS SDK); the platform
    never receives or stores PAN/CVV (`01/01-03`).
  - The amount charged equals the order's server-computed total in the order currency.
  - A payment record stores the gateway reference and outcome, not card data.
  - The gateway is selected through `IPaymentGateway` (`03/02-07`).
- **Acceptance Criteria:**
  - A successful authorization records a payment with the gateway reference.
  - A client-supplied amount that differs from the computed total is rejected.
  - No card number or CVV is persisted or logged.

---

## FR-CHK-004 — Confirm and place the order

- **Actor:** Guest; Customer
- **Description:** Finalize checkout by transitioning the order from `cart` to
  `awaiting_payment`, reserving stock, and emitting `OrderPlaced`.
- **Rules:**
  - Confirm is permitted only when delivery and payment steps are complete.
  - The transition writes the order state change, stock reservation, and the `OrderPlaced`
    outbox row in a single transaction (FR-ORD-007).
  - On a successful capture the order advances to `awaiting_fulfillment`; an
    authorize-only flow leaves it at `awaiting_payment` until capture.
  - Confirm is idempotent for a repeated request on the same checkout session.
- **Acceptance Criteria:**
  - A completed confirm moves the order to `awaiting_payment` and emits `OrderPlaced`.
  - Stock is reserved in the same transaction as the state change.
  - Re-submitting the same confirm request does not create a second order or double charge.

---

## FR-CHK-005 — Guest checkout

- **Actor:** Guest
- **Description:** Allow an unauthenticated visitor to complete checkout, optionally creating
  an account during the flow.
- **Rules:**
  - Guest checkout requires an email address to send the confirmation and receipt.
  - No account is required to place an order.
  - A guest may create an account during or after checkout; the accounts module
    (`01/02-10`) defines how the resulting order is linked.
  - Guest order lookup after checkout requires the order number plus the email used, or an
    authenticated account.
- **Acceptance Criteria:**
  - A visitor with no token can complete delivery, payment, and confirm.
  - The order is placed and the confirmation email is queued to the guest's address.
  - Creating an account during checkout does not interrupt or duplicate the order.

---

## FR-CHK-006 — Stock reservation on confirm

- **Actor:** System
- **Description:** Reserve the ordered quantities at the moment the order is confirmed, so
  committed stock cannot be oversold during payment.
- **Rules:**
  - Reservation happens in the confirm transaction, never on add-to-cart.
  - A reservation does not move `on_hand`; it reduces `available` (see `01/02-08`).
  - If any line cannot be reserved, the whole confirm fails and the order stays in `cart`.
  - Reserved stock is released on cancellation or reservation expiry.
- **Acceptance Criteria:**
  - Confirming an order reserves exactly the ordered quantities.
  - A confirm that would oversell a location without backorder policy is rejected atomically.
  - Canceling the order releases the reservation.

---

## FR-CHK-007 — Payment failure and retry

- **Actor:** Guest; Customer
- **Description:** Handle gateway declines and errors without losing the cart, and allow the
  shopper to retry payment with a different method or card.
- **Rules:**
  - A failed payment leaves the order in its current state with line items and addresses
    intact.
  - The failure reason is surfaced to the shopper and recorded against the payment attempt.
  - Retry creates a new payment attempt rather than mutating the failed one.
  - Reserved stock from a failed confirm is released or held per the reservation policy.
- **Acceptance Criteria:**
  - A declined card returns the shopper to the payment step with items preserved.
  - A retry records a separate payment attempt and can succeed.
  - The order is not placed while all payment attempts have failed.

---

## FR-CHK-008 — Coupon entry

- **Actor:** Guest; Customer
- **Description:** Accept a coupon code at checkout, validate it server-side, and apply the
  resulting promotion as an adjustment before payment.
- **Rules:**
  - Coupon validation is server-side; the client is never trusted for discount amounts.
  - A valid coupon applies its promotion through the promotion engine (`01/02-07`) and
    produces a discount adjustment (FR-ORD-005).
  - Invalid, expired, ineligible, or usage-exhausted coupons are rejected with a reason.
  - Removing a coupon removes its adjustment and recomputes totals.
- **Acceptance Criteria:**
  - Entering a valid coupon reduces the computed total and adds an adjustment.
  - An expired coupon is rejected without changing the total.
  - Removing the coupon restores the total to its pre-coupon value.

---

## FR-CHK-009 — Server-computed pricing, tax, and totals

- **Actor:** System
- **Description:** Recompute pricing, tax, and totals on the server at every checkout step so
  the amount charged always matches the order.
- **Rules:**
  - Tax is computed on post-promotion amounts per the tax engine (`01/02-06`).
  - Totals are derived from source + adjustments + tax and never cached in one column
    (FR-ORD-006).
  - Prices come from the store's price sets for the order currency (FR-CAT-007).
  - If any contributing value changes, the displayed and charged totals are recomputed before
    payment.
- **Acceptance Criteria:**
  - Adding a coupon recomputes tax and total before the payment step.
  - The amount sent to the gateway equals the freshly computed server total.
  - A changed cart invalidates a stale payment amount.

---

## FR-CHK-010 — Idempotency and double-charge protection

- **Actor:** System
- **Description:** Guard the confirm and payment operations against duplicate submission,
  network retries, and concurrent requests so a shopper is never charged twice.
- **Rules:**
  - Confirm and payment requests carry an idempotency key unique to the checkout attempt.
  - Replaying the same key returns the original result rather than performing the operation
    again.
  - Concurrent confirms for the same order serialize so only one places it.
  - Double-charge runbook and gateway reconciliation are documented in `08/03-04`.
- **Acceptance Criteria:**
  - Two identical confirm requests produce one order.
  - Two identical payment requests produce one charge.
  - A concurrent second confirm returns the first result or a conflict, not a second order.

---

## FR-CHK-011 — Store checkout options

- **Actor:** Store Admin; Platform Admin
- **Description:** Configure per-store checkout behavior, including supported currencies,
  locales, enabled payment methods, and guest-checkout availability.
- **Rules:**
  - Checkout options are resolved from the store before the flow begins.
  - Only payment methods enabled for the store are offered.
  - A store may disable guest checkout, requiring an account to purchase.
  - Changing checkout options affects new checkouts, not in-flight orders.
- **Acceptance Criteria:**
  - Disabling a payment method removes it from the store's checkout.
  - A store with guest checkout disabled rejects an anonymous confirm.
  - Existing in-flight orders keep the options they began with.

---

## Open Questions

1. Whether reservations expire on a timer or only on explicit cancellation is owned by the
   inventory model (`03/02-06`); this file requires reservation-at-confirm either way.
2. The exact idempotency-key transport (header vs. body field) is finalized in the public API
   reference `09/03-03`.
3. Whether a store may require account creation before the payment step rather than at
   confirm is a checkout-options detail finalized in `04-design/01-01`.
