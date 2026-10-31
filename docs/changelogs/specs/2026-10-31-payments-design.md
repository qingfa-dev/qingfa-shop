# Payments Module — Design Specification

- **Status:** Approved (design) — implementation pending
- **Date:** 2026-10-31
- **Module:** `Payments`
- **Supersedes:** none
- **Related:** `2026-10-30-building-blocks-modules-refactor-design.md`, `2026-10-30-api-result-envelope-design.md`, `2026-10-30-identity-feature-slices-design.md`
- **Research basis:** Solidus `Spree::Payment` / `PaymentMethod` / `Refund` / `Payment::Processing`; Spree 6 `Spree::Payment` / `PaymentMethod` / `PaymentSession` / `Refund`.

## 1. Context

The solution has no payment capability: no order, checkout, invoice, or provider adapter exists.
The API already returns `Result` / `Result<T>` directly, identity is a MediatR feature slice, and
the layers split into `BuildingBlocks` (platform) and `Modules` (business).

This spec defines the first payments slice: a `Payments` module that owns payment intents and
refunds, integrates Stripe and PayPal behind one port, and serves customers, admins, and provider
webhooks.

## 2. Goals

- G1: Own a `Payment` aggregate with an explicit, testable lifecycle.
- G2: Own `Refund` as a first-class entity with its own provider reference and status.
- G3: Integrate two real providers (Stripe, PayPal) plus a deterministic fake, behind one port.
- G4: Accept signed provider webhooks and settle idempotently.
- G5: Expose customer, admin, and webhook HTTP surfaces using the existing `Result` contract.
- G6: Persist events as the source of truth and expose queryable projections.

## 3. Non-goals

- N1: Orders, carts, checkout orchestration, taxes, shipping, invoices.
- N2: Storing card data, PAN, CVV, or reusable payment sources.
- N3: Store credit, gift cards, negative/offset payments, payment splitting across orders.
- N4: Disputes/chargebacks changing payment state.
- N5: Background reconciliation polling, FX conversion, payout/settlement reporting.
- N6: Frontend screens (storefront or admin).
- N7: Recurring/subscription billing and saved payment methods.

## 4. Locked decisions

| # | Decision | Rationale |
|---|---|---|
| D-1 | Payments live in `Modules/Payments` on all three layers. | Matches the BuildingBlocks/Modules split; nothing here is platform-reusable. |
| D-2 | `IPaymentGateway` is module-local, not a `BuildingBlocks/Ports` entry. | Exactly one consumer: the Payments module. |
| D-3 | Canonical port + small capability interfaces + metadata passthrough. | Two providers differ (refund/void semantics); a lowest-common-denominator port would silently downgrade behaviour. |
| D-4 | Event store is the source of truth; `payments` / `payment_refunds` are projections. | Full audit trail without a second write model; reuses the existing event-sourcing stack. |
| D-5 | No separate `PaymentSession` record; the `Payment` row is the intent. | No order/source requirements, so the split buys nothing; the settle command handles the race. |
| D-6 | Confirm-return and webhook converge on one `SettlePayment` command. | Prevents the double-capture path Spree explicitly warns about. |
| D-7 | Provider reference is unique per `(provider, provider_reference)`; idempotency key unique per `(customer_id, idempotency_key)`. | Makes provider callbacks resolvable and request replays safe at the storage layer, not just in code. |
| D-8 | Refunds are entities with their own status and provider reference; refund is idempotent by refund id. | A failed refund must stay auditable and a retry must not double-credit. |
| D-9 | Status transitions are aggregate methods returning `Result`; no state-machine library. | Matches the repository's monadic style; the transition table stays explicit and testable. |
| D-10 | Stripe uses `Stripe.net` (already referenced in `Infrastructure.csproj` at 53.0.0); PayPal uses a typed `HttpClient`. | No new Stripe dependency; PayPal has no maintained official .NET SDK. |
| D-11 | Webhooks enter through dedicated gateway routes that **preserve** the `/api` prefix, declared before the `/api` catch-all. | The API serves Carter modules under `/api/*`; preserving the prefix avoids depending on the catch-all transform. |
| D-12 | Provider declines are domain outcomes (`Failed`), not HTTP errors. | A declined payment still exists as a resource and must be readable/auditable. |
| D-13 | Money is always `Money(decimal Amount, Currency Currency)` from `Core.BuildingBlocks.Domain.ValueObjects.Money`; adapters own minor-unit conversion. | Keeps the provider-specific exponent rules out of the domain. |
| D-14 | `Fake` may not be enabled in `Production`. | A fake gateway in production would accept money that never moves. |

## 5. Module layout

