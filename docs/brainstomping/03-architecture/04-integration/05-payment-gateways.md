# Payment Gateways

The `IPaymentGateway` extension contract, the six built-in gateway adapters, idempotency keys, webhook handling, and the failure/retry policy that keeps a charge from being taken twice.

**Links:** [Module contracts](../02-components/02-module-contracts.md) · [Extension architecture](../02-components/07-extension-architecture.md) · [Order lifecycle](../02-components/03-order-lifecycle.md) · [REST admin API](./02-rest-admin.md) · [REST public API](./01-rest-public.md) · [Entity model](../03-data/01-entities.md) · [Payments requirements](../../01-requirements/02-functional/05-payments.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

The Payments module turns a checkout total into money received without coupling the platform to any
provider. It does so through one extension contract, `IPaymentGateway` (`03/02-07`), resolved by key
through `IPaymentGatewayRegistry` (`03/02-02`). This file is the authoritative definition of that
contract's full surface, the six built-in adapters, the idempotency and webhook rules, and the
retry policy. The payment state machine and its effect on orders are `03/02-03`; the admin
operations that drive refunds and offline confirmation are `03/04-02`; the double-charge runbook is
`08/03-04`.

The single rule: **every charge carries an idempotency key, the platform never stores card data, and
a retried request returns the original outcome rather than charging again** (`FR-PAY-005`,
`FR-PAY-010`).

---

## IPaymentGateway Contract — `Resys.Core.Payments`

The full contract below, also carried by the extension catalogue `03/02-07`, is required by
`FR-PAY-002`. This file is the authoritative integration definition of that surface; the six
operations are authorize, capture, refund, void, status query, and webhook handling. The key is the
adapter's registry key (`stripe`, `paypal`, `bank-transfer`, `check`, `store-credit`,
`cash-on-delivery`).

```csharp
namespace Resys.Core.Payments;

public interface IPaymentGateway : IHasKey
{
    Task<PaymentAuthorizationResult> AuthorizeAsync(PaymentAuthorizationRequest request, CancellationToken ct = default);
    Task<PaymentCaptureResult> CaptureAsync(PaymentCaptureRequest request, CancellationToken ct = default);
    Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken ct = default);
    Task<VoidResult> VoidAsync(VoidRequest request, CancellationToken ct = default);
    Task<PaymentStatusResult> GetStatusAsync(string providerReference, CancellationToken ct = default);
    Task<WebhookOutcome> HandleWebhookAsync(PaymentWebhook webhook, CancellationToken ct = default);
}
```

Requests carry `PaymentId`, a `Money` amount, the originating `StoreId`, and an `IdempotencyKey`;
online adapters also receive a provider token (never a PAN). Results carry a success flag, an
optional provider reference, an optional decline code, and a `Retryable` flag that classifies the
failure for the caller.

```csharp
public sealed record PaymentAuthorizationRequest(
    PaymentId PaymentId, Money Amount, string IdempotencyKey,
    string? PaymentToken, StoreId StoreId, string? CustomerReference,
    IReadOnlyDictionary<string, string>? Metadata);

public sealed record PaymentCaptureRequest(
    PaymentId PaymentId, string ProviderReference, Money Amount, string IdempotencyKey);

public sealed record RefundRequest(
    PaymentId PaymentId, string ProviderReference, Money Amount,
    string IdempotencyKey, string? Reason);

public sealed record VoidRequest(PaymentId PaymentId, string ProviderReference, string IdempotencyKey);

public sealed record PaymentWebhook(
    string ProviderKey, string EventId, string EventType, string RawPayload,
    IReadOnlyDictionary<string, string> Headers);

public sealed record PaymentAuthorizationResult(
    bool Succeeded, string? ProviderReference, string? DeclineCode, bool Retryable);
public sealed record PaymentCaptureResult(
    bool Succeeded, string? ProviderReference, string? DeclineCode, bool Retryable);
public sealed record RefundResult(bool Succeeded, string? RefundReference, bool Retryable);
public sealed record VoidResult(bool Succeeded, bool Retryable);
public sealed record PaymentStatusResult(string State, Money? CapturedAmount, Money? RefundedAmount);
public sealed record WebhookOutcome(bool Accepted, string? ProviderReference, string? NewState, bool Ignored);
```

`IPaymentService` (`03/02-02`) resolves the adapter with `IPaymentGatewayRegistry.Resolve(key)` and
persists only the result: a `Payment` row and an append-only `PaymentTransaction` row per operation
(`03/03-01`). No adapter result contains card fields; the API layer never receives PAN or CVV.

---

## Built-in Gateways

| Key | Flow | Authorize | Capture | Refund / Void | External call |
|---|---|---|---|---|---|
| `stripe` | Online (Stripe Elements / Payment Element) | `PaymentIntent` with `capture_method=manual` | `PaymentIntent` capture | Stripe refund / cancel | Yes |
| `paypal` | Online (PayPal JS SDK) | PayPal Orders authorize | Orders capture | PayPal refund / void | Yes |
| `bank-transfer` | Offline (manual) | Records intent | Admin marks paid | Admin records refund with audit entry | No |
| `check` | Offline (manual) | Records intent | Admin marks paid | Admin records refund with audit entry | No |
| `store-credit` | Internal ledger | Debits ledger atomically | Settles the debit | Credits the ledger | No |
| `cash-on-delivery` | Offline (on delivery) | Records intent | Admin marks paid at delivery | Admin records refund with audit entry | No |

