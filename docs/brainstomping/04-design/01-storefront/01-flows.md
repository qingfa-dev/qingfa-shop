# Storefront Flows

Journey maps for the public Nuxt 4 storefront: discover a product, build a cart, complete the three-step checkout, and manage an account.

**Links:** [Wireframes & Pages](./02-wireframes-pages.md) · [Components](./03-components.md) · [Design Tokens](./04-tokens.md) · [Browse & Buy stories](../../01-requirements/04-user-stories/01-browse-buy.md) · [Account stories](../../01-requirements/04-user-stories/02-account.md) · [REST public API](../../03-architecture/04-integration/01-rest-public.md) · [Design index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file maps the end-to-end storefront journeys to the routes in `04/01-02`, the components in
`04/01-03`, and the `/api/v1` calls in `03/04-01`. It realizes the browse-and-buy stories
`01/04-01` and the account stories `01/04-02`, and is the journey source cited by the E2E plan
`06/01-04` and the customer guide `09/02-01`. The storefront is Nuxt SSR with Pinia; cart, auth,
and checkout state are held in Pinia stores and re-hydrated from the server on each navigation.

Every state-changing call carries the cart token (`X-Cart-Token`) or the customer access token as
required. Prices, tax, totals, stock, and eligibility are always server-computed; the client never
posts an amount (`03/04-01`).

---

## Journey 1 — Browse → Product → Cart → 3-Step Checkout → Confirmation

This is the primary guest or customer path. Steps 1–7 are unauthenticated; a customer may sign in
at any point without losing the cart.

| # | Route | Shopper action | API call(s) | Result / state |
|---|---|---|---|---|
| 1 | `/` | Lands on the storefront home | `GET /api/v1/categories?depth=2`; `GET /api/v1/products?sort=featured&per_page=8` | Store resolved from the request domain; locale from `Accept-Language` |
| 2 | `/c/{slug}` | Opens a category and pages through it | `GET /api/v1/categories/{slug}`; `GET /api/v1/products?taxon={slug}&page=1&per_page=20` | Listing is store-scoped, active-only, stable order |
| 3 | `/search` | Enters a term and applies facets | `GET /api/v1/search?q=&facets=&page=&per_page=` | Facet counts reflect only visible active products (`FR-CAT-006`) |
| 4 | `/products/{slug}` | Opens a product | `GET /api/v1/products/{slug}?currency=`; `GET /api/v1/products/{slug}/reviews?page=1` | Detail carries variants, options, images, `Money` price, stock, rating |
| 5 | `/products/{slug}` | Picks option values | none (client resolution over the detail payload) | Exactly one variant resolves; stock label shown from server data (`FR-CAT-002`, `FR-INV-009`) |
| 6 | `/products/{slug}` | Sets quantity and adds to cart | `POST /api/v1/cart` (first add) then `POST /api/v1/cart/line_items` | `cart_token` stored in Pinia + cookie; cart in `cart` state (`FR-ORD-001`) |
| 7 | `/cart` | Reviews and edits lines, applies a coupon | `GET /api/v1/cart`; `PATCH`/`DELETE /api/v1/cart/line_items/{id}`; `POST`/`DELETE /api/v1/cart/coupon` | Totals re-derived server-side; coupon is a preview revalidated at confirm (`FR-CHK-008`) |
| 8 | `/checkout/delivery` | Enters a shipping address, picks a method | `GET /api/v1/checkout`; `PUT /api/v1/checkout/delivery`; `GET /api/v1/checkout/shipping_rates` | Step advances only with a valid address and method (`FR-CHK-002`) |
| 9 | `/checkout/payment` | Picks a method and pays | `PUT /api/v1/checkout/payment` with the gateway token/reference | Card data stays in provider-hosted UI; only a token crosses the wire (`FR-PAY-010`) |
| 10 | `/checkout/confirm` | Reviews and confirms | `POST /api/v1/checkout/confirm` with `Idempotency-Key` | Single transaction: state → `awaiting_payment`, stock reserved, `OrderPlaced` outbox (`FR-CHK-004`) |
| 11 | `/orders/{number}/confirmation` | Sees confirmation | `GET /api/v1/account/orders/{id}` (customer) or `POST /api/v1/checkout/order_lookup` (guest) | Order number, lines, and computed total shown; email queued (`FR-CHK-005`) |

**Step gating.** The checkout is strictly `delivery → payment → confirm`. The server rejects a
step whose predecessor is incomplete with `409`; the `CheckoutStepper` disables forward navigation
until the current step's required fields are satisfied (`FR-CHK-001`).

**Account at checkout.** A guest may create an account on the confirm step when the store setting
`guest_checkout` permits guest orders; the new account is linked to the in-flight order without
resetting the cart or totals (`FR-CHK-005`, `01/04-02` US-004). When a store requires an account,
the shopper is redirected to `/register` after the payment step and returned to confirm.

---

## Journey 2 — Failed Payment Recovery

A decline never discards the cart, addresses, or line items.

1. The gateway returns a decline from `PUT /api/v1/checkout/payment`; the response is `402`.
2. The storefront stays on `/checkout/payment`, shows the failure reason, and leaves the cart and
   delivery selection intact.
3. The shopper picks another method or retries the same one. Each retry creates a **new** payment
   attempt; the failed attempt is never mutated (`FR-CHK-007`).
4. The order is not placed while every attempt has failed. Confirm stays blocked.
5. A successful retry proceeds to confirm with a fresh `Idempotency-Key` (`FR-CHK-010`).

---

## Journey 3 — Account

Account pages require a customer access token; the storefront stores the short-lived access token
in memory and the refresh token per the auth flow in `03/04-03`.

### 3a — Register and sign in

| Route | Action | API call(s) | Result |
|---|---|---|---|
| `/register` | Submits email, password, name | `POST /api/v1/account/register` | `AuthTokenDto`; account created, `FR-ACC-001` |
| `/login` | Submits credentials | `POST /api/v1/account/login` | `AuthTokenDto`; `423` when deactivated/locked, `401` on bad credentials |
| shell | Token expires | `POST /api/v1/account/refresh` | New access token; a failed refresh returns the shopper to `/login` |
| shell | Signs out | `POST /api/v1/account/logout` | Refresh token revoked; session cleared |

### 3b — Profile, addresses, and order history

| Route | Action | API call(s) | Result |
|---|---|---|---|
| `/account/profile` | Views/updates name, locale, currency | `GET`/`PATCH /api/v1/account` | Own profile only; email change re-verifies (`FR-ACC-006`) |
| `/account/addresses` | Adds, edits, deletes, marks defaults | `GET`/`POST`/`PATCH`/`DELETE /api/v1/account/addresses` | Checkout snapshots the chosen address onto the order (`FR-ACC-007`) |
| `/account/orders` | Lists own orders | `GET /api/v1/account/orders?page=` | Only the caller's store-scoped orders (`FR-ORD-011`) |
| `/account/orders/{id}` | Opens one order | `GET /api/v1/account/orders/{id}` | Lines, state, computed total, shipment tracking when present |
| `/orders/lookup` | Guest looks up an order | `POST /api/v1/checkout/order_lookup` (`number` + `email`) | No account required; a wrong pair is a generic not-found (`FR-CHK-005`) |

A guest order placed with the same verified email appears in history after sign-in and is claimed
idempotently without changing its contents or totals (`01/04-02` US-008).

### 3c — Wishlists

| Route | Action | API call(s) | Result |
|---|---|---|---|
| `/account/wishlists` | Lists wishlists with live price/stock | `GET /api/v1/account/wishlists` | Items resolve the current price and availability (`FR-WIS-002`) |
| product page | Saves the selected variant | `POST /api/v1/account/wishlists/{id}/items` | Idempotent; records no price and reserves no stock |
| `/account/wishlists` | Moves an item to the cart | `POST /api/v1/cart/line_items` then `DELETE .../items/{item_id}` | Move obeys catalog and stock rules (`FR-WIS-003`) |
| `/account/wishlists` | Removes an item | `DELETE /api/v1/account/wishlists/{id}/items/{item_id}` | Only the owning customer may act |

---

## Journey 4 — Review Submission

1. A signed-in customer opens `/products/{slug}` and selects **Write a review**.
2. The form posts a 1–5 integer rating and an optional bounded body to
   `POST /api/v1/products/{slug}/reviews` (`FR-REV-001`).
3. The review enters `pending`; it is not shown publicly until an admin publishes it
   (`FR-REV-003`). The customer sees a "pending moderation" state on their own review.
4. At most one active review per product per store exists; resubmitting edits the pending review
   rather than duplicating it. A duplicate returns `409`.
5. The verified-purchase flag is computed server-side from the customer's shipped/completed orders
   and is never sent by the client (`FR-REV-004`).
6. Editing a published review returns it to `pending` and removes it from the public list;
   deleting removes it from publication and from the aggregate (`FR-REV-005`).

The public review list on the product page reads `GET /api/v1/products/{slug}/reviews` and shows
only `published` reviews, with the aggregate `{ average, count }` from the product detail.

---

## Cross-Journey Rules

- **Store resolution is server-side.** No public call supplies a store id; the store comes from the
  request host (or the dev `X-Store` header) (`03/03-02`).
- **Money is typed.** Every rendered amount is a `Money` value in the store currency; the client
  formats but never converts (`03/03-03`).
- **Locale fallback.** Missing translations fall back to the store default locale rather than
  rendering an empty field (`FR-I18N-002`).
- **Idempotency.** Confirm requires an `Idempotency-Key`; replaying the key returns the original
  order with `200` instead of placing a second (`FR-CHK-010`).
- **Errors.** Failures use RFC 9457 problem details; the storefront maps `422` to field errors and
  `402`/`409` to inline step messages (`03/04-01`).

---

## Open Questions

1. Whether a storefront shopper may switch currency within a session or is fixed to the store
   default is a store-setting detail owned by `01/02-12`; the design renders a currency selector
   only when the store enables more than one currency.
2. Whether guest order lookup returns shipment tracking in the same response or a follow-up call is
   fixed by the account API in `03/04-01`; the confirmation page renders tracking when present.
3. The exact default page size for listings and reviews is 20 with a 100 maximum per `03/04-01`;
   the storefront exposes 12/24/48 as display options and always passes `per_page` explicitly.