```
src/Core/Modules/Payments/
  Common/
    PaymentProvider.Enum.cs           Stripe | PayPal | Fake
    PaymentCaptureMode.Enum.cs        Automatic | Manual
  Intents/
    Payment.cs                       aggregate root
    Payment.Status.Enum.cs
    Payment.Transition.cs            guarded transition methods
    Payment.Validator.cs
    Payment.IdempotencyKey.cs        value object
    Payment.ExternalReference.cs     value object
  Refunds/
    Refund.cs                        aggregate entity
    Refund.Status.Enum.cs
    Refund.Validator.cs
  Events/
    PaymentInitiated.Event.cs
    PaymentProcessing.Event.cs
    PaymentAuthorized.Event.cs
    PaymentCaptured.Event.cs
    PaymentPartiallyCaptured.Event.cs
    PaymentFailed.Event.cs
    PaymentCancelled.Event.cs
    PaymentExpired.Event.cs
    RefundRequested.Event.cs
    RefundSucceeded.Event.cs
    RefundFailed.Event.cs
  PaymentsOutcome.cs                 module error codes

src/Application/Modules/Payments/
  Intents/
    Commands/  CreatePayment, ConfirmPayment, CancelPayment, CapturePayment, SettlePayment
    Queries/   GetPayment, ListMyPayments
    Models/    CreatePayment.UseCase, PaymentResponse.Model, PaymentAmount.Model
    Mappings/  Payment.Mapping.cs
    Validators/
  Refunds/
    Commands/  RefundPayment
    Queries/   GetRefund
    Models/    RefundPayment.UseCase, RefundResponse.Model
    Validators/
  Admin/
    Queries/   SearchPayments, GetPaymentForAdmin
    Models/    PaymentSearch.Param
  Webhooks/
    Commands/  ProcessPaymentWebhook
    Models/    PaymentWebhook.UseCase
  Ports/
    IPaymentGateway.Interface.cs
    IPaymentGatewayRegistry.Interface.cs
    ISupportsPartialRefund.Interface.cs
    ISupportsManualCapture.Interface.cs
    Models/    PaymentIntentRequest, PaymentIntentResult, PaymentCaptureRequest,
               PaymentCaptureResult, PaymentCancelRequest, PaymentCancelResult,
               PaymentRefundRequest, PaymentRefundResult, PaymentProviderEvent,
               PaymentProviderIntentStatus, PaymentProviderEventType
  Persistence/
    IPaymentReadStore.Interface.cs
    IRefundReadStore.Interface.cs
    IWebhookEventStore.Interface.cs
  Events/
    PaymentCaptured.Integration.cs
    PaymentRefunded.Integration.cs
  Permissions/
    PaymentPermissionCatalog.cs
  Payments.Composition.cs

src/Infrastructure/Modules/Payments/
  Adapters/Fake/       Fake.PaymentGateway.cs
  Adapters/Stripe/     Stripe.PaymentGateway.cs, Stripe.Money.Converter.cs,
                       Stripe.Webhook.Verifier.cs
  Adapters/PayPal/     PayPal.PaymentGateway.cs, PayPal.Money.Converter.cs,
                       PayPal.Webhook.Verifier.cs, PayPal.Token.Provider.cs
  Persistence/         Payment.Projection.cs, Refund.Projection.cs,
                       PaymentReadStore.cs, RefundReadStore.cs,
                       WebhookEventStore.cs, PaymentWebhookEvent.Model.cs
  Payments.Composition.cs

app/Web.Api/Endpoints/Payments/
  Payment.Endpoint.cs               customer routes
  Payment.Admin.Endpoint.cs         permission-gated routes
  Payment.Webhook.Endpoint.cs       anonymous, signature-verified

src/Infrastructure/Migrations/      AddPayments migration
```

Hard rules:
- `Core` has no reference to `Application` or `Infrastructure`.
- `BuildingBlocks` never references `Modules`.
- No payment adapter is registered in `BuildingBlocks/Adapters`.

## 6. Domain model

### 6.1 Aggregate state

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | aggregate id / event stream key |
| `Provider` | `PaymentProvider` | `Stripe`, `PayPal`, `Fake` |
| `ProviderReference` | `string` | intent/order id from the provider; assigned on settle or create |
| `Amount` | `Money` | immutable after creation |
| `CaptureMode` | `PaymentCaptureMode` | `Automatic` or `Manual` |
| `ExternalReference` | `PaymentExternalReference` | caller's key (e.g. cart id); unique per `(customer_id, provider)` |
| `IdempotencyKey` | `PaymentIdempotencyKey` | unique per `customer_id` |
| `CustomerId` | `Guid` | owner from the `sub` claim; required |
| `Status` | `PaymentStatus` | see 6.2 |
| `AuthorizedAmount` | `Money` | equals `Amount` in automatic mode, `0` until authorized in manual mode |
| `CapturedAmount` | `Money` | accumulated from capture events |
| `RefundedAmount` | `Money` | sum of succeeded refunds |
| `FailureCode` | `string?` | canonical code from the adapter |
| `FailureMessage` | `string?` | provider-safe message, never raw payload |
| `ApprovalUrl` | `string?` | hosted-checkout redirect target |
| `ExpiresAt` | `DateTimeOffset?` | intent lifetime end |
| `CreatedAt` / `UpdatedAt` | `DateTimeOffset` | UTC |
| `Version` | `int` | event stream version |
| `Refunds` | `IReadOnlyList<Refund>` | entities within the aggregate |

### 6.2 Status

`PaymentStatus`: `Pending`, `Processing`, `Authorized`, `Captured`, `PartiallyRefunded`,
`Refunded`, `Failed`, `Cancelled`, `Expired`.

### 6.3 Transition table

Every row appends exactly one event. A row whose guard fails returns
`PaymentsOutcome.Failure.InvalidState` (`409`) and appends nothing.

| From | Trigger | To | Guard | Event |
|---|---|---|---|---|
| — | `CreatePayment` (provider returns approval url / processing) | `Pending` | amount/currency valid; idempotency key new | `PaymentInitiated` |
| `Pending` | confirm/webhook reports processing | `Processing` | — | `PaymentProcessing` |
| `Pending`, `Processing` | settle reports authorized | `Authorized` | `CaptureMode = Manual` | `PaymentAuthorized` |
| `Pending`, `Processing` | settle reports captured, auto capture | `Captured` | observed amount = `Amount`; currency matches | `PaymentCaptured` |
| `Authorized` | capture full remaining | `Captured` | `0 < requested ≤ Amount − CapturedAmount` | `PaymentCaptured` |
| `Authorized` | capture partial | `Authorized` | `0 < requested < Amount − CapturedAmount` | `PaymentPartiallyCaptured` |
| `Pending`, `Processing` | settle reports failure / provider declines | `Failed` | failure code present | `PaymentFailed` |
| `Pending`, `Processing`, `Authorized` | cancel command or settle reports cancelled (void) | `Cancelled` | `CapturedAmount = 0` | `PaymentCancelled` |
| `Authorized` | provider rejects a capture | `Authorized` (unchanged) | — | `PaymentCaptureFailed` |
| `Pending` | expiry sweep or lazy expiry on read/cancel | `Expired` | now ≥ `ExpiresAt + grace` | `PaymentExpired` |
| `Expired` | settle reports captured (provider won the race) | `Captured` | observed amount = `Amount`; currency matches | `PaymentCaptured` (`reason = LateSettlement`) |
| `Failed` | settle reports captured after an earlier failure | `Captured` | observed amount = `Amount`; currency matches | `PaymentCaptured` (`reason = SettledAfterFailure`) |
| `Captured`, `PartiallyRefunded` | refund succeeded, cumulative < `CapturedAmount` | `PartiallyRefunded` | `0 < refund ≤ CapturedAmount − RefundedAmount` | `RefundSucceeded` |
| `Captured`, `PartiallyRefunded` | refund succeeded, cumulative = `CapturedAmount` | `Refunded` | same | `RefundSucceeded` |
| any terminal | duplicate/ignored provider event | unchanged | event id already applied, or transition not permitted | none (no-op) |

A provider capture rejection appends `PaymentCaptureFailed` and leaves `Status` unchanged: the
authorization is still live, so the command is retryable with the same idempotency key.