### Stripe — first online gateway

The Stripe adapter uses the `Stripe.net` SDK behind the contract. Card entry happens in Stripe
Elements in the storefront; the API receives a `payment_method` token and passes it as
`PaymentToken`. `AuthorizeAsync` creates a `PaymentIntent` with manual capture and sends the client
`IdempotencyKey` as Stripe's `Idempotency-Key` header; `CaptureAsync` captures the intent;
`RefundAsync` issues a refund against the stored `PaymentIntent` id; `VoidAsync` cancels an
uncaptured intent (`FR-PAY-003`, `FR-PAY-006`). The `payment_intent.succeeded`,
`payment_intent.canceled`, and `charge.refunded` events are consumed through the webhook entry
point. Only the publishable key reaches the storefront; the secret key is server configuration
(`05/01-02`).

### PayPal — second online gateway

The PayPal adapter uses the PayPal .NET SDK and the JS SDK in the storefront. `AuthorizeAsync`
creates an order and authorizes it; `CaptureAsync` captures it; `RefundAsync` refunds a captured
capture. PayPal voids an authorization through its own authorization-void endpoint rather than its
refund endpoint, so `VoidAsync` calls the void endpoint and the resulting state is confirmed
through `GetStatusAsync`. The client sends
`PayPal-Request-Id` (mapped from `IdempotencyKey`). Sandbox and live credentials are server-only.

### Bank transfer

An offline method (`FR-PAY-003`). `AuthorizeAsync` records the intent and returns a synthetic
`ProviderReference` (the payment id); no provider round-trip occurs. The order stays in
`awaiting_payment` until an admin marks the payment paid through `/api/admin`
(`FR-PAY-009`), which records a capture transaction and drives the order transition. A refund on an
offline payment is recorded manually with an audit entry (`FR-PAY-006`).

### Check

Same offline behavior as bank transfer, with a different display label and no account details.
`AuthorizeAsync` records the intent; an admin confirms receipt and marks the payment paid.

### Store credit

An internal, ledger-backed method (`FR-PAY-007`). `AuthorizeAsync` checks the customer's derived
balance and atomically writes a debit (clamped to the balance) in the same transaction as the
payment row; `CaptureAsync` settles the debit; `RefundAsync` writes a credit; `VoidAsync` reverses
an unsettled debit. It is offered only to signed-in customers with a positive balance and is
unavailable to guests (`FR-PAY-008`). The ledger is store-scoped here; whether a customer's credit
is shared across sibling stores remains owned by `03/03-02`.

### Cash-on-delivery

An offline method. `AuthorizeAsync` records the intent so the order can proceed to fulfillment;
`CaptureAsync` is invoked when the admin confirms collection at delivery. No provider call is made
and no card data is involved.

---

## Idempotency Keys

Idempotency is enforced at three layers so a retried or concurrent request never charges twice
(`FR-PAY-005`).

1. **Client key.** `POST /api/v1/checkout/confirm` requires an `Idempotency-Key` header
   (`03/04-01`); the Payments module derives the payment's key from it and stores it on the
   `Payment` row. The unique index `(StoreId, IdempotencyKey)` (`03/03-01`) rejects a duplicate
   insert.
2. **Attempt scope.** Replaying the same key returns the original `Payment` and outcome without
   calling the provider. A deliberately new attempt (for example after a terminal decline) uses a
   new key and creates a new row.
3. **Provider key.** Each adapter forwards the key using the provider's native mechanism — Stripe
   `Idempotency-Key`, PayPal `PayPal-Request-Id` — and records the provider's reference. For
   offline and store-credit methods the database is the sole guard.

Concurrent captures for one order serialize on the payment row (row lock plus the unique index):
exactly one capture succeeds and the other returns the stored result or a conflict. Refunds use a
fresh key per refund and additionally re-check the captured amount minus prior refunds inside the
transaction (`FR-PAY-006`).

---

## Webhook Handling

Provider callbacks are a correction channel, never the primary result: the synchronous
authorize/capture response remains authoritative, and a webhook reconciles state that changed
outside the request (for example an asynchronous dispute or a delayed capture). The endpoint is
`POST /api/v1/webhooks/payments/{gateway}` — public by necessity but authenticated by the provider
signature, not by a bearer token.

1. **Verify.** The adapter validates the signature over the raw payload using the provider secret:
   Stripe `Stripe-Signature` via the SDK, PayPal transmission-signature verification via the SDK. A
   failed verification is `401` and is logged; the payload is never trusted.
2. **Deduplicate.** The provider `EventId` is recorded in a processed-events set; a redelivery of
   the same event is accepted and ignored, so at-least-once provider delivery is safe.
