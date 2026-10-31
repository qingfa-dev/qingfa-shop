# Customer Guide

Shop the resys-eshop storefront: create an account, find products, build a cart, check out in three steps, then track orders, wishlists, and reviews.

**Links:** [Storefront Flows](../../04-design/01-storefront/01-flows.md) · [Storefront Pages](../../04-design/01-storefront/02-wireframes-pages.md) · [Checkout Requirements](../../01-requirements/02-functional/03-checkout.md) · [Wishlists & Reviews](../../01-requirements/02-functional/11-wishlists-reviews.md) · [Accounts](../../01-requirements/02-functional/10-accounts.md) · [Public REST API](../../03-architecture/04-integration/01-rest-public.md) · [User docs index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This guide is for shoppers on the public Nuxt 4 storefront. It walks through the full
customer journey — register or sign in, browse and search, pick a product and variant, build a
cart, complete the three-step checkout, and track the order afterward — plus wishlists, reviews,
and profile settings. Every step maps to the storefront journeys in `04/01-01` and the
`/api/v1` contract in `03/04-01`. The storefront talks to the platform over REST only; prices,
tax, totals, stock, and coupon validity are computed on the server and never trusted from the
browser.

The active store is resolved from the site domain, so the catalog, currency, and enabled payment
methods you see are the ones that store has configured. Cart state is tied to an anonymous cart
token until you sign in, at which point the cart is associated with your account rather than
being lost (`FR-ACC-005`, `FR-ORD-001`).

---

## Creating an Account and Signing In

An account is optional to buy, but it keeps your order history, saved addresses, and wishlists in
one place.

1. **Register.** Open `/register` and submit an email, a password meeting the store policy, and a
   name. The storefront calls `POST /api/v1/account/register` and receives an auth token
   (`FR-ACC-001`).
2. **Sign in.** Open `/login` and submit your credentials to
   `POST /api/v1/account/login`. A wrong email or password returns a generic error that does not
   reveal whether the account exists (`FR-ACC-010`).
3. **Stay signed in.** The short-lived access token lives in memory and is renewed in the
   background with `POST /api/v1/account/refresh`; if the refresh fails you are returned to
   `/login`. The refresh token is stored so a page reload does not sign you out (`03/04-03`).
4. **Sign out.** `POST /api/v1/account/logout` revokes the refresh token and clears the session.

If you add items as a guest and then sign in, the anonymous cart is associated with your account
and is not discarded. An account that is deactivated or locked returns `423` on sign-in; wait for
the lockout window or contact support.

---

## Browsing and Searching

- **Home.** `/` shows featured categories and products for the resolved store
  (`GET /api/v1/categories`, `GET /api/v1/products?sort=featured`).
- **Category.** `/c/{slug}` lists the products in a category with pagination
  (`GET /api/v1/categories/{slug}`, `GET /api/v1/products?taxon={slug}`).
- **Search.** `/search` takes a term and facets and returns matching active products
  (`GET /api/v1/search?q=&facets=`). Facet counts reflect only products that are currently
  visible, so a category with no matches is not offered (`FR-CAT-006`).
- **Pagination.** Listings and reviews are server-paginated with the default and maximum page
  sizes fixed by the performance NFR (`01/03-01`); the storefront always sends `per_page` explicitly.

Results are store-scoped and show only active products. A product that is unavailable is either
hidden or shown as out of stock, depending on its inventory policy (`FR-INV-009`).

---

## Choosing a Product and a Variant

1. Open `/products/{slug}`. The detail response carries the description, images, options, the
   available variants, the current `Money` price, stock state, and the aggregate rating
   (`GET /api/v1/products/{slug}?currency=`).
2. **Select option values** — for example size and color. The storefront resolves exactly one
   variant from the selected values; if the combination does not exist, the add-to-cart control
   stays disabled (`FR-CAT-002`).
3. **Check availability.** Each variant shows a stock label derived from server data. A variant
   that is out of stock and not backorderable cannot be added (`FR-INV-009`).
4. **Set a quantity** and choose **Add to cart**. If this is your first add, the storefront
   creates the cart (`POST /api/v1/cart`) and stores the cart token; otherwise it adds a line
   (`POST /api/v1/cart/line_items`).
5. **Save for later** with the wishlist control if you are signed in — see the Wishlists section.

Prices are shown in the store currency and are never converted in the browser; the server returns
the correct `Money` value for the store (`03/03-03`).

---

## The Cart

Open `/cart` to review and edit the cart before checkout (`GET /api/v1/cart`).

- **Change quantity** with `PATCH /api/v1/cart/line_items/{id}`.
- **Remove a line** with `DELETE /api/v1/cart/line_items/{id}`.
- **Apply a coupon** with `POST /api/v1/cart/coupon`; remove it with
  `DELETE /api/v1/cart/coupon`.
- **Totals** — item subtotal, adjustments, tax, shipping, and grand total — are re-derived on the
  server after every change and are never computed from client input (`FR-CHK-009`).

A coupon entered here is a preview. It is revalidated at confirm, so a coupon that expires or
reaches its usage limit between browsing and checkout is rejected rather than silently applied
(`FR-CHK-008`). The cart is store-scoped: if the site domain changes to a different store, the
cart for that store is used.

---

## Checkout: Delivery → Payment → Confirm

Checkout is exactly three steps and must be completed in order. The server rejects a step whose
predecessor is incomplete with `409`, and the stepper disables forward navigation until the
current step is valid (`FR-CHK-001`).

### Step 1 — Delivery

1. Enter or choose a shipping address. A saved account address can be selected and is snapshotted
   onto the order so later edits to your address book do not change it (`FR-ACC-007`).
2. The address is validated and the available shipping methods for the order and destination are
   quoted (`PUT /api/v1/checkout/delivery`, `GET /api/v1/checkout/shipping_rates`).
3. Select a method. Its cost becomes a shipping adjustment on the order (`FR-CHK-002`).
4. Changing the address or the items re-quotes the methods and clears a now-invalid selection, so
   the quoted rate always matches the destination.

### Step 2 — Payment

1. Choose from the payment methods the store has enabled.
2. Enter card details only in the provider-hosted interface (Stripe Elements or the PayPal JS
   SDK). Card data never reaches the platform — only a provider token is sent
   (`FR-CHK-003`, `FR-PAY-010`).
3. The amount charged is the server-computed order total in the order currency, not a value sent
   by the browser.
4. On success the payment is recorded against the order (`PUT /api/v1/checkout/payment`).

A decline returns `402`; the storefront stays on the payment step, shows the reason, and keeps
your cart, address, and delivery selection intact. Retrying creates a **new** payment attempt and
never mutates the failed one (`FR-CHK-007`). The order is not placed while every attempt has
failed.

### Step 3 — Confirm

1. Review the order: lines, addresses, shipping, any coupon, tax, and the grand total.
2. Submit **Place order** (`POST /api/v1/checkout/confirm` with an `Idempotency-Key`).
3. In one transaction the order moves from `cart` to `awaiting_payment`, stock is reserved, and
   the confirmation event is queued (`FR-CHK-004`, `FR-CHK-006`).
4. A guest may create an account on this step; the in-flight order is linked to the new account
   without resetting the cart or totals (`FR-CHK-005`).

If you double-click or the network retries, the idempotency key ensures only one order is placed
and only one charge is made (`FR-CHK-010`). A successful capture moves the order on to
fulfillment; an offline method such as bank transfer may leave it awaiting payment confirmation
until an admin marks it paid (`FR-PAY-003`, `FR-PAY-004`).

---

## Order Tracking and History

- **Confirmation.** After confirm you land on `/orders/{number}/confirmation`, which shows the
  order number, lines, and computed total. The confirmation email is queued at the same time
  (`FR-CHK-005`).
- **Order history.** Signed-in customers open `/account/orders` to list their own orders and
  `/account/orders/{id}` for one order (`GET /api/v1/account/orders`). Each order shows its
  state, lines, computed total, and shipment tracking when present (`FR-ORD-011`).
- **Guest lookup.** Without an account, use `/orders/lookup` with the order number and the email
  used at checkout (`POST /api/v1/checkout/order_lookup`). A wrong pair returns a generic
  not-found rather than revealing an order (`FR-CHK-005`).
- **Claiming.** A guest order placed with the same email as your account appears in history after
  you sign in and is claimed without changing its contents or totals (`FR-ACC-008`).

Order state follows the lifecycle `cart → awaiting_payment → awaiting_fulfillment → shipped →
completed`, with `canceled` and `returnable` reached where the order allows it (`FR-ORD-004`).

---

## Wishlists

A wishlist saves variants for later without reserving stock or creating a cart line. You must be
signed in; anonymous wishlists are not persisted (`FR-WIS-004`).

1. **Save a variant** from a product page with `POST /api/v1/account/wishlists/{id}/items`.
   Adding the same variant twice is idempotent and creates no duplicate (`FR-WIS-002`).
2. **View a wishlist** at `/account/wishlists` (`GET /api/v1/account/wishlists`). Each item shows
   the current price and availability resolved at display time, so a price change is reflected
   without rewriting the saved item.
3. **Move to cart** adds the variant under normal catalog and stock rules and then removes it from
   the wishlist; **add to cart** leaves the wishlist item in place (`FR-WIS-003`). A variant that
   is out of stock and not backorderable cannot be moved.
4. **Remove an item** with `DELETE /api/v1/account/wishlists/{id}/items/{item_id}`.

A customer may keep more than one wishlist and mark one as the default (`FR-WIS-001`). Wishlists
belong only to you and are never visible to another customer.

---

## Writing a Review

1. Open a product you are signed in as a customer and choose **Write a review**.
2. Submit an integer rating on the documented scale and an optional bounded body
   (`POST /api/v1/products/{slug}/reviews`).
3. The review enters `pending`. It is not shown publicly until an admin publishes it, and you see
   a "pending moderation" state on your own submission (`FR-REV-003`).
4. You may have at most one active review per product per store; resubmitting edits the pending
   one rather than creating a second, and a duplicate returns `409` (`FR-REV-001`).
5. If you bought the product on a completed or shipped order, the review is marked **verified
   purchase**; the flag is computed by the server and cannot be set by the browser
   (`FR-REV-004`).
6. Editing a published review returns it to `pending` and removes it from the public list; deleting
   it removes it from publication and from the rating aggregate (`FR-REV-005`).

Published reviews and the aggregate `{ average, count }` appear on the product page. A product
with no published reviews shows no rating rather than a zero (`FR-REV-002`).

---

## Profile, Addresses, and Preferences

- **Profile.** `/account/profile` reads and updates your name, locale, and currency through
  `GET`/`PATCH /api/v1/account`; changing your email requires uniqueness and re-verification
  (`FR-ACC-006`).
- **Addresses.** `/account/addresses` manages saved addresses with default shipping and billing
  designations (`GET`/`POST`/`PATCH`/`DELETE /api/v1/account/addresses`; `FR-ACC-007`). Checkout
  snapshots the chosen address onto the order.
- **Currency.** The storefront renders a currency selector only when the store enables more than
  one currency. Orders are created and kept in a single currency for their whole lifecycle, so an
  existing order is never re-priced after a store change (`FR-STO-003`, `FR-ORD-012`).

---

## Open Questions

1. Whether a shopper may switch currency within a session or is fixed to the store default is a
   store-setting detail owned by `01/02-12`; the storefront shows a selector only when the store
   enables more than one currency.
2. Whether guest order lookup returns shipment tracking in the same response or a follow-up call
   is fixed by the account API in `03/04-01`; the confirmation page renders tracking when present.
3. The exact rating scale (1–5 vs. 1–10) is finalized with the catalog and review UI in `04/01`;
   this guide assumes the documented integer scale.