`Cancelled`, `Refunded`, and (normally) `Expired` are terminal. `Failed` is terminal for
customer actions but accepts late settlement, per the table.

### 6.4 Invariants

- **I-1** `Amount` and `Currency` never change after creation.
- **I-2** All amounts on a payment share its currency.
- **I-3** `0 < Amount.Amount ≤ Payments:Limits:MaxAmount`; decimal places ≤ currency exponent.
- **I-4** `CapturedAmount ≤ AuthorizedAmount ≤ Amount`, and `CapturedAmount = 0` in `Pending`/`Processing`.
- **I-5** `RefundedAmount ≤ CapturedAmount`; `RefundedAmount` only increases via `RefundSucceeded`.
- **I-6** `Status = Refunded` iff `CapturedAmount > 0` and `RefundedAmount = CapturedAmount`.
- **I-7** `IdempotencyKey` is unique per `CustomerId`.
- **I-8** `ProviderReference` is unique per `Provider`.
- **I-9** Status never moves backwards except across the two documented late-settlement edges
  (`Expired → Captured`, `Failed → Captured`).
- **I-10** Every accepted transition appends exactly one event and increments `Version` by one.
- **I-11** Every transition records `Actor` (`Customer`, `Admin`, `System`) and a correlation id.

### 6.5 Refund rules

- `Refund` fields: `Id`, `PaymentId`, `Amount`, `Status`, `ProviderReference?`, `Reason`,
  `FailureCode?`, `FailureMessage?`, `IdempotencyKey`, `CreatedAt`, `UpdatedAt`.
- `RefundStatus`: `Pending`, `Succeeded`, `Failed`.
- **R-1** A refund requires `Status ∈ {Captured, PartiallyRefunded}`.
- **R-2** `0 < Amount ≤ CapturedAmount − RefundedAmount − Σ(Pending refunds)` (409
  `Payments.Refund.ExceedsCredit` otherwise). Pending refunds reserve credit.
- **R-3** Refunding more than `ISupportsPartialRefund` allows is split into one refund if it equals the
  full remaining amount; otherwise `400 Payments.Refund.PartialUnsupported`.
- **R-4** A failed refund keeps its row (`Failed`), never mutates the payment's refund total; retry
  uses a new refund id and a new idempotency key.
- **R-5** Voiding/cancelling an uncaptured authorization is `CancelPayment`, never a refund.
- **R-6** `CompleteRefund` and `FailRefund` require the refund to be `Pending`; `Succeeded` and `Failed`
  are terminal for that refund id, and a retry uses a new id.
- **R-7** Re-requesting an existing refund id returns that refund unchanged, even if the payment is no
  longer refundable.

### 6.6 Money rules

- Domain and API always carry `{ "amount": 12.34, "currency": "USD" }`.
- Adapter conversion is per currency exponent: 2 for most, 0 for `JPY`, `KRW`, `VND`, `CLP`, `ISK`,
  `HUF`, `TWD`; 3 for `BHD`, `JOD`, `KWD`, `OMR`, `TND`.
- Rounding is banker-free: the domain rejects amounts with more decimals than the exponent rather
  than rounding them (`400 Payments.Payment.AmountInvalid`).

### 6.7 Idempotency and deduplication

- **ID-1** `POST /api/payments` and refund creation require `Idempotency-Key`.
- **ID-2** Same key + same request fingerprint → return the original resource, `Idempotent-Replay: true`.
- **ID-3** Same key + different fingerprint → `409 Payments.Payment.IdempotencyConflict`.
- **ID-4a** A reservation whose payment was never persisted is released, so a retry can create it.
- **ID-4** Outbound provider keys are deterministic: `create:{paymentId}`, `capture:{paymentId}:{n}`,
  `cancel:{paymentId}`, `refund:{refundId}`.
- **ID-5** Inbound provider events dedupe on `(provider, provider_event_id)` and are never applied twice.

### 6.8 Expiry

- **E-1** `ExpiresAt = CreatedAt + Payments:IntentLifetimeSeconds` for hosted-checkout providers.
- **E-2** Expiry is evaluated lazily on read, cancel, and settle; no background sweeper in this slice.
- **E-3** Grace period `Payments:ExpiryGraceSeconds` (default 300) absorbs provider clock skew; the
  Application applies it before calling `Payment.Expire`, and the aggregate enforces
  `nowUtc >= ExpiresAt`.
- **E-5** A provider-reported expiry is authoritative: `ApplyObservation(Expired)` may expire a
  `Pending` or `Processing` payment without consulting `ExpiresAt`.
- **E-4** A capture observed after expiry still settles the payment (late-settlement edge) and is
  tagged `LateSettlement` in the event metadata.

### 6.9 Domain operations

Provider observations are mapped onto the aggregate; handlers never switch on provider events.

| Operation | Signature | Rules |
|---|---|---|
| `RemainingCapturable` | `decimal` | `Amount − CapturedAmount`, never negative |
| `CanCapture` | `bool` | true only in `Pending`/`Processing`/`Authorized` (plus documented late edges) |
| `CanRefund` | `bool` | true only in `Captured`/`PartiallyRefunded` and `CreditAllowed > 0` |
| `CreditAllowed` | `decimal` | `CapturedAmount − RefundedAmount − Σ(Pending refunds)` |
| `ApplyObservation` | `Result<Payment>` | maps `PaymentObservation` (`Authorized`, `Captured`, `Failed`, `Cancelled`, `Expired`) onto the correct transition and reason |
| `Capture` | `Result<Payment>` | owns remaining-amount, currency, and state guards |
| `RequestRefund` | `Result<Payment>` | owns `creditAllowed`, duplicate-id, and state guards |

- **DR-1** `SettlePayment` calls `ApplyObservation`; no provider-event `switch` exists in Application.
- **DR-2** Every guard failure returns a `PaymentsOutcome` error, never a generic exception.
- **DR-3** Capture reasons (`Initial`, `LateSettlement`, `SettledAfterFailure`) are chosen inside the aggregate.

## 7. Events