3. **Map.** A known event maps to a payment state transition; an unrecognized event type returns
   `WebhookOutcome(..., Ignored: true)` and changes nothing.
4. **Apply.** The state change and its `PaymentTransaction` row are written in one transaction and
   the transition event is written to the outbox (`03/02-03`); the relay then notifies Orders and
   the email consumer. A webhook never trusts a client-supplied amount: the amount is read from
   `GetStatusAsync` and reconciled against the stored payment.
5. **Respond.** The endpoint returns `2xx` once the event is durably recorded, so the provider stops
   retrying. Processing errors return `5xx` and let the provider redeliver.

Signature secrets and endpoints are per store and per provider, supplied through options
(`05/01-02`); the storefront and admin never see them.

---

## Failure and Retry

Every adapter result is classified by the `Retryable` flag, and the Payments module acts on it:

- **Transient** (`Retryable = true`: network timeout, provider `5xx`, rate limit). The attempt is
  retried with exponential backoff and jitter for a bounded number of attempts, reusing the **same
  idempotency key** so the provider cannot create a second charge. If the budget is exhausted the
  attempt is recorded `failed` and surfaced to the caller.
- **Terminal** (`Retryable = false`: decline, invalid token, insufficient funds). No retry occurs;
  the attempt is recorded `failed`, the order does not advance, and the shopper may start a new
  attempt with a **new key** (`FR-PAY-004`).
- **Unknown outcome** (a timeout after the provider may have charged). The adapter calls
  `GetStatusAsync` to reconcile; if the provider confirms the charge it is recorded as captured,
  otherwise it is treated as transient. This is the case the `08/03-04` runbook covers.
- **Isolation.** A failed attempt earns a transaction row but advances no order state and emits no
  transition event (`03/02-03`). Notification failures never roll back a payment transition.
- **Reconciliation.** A periodic job compares payments in a non-terminal state against provider
  status and emits `PaymentFailed` or `PaymentCaptured` when the provider disagrees with the local
  record.

---

## Decision — Spec §13 Open Question 3 (gateway default order)

**Resolved: Stripe is the first/default online gateway; PayPal is second.**

This confirms the tentative choice in Spec §13. Both adapters ship in the box (`FR-PAY-003`); the
decision concerns which is the default and which is positioned first at checkout.

- **Stripe first.** The `Stripe.net` SDK is mature and typed; Stripe Elements keeps PAN/CVV entirely
  in the browser, satisfying `FR-PAY-010` with no client-side card handling; Stripe provides a
  first-class `Idempotency-Key` header and signed webhooks, satisfying `FR-PAY-005` directly; and its
  test mode plus broad card/wallet coverage make it the lowest-friction default for a new store.
- **PayPal second.** PayPal adds buyer reach and an alternative funding path, but its authorization
  void uses a separate endpoint from refunds and its webhook verification is more involved, so it
  ships as the second online method rather than the default.
- **Effect.** The seed data enables Stripe first and PayPal second for a new store; checkout lists
  enabled methods in registry order and preselects Stripe when both are enabled. Disabling or
  reordering methods is a per-store admin setting and affects new checkouts only (`FR-PAY-001`,
  `FR-CHK-011`). This ordering is a product default, not a `KeyedRegistry` semantic: the registry's
  "last-registered wins" rule still governs an extension that replaces an adapter by key
  (`03/02-07`).

## Open Questions

1. Whether store credit is per store or shared across a customer's sibling stores is owned by
   `03/03-02`; this file fixes it as an internal store-scoped ledger gateway.
2. The exact provider SDK versions, test/live key naming, and webhook secrets are environment
   configuration owned by `05/01-02`.
3. Provider-side retry windows and dispute webhook coverage are finalized with the `08/03-04`
   double-charge runbook; the local idempotency and reconciliation rules here are unaffected.

## Links

- [Module contracts](../02-components/02-module-contracts.md)
- [Extension architecture](../02-components/07-extension-architecture.md)
- [Order lifecycle](../02-components/03-order-lifecycle.md)
- [REST public API](./01-rest-public.md)
- [REST admin API](./02-rest-admin.md)
- [Entity model](../03-data/01-entities.md)
- [Payments requirements](../../01-requirements/02-functional/05-payments.md)
- [Checkout requirements](../../01-requirements/02-functional/03-checkout.md)
- [Returns requirements](../../01-requirements/02-functional/09-returns.md)
- [ADR 0002 — Modular monolith](../../00-governance/01-adr/02-modular-monolith.md)
- [Design spec](../../11-spec/README.md) — §6 (payments domain),
  §8 (extension contracts), §13 (Open Question 3, resolved above).
- Extension contracts: `03/02-07`; module contracts: `03/02-02`; order lifecycle: `03/02-03`;
  multi-store and store credit: `03/03-02`; environment config: `05/01-02`; gateway runbook:
  `08/03-03`; double-charge runbook: `08/03-04`; cart/checkout reference: `09/03-03`; admin API
  reference: `09/03-05`.
