# API Reference — Account

Read and update the signed-in customer's profile, password, orders, addresses, and wishlists over `/api/v1/account`.

**Links:** [REST Public API](../../03-architecture/04-integration/01-rest-public.md) · [Authentication](./01-auth.md) · [Cart & Checkout API](./03-cart-checkout.md) · [Error Reference](./06-errors.md) · [Customer Guide](../02-customer/01-guide.md) · [User docs index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the published reference for the authenticated customer account endpoints. It mirrors
the Account table of `03/04-01`. All routes are under `/api/v1`, require a customer bearer token,
and operate only on the caller: the account id in a token is always the caller, and an account never
reads another account's orders or addresses (`03/04-03`). Account orders are store-scoped to the
resolved store.

Every endpoint below uses the literal token form `METHOD /api/...` so it can be compared verbatim
with the architecture contract.

---

## Endpoints

| Endpoint | Auth | Request | Response | Status |
|---|---|---|---|---|
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

Passwords follow the Identity policy; a change rotates the security stamp and ends other sessions
(`03/04-03`). A wishlist item is unique per variant in a wishlist, so adding a duplicate is `409`; a
non-existent wishlist or variant is `404`.

---

## Example 1 — Read and update the profile

```bash
curl -sS https://api.example.com/api/v1/account \
  -H 'Authorization: Bearer <access_token>'

curl -sS -X PATCH https://api.example.com/api/v1/account \
  -H 'Authorization: Bearer <access_token>' \
  -H 'Content-Type: application/json' \
  -d '{ "first_name": "Ada", "last_name": "Lovelace", "locale": "en-US", "currency": "USD" }'
```

Response `200 OK`:

```json
{
  "id": "7f3c2a9e-0d51-4c8b-9f60-2b1a4e6d8c01",
  "email": "shopper@example.com",
  "first_name": "Ada",
  "last_name": "Lovelace",
  "locale": "en-US",
  "currency": "USD",
  "created_at": "2026-08-01T09:00:00Z"
}
```

`PATCH` re-validates any field it receives; invalid values return `422`. The `locale` and `currency`
preferences affect display only and are never authoritative for pricing (`03/04-03`).

---

## Example 2 — Change password

```bash
curl -sS -i -X POST https://api.example.com/api/v1/account/password \
  -H 'Authorization: Bearer <access_token>' \
  -H 'Content-Type: application/json' \
  -d '{ "current_password": "correct-horse-battery", "new_password": "staple-battery-correct" }'
```

`204 No Content` on success. A wrong current password or a new password below policy returns the
matching `401`/`422`. The change rotates the security stamp and revokes refresh tokens, so other
sessions end at the next validation.

---

## Example 3 — Orders

```bash
curl -sS 'https://api.example.com/api/v1/account/orders?page=1&per_page=20' \
  -H 'Authorization: Bearer <access_token>'

curl -sS https://api.example.com/api/v1/account/orders/5e4d3c2b-1111-2222-3333-444455556666 \
  -H 'Authorization: Bearer <access_token>'
```

The list returns an `OrderPageDto`; the detail returns an `OrderDto` (the same shape as the order in
`09/03-03`). Requesting another account's order or a non-existent id returns `404`; a missing token
returns `401`.

---

## Example 4 — Addresses

```bash
curl -sS https://api.example.com/api/v1/account/addresses \
  -H 'Authorization: Bearer <access_token>'

curl -sS -X POST https://api.example.com/api/v1/account/addresses \
  -H 'Authorization: Bearer <access_token>' \
  -H 'Content-Type: application/json' \
  -d '{ "name": "Ada Lovelace", "line1": "1 Analytical Way", "city": "Portland", "country_code": "US", "postal_code": "97201", "is_default": true }'
```

Response `201 Created`:

```json
{
  "id": "8a7b6c5d-4321-0987-6543-210fedcba987",
  "name": "Ada Lovelace",
  "line1": "1 Analytical Way",
  "city": "Portland",
  "country_code": "US",
  "postal_code": "97201",
  "is_default": true
}
```

`PATCH /api/v1/account/addresses/{id}` returns the updated `AddressDto`;
`DELETE /api/v1/account/addresses/{id}` returns `204`; a missing or foreign address is `404`.

---

## Example 5 — Wishlists

```bash
curl -sS https://api.example.com/api/v1/account/wishlists \
  -H 'Authorization: Bearer <access_token>'

curl -sS -X POST https://api.example.com/api/v1/account/wishlists/{id}/items \
  -H 'Authorization: Bearer <access_token>' \
  -H 'Content-Type: application/json' \
  -d '{ "variant_id": "b1a2c3d4-1111-2222-3333-444455556666" }'

curl -sS -i -X DELETE https://api.example.com/api/v1/account/wishlists/{id}/items/{item_id} \
  -H 'Authorization: Bearer <access_token>'
```

The list returns a `WishlistListDto`; adding returns `201` with a `WishlistItemDto`; removing returns
`204`. Adding a variant already on the wishlist is `409`; an unknown wishlist or variant is `404`.

---

## Status Codes

| Status | Meaning |
|---|---|
| 200 | The read or update succeeded. |
| 201 | An address or wishlist item was created. |
| 204 | The password change, address delete, or wishlist-item delete succeeded. |
| 401 | Missing or invalid access token, or wrong current password. |
| 404 | The order, address, wishlist, or variant was not found for this caller. |
| 409 | The variant is already on the wishlist. |
| 422 | Semantic validation failure (field errors in `errors`). |

Every failure uses the RFC 9457 envelope in `09/03-06`. Account routes are store-scoped and
rate-limited per authenticated user.

---

## Open Questions

1. Whether a customer may have more than one default address per address type is a data-model detail
   owned by `03/03-01`; the API accepts an `is_default` flag and resolves conflicts server-side.
2. Wishlist sharing or gifting is out of scope; if added it must be a new endpoint owned by
   `03/04-01`.

## Links

- [REST Public API](../../03-architecture/04-integration/01-rest-public.md)
- [Authentication](./01-auth.md)
- [Catalog API](./02-catalog.md)
- [Cart & Checkout API](./03-cart-checkout.md)
- [Error Reference](./06-errors.md)
- [Customer Guide](../02-customer/01-guide.md)
- [Accounts requirements](../../01-requirements/02-functional/10-accounts.md)
- [Design spec](../../11-spec/README.md) — §7 (public API surface),
  §6 (domain).