| Event | Payload highlights |
|---|---|
| `PaymentInitiated` | paymentId, provider, amount, captureMode, externalReference, customerId, approvalUrl?, expiresAt |
| `PaymentProcessing` | paymentId, providerReference? |
| `PaymentAuthorized` | paymentId, providerReference, authorizedAmount |
| `PaymentCaptured` | paymentId, providerReference, capturedAmount, reason (`Initial`/`LateSettlement`/`SettledAfterFailure`) |
| `PaymentPartiallyCaptured` | paymentId, capturedAmount, totalCapturedAmount |
| `PaymentCaptureFailed` | paymentId, attemptedAmount, failureCode, failureMessage |
| `PaymentFailed` | paymentId, failureCode, failureMessage |
| `PaymentCancelled` | paymentId, reason |
| `PaymentExpired` | paymentId, expiredAt |
| `RefundRequested` | refundId, paymentId, amount, idempotencyKey |
| `RefundSucceeded` | refundId, paymentId, amount, providerReference |
| `RefundFailed` | refundId, paymentId, failureCode, failureMessage |

Integration events (outbox, published after commit): `PaymentCapturedIntegration`,
`PaymentRefundedIntegration`. They are consumed only for notification; they do not mutate state.

## 8. Ports and adapters

### 8.1 Port

```csharp
using Core.BuildingBlocks.Functional.Maybes;

public interface IPaymentGateway
{
    PaymentProvider Provider { get; }
    Task<Result<PaymentIntentResult>> CreateIntentAsync(PaymentIntentRequest request, CancellationToken ct);
    Task<Result<PaymentCaptureResult>> CaptureAsync(PaymentCaptureRequest request, CancellationToken ct);
    Task<Result<PaymentCancelResult>> CancelAsync(PaymentCancelRequest request, CancellationToken ct);
    Task<Result<PaymentRefundResult>> RefundAsync(PaymentRefundRequest request, CancellationToken ct);
    Task<Result<Maybe<PaymentProviderEvent>>> ParseWebhookAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken ct);
}
```

- **P-1** `ParseWebhookAsync` returns `Maybe<PaymentProviderEvent>.Empty()` for event types the module does not
  act on. `Result` never carries a null value.
- **P-2** Invalid signatures return a failure with code `Payments.Webhook.SignatureInvalid` (`400`);
  no event escapes.
- **P-3** Adapters never throw for provider-level failures; they return `Result` failures with the
  canonical codes in 8.7.
- **P-5** Every port method returns either `Result<T>` or `Result<Maybe<T>>`; no `null` value escapes a
  successful result.
- **P-4** Adapters are stateless apart from cached credentials; no request scope leakage.

### 8.2 Canonical models

- `PaymentIntentRequest`: `PaymentId`, `Amount`, `Provider`, `CustomerId?`, `ExternalReference`,
  `ReturnUrl`, `CancelUrl`, `CaptureMode`, `IdempotencyKey`, `Metadata`.
- `PaymentIntentResult`: `ProviderReference`, `Status` (`RequiresAction`, `Processing`, `Authorized`,
  `Captured`, `Failed`, `Cancelled`), `ApprovalUrl?`, `ClientSecret?`, `ExpiresAt?`, `FailureCode?`.
- `PaymentProviderEvent`: `EventId`, `Type` (`Authorized`, `Captured`, `Failed`, `Cancelled`, `Expired`,
  `Refunded`, `RefundFailed`), `ProviderReference` (payment intent/order), `RefundReference?`,
  `Amount?`, `Currency?`, `OccurredAt`, `Metadata`.

### 8.3 Capabilities

```csharp
public interface ISupportsPartialRefund { bool CanPartiallyRefund(); }
public interface ISupportsManualCapture { bool CanCaptureManually(); }
```

Capability checks are the only place provider differences are consulted. No `switch (provider)` exists
outside `IPaymentGatewayRegistry`.

### 8.4 Providers

| Provider | Transport | Webhook verification | Manual capture | Partial refund |
|---|---|---|---|---|
| `Fake` | in-memory | `X-Fake-Signature: sha256=<hex>` over raw body | yes | yes |
| `Stripe` | `Stripe.net` | `Stripe-Signature` via `EventUtility.ConstructEvent`, 300 s tolerance | yes | yes |
| `PayPal` | typed `HttpClient` (Orders v2, Payments v1) | `POST /v1/notifications/verify-webhook-signature` with `PAYPAL-TRANSMISSION-*` headers | yes | no (refunds deferred in this slice) |

Fake outcomes are scripted by `externalReference` prefix (`decline:`, `requires-action:`, `timeout:`)
so tests never depend on random behaviour.

### 8.5 Money conversion

- Stripe: minor units, `long`; zero-decimal currencies sent as-is.
- PayPal: decimal string with the currency's exponent.
- Both adapters reject a currency/exponent they do not support with
  `Payments.Payment.CurrencyUnsupported` (`400`) before calling the provider.

### 8.6 Registry

`IPaymentGatewayRegistry.Resolve(PaymentProvider)` returns the enabled adapter or
`Payments.Provider.Disabled` (`503`). Configuration decides enablement; a missing secret for an
enabled provider fails startup.

### 8.7 Failure mapping

| Provider outcome | Canonical code | HTTP |
|---|---|---|
| Card/account declined at create | domain `Failed`, code `Payments.Provider.Declined` | `201`/`200` on the resource |
| Card/account declined at capture/refund | `Payments.Provider.Declined` | `502` (authorization stays live) |
| Provider 4xx (bad request) | `Payments.Provider.RequestRejected` | `502` |
| Provider 5xx / timeout | `Payments.Provider.Unavailable` | `502` |
| Auth/credential failure | `Payments.Provider.ConfigurationInvalid` | `500` |
| Unknown provider | `Payments.Provider.Disabled` | `503` |

## 9. Application slices

| Slice | Type | Input | Output |
|---|---|---|---|
| `CreatePayment` | command | amount, currency, provider, externalReference, return/cancel url, captureMode | `PaymentResponse` |
| `ConfirmPayment` | command | paymentId | `PaymentResponse` |
| `CancelPayment` | command | paymentId | `PaymentResponse` |
| `CapturePayment` | command | paymentId, amount? | `PaymentResponse` |
| `SettlePayment` | command (internal) | paymentId or provider+reference, observed status, captured amount?, provider event id | `PaymentResponse` |
| `RefundPayment` | command | paymentId, amount, reason | `RefundResponse` |
| `GetPayment` | query | paymentId | `PaymentResponse` |
| `ListMyPayments` | query | externalReference?, page | `QueryPage<PaymentResponse>` |
| `GetRefund` | query | paymentId, refundId | `RefundResponse` |
| `SearchPayments` | query | status?, provider?, createdFrom/To?, page | `QueryPage<PaymentResponse>` |
| `ProcessPaymentWebhook` | command | provider, raw event | `Result<bool>` (`true` = settled) |

