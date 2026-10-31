# API Reference — Catalog

Browse products, categories, variants, search facets, option types, and reviews on the public `/api/v1` surface, with runnable `curl` examples.

**Links:** [REST Public API](../../03-architecture/04-integration/01-rest-public.md) · [Authentication](./01-auth.md) · [Cart & Checkout API](./03-cart-checkout.md) · [Error Reference](./06-errors.md) · [Customer Guide](../02-customer/01-guide.md) · [User docs index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the published reference for the read-mostly catalog endpoints. It mirrors the Catalog
and Reviews tables of `03/04-01`. All routes are under `/api/v1` and are public; the store is
resolved from the request host before any query runs, so a client never supplies a store id. Product
text is returned localized from `Accept-Language`, and stock state comes from Inventory and is never
supplied by the client (`03/02-06`).

Every endpoint below uses the literal token form `METHOD /api/...` so it can be compared verbatim
with the architecture contract.

---

## Endpoints

| Endpoint | Auth | Request | Response | Status |
|---|---|---|---|---|
| `GET /api/v1/products` | public | query: `taxon`, `q`, `page`, `per_page`, `currency`, `sort` | `ProductPageDto` | 200, 400 |
| `GET /api/v1/products/{slug}` | public | path slug; query: `currency` | `ProductDetailDto` | 200, 404 |
| `GET /api/v1/products/{slug}/variants` | public | path slug; query: `currency` | `VariantListDto` | 200, 404 |
| `GET /api/v1/categories` | public | query: `depth` | `CategoryTreeDto` | 200 |
| `GET /api/v1/categories/{slug}` | public | path slug | `CategoryDto` | 200, 404 |
| `GET /api/v1/search` | public | query: `q`, `facets`, `page`, `per_page` | `SearchResultDto` | 200, 400 |
| `GET /api/v1/option_types` | public | — | `OptionTypeListDto` | 200 |
| `GET /api/v1/products/{slug}/reviews` | public | query: `page`, `per_page` | `ReviewPageDto` | 200, 404 |
| `POST /api/v1/products/{slug}/reviews` | customer | `SubmitReviewRequest` (`rating`, `body?`) | `ReviewDto` | 201, 401, 409, 422 |

List endpoints accept `?page=` and `?per_page=` (default 1 and 20, max 100) and return
`{ "data": [...], "meta": { "page", "per_page", "total", "total_pages" } }`. Only `published`
reviews are listed publicly; a customer may submit at most one review per product, and a duplicate
is `409`. A newly submitted review enters `pending` and is moderated by an admin (`03/03-01`).

---

## Example 1 — List products

```bash
curl -sS 'https://api.example.com/api/v1/products?taxon=apparel&q=tee&page=1&per_page=20&currency=USD&sort=price_asc' \
  -H 'Accept-Language: en-US'
```

Response `200 OK`:

```json
{
  "data": [
    {
      "id": "c0ffee00-0000-1111-2222-333344445555",
      "slug": "classic-tee",
      "name": "Classic Tee",
      "price": { "amount": 24.00, "currency": "USD" },
      "thumbnail_url": "https://cdn.example.com/tee-m.jpg",
      "in_stock": true,
      "rating": { "average": 4.5, "count": 18 }
    }
  ],
  "meta": { "page": 1, "per_page": 20, "total": 1, "total_pages": 1 }
}
```

An unknown `sort` key or a bad `per_page` returns `400`.

---

## Example 2 — Product detail

```http
GET /api/v1/products/classic-tee?currency=USD
Accept-Language: en-US
```

```bash
curl -sS 'https://api.example.com/api/v1/products/classic-tee?currency=USD' \
  -H 'Accept-Language: en-US'
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

An unknown slug in the resolved store returns `404`.

---

## Example 3 — Variants, categories, and search

```bash
curl -sS 'https://api.example.com/api/v1/products/classic-tee/variants?currency=USD'

curl -sS 'https://api.example.com/api/v1/categories?depth=2'
curl -sS 'https://api.example.com/api/v1/categories/apparel'

curl -sS 'https://api.example.com/api/v1/search?q=tee&facets=size,color&page=1&per_page=20'
```

`variants` returns a `VariantListDto` (each variant with `sku`, `price`, `in_stock`, `available`).
`categories` returns a `CategoryTreeDto`; `categories/{slug}` returns a single `CategoryDto`. Search
returns a `SearchResultDto` with matching products plus facet counts and an applied-filter echo.

---

## Example 4 — Option types and reviews

```bash
curl -sS 'https://api.example.com/api/v1/option_types'

curl -sS 'https://api.example.com/api/v1/products/classic-tee/reviews?page=1&per_page=20'

curl -sS -X POST 'https://api.example.com/api/v1/products/classic-tee/reviews' \
  -H 'Authorization: Bearer <access_token>' \
  -H 'Content-Type: application/json' \
  -d '{ "rating": 5, "body": "Soft and true to size." }'
```

Response `201 Created`:

```json
{
  "id": "aa11bb22-cc33-dd44-ee55-ff6677889900",
  "product_id": "c0ffee00-0000-1111-2222-333344445555",
  "rating": 5,
  "body": "Soft and true to size.",
  "status": "pending",
  "created_at": "2026-09-23T10:15:00Z"
}
```

A rating outside 1–5 or a missing rating is `422`; a second review for the same product is `409`.

---

## Status Codes

| Status | Meaning |
|---|---|
| 200 | The read succeeded. |
| 201 | The review was accepted for moderation. |
| 400 | Malformed query, unknown sort key, or bad pagination. |
| 401 | Missing or invalid access token on review submission. |
| 404 | Product, variant, or category not found in the resolved store. |
| 409 | The customer already reviewed this product. |
| 422 | Semantic validation failure (field errors in `errors`). |

Every failure uses the RFC 9457 envelope in `09/03-06`. Public reads are rate-limited per client.

---

## Open Questions

1. The exact product sort keys and facet parameter grammar are finalized with the storefront listing
   design (`04/01-02`); the endpoint set here is fixed by `03/04-01`.
2. Whether review moderation exposes a dedicated admin endpoint is deferred to the admin catalog/API
   work; this reference documents only the public listing and submission.

## Links

- [REST Public API](../../03-architecture/04-integration/01-rest-public.md)
- [Authentication](./01-auth.md)
- [Cart & Checkout API](./03-cart-checkout.md)
- [Account API](./04-account.md)
- [Admin API](./05-admin-api.md)
- [Error Reference](./06-errors.md)
- [Customer Guide](../02-customer/01-guide.md)
- [Catalog requirements](../../01-requirements/02-functional/01-catalog.md)
- [Design spec](../../11-spec/README.md) — §7 (public API surface),
  §6 (domain).
