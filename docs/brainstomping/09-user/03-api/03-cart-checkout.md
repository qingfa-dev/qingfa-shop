# API Reference — Cart & Checkout

Create and mutate a cart, apply a coupon, complete the three-step checkout, look up a guest order, and receive payment-gateway callbacks.

**Links:** [REST Public API](../../03-architecture/04-integration/01-rest-public.md) · [Payment Gateways](../../03-architecture/04-integration/05-payment-gateways.md) · [Authentication](./01-auth.md) · [Error Reference](./06-errors.md) · [Customer Guide](../02-customer/01-guide.md) · [User docs index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the published reference for cart, checkout, and the payment webhook. It mirrors the
Cart and Checkout tables of `03/04-01` plus the gateway callback noted in `03/04-05`. All public
routes are under `/api/v1`; the store is resolved from the request host. Cart identity is the
`X-Cart-Token` returned by `POST /api/v1/cart`; a signed-in customer is matched by user id and may
present a token to merge a guest cart.

Checkout advances the current cart through `delivery → payment → confirm`; each step is gated so a
shopper cannot skip ahead (`03/02-03`). Every endpoint below uses the literal token form
`METHOD /api/...` so it can be compared verbatim with the architecture contract.

---

## Cart Endpoints

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
the store currency is `422`; adding a variant with no availability at a non-backorder location is
`409`. Coupon application is a preview; the code is revalidated at confirm (`03/02-04`).

---

## Checkout Endpoints

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
`Idempotency-Key` is `400`; replaying a used key returns the original order with `200`.

Guest order lookup is unauthenticated and rate-limited: `POST /api/v1/checkout/order_lookup` with
`{ "number", "email" }` returns a `200` order summary, `404` on a non-matching pair, `422` on
malformed input, and never another customer's order.

---

## Payment Webhook

| Endpoint | Auth | Request | Response | Status |
|---|---|---|---|---|
| `POST /api/v1/webhooks/payments/{gateway}` | provider signature | gateway event body | — | 200, 400, 401, 5xx |

The callback is public by necessity but authenticated by the provider's signature; it is idempotent
on the gateway event id and never exposes PAN (`03/04-05`). A failed signature verification is `401`
and is logged; a malformed body is `400`. The endpoint returns `2xx` once the event is durably
recorded so the provider stops retrying; a processing error returns `5xx` and lets the provider
redeliver. A redelivery of an already-recorded event is accepted and ignored.

---

## Example 1 — Create a cart and add a line item

```bash
curl -sS -X POST https://api.example.com/api/v1/cart \
  -H 'Content-Type: application/json' \
  -d '{ "currency": "USD" }'
```

Response `201 Created`:

```json
{
  "id": "0f6f5e4d-aaaa-bbbb-cccc-ddddeeeeffff",
  "state": "cart",
  "currency": "USD",
  "cart_token": "7f3c2a9e-0d51-4c8b-9f60-2b1a4e6d8c01",
  "line_items": [],
  "adjustments": [],
  "item_total": { "amount": 0.00, "currency": "USD" },
  "adjustment_total": { "amount": 0.00, "currency": "USD" },
  "tax_total": { "amount": 0.00, "currency": "USD" },
  "total": { "amount": 0.00, "currency": "USD" }
}
```

Then add a variant:

```bash
curl -sS -X POST https://api.example.com/api/v1/cart/line_items \
  -H 'X-Cart-Token: 7f3c2a9e-0d51-4c8b-9f60-2b1a4e6d8c01' \
  -H 'Content-Type: application/json' \
  -d '{ "variant_id": "b1a2c3d4-1111-2222-3333-444455556666", "quantity": 2 }'
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

`PATCH /api/v1/cart/line_items/{id}` updates `quantity`; `DELETE /api/v1/cart/line_items/{id}`
returns `204`. Apply and remove a coupon with `POST /api/v1/cart/coupon` and
`DELETE /api/v1/cart/coupon`.

---

## Example 2 — Delivery and shipping rates

```bash
curl -sS https://api.example.com/api/v1/checkout \
  -H 'X-Cart-Token: 7f3c2a9e-0d51-4c8b-9f60-2b1a4e6d8c01'

curl -sS 'https://api.example.com/api/v1/checkout/shipping_rates' \
  -H 'X-Cart-Token: 7f3c2a9e-0d51-4c8b-9f60-2b1a4e6d8c01'

curl -sS -X PUT https://api.example.com/api/v1/checkout/delivery \
  -H 'X-Cart-Token: 7f3c2a9e-0d51-4c8b-9f60-2b1a4e6d8c01' \
  -H 'Content-Type: application/json' \
  -d '{ "ship_address": { "name": "Ada Lovelace", "line1": "1 Analytical Way", "city": "Portland", "country_code": "US", "postal_code": "97201" }, "shipping_method_id": "3f2e1d0c-aaaa-bbbb-cccc-ddddeeeeffff" }'
```

`shipping_rates` returns a `ShippingRateListDto`; a cart with an incomplete address is `422`.
`delivery` returns the updated `CheckoutDto`; an unavailable shipping method for the address is
`409`.

---

## Example 3 — Payment and confirm

```bash
curl -sS -X PUT https://api.example.com/api/v1/checkout/payment \
  -H 'X-Cart-Token: 7f3c2a9e-0d51-4c8b-9f60-2b1a4e6d8c01' \
  -H 'Content-Type: application/json' \
  -d '{ "bill_address": null, "payment_method_key": "stripe" }'

curl -sS -X POST https://api.example.com/api/v1/checkout/confirm \
  -H 'X-Cart-Token: 7f3c2a9e-0d51-4c8b-9f60-2b1a4e6d8c01' \
  -H 'Idempotency-Key: 9c2f8e7d-3b41-4a52-8c93-0e1f2a3b4c5d' \
  -H 'Content-Type: application/json' \
  -d '{ "email": "shopper@example.com", "payment_method_key": "stripe", "payment_token": "tok_visa" }'
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

A missing `Idempotency-Key` is `400`; replaying the key returns the same order with `200`. A
declined authorization is `402`; changed stock or a locked cart is `409`; validation failure is
`422`.

---

## Example 4 — Guest order lookup

```bash
curl -sS -X POST https://api.example.com/api/v1/checkout/order_lookup \
  -H 'Content-Type: application/json' \
  -d '{ "number": "R100000123", "email": "shopper@example.com" }'
```

Response `200 OK`:

```json
{
  "number": "R100000123",
  "state": "awaiting_fulfillment",
  "currency": "USD",
  "total": { "amount": 57.37, "currency": "USD" },
  "placed_at": "2026-09-23T10:15:00Z",
  "shipments": []
}
```

A pair that does not match returns `404`; malformed input returns `422`; the endpoint never returns
another customer's order.

---

## Status Codes

| Status | Meaning |
|---|---|
| 200 | Read, update, coupon, or idempotent confirm replay. |
| 201 | Cart, line item, or order created. |
| 204 | Line item or coupon removed. |
| 400 | Missing `Idempotency-Key` or malformed request. |
| 402 | Payment required or authorization declined. |
| 404 | Cart, line item, coupon, or order pair not found. |
| 409 | Unavailable stock or invalid state transition. |
| 422 | Semantic validation failure (field errors in `errors`). |
| 429 | Cart/checkout or guest-lookup rate limit exceeded. |

Every failure uses the RFC 9457 envelope in `09/03-06`.

---

## Open Questions

1. Whether guest checkout is always available or a per-store setting is modeled as a `StoreSetting`
   key (`03/03-01`); the API honors the resolved store value.
2. Which gateway-specific event types the webhook accepts is owned by `03/04-05`; the endpoint,
   signature check, and idempotency behavior are fixed here.

## Links

- [REST Public API](../../03-architecture/04-integration/01-rest-public.md)
- [Payment Gateways](../../03-architecture/04-integration/05-payment-gateways.md)
- [Authentication](./01-auth.md)
- [Catalog API](./02-catalog.md)
- [Account API](./04-account.md)
- [Admin API](./05-admin-api.md)
- [Error Reference](./06-errors.md)
- [Customer Guide](../02-customer/01-guide.md)
- [Checkout requirements](../../01-requirements/02-functional/03-checkout.md)
- [Design spec](../../11-spec/README.md) — §7 (public API surface),
  §6 (domain).