- **A-1** Endpoints contain no payment logic: authenticate, bind, send, map `Location`.
- **A-2** `SettlePayment` is the only writer of capture/authorize/failed states.
- **A-3** Validation lives in Infrastructure/service or the domain; Application does not duplicate it.

## 10. API contract

### 10.1 Routes

| Method | Path | Auth | Success |
|---|---|---|---|
| `POST` | `/api/payments` | authenticated customer | `201` + `Location: /api/payments/{id}` |
| `GET` | `/api/payments/{id:guid}` | owner | `200` |
| `GET` | `/api/payments?externalReference=` | owner | `200` |
| `POST` | `/api/payments/{id:guid}/confirm` | owner | `200` |
| `POST` | `/api/payments/{id:guid}/cancel` | owner | `200` |
| `GET` | `/api/admin/payments` | `payments.read` | `200` |
| `GET` | `/api/admin/payments/{id:guid}` | `payments.read` | `200` |
| `POST` | `/api/admin/payments/{id:guid}/capture` | `payments.manage` | `200` |
| `POST` | `/api/admin/payments/{id:guid}/refunds` | `payments.refund` | `201` + `Location` |
| `GET` | `/api/admin/payments/{id:guid}/refunds/{refundId:guid}` | `payments.read` | `200` |
| `POST` | `/api/payments/webhooks/{provider}` | anonymous, signature-verified | `200` |

- **A-4** `{id:guid}` / `{refundId:guid}` constraints are mandatory; `admin` and `webhooks` must never
  bind as ids.
- **A-5** Customer routes return `404` for another customer's payment (existence is not disclosed),
  never `403`.

### 10.2 Create request

```json
POST /api/payments
Idempotency-Key: 4f1c2a9e-...
{
  "amount": { "amount": 49.90, "currency": "USD" },
  "provider": "stripe",
  "externalReference": "cart-12345",
  "captureMode": "automatic",
  "returnUrl": "https://shop.example/checkout/return",
  "cancelUrl": "https://shop.example/checkout/cancel"
}
```

`201` body (`Result<PaymentResponse>`):

```json
{
  "isSuccess": true,
  "value": {
    "id": "0f6f...",
    "provider": "stripe",
    "status": "pending",
    "amount": { "amount": 49.90, "currency": "USD" },
    "capturedAmount": { "amount": 0, "currency": "USD" },
    "refundedAmount": { "amount": 0, "currency": "USD" },
    "externalReference": "cart-12345",
    "approvalUrl": "https://checkout.stripe.com/...",
    "expiresAt": "2026-10-31T12:30:00Z",
    "createdAt": "2026-10-31T12:00:00Z",
    "failureCode": null,
    "failureMessage": null
  },
  "errors": [],
  "metadata": {},
  "statusCode": 201
}
```

- **A-6** `approvalUrl` is present only while the customer must act; it is omitted once settled.
- **A-7** `providerReference`, `customerId`, and raw provider payloads never appear in responses.
- **A-8** Admin reads may include `providerReference`; customer reads never do.

### 10.3 Idempotency semantics

| Case | Result |
|---|---|
| First request | `201` (create) / `201` (refund) |
| Replay, same key + same body | original resource, same status, `Idempotent-Replay: true` |
| Same key + different body | `409 Payments.Payment.IdempotencyConflict` |
| Missing key | `400 Payments.Payment.IdempotencyKeyRequired` |

### 10.4 Authorization

- **S-1** Customer routes require a valid bearer token and match `CustomerId` from `sub`.
- **S-2** Admin routes require the listed permission from the existing policy pipeline.
- **S-3** Webhook route is anonymous at gateway and API; authenticity comes only from signature
  verification.
- **S-4** Permission identifiers: `payment.payments.payment.read`, `payment.payments.payment.manage`,
  `payment.payments.payment.refund` (via `PermissionMetadata.Create("payment", "payments", "payment", …)`).
- **S-5** Only HTTPS `approvalUrl` values are returned; adapters reject `http:` provider URLs.

### 10.5 Error codes

Codes are grouped by category: `Payments.Payment.*` (validation/state/idempotency), `Payments.Refund.*`,
`Payments.Provider.*`, `Payments.Webhook.*`, and `Payments.Customer.*` (auth). Every failure is produced by
`PaymentsOutcome.Failure`; no handler or adapter constructs an `Error` inline.

| Code | HTTP | When |
|---|---|---|
| `Payments.Payment.NotFound` | 404 | unknown id, or not owned by caller |
| `Payments.Payment.InvalidState` | 409 | illegal transition |
| `Payments.Payment.AmountInvalid` | 400 | zero/negative/too many decimals/over max |
| `Payments.Payment.ReferenceTooLong` | 400 | external reference exceeds 128 chars |
| `Payments.Payment.IdempotencyKeyTooLong` | 400 | key exceeds 128 chars |
| `Payments.Payment.CurrencyMismatch` | 400 | amount currency differs from the payment currency |
| `Payments.Payment.Expired` | 409 | customer action on an expired intent |
| `Payments.Payment.CurrencyUnsupported` | 400 | exponent unknown or provider unsupported |
| `Payments.Payment.IdempotencyKeyRequired` | 400 | header missing |
| `Payments.Customer.Required` | 401 | access token has no usable subject |
| `Payments.Payment.IdempotencyConflict` | 409 | key reused with different body |
| `Payments.Refund.ExceedsCredit` | 409 | amount > capturable remainder |
| `Payments.Refund.NotCaptured` | 409 | payment not in a refundable state |
| `Payments.Refund.NotFound` | 404 | unknown refund for that payment |
| `Payments.Refund.PartialUnsupported` | 400 | partial refund on a provider without support |
| `Payments.Refund.ReasonRequired` | 400 | empty or over-long refund reason |
| `Payments.Refund.AlreadySettled` | 409 | refund already succeeded and cannot change |
| `Payments.Provider.Declined` | 502 | capture/refund declined; a create decline is a `Failed` resource instead |
| `Payments.Provider.RequestRejected` | 502 | provider rejected the request |
| `Payments.Provider.Unavailable` | 502 | provider unreachable/timeout |
| `Payments.Provider.ConfigurationInvalid` | 500 | credentials rejected at runtime |
| `Payments.Provider.ReferenceMissing` | 409 | provider call attempted before a reference existed |
| `Payments.Provider.Disabled` | 503 | provider not enabled/configured |
| `Payments.Webhook.SignatureInvalid` | 400 | bad/missing signature |
| `Payments.Webhook.ContentTypeUnsupported` | 415 | body is not `application/json` |
| `Payments.Webhook.PayloadInvalid` | 400 | body cannot be parsed |
| `Payments.Webhook.PayloadTooLarge` | 413 | body > `WebhookMaxBytes` |
| `Payments.Webhook.ProviderUnsupported` | 404 | unknown `{provider}` route value |
| `Payments.Webhook.AmountMismatch` | 200 (recorded) | amount/currency disagrees with the payment; outcome `AmountMismatch`, no settlement |

