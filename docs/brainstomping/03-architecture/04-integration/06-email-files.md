# Email & Files

The `IEmailSender` and `IFileStorage` cross-cutting contracts, the four transactional email templates with their variables, and the product-image upload, processing, and URL strategy.

**Links:** [App topology](../01-overview/02-app-topology.md) · [C4 model](../01-overview/01-c4.md) · [Data flow](../01-overview/03-data-flow.md) · [Module contracts](../02-components/02-module-contracts.md) · [REST admin API](./02-rest-admin.md) · [Entity model](../03-data/01-entities.md) · [Catalog requirements](../../01-requirements/02-functional/01-catalog.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Email and file storage are cross-cutting concerns: they live in `Resys.Core` as contracts and are
implemented by `Resys.Infrastructure` (`03/02-02`). This file fixes the two contracts, the four
transactional templates and the variables each may reference, the two storage backends, the
end-to-end product-image flow, and the rule that the database stores object keys while URLs are
generated at read time. Email is triggered by domain events through the outbox, never sent inline
on the request thread (`03/01-03`); images enter through the admin catalog API (`03/04-02`). The
provider ports and dev transports are `03/01-02`; the environment configuration is `05/01-02`.

---

## IEmailSender

> **Superseded (2026-09-24 audit):** `IEmailSender` is **not** a kit contract. Modules call
> `INotificationService` (`BuildingBlocks.Application/Notifications`) with a template key; email is
> the Notifications **Emails channel** (`Infrastructure/Notifications/Channels/Emails`) per
> ReSys.Shop `Shared.Integration`. The shape below is retained as the **payload model**
> (`EmailMessage` / `EmailSendResult`) the channel renders — implement it as records under
> `Application/Notifications` if still useful, not as a parallel sender port.

```csharp
namespace BuildingBlocks.Application.Notifications;

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default);
}

public sealed record EmailMessage(
    StoreId StoreId,
    string TemplateKey,
    string To,
    string Locale,
    IReadOnlyDictionary<string, object?> Model,
    string? ReplyTo = null,
    string? CorrelationId = null);

public sealed record EmailSendResult(bool Sent, string? ProviderMessageId, bool Retryable);
```

`TemplateKey` is one of the four keys below; `Model` is the variable bag the template renders;
`Locale` selects the translated template with a fallback to the store default (`03/03-03`). The two
implementations are selected by configuration (`Email:Provider`):

| Backend | Transport | Use |
|---|---|---|
| `ses` | Amazon SES SDK | Production default; per-store verified from address. |
| `smtp` | SMTP relay (MailKit) | Self-hosted deployments, and the dev mail catcher on port `1025` (`03/01-02`). |

Rules:

- **Event-driven, never inline.** A consumer subscribes to the relevant domain event after the
  outbox relay publishes it and calls `SendAsync`; a failed send is retried with backoff and never
  rolls back the state change that produced the event (`FR-RET-009`, `03/02-03`).
- **Idempotent delivery.** Each send carries the source event id as `CorrelationId`. The consumer
  records `(CorrelationId, TemplateKey)` and suppresses a duplicate, so at-least-once outbox
  delivery does not send twice.
- **Store-scoped.** The from address, branding, and reply-to come from the resolved store; a payment
  event for one store never sends through another store's identity.
- **No sensitive data.** Templates never include PAN, CVV, tokens, or full gateway payloads; logs
  and traces redact recipients and payment references (`FR-PAY-010`).
- **Rendering.** Templates are server-rendered from a strongly-typed model with HTML and plain-text
  variants; the renderer is the only consumer of `Model`. Money values render through the store
  currency formatter (`03/03-03`).

---

## Transactional Templates

| Template key | Trigger event | Recipient | Subject (localized) |
|---|---|---|---|
| `order-confirmation` | `OrderPlaced` | Order email (guest or customer) | Order {order_number} confirmed |
| `payment-receipt` | `PaymentCaptured` | Order email | Payment received for {order_number} |
| `shipment` | `OrderShipped` | Order email | Your order {order_number} has shipped |
| `return-status` | `ReturnRequested`, `ReturnApproved`, `ReturnReceived`, `ReturnCompleted` | Order email | Return {return_id} is {status} |

All four may reference the common envelope variables: `store_name`, `store_url`, `locale`,
`currency`, `customer_name` (optional), `order_number`, `order_status_url`, and `unsubscribe_url`
(transactional mail still carries a preference link). Each template additionally accepts the
following variables.

### order-confirmation

The order's placement summary. Triggered once per `OrderPlaced`.

- `order_id`, `placed_at`, `email`
- `line_items[]` — each with `name`, `sku`, `quantity`, `unit_price`, `total`
- `adjustments[]` — each with `source` (`promotion`/`shipping`/`manual`), `label`, `amount`
- `item_total`, `adjustment_total`, `tax_total`, `total` (each a `Money`)
- `shipping_address`, `billing_address` (formatted lines)
- `shipping_method`, `payment_method` (display name)
- `is_guest` (boolean)

`tax_total` is a separate component and never appears as an adjustment; a refund is never rendered
here because it is a later payment/ledger operation, not an order adjustment (`03/02-03`).

### payment-receipt

A receipt for a captured payment, including an offline payment marked paid by an admin.

- `order_id`, `order_number`
- `payment_id`, `method` (display name), `gateway_reference`
- `captured_at`, `amount` (`Money`)
- `is_offline` (boolean), `store_credit_balance_after` (`Money`, present only for store credit)
- `refunds_url` (account link)
- `refund_amount` (`Money`, present only when the receipt is sent in refund mode), `refund_kind`
  (`refund`/`store_credit`, present only in refund mode)

A refund triggers its own notification, not an amended receipt (`FR-PAY-006`). A return-driven
refund is delivered by the `return-status` settlement notification. A standalone or manual refund
with no return — for example an admin good-will refund on `/api/admin/orders/{id}/refund` — is sent
through `payment-receipt` in refund mode, carrying the original `payment_id`, `gateway_reference`,
and the `refund_amount`/`refund_kind`; refunds are therefore never silent just because no return
exists.

### shipment

Confirms a dispatched shipment and its tracking. Triggered once per `OrderShipped`.

- `order_id`, `shipment_id`, `shipped_at`
- `carrier`, `shipping_method`
- `tracking_number` (optional), `tracking_url` (optional)
- `shipment_lines[]` — each with `name`, `sku`, `quantity`
- `shipping_address`
- `is_partial` (boolean, true when other lines remain unshipped)

### return-status

Notifies the customer as a return advances or settles. Triggered by each return state event.

- `return_id`, `order_number`, `return_status` (`requested`/`authorized`/`received`/`inspected`/`completed`/`canceled`/`rejected`)
- `requested_at`, `approved_at` (optional), `received_at` (optional)
- `return_items[]` — each with `name`, `sku`, `quantity`, `reason`
- `settlement_kind` (`refund`/`store_credit`, present only on completion)
- `settlement_amount`, `currency` (present only on completion)
- `return_status_url`

On completion the template names the refund or the store-credit grant; the settlement amount comes
from the return, never from an edited order total (`FR-RET-005`, `FR-RET-008`).

---

## IFileStorage

```csharp
namespace BuildingBlocks.Application.Storage;

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(Stream content, FileWrite write, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string key, CancellationToken ct = default);
    Task<Uri> GetUrlAsync(string key, FileUrlAccess access, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}

public sealed record FileWrite(string Key, string ContentType, long Length);
public sealed record StoredFile(string Key, Uri Url, string ContentType, long Length);
public enum FileUrlAccess { Public, Private }
```

Two backends are selected by configuration (`Storage:Provider`):

| Backend | Implementation | Use |
|---|---|---|
| `local` | Files under a configured root, served by the API in development. | Local dev default (Spec §13 Open Question 2). |
| `s3` | S3-compatible bucket (AWS SDK / MinIO) with endpoint, region, bucket, and credentials from options. | Production. |

Rules:

- **Keys, not blobs, in the database.** `ProductImage.Url` (`03/03-01`) stores the object key; the
  API generates the URL on read through `GetUrlAsync`.
- **Generated keys only.** A client filename is never trusted; a key is built as
  `stores/{storeId}/products/{productId}/{imageId}/{size}{ext}`, where a derivative `{size}` uses
  the `.webp` extension and the `original` object keeps its validated source extension (`.jpg`,
  `.png`, `.webp`, or `.avif`), so a store prefix isolates tenants and the object name is
  deterministic.
- **Validated uploads.** Content type is confirmed by magic bytes, the size limit is enforced before
  any write, and a rejected upload is the `413` documented in `03/04-02`. Accepted types are JPEG,
  PNG, WebP, and AVIF.
- **Private originals.** Originals are stored with `Private` access; public derivative sizes use
  `Public`. `GetUrlAsync` returns a signed URL for private keys and the public base URL otherwise.
- **Best-effort deletion.** A failed object delete is logged and retried; a periodic reconciliation
  removes orphaned keys and re-creates missing derivatives. An object delete never blocks a database
  change, and a database change never leaves a half-written object.

---

## Product-Image Upload and Processing

Images are managed only through the admin catalog API (`03/04-02`); the storefront reads them.

1. **Upload.** `POST /api/admin/products/{id}/images` receives `multipart/form-data` with `file`,
   `position?`, and `variant_id?`. The endpoint requires `admin` plus store scope and is audited.
2. **Validate.** Size and magic-byte type are checked; an oversize or unsupported upload is `413` or
   `422` and nothing is stored.
3. **Store original.** The original bytes are saved under the `original` key with their validated
   source extension and `Private` access.
4. **Process derivatives.** The processor emits WebP derivatives at fixed sizes — `thumb` 200px,
   `card` 600px, `full` 1600px — preserving aspect ratio, stripping metadata, and applying the
   store's configured background for transparent images. Processing runs in-request for uploads
   within the size limit; a processing failure leaves no `ProductImage` row.
5. **Persist.** In one transaction the `ProductImage` row is written with the key, `Position`,
   `VariantId?`, and `IsPrimary` (one primary per product, `FR-CAT-005`), and the audit row is
   appended.
6. **Respond.** `ProductImageDto` returns `id`, the generated `url` for the `card` size, `position`,
   `is_primary`, and any variant override.
7. **Delete.** `DELETE /api/admin/products/{id}/images/{image_id}` removes the row and then
   best-effort deletes every derivative and the original.

A `VariantId` on a `ProductImage` overrides the product image for that variant's listing; the
primary product image is the fallback. Changing an image changes the object key, so the old key is
deleted rather than overwritten.

---

## URL Strategy

The API never persists an absolute URL for an uploaded asset.

- `IFileStorage.GetUrlAsync` composes the URL from `Storage:PublicBaseUrl` and the key. In production
  this base is the CDN or bucket host; in development it points at `Resys.Api`, which streams local
  files. Only `Resys.Api` holds storage credentials (`03/01-02`).
- **Public derivatives** (`thumb`, `card`, `full`) are world-readable and cached aggressively: keys
  are immutable, so the URL is deterministic and safe to cache for a long TTL. Rotating an image
  produces a new key.
- **Private assets** (originals, and any future invoice or export) are served through short-lived
  signed URLs generated per request; a private key is never exposed in a public page.
- `ProductImageDto.url` and `ProductDetailDto.images[].url` (`03/04-01`, `03/04-02`) always carry an
  absolute HTTPS URL, so the storefront never reconstructs storage paths from keys.
- Which host fronts the public base — CDN versus API proxy — is a deployment choice owned by
  `08/01-01`; the `IFileStorage` contract and the key/URL split here are unchanged either way.

## Open Questions

1. The exact production object-storage provider and CDN host are deployment configuration owned by
   `08/01-01`; this file fixes the abstraction, the key scheme, and the public/private access split.
2. Whether the SES from-address set and SMTP relay credentials are per store or per environment is
   confirmed in `05/01-02`; the store-scoped identity rule here is fixed.
3. The image derivative sizes are the initial defaults; adding a size is an additive change that
   re-processes existing originals and does not alter the stored keys.

## Links

- [App topology](../01-overview/02-app-topology.md)
- [C4 model](../01-overview/01-c4.md)
- [Data flow](../01-overview/03-data-flow.md)
- [Module contracts](../02-components/02-module-contracts.md)
- [REST admin API](./02-rest-admin.md)
- [REST public API](./01-rest-public.md)
- [Payment gateways](./05-payment-gateways.md)
- [Entity model](../03-data/01-entities.md)
- [i18n & money](../03-data/03-i18n-money.md)
- [Catalog requirements](../../01-requirements/02-functional/01-catalog.md)
- [Returns requirements](../../01-requirements/02-functional/09-returns.md)
- [Design spec](../../11-spec/README.md) — §10 (events, files,
  email), §13 (Open Question 2).
- App topology and provider ports: `03/01-02`; data flow and events: `03/01-03`; environment
  configuration: `05/01-02`; gateway runbook: `08/03-03`; deployment and CDN: `08/01-01`; published
  API reference: `09/03-03`, `09/03-05`.
