# REST Public API — /api/v1

The public storefront contract: every `/api/v1` endpoint for catalog, cart, checkout, account, and reviews, with auth, DTO shapes, and status codes.

**Links:** [REST admin API](./02-rest-admin.md) · [Authentication](./03-auth.md) · [OpenAPI & Scalar](./04-openapi.md) · [Order lifecycle](../02-components/03-order-lifecycle.md) · [Multi-store scoping](../03-data/02-multi-store.md) · [i18n & money](../03-data/03-i18n-money.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the authoritative public REST contract. The storefront (`web/storefront`) and the
shared client consume only these endpoints, all under the versioned base path `/api/v1`. The admin
surface is separate (`03/04-02`); authentication, roles, and the cookie flow are `03/04-03`; the
generated OpenAPI document is `03/04-04`. The published user reference in `09-user/03-api/` mirrors
this contract endpoint for endpoint.

Each endpoint is rendered below as `METHOD /api/v1/...`; a later parity check compares those exact
tokens against the API reference docs. DTO names live in `Resys.Shared` (`03/02-02`); a DTO never
exposes a module entity.

---

## Conventions

- **Base path.** Every route is prefixed `/api/v1`. The surface is evolved additively; a breaking
  change triggers `/api/v2` (ADR 0004).
- **Store resolution.** The store is resolved from the request host (or the dev-only `X-Store`
  header) before any query runs; a client never supplies a store id to a public endpoint
  (`03/01-02`, `03/03-02`).
- **Money.** Every amount crosses the wire as `{ "amount": number, "currency": "USD" }`
  (ISO-4217). No bare number is accepted or returned (`03/03-03`).
- **Identifiers.** All ids are UUID strings. Paths use lowercase segments; `{id}`/`{slug}` are the
  literal placeholders.
- **Cart identity.** `POST /api/v1/cart` returns a `cart_token`; the client sends it as the
  `X-Cart-Token` header on later cart and checkout calls. A signed-in customer is matched by user id
  and may present a token to merge a guest cart.
- **Pagination.** List endpoints accept `?page=` and `?per_page=` (default 1 and 20, max 100) and
  return `{ "data": [...], "meta": { "page", "per_page", "total", "total_pages" } }`.
- **Localization.** Send `Accept-Language`; missing translations fall back to the store default (`03/03-03`).
- **Errors.** Failures use RFC 9457 problem details (see the Error Model below).
- **Idempotency.** `POST /api/v1/checkout/confirm` requires an `Idempotency-Key` header; replaying
  the same key returns the original order instead of placing a second one (`03/02-03`).
- **Rate limiting.** Public endpoints are rate-limited per client; exceeding the limit returns
  `429` (`03/04-03`).

---

## Catalog

| Endpoint | Auth | Request | Response | Status |
|---|---|---|---|---|
| `GET /api/v1/products` | public | query: `taxon`, `q`, `page`, `per_page`, `currency`, `sort` | `ProductPageDto` | 200, 400 |
| `GET /api/v1/products/{slug}` | public | path slug; query: `currency` | `ProductDetailDto` | 200, 404 |
| `GET /api/v1/products/{slug}/variants` | public | path slug; query: `currency` | `VariantListDto` | 200, 404 |
| `GET /api/v1/categories` | public | query: `depth` | `CategoryTreeDto` | 200 |
| `GET /api/v1/categories/{slug}` | public | path slug | `CategoryDto` | 200, 404 |
| `GET /api/v1/search` | public | query: `q`, `facets`, `page`, `per_page` | `SearchResultDto` | 200, 400 |
| `GET /api/v1/option_types` | public | — | `OptionTypeListDto` | 200 |

`ProductDetailDto` carries localized name/description, per-currency `price`, option types and
values, variants (each with `sku`, `price`, `in_stock`, `available`), images, categories, and an
aggregate `rating` (`{ average, count }`). Stock state is read from Inventory and is never supplied
by the client (`03/02-06`).

---

## Cart

| Endpoint | Auth | Request | Response | Status |
|---|---|---|---|---|
| `POST /api/v1/cart` | public | `CreateCartRequest` (`currency?`) | `CartDto` | 201, 422 |
| `GET /api/v1/cart` | cart token | — | `CartDto` | 200, 404 |
| `POST /api/v1/cart/line_items` | cart token | `AddLineItemRequest` (`variant_id`, `quantity`) | `CartDto` | 201, 404, 409, 422 |
| `PATCH /api/v1/cart/line_items/{id}` | cart token | `UpdateLineItemRequest` (`quantity`) | `CartDto` | 200, 404, 422 |
| `DELETE /api/v1/cart/line_items/{id}` | cart token | — | — | 204, 404 |
| `POST /api/v1/cart/coupon` | cart token | `ApplyCouponRequest` (`code`) | `CartDto` | 200, 404, 422 |
| `DELETE /api/v1/cart/coupon` | cart token | — | `CartDto` | 200, 404 |

`CartDto` returns the line items, adjustments (promotion and shipping), and the derived totals
`item_total`, `adjustment_total`, `tax_total`, and `total`. Adding a variant with no active price in
the store currency is rejected `422` (`03/03-03`); adding a variant with no availability and a
non-backorder location is rejected `409` (`03/02-06`). Coupon application is a preview; the code is
revalidated at confirm (`03/02-04`).

---

## Checkout

Checkout operates on the current cart and advances it through `delivery → payment → confirm`; each
step is gated so a shopper cannot skip ahead (`03/02-03`).

| Endpoint | Auth | Request | Response | Status |
|---|---|---|---|---|
| `GET /api/v1/checkout` | cart token | — | `CheckoutDto` | 200, 404 |
| `PUT /api/v1/checkout/delivery` | cart token | `SetDeliveryRequest` (`ship_address`, `shipping_method_id?`) | `CheckoutDto` | 200, 409, 422 |
| `GET /api/v1/checkout/shipping_rates` | cart token | — | `ShippingRateListDto` | 200, 422 |
| `PUT /api/v1/checkout/payment` | cart token | `SetPaymentRequest` (`bill_address?`, `payment_method_key`) | `CheckoutDto` | 200, 402, 422 |
| `POST /api/v1/checkout/confirm` | cart token | `ConfirmCheckoutRequest` (`email`, `payment_method_key`, `payment_token?`), header `Idempotency-Key` | `OrderDto` | 200, 201, 400, 402, 409, 422 |
| `POST /api/v1/checkout/order_lookup` | public (rate-limited) | `OrderLookupRequest` (`number`, `email`) | `OrderSummaryDto` | 200, 404, 422 |

`CheckoutDto` exposes the current step, offered shipping rates, the selected payment method, and the
server-computed totals. Delivery re-quotes shipping and recomputes tax; payment authorizes against
the freshly computed total; confirm reserves stock, locks adjustments, computes final tax, and
transitions the order in one transaction (`03/02-03`, `03/02-05`, `03/02-06`). A missing
`Idempotency-Key` is `400`; a key already used returns the original order with `200`.
Guest lookup is unauthenticated and rate-limited: `POST /api/v1/checkout/order_lookup` with `{ "number", "email" }` returns a `200` order summary, `404` on a non-matching pair, `422` on malformed input, and never another customer's order.

---

## Account

| Endpoint | Auth | Request | Response | Status |
|---|---|---|---|---|
| `POST /api/v1/account/register` | public | `RegisterRequest` (`email`, `password`, `first_name?`, `last_name?`) | `AuthTokenDto` | 201, 409, 422 |
| `POST /api/v1/account/login` | public | `LoginRequest` (`email`, `password`) | `AuthTokenDto` | 200, 401, 423 |
| `POST /api/v1/account/refresh` | public | `RefreshRequest` (`refresh_token`) | `AuthTokenDto` | 200, 401 |
| `POST /api/v1/account/logout` | customer | `RefreshRequest` (`refresh_token`) | — | 204, 401 |
| `GET /api/v1/account` | customer | — | `AccountDto` | 200, 401 |
| `PATCH /api/v1/account` | customer | `UpdateAccountRequest` (`first_name?`, `last_name?`, `locale?`, `currency?`) | `AccountDto` | 200, 401, 422 |
| `POST /api/v1/account/password` | customer | `ChangePasswordRequest` (`current_password`, `new_password`) | — | 204, 401, 422 |
| `GET /api/v1/account/orders` | customer | query: `page`, `per_page` | `OrderPageDto` | 200, 401 |
| `GET /api/v1/account/orders/{id}` | customer | path id | `OrderDto` | 200, 401, 404 |
| `GET /api/v1/account/addresses` | customer | — | `AddressListDto` | 200, 401 |
| `POST /api/v1/account/addresses` | customer | `AddressRequest` | `AddressDto` | 201, 401, 422 |
| `PATCH /api/v1/account/addresses/{id}` | customer | `AddressRequest` | `AddressDto` | 200, 401, 404, 422 |
| `DELETE /api/v1/account/addresses/{id}` | customer | — | — | 204, 401, 404 |
| `GET /api/v1/account/wishlists` | customer | — | `WishlistListDto` | 200, 401 |
| `POST /api/v1/account/wishlists/{id}/items` | customer | `AddWishlistItemRequest` (`variant_id`) | `WishlistItemDto` | 201, 401, 404, 409 |
| `DELETE /api/v1/account/wishlists/{id}/items/{item_id}` | customer | — | — | 204, 401, 404 |

`AuthTokenDto` carries `access_token`, `expires_in`, `refresh_token`, and the account summary. The
account id in a token is always the caller; an account never reads another account's orders or
addresses. A deactivated account receives `423` at login. Account orders are store-scoped to the
resolved store.

---

## Reviews

| Endpoint | Auth | Request | Response | Status |
|---|---|---|---|---|
| `GET /api/v1/products/{slug}/reviews` | public | query: `page`, `per_page` | `ReviewPageDto` | 200, 404 |
| `POST /api/v1/products/{slug}/reviews` | customer | `SubmitReviewRequest` (`rating`, `body?`) | `ReviewDto` | 201, 401, 409, 422 |

Only `published` reviews are listed publicly. A customer may submit at most one review per product
(`409` on a duplicate); a new review enters `pending` and is moderated by an admin (`03/03-01`).

---

## Error Model

Every failure returns `application/problem+json` (RFC 9457):

```json
{ "type": "https://docs.resys-eshop.example/errors/validation", "title": "One or more validation errors occurred.", "status": 422, "detail": "The request body failed validation.", "instance": "/api/v1/cart/line_items", "errors": { "quantity": ["must be greater than 0"] } }
```

| Status | Meaning |
|---|---|
| 400 | Malformed request or missing required header (for example `Idempotency-Key`). |
| 401 | Missing or invalid access token on an authenticated endpoint. |
| 402 | Payment required or payment authorization declined. |
| 403 | Authenticated but not permitted (for example another account's order). |
| 404 | Resource not found in the resolved store. |
| 409 | Conflict: duplicate review, unavailable stock, or invalid state transition. |
| 422 | Semantic validation failure (field errors in `errors`). |
| 423 | Account locked out or deactivated; login is rejected. |
| 429 | Public rate limit exceeded. |

---

## Examples

### Example 1 — Add a line item
Request:

```http
POST /api/v1/cart/line_items
X-Cart-Token: 7f3c2a9e-0d51-4c8b-9f60-2b1a4e6d8c01
Content-Type: application/json

{ "variant_id": "b1a2c3d4-1111-2222-3333-444455556666", "quantity": 2 }
```

Response `201 Created`:

```json
{
  "id": "0f6f5e4d-aaaa-bbbb-cccc-ddddeeeeffff",
  "state": "cart",
  "currency": "USD",
  "line_items": [
    { "id": "9a8b7c6d-1234-5678-9abc-def012345678", "variant_id": "b1a2c3d4-1111-2222-3333-444455556666", "product_slug": "classic-tee", "name": "Classic Tee — Medium", "quantity": 2, "unit_price": { "amount": 24.00, "currency": "USD" }, "total": { "amount": 48.00, "currency": "USD" } }
  ],
  "adjustments": [],
  "item_total": { "amount": 48.00, "currency": "USD" },
  "adjustment_total": { "amount": 0.00, "currency": "USD" },
  "tax_total": { "amount": 3.96, "currency": "USD" },
  "total": { "amount": 51.96, "currency": "USD" }
}
```

### Example 2 — Product detail
Request:

```http
GET /api/v1/products/classic-tee?currency=USD
```

Response `200 OK`:

```json
{
  "id": "c0ffee00-0000-1111-2222-333344445555",
  "slug": "classic-tee",
  "name": "Classic Tee",
  "description": "Heavyweight cotton tee.",
  "status": "active",
  "currency": "USD",
  "price": { "amount": 24.00, "currency": "USD" },
  "options": [ { "type": "Size", "values": ["Small", "Medium", "Large"] } ],
  "variants": [
    { "id": "b1a2c3d4-1111-2222-3333-444455556666", "sku": "TEE-M", "price": { "amount": 24.00, "currency": "USD" }, "in_stock": true, "available": 12 }
  ],
  "images": [ { "url": "https://cdn.example.com/tee-m.jpg", "is_primary": true } ],
  "categories": [ { "id": "d0d0d0d0-0000-0000-0000-000000000001", "slug": "apparel", "name": "Apparel" } ],
  "rating": { "average": 4.5, "count": 18 }
}
```

### Example 3 — Confirm checkout
Request:

```http
POST /api/v1/checkout/confirm
X-Cart-Token: 7f3c2a9e-0d51-4c8b-9f60-2b1a4e6d8c01
Idempotency-Key: 9c2f8e7d-3b41-4a52-8c93-0e1f2a3b4c5d
Content-Type: application/json

{ "email": "shopper@example.com", "payment_method_key": "stripe", "payment_token": "tok_visa" }
```

Response `201 Created`:

```json
{
  "id": "5e4d3c2b-1111-2222-3333-444455556666",
  "number": "R100000123",
  "state": "awaiting_fulfillment",
  "currency": "USD",
  "email": "shopper@example.com",
  "line_items": [
    { "id": "9a8b7c6d-1234-5678-9abc-def012345678", "variant_id": "b1a2c3d4-1111-2222-3333-444455556666", "name": "Classic Tee — Medium", "quantity": 2, "unit_price": { "amount": 24.00, "currency": "USD" }, "total": { "amount": 48.00, "currency": "USD" } }
  ],
  "adjustments": [
    { "source": "shipping", "label": "Standard (3–5 days)", "amount": { "amount": 5.00, "currency": "USD" } }
  ],
  "item_total": { "amount": 48.00, "currency": "USD" },
  "adjustment_total": { "amount": 5.00, "currency": "USD" },
  "tax_total": { "amount": 4.37, "currency": "USD" },
  "total": { "amount": 57.37, "currency": "USD" },
  "payments": [
    { "id": "aa11bb22-cc33-dd44-ee55-ff6677889900", "state": "captured", "amount": { "amount": 57.37, "currency": "USD" }, "gateway_reference": "pi_3Qexample" }
  ],
  "shipments": [],
  "placed_at": "2026-09-23T10:15:00Z"
}
```

---

## Open Questions

1. The exact product sort keys and facet parameter grammar are finalized with the storefront listing
   design (`04/01-02`); the endpoint set here is fixed.
2. Whether guest checkout is always available or a per-store setting is already modeled as a
   `StoreSetting` key (`03/03-01`); the API honors the resolved store value.

## Links

- [REST admin API](./02-rest-admin.md)
- [Authentication](./03-auth.md)
- [OpenAPI & Scalar](./04-openapi.md)
- [App topology](../01-overview/02-app-topology.md)
- [Data flow](../01-overview/03-data-flow.md)
- [Order lifecycle](../02-components/03-order-lifecycle.md)
- [Promotion engine](../02-components/04-promotion-engine.md)
- [Tax engine](../02-components/05-tax-engine.md)
- [Inventory model](../02-components/06-inventory-model.md)
- [Entity model](../03-data/01-entities.md)
- [Multi-store scoping](../03-data/02-multi-store.md)
- [i18n & money](../03-data/03-i18n-money.md)
- [Checkout requirements](../../01-requirements/02-functional/03-checkout.md)
- [Catalog requirements](../../01-requirements/02-functional/01-catalog.md)
- [ADR 0004 — Headless API and JWT auth](../../00-governance/01-adr/04-headless-jwt.md)
- [Design spec](../../11-spec/README.md) — §7 (public API
  surface), §6 (domain).
- Admin API: `03/04-02`; auth: `03/04-03`; OpenAPI: `03/04-04`; published reference: `09/03-01` …
  `09/03-06`.