### 10.6 Webhook contract

- **W-1** Route: `POST /api/payments/webhooks/{provider}`; body read raw, no model binding.
- **W-2** Content type must be `application/json`; otherwise `415`.
- **W-3** Body > `WebhookMaxBytes` (64 KiB) → `413`, nothing persisted.
- **W-4** Signature verified before any persistence; failure → `400`, nothing persisted.
- **W-5** Ignored event type (`Maybe.Empty`) → `200`, nothing persisted, `webhooks.ignored` incremented.
- **W-6** Duplicate `(provider, provider_event_id)` → `200`, nothing reprocessed.
- **W-7** Accepted event is persisted before settlement; settlement failure returns `500` so the
  provider retries.
- **W-8** Amount/currency mismatch records the event with outcome `AmountMismatch`, returns `200`,
  and does not settle. This is an alerting signal, not a retry signal.
- **W-9** Response body is always `Result.Success(200)`; no payment data is echoed.
- **W-10** `{provider}` is case-insensitive and must match an enabled provider; unknown or disabled
  values return `404 Payments.Webhook.ProviderUnsupported`.

### 10.7 Gateway routes

The API mounts Carter modules under `/api/*` (for example `/api/auth`), while the existing
`/api/{**remainder}` catch-all applies `StripPrefix("/{remainder}")`, which rewrites
`/api/auth/login` to `/auth/login`. Payments must not depend on that transform, so it declares its
own routes **before** the catch-all and **preserves** the path:

```csharp
// Validate: providers cannot present a bearer token, and the catch-all would
// strip /api from a path the Api serves verbatim.
new RouteConfig
{
    RouteId = "api-payments-webhook",
    ClusterId = ApiCluster,
    Match = new RouteMatch { Path = "/api/payments/webhooks/{**remainder}" },
    AuthorizationPolicy = AnonymousPolicy,
},
new RouteConfig
{
    RouteId = "api-payments",
    ClusterId = ApiCluster,
    Match = new RouteMatch { Path = "/api/payments/{**remainder}" },
    AuthorizationPolicy = ProtectedApiPolicy,
},
```

- **G-1** A gateway integration test asserts `/api/payments/webhooks/fake` reaches the Api without a
  token and returns `400` (bad signature), proving routing before signature logic.
- **G-2** The same test asserts `/api/payments` without a token is `401`, and with a token reaches
  the customer route.
- **G-3** Route order: both payment routes precede the `/api` catch-all; the storefront catch-all
  remains last.

## 11. Persistence

### 11.1 Tables

`payments` (projection)

| Column | Type | Notes |
|---|---|---|
| `id` | uuid PK | |
| `provider` | text | indexed |
| `provider_reference` | text? | unique `(provider, provider_reference)` |
| `status` | text | indexed |
| `amount` / `currency` | numeric(19,4) / char(3) | |
| `authorized_amount` / `captured_amount` / `refunded_amount` | numeric(19,4) | |
| `capture_mode` | text | |
| `external_reference` | text | |
| `customer_id` | uuid | unique `(customer_id, idempotency_key)`; unique `(customer_id, provider, external_reference)` |
| `idempotency_key` | text | |
| `failure_code` / `failure_message` | text? | |
| `expires_at` / `created_at` / `updated_at` | timestamptz | `created_at` indexed |
| `version` | int | event stream version |

`payment_refunds`

| Column | Type | Notes |
|---|---|---|
| `id` | uuid PK | |
| `payment_id` | uuid FK → payments | indexed |
| `status` | text | |
| `amount` / `currency` | numeric(19,4) / char(3) | |
| `provider_reference` | text? | |
| `reason` / `failure_code` / `failure_message` | text? | |
| `idempotency_key` | text | unique `(payment_id, idempotency_key)` |
| `created_at` / `updated_at` | timestamptz | |

`payment_idempotency_keys` (synchronous reservation, independent of projections)

| Column | Type | Notes |
|---|---|---|
| `id` | uuid PK | |
| `customer_id` | uuid | unique `(customer_id, idempotency_key)` |
| `idempotency_key` | text | |
| `fingerprint` | text | request fingerprint for replay comparison |
| `payment_id` | uuid | the reserved payment |
| `created_at` | timestamptz | |

`payment_references` (synchronous provider reference index)

| Column | Type | Notes |
|---|---|---|
| `id` | uuid PK | |
| `provider` | text | unique `(provider, provider_reference)` |
| `provider_reference` | text | |
| `payment_id` | uuid | |
| `created_at` | timestamptz | |

`payment_webhook_events`

| Column | Type | Notes |
|---|---|---|
| `id` | uuid PK | |
| `provider` | text | unique `(provider, provider_event_id)` |
| `provider_event_id` | text | |
| `event_type` | text | |
| `raw_body` | text | never returned by any endpoint |
| `outcome` | text | `Settled`, `Ignored`, `Duplicate`, `AmountMismatch`, `Failed` |
| `received_at` / `processed_at` | timestamptz | `received_at` indexed |

### 11.2 Projection behavior

- **DB-1** Projections are idempotent per `(stream_id, version)`; replay produces the same rows.
- **DB-2** Projection checkpoints use the existing `ProjectionCheckpoint` store.
- **DB-3** Refund totals are recomputed from succeeded refund rows, never incremented blindly.
- **DB-4** `payments.customer_id` is required; only authenticated customers create payments in this slice.

### 11.3 Migration

- **DB-5** One `AddPayments` migration in `src/Infrastructure/Migrations`, created with
  `dotnet ef migrations add AddPayments -p src/Infrastructure -s app/Web.Api`.
- **DB-6** The migration is additive only; no existing table is altered.

