# Payments — Functional Requirements

What the payments module must do: the payment method registry, gateway adapters, payment
states, refunds and voids, the store-credit ledger, and idempotent charges.

**Links:** [Orders](./02-orders.md) · [Checkout](./03-checkout.md) · [Tax](./06-tax.md) · [Requirements index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Payments turns a checkout total into money received. It owns the per-store **payment method
registry**, the gateway adapters behind `IPaymentGateway`, the **payment state machine**, and
the **store-credit ledger**. Built-in gateways are Stripe, PayPal, bank transfer, check, store
credit, and cash-on-delivery, spanning online provider-hosted flows and offline manual flows
(spec §8).

A payment records only the gateway reference and outcome; the platform never stores, logs, or
transmits PAN or CVV (`01/01-03`). Charges are idempotent so a retried or concurrent request
cannot double-charge. A successful capture drives the order transition
`awaiting_payment → awaiting_fulfillment` (FR-ORD-004). Gateway integration is planned in
`03/04-05`; the double-charge runbook is `08/03-04`.

Actors are defined in `01/01-02`.

---

## FR-PAY-001 — Payment method registry

- **Actor:** Store Admin; Platform Admin
- **Description:** Register and enable payment methods per store so checkout offers only the
  methods that store has turned on.
- **Rules:**
  - A payment method is store-scoped and binds a gateway adapter or offline handler plus
    display configuration.
  - Methods can be enabled or disabled; disabled methods are never offered.
  - A method identifier is unique within a store.
  - Changing enabled methods affects new checkouts, not in-flight orders (FR-CHK-011).
- **Acceptance Criteria:**
  - Enabling Stripe makes it available in that store's checkout.
  - Disabling a method removes it from new checkouts.
  - Store A's method list is independent of store B's.

---

## FR-PAY-002 — Gateway contract (`IPaymentGateway`)

- **Actor:** System; Payment/Shipping Providers
- **Description:** Perform authorize, capture, refund, and void through `IPaymentGateway` so the
  platform is independent of any single provider.
- **Rules:**
  - `IPaymentGateway` exposes authorize, capture, refund, void, and a webhook/event entry point.
  - Adapters are registered in DI; last-registered wins per key (spec §8).
  - The platform passes an amount (`Money`) and a reference; it never passes or stores card
    data.
  - Each operation returns an outcome plus a provider reference, recorded on the payment.
- **Acceptance Criteria:**
  - A Stripe adapter and a PayPal adapter both satisfy the same contract.
  - A custom gateway can be registered without core changes.
  - Every operation result stores a provider reference.

---

## FR-PAY-003 — Supported gateways

- **Actor:** Store Admin
- **Description:** Ship built-in adapters for Stripe, PayPal, bank transfer, check, store credit,
  and cash-on-delivery, covering both online and offline flows.
- **Rules:**
  - Stripe and PayPal are online gateways using provider-hosted UI (Stripe Elements / PayPal JS
    SDK).
  - Bank transfer, check, and cash-on-delivery are offline methods that record an intent and
    complete on manual confirmation.
  - Store credit is an internal, ledger-backed method (FR-PAY-007).
  - Offline methods do not call an external gateway and may leave the order awaiting
    confirmation until an admin marks it paid.
- **Acceptance Criteria:**
  - A store can offer Stripe, PayPal, bank transfer, check, store credit, and cash-on-delivery
    independently.
  - An offline method can be selected without any provider round-trip.
  - Store credit is offered only when the customer has a positive balance.

---

## FR-PAY-004 — Payment states and lifecycle

- **Actor:** System; Store Admin
- **Description:** Track each payment through a locked state set and drive the order's payment
  transition.
- **Rules:**
  - Payment states are `pending → authorized → captured`, or `failed`, plus `voided`,
    `refunded`, and `partially_refunded`.
  - A successful capture, or an offline method marked paid, triggers `awaiting_payment →
    awaiting_fulfillment` (FR-ORD-004).
  - A failed attempt neither places nor advances the order; a retry creates a new attempt
    (FR-CHK-007).
  - A void releases an authorization; a refund returns captured funds.
- **Acceptance Criteria:**
  - Authorize then capture moves the payment to `captured` and the order to
    `awaiting_fulfillment`.
  - A decline records a `failed` attempt and leaves the order in its prior state.
  - A full refund moves the payment to `refunded`; a partial refund to `partially_refunded`.

---

## FR-PAY-005 — Idempotent charges

- **Actor:** System
- **Description:** Guard authorize and capture against duplicate submission and retries so a
  shopper is never charged twice.
- **Rules:**
  - Every charge request carries an idempotency key unique to the payment attempt
    (FR-CHK-010).
  - Replaying the same key returns the original outcome rather than charging again.
  - Concurrent requests for the same order serialize so only one capture succeeds.
  - Provider idempotency support is used where available and the reference recorded.
- **Acceptance Criteria:**
  - Two identical charge requests produce one provider charge and one payment record.
  - A retried request with the same key returns the first result.
  - Concurrent captures yield one capture and a conflict or the original result for the other.

---

## FR-PAY-006 — Refunds and voids

- **Actor:** Store Admin; System
- **Description:** Refund or void a payment through the gateway that captured it, recording the
  reference and the outcome.
- **Rules:**
  - Refunds and voids must use the originating gateway (`01/01-03`).
  - A refund may be full or partial, never exceeding the captured amount minus prior refunds.
  - A refund records a new transaction row referencing the original payment; it never mutates
    the original.
  - A refund on an offline payment is recorded manually with an audit entry.
- **Acceptance Criteria:**
  - A full refund records a refund transaction against the original payment.
  - A refund exceeding the captured remainder is rejected.
  - A refund is routed to the same gateway that captured the payment.

---

## FR-PAY-007 — Store-credit ledger

- **Actor:** Customer; Store Admin
- **Description:** Maintain store credit as an append-only ledger of credits and debits whose
  balance is derived, and allow it to pay for orders.
- **Rules:**
  - Balance = sum of credits − sum of debits; it is never stored as a single mutable column.
  - Credits arise from refunds, grants, or adjustments; debits arise from spending at checkout.
  - Spending store credit cannot drive the balance negative; the spendable amount is clamped to
    the balance.
  - Ledger entries are store-scoped and reference the order or source that produced them.
- **Acceptance Criteria:**
  - Granting credit increases the derived balance by exactly that amount.
  - Paying with store credit debits the balance and records the order reference.
  - Attempting to spend more than the balance is rejected or clamped to the balance.

---

## FR-PAY-008 — Payment method availability at checkout

- **Actor:** Guest; Customer
- **Description:** Offer only payment methods valid for the store, the order currency, the order
  amount, and, for store credit, the customer's balance.
- **Rules:**
  - Checkout resolves available methods from the store registry before the payment step.
  - A method requiring a customer, such as store credit, is unavailable to guests.
  - The amount charged equals the order's server-computed total in the order currency
    (FR-CHK-009).
  - A client-supplied amount is never trusted.
- **Acceptance Criteria:**
  - A guest sees only guest-eligible methods.
  - Store credit appears for a customer with a positive balance and not otherwise.
  - A client amount that differs from the computed total is rejected.

---

## FR-PAY-009 — Admin payment operations and audit

- **Actor:** Store Admin; Platform Admin
- **Description:** List payments, capture, void, refund, and mark offline payments paid through
  `/api/admin`, scoped to the store.
- **Rules:**
  - Every operation requires the `admin` role plus store scope and is written to the audit log.
  - Payment detail includes the gateway reference, state, amount, and transaction history.
  - State changes go through the payment state machine, never through direct writes.
- **Acceptance Criteria:**
  - An admin can mark a bank-transfer order paid and advance its payment.
  - A refund action records an audit entry and a transaction row.
  - An admin scoped to store A cannot view store B's payments.

---

## FR-PAY-010 — No card data (security constraint)

- **Actor:** System
- **Description:** Ensure the platform never receives, stores, logs, or transmits PAN or CVV.
- **Rules:**
  - Card entry happens in provider-hosted UI; the platform receives only tokens and references.
  - Logs and traces redact payment data; no PII appears in logs (`01/01-03`).
  - Payment records store the gateway reference and outcome only.
- **Acceptance Criteria:**
  - No card number or CVV appears in the database or in logs.
  - A payment can be refunded using only the stored provider reference.
  - An inspection of payment records finds no card fields.

---

## Open Questions

1. The default gateway order (tentatively Stripe first, PayPal second) is confirmed in
   `03/04-05`.
2. Whether store credit is per store or shared across a customer's stores is resolved with the
   multi-store model `03/03-02`.
3. Webhook signature verification and provider retry policy are finalized in `03/04-05`.