## 12. Configuration

```json
"Payments": {
  "DefaultProvider": "Fake",
  "IntentLifetimeSeconds": 1800,
  "ExpiryGraceSeconds": 300,
  "Limits": { "MaxAmount": 100000, "WebhookMaxBytes": 65536 },
  "Providers": {
    "Fake":   { "Enabled": true },
    "Stripe": { "Enabled": false, "SecretKey": "", "PublishableKey": "", "WebhookSecret": "" },
    "PayPal": { "Enabled": false, "ClientId": "", "ClientSecret": "", "WebhookId": "", "Environment": "Sandbox" }
  }
}
```

Validation conditions (fail fast at startup):

- **C-1** `MaxAmount > 0`.
- **C-2** `1 ≤ IntentLifetimeSeconds ≤ 86400`.
- **C-3** `0 ≤ ExpiryGraceSeconds ≤ 3600`.
- **C-4** `WebhookMaxBytes ∈ [1024, 1048576]`.
- **C-5** Any `Enabled` real provider must have all non-empty secrets.
- **C-6** `Fake.Enabled` must be `false` when `ASPNETCORE_ENVIRONMENT=Production`.
- **C-7** `DefaultProvider` must be enabled.
- **C-8** `PayPal.Environment ∈ {Sandbox, Live}`.
- **C-9** No secret value ever appears in `appsettings*.json`, Compose files, or logged output.
- **C-10** AppHost forwards `Payments__Providers__*` when present; no new backing service is added and
  `infra.backing.*` stays unchanged.

## 13. Observability

- Counters: `payments.initiated`, `payments.failed`, `payments.settled`, `refunds.succeeded`,
  `webhooks.received`, `webhooks.duplicate`, `webhooks.ignored`, `webhooks.mismatch`.
- Histogram: `payments.provider.duration` tagged by `provider` and `operation`.
- Trace: one span per command and one per provider call; correlation id propagates through the
  existing HTTP handler and messaging pipeline.
- **O-1** Tags never include customer id, email, amount, or webhook body.
- **O-2** Failure codes are logged as structured properties, not interpolated strings.

## 14. Testing and acceptance conditions

### 14.1 Unit — Core

- **T-1** Every allowed transition in 6.3 succeeds and appends exactly one event.
- **T-2** Every disallowed transition returns `Payments.Payment.InvalidState` and appends nothing.
- **T-3** Invariants I-1…I-11 each have at least one failing-path test.
- **T-3a** Every guard branch in `Payment` and every `PaymentObservation` arm has a test; Core branch
  coverage target is 95% (gate remains 80%).
- **T-4** Partial capture accumulation and refund math hit the `CapturedAmount`/`RefundedAmount` edges.
- **T-5** Late settlement from `Expired` and `Failed` records the documented reason.
- **T-28** A rejected capture appends `PaymentCaptureFailed`, leaves the status `Authorized`, and a
  retry with the same idempotency key can still capture.

### 14.2 Unit — Application

- **T-6** Decline → `Failed` resource, not an error response.
- **T-7** Provider outage → `502 Payments.Provider.Unavailable`, no state change.
- **T-8** Idempotent replay returns the stored resource; conflicting body → `409`.
- **T-9** Refund over `creditAllowed` → `409`; partial refund on PayPal → `400`.
- **T-10** `SettlePayment` from confirm and from webhook produces identical state.

### 14.3 Unit — Adapters

- **T-11** Money conversion table covered for 0-, 2-, and 3-decimal currencies.
- **T-12** Stripe signature: valid, expired timestamp, tampered body, missing header.
- **T-13** PayPal verify request shape and rejection path (stubbed).
- **T-14** Webhook normalization: supported types map; unsupported return `null`.
- **T-15** Fake adapter scripts decline/requires-action/timeout deterministically.

### 14.4 Integration — API

- **T-16** Full flow through Postgres + fake: create → webhook captured → refund → refunded.
- **T-17** Confirm path and webhook path converge on the same state.
- **T-18** Customer cannot read another customer's payment (`404`, not `403`).
- **T-19** Admin routes reject missing permission (`403`) and accept granted permission.
- **T-20** Bad signature → `400`, no `payment_idempotency_keys` (synchronous reservation, independent of projections)

| Column | Type | Notes |
|---|---|---|
| `id` | uuid PK | |
| `customer_id` | uuid | unique `(customer_id, idempotency_key)` |
| `idempotency_key` | text | |
| `fingerprint` | text | request fingerprint for replay comparison |
| `payment_id` | uuid | the reserved payment |
| `created_at` | timestamptz | |

`payment_references` (synchronous provider reference index)

| Column | Type | Notes |
|---|---|---|
| `id` | uuid PK | |
| `provider` | text | unique `(provider, provider_reference)` |
| `provider_reference` | text | |
| `payment_id` | uuid | |
| `created_at` | timestamptz | |

`payment_webhook_events` row.
- **T-21** Duplicate webhook → `200`, one row, one state change.
- **T-22** Oversized body → `413`; unknown provider → `404`.
- **T-23** `Idempotency-Key` missing → `400`; replay → same id and `Idempotent-Replay`.
- **T-24** Gateway route test: webhook anonymous, `/api/payments` protected.

### 14.5 Live sandbox (optional, not in CI)

- **T-25** `make test.payments.sandbox` runs only when `STRIPE__SECRETKEY` / `PAYPAL__CLIENTSECRET`
  are set, otherwise skips with a visible reason; never part of `make ci`.

### 14.6 Gates

- **T-26** `make lint`, `make build`, `make test`, `make coverage` meet 90% line / 80% branch for
  `Core`, `Application`, `Infrastructure`, `Web.Api`.
- **T-27** `make ci` green; `make ci.full` green.

### 14.7 Edge-case matrix

Each row is a required automated test. "Where" names the owning suite.

| ID | Case | Expected | Where |
|---|---|---|---|
| E-1 | Zero or negative amount | `400 Payments.Payment.AmountInvalid` | Core unit |
| E-2 | More decimals than the currency exponent | `400 Payments.Payment.AmountInvalid` | Adapter unit |
| E-3 | Provider rejects a currency it does not support | `400 Payments.Payment.CurrencyUnsupported` (mapped from the provider rejection) | Adapter unit |
| E-4 | Currency mismatch between request and payment | `400 Payments.Payment.CurrencyMismatch`, no state change | Core unit |
| E-5 | Capture above the remaining amount | `400 Payments.Payment.AmountInvalid`, captured total unchanged | Core unit |
| E-6 | Capture on `Cancelled` / `Refunded` | `409 Payments.Payment.InvalidState` | Core unit |
| E-7 | Cancel after capture | `409 Payments.Payment.InvalidState` | Core unit |
| E-8 | Expire a non-`Pending` payment | `409 Payments.Payment.InvalidState` | Core unit |
| E-9 | Capture after expiry | `Captured`, event reason `LateSettlement` | Core unit |
| E-10 | Capture after failure | `Captured`, event reason `SettledAfterFailure` | Core unit |
| E-11 | Same refund id requested twice | No-op, one refund row | Core unit |
| E-12 | Refund on `Pending` | `409 Payments.Refund.NotCaptured` | Core unit |
| E-13 | Refund above `creditAllowed` | `409 Payments.Refund.ExceedsCredit` | Core unit |
| E-14 | Failed refund | Refund `Failed`, payment totals unchanged | Core unit |
| E-15 | Partial refund on a provider without the capability | `400 Payments.Refund.PartialUnsupported` | Application unit |
| E-16 | Missing `Idempotency-Key` | `400 Payments.Payment.IdempotencyKeyRequired` | API integration |
| E-17 | Replay with the same body | `200`, `Idempotent-Replay: true`, no provider call | Application unit + API |
| E-18 | Replay with a different body | `409 Payments.Payment.IdempotencyConflict` | API integration |
| E-19 | Concurrent create with the same key | One payment; unique index resolves the race | Infrastructure unit |
| E-20 | Provider timeout | `502 Payments.Provider.Unavailable`, no state change | Adapter unit + Application |
| E-21 | Provider disabled | `503 Payments.Provider.Disabled` | Adapter unit |
| E-22 | Malformed webhook JSON with a valid signature | `400 Payments.Webhook.PayloadInvalid` | API integration |
| E-23 | Unknown webhook event type | `200`, nothing persisted | API integration |
| E-24 | Duplicate webhook delivery | `200`, one row, one state change | API integration |
| E-25 | Webhook amount/currency mismatch | `200`, outcome `AmountMismatch`, no settlement | API integration |
| E-26 | Oversized webhook body | `413 Payments.Webhook.PayloadTooLarge` | API integration |
| E-27 | Wrong webhook content type | `415 Payments.Webhook.ContentTypeUnsupported` | API integration |
| E-28 | Unknown webhook provider | `404 Payments.Webhook.ProviderUnsupported` | API integration |
| E-29 | Unauthenticated create | `401` | API integration |
| E-30 | Another customer's payment | `404` (never `403`) | API integration |
| E-31 | Admin route without permission | `403` | API integration |
| E-32 | Confirm an already captured payment | `200`, no duplicate capture | Application unit |
| E-33 | Provider decline at create | `201` resource with `status = failed` | Application unit |
| E-34 | Partial capture then full capture | `Authorized` then `Captured`, totals correct | Core unit |
| E-35 | Manual capture before authorization | `409 Payments.Payment.InvalidState` | Core unit |
| E-36 | Capture without a provider reference | `409 Payments.Provider.ReferenceMissing` | Core unit |
| E-37 | Missing external reference | `400 Payments.Payment.ReferenceRequired` | Core unit |
| E-38 | Authorized observation on automatic capture | `409 Payments.Payment.InvalidState` | Core unit |
| E-39 | Each observation arm | Maps to the documented status | Core unit |
| E-40 | Remaining capturable after full capture | `0`, never negative | Core unit |
| E-41 | Can refund after a full refund | `false` | Core unit |
| E-42 | Complete an unknown refund id | `404 Payments.Refund.NotFound` | Core unit |
| E-43 | Empty refund reason | `400 Payments.Refund.ReasonRequired` | Application unit |
| E-44 | Unknown observation value | `409 Payments.Payment.InvalidState`, no event | Core unit |

## 15. Delivery sequence

| Step | Content | Gate |
|---|---|---|
| 1 | Core aggregate, events, invariants, unit tests | `make test.dotnet.unit` |
| 2 | Ports, fake adapter, Application slices, API endpoints | `make api.test` |
| 3 | Projection, read stores, migration | `make api.test` + migration applies |
| 4 | Stripe adapter + stubbed contract tests | `make test.dotnet.unit` |
| 5 | PayPal adapter + stubbed contract tests | `make test.dotnet.unit` |
| 6 | Gateway route, docs, release note, full gates | `make ci.full` |

Each step is independently committable and green.

## 16. Deferred

- Background reconciliation against provider state.
- Dispute/chargeback transitions.
- Webhook retention/cleanup job.
- FX and multi-currency settlement.
- Saved payment methods, setup sessions, subscriptions.
- Frontend payment screens.
- PayPal partial refund support (capability returns `false`).

## 17. Risks

| Risk | Mitigation |
|---|---|
| Confirm/webhook race double-settles | Single `SettlePayment` command + event-version concurrency + unique provider reference |
| Out-of-order webhooks regress state | Transition guards reject backwards moves (I-9) |
| Zero-decimal currency bug | Per-adapter exponent table + T-11 |
| Webhook blocked at the gateway | Explicit anonymous route + T-24 |
| Secrets leak into logs/config | C-9, O-1, no raw payload logging |
| Provider SDK churn | Stripe pinned via `Directory.Packages.props`; PayPal isolated behind the typed client |
| Pre-existing: `/api` catch-all strips `/api`, which conflicts with Carter modules served under `/api/*` | Payments uses explicit preserving routes (10.7); the catch-all defect is recorded, not fixed, in this slice |

## 18. References

- Solidus: `core/app/models/spree/payment.rb`, `core/app/models/spree/payment/processing.rb`,
  `core/app/models/spree/payment_method.rb`, `core/app/models/spree/refund.rb`.
- Spree: `spree/core/app/models/spree/payment.rb`, `spree/core/app/models/spree/payment_method.rb`,
  `spree/core/app/models/spree/payment_session.rb`, `spree/core/app/models/spree/refund.rb`.
- Repository: `app/Gateway/Gateway.Router.cs`, `src/Application/Modules/Todos/Permissions/TodoPermissionCatalog.cs`,
  `src/Infrastructure/Integrations/Serialization/ApiResult.cs`,
  `src/Core/BuildingBlocks/Domain/ValueObjects/Money/Money.cs`.
