# Storefront Wireframes & Pages

ASCII layout for every storefront page, each labelled with its Nuxt route and its key regions and data.

**Links:** [Flows](./01-flows.md) · [Components](./03-components.md) · [Design Tokens](./04-tokens.md) · [REST public API](../../03-architecture/04-integration/01-rest-public.md) · [Browse & Buy stories](../../01-requirements/04-user-stories/01-browse-buy.md) · [Design index](../README.md) · [Spec](../../11-spec/README.md)

---
## Purpose

This file fixes the page inventory and layout skeleton for `web/storefront` (Nuxt 4 SSR + Tailwind
v4). Each route names the components from `04/01-03` it composes and the `/api/v1` reads in
`03/04-01` that populate it. All pages render inside the shared `AppHeader` + `AppFooter` shell, so
the wireframes below omit that chrome; regions noted **SSR** render on the server. Routes are Nuxt
file-based (`pages/products/[slug].vue`); responsive breakpoints are in `04/01-04`.
## Home — `/`

```
┌─────────────────────────────┐
│ Hero banner (store branding)│
│ Category tiles (depth=2)    │
│ Featured products (4-up)    │
└─────────────────────────────┘
```
SSR reads: `GET /api/v1/categories?depth=2`, `GET /api/v1/products?sort=featured&per_page=8`.
Empty state hides the featured rail and keeps the category tiles.
## Category / Listing — `/c/[slug]`

```
┌───────────────┬──────────────────────┐
│ FilterSidebar │ Breadcrumb · Sort ▾  │
│ ▸ Category    │ Showing 1–20 of 132  │
│ ▸ Options     │ ┌──┐ ┌──┐ ┌──┐       │
│ ▸ Price range │ │  │ │  │ │  │       │
│ ▸ Availability│ └──┘ └──┘ └──┘       │
│ [Clear all]   │ ┌──┐ ┌──┐ ┌──┐       │
│               │ │  │ │  │ │  │       │
│               │ └──┘ └──┘ └──┘       │
│               │ Pagination ‹1 2 … 7› │
└───────────────┴──────────────────────┘
```
SSR reads: `GET /api/v1/categories/{slug}`, `GET /api/v1/products?taxon={slug}&page=&per_page=`.
Facets are category, option, price range, and availability (`FR-CAT-006`); filters and page are
encoded in the query string so the URL is shareable and SSR-renderable.
## Search — `/search`

```
┌───────────────┬──────────────────────┐
│ FilterSidebar │ "42 results for 'tee'"│
│ (same facets) │ Sort ▾               │
│               │ ┌──┐ ┌──┐ ┌──┐       │
│               │ │  │ │  │ │  │       │
│               │ └──┘ └──┘ └──┘       │
│               │ Pagination           │
└───────────────┴──────────────────────┘
```
Read: `GET /api/v1/search?q=&facets=&page=&per_page=`. Matches name, description, and variant SKU
in the active locale (`FR-CAT-006`). No-query state shows categories; zero results suggests
clearing facets.
## Product Detail — `/products/[slug]`

```
┌───────────────────────┬──────────────────┐
│ ProductGallery        │ Breadcrumb       │
│ (variant-aware)       │ Name             │
│                       │ RatingStars      │
│                       │ PriceTag (Money) │
│                       │ VariantPicker:   │
│                       │  Size S M L      │
│                       │  Color Black/White│
│                       │ StockBadge       │
│                       │ Qty [−] 1 [+]    │
│                       │ [Add to cart]    │
│                       │ [Save to wishlist]│
│                       │ SKU: TEE-M       │
├───────────────────────┴──────────────────┤
│ Tabs: Description · Reviews              │
│ ReviewList; [Write a review] → ReviewForm│
│ Related products (ProductGrid)           │
└──────────────────────────────────────────┘
```
SSR reads: `GET /api/v1/products/{slug}?currency=`, `GET /api/v1/products/{slug}/reviews?page=`.
`VariantPicker` resolves exactly one variant; `StockBadge` shows in stock, backordered, or out of
stock from server data and blocks add-to-cart for a non-backorder out-of-stock variant
(`FR-INV-009`).
## Cart — `/cart`

```
┌──────────────────────────────┬────────────┐
│ CartLine × n                 │ CartSummary│
│ ┌──┬───────────┬─────┬─────┐ │ item total │
│ │img│Name/opts │Qty  │Price│ │ discounts  │
│ │   │SKU       │[−][+]│     │ │ shipping   │
│ │   │[remove]  │     │     │ │ tax        │
│ └──┴───────────┴─────┴─────┘ │ ───────────│
│ CouponForm [code] [Apply]    │ total      │
│                              │[Checkout →]│
└──────────────────────────────┴────────────┘
```
Reads/writes: `GET /api/v1/cart`, `PATCH`/`DELETE /api/v1/cart/line_items/{id}`,
`POST`/`DELETE /api/v1/cart/coupon`. Quantity `0` removes a line; totals are re-derived server-side
(`FR-ORD-002`, `FR-CHK-009`). Empty state prompts continued shopping.
## Checkout — Delivery — `/checkout/delivery`

```
┌──────────────────────────────┬────────────┐
│ CheckoutStepper (1)(2)(3)    │ OrderSummary│
│ AddressForm (ship)           │ lines      │
│ [ ] Billing same as shipping │ totals     │
│ BillAddressForm (if unchecked)│           │
│ ShippingMethodPicker (rates) │           │
│ [Continue to payment]        │           │
└──────────────────────────────┴────────────┘
```
Reads/writes: `GET /api/v1/checkout`, `PUT /api/v1/checkout/delivery`,
`GET /api/v1/checkout/shipping_rates`. Changing country re-quotes methods and clears an invalid
selection; Continue is disabled until the address validates and a method is chosen (`FR-CHK-002`).
## Checkout — Payment — `/checkout/payment`

```
┌──────────────────────────────┬────────────┐
│ CheckoutStepper (1)(2)(3)    │ OrderSummary│
│ PaymentMethodPicker          │ (locked)   │
│  ○ Card (Stripe Elements)    │            │
│  ○ PayPal                    │            │
│  ○ Store credit (balance>0)  │            │
│  ── provider-hosted fields ──│            │
│ [Back]     [Continue to confirm]│          │
└──────────────────────────────┴────────────┘
```
Write: `PUT /api/v1/checkout/payment` with a gateway token/reference. Only store-enabled,
shopper-eligible methods appear; card fields stay in provider-hosted UI so no PAN reaches the app
(`FR-PAY-008`, `FR-PAY-010`). A `402` decline is shown inline and keeps the shopper here.
## Checkout — Confirm — `/checkout/confirm`

```
┌──────────────────────────────┬────────────┐
│ CheckoutStepper (1)(2)(3)    │ OrderSummary│
│ Review address (edit)        │ item total │
│ Review payment (edit)        │ discounts  │
│ Email [__________]           │ shipping   │
│ [ ] Create an account        │ tax        │
│   password [________]        │ total      │
│ [Back]     [Place order]     │            │
└──────────────────────────────┴────────────┘
```
Write: `POST /api/v1/checkout/confirm` with an `Idempotency-Key` header. Confirm is disabled until
delivery and payment are complete and is locked after the first submit so a double click cannot
place two orders (`FR-CHK-004`, `FR-CHK-010`). Email is prefilled for a customer.
## Confirmation — `/orders/[number]/confirmation`

```
┌──────────────────────────────────────────┐
│ ✓ Thank you — order R100000123 placed     │
│ A confirmation email is on its way.       │
│ OrderSummary (lines, discounts, shipping, │
│ tax, total, currency)                     │
│ Ship-to address · Shipping method         │
│ Shipment tracking (when present)          │
│ [View order]  [Continue shopping]         │
└──────────────────────────────────────────┘
```
Reads: `GET /api/v1/account/orders/{id}` for a customer, or `POST /api/v1/checkout/order_lookup` for
a guest. Reachable only with the order number from the confirm response; it never exposes another
shopper's order (`FR-CHK-005`).
## Account — Dashboard — `/account`

```
┌───────────────┬──────────────────────────┐
│ AccountNav    │ Welcome back, {first_name}│
│  Dashboard    │ Recent orders (3)        │
│  Orders       │ R100000123 · shipped · $57│
│  Addresses    │ Default address card     │
│  Wishlists    │ [Track a guest order]    │
│  Reviews      │                          │
│  Profile      │                          │
└───────────────┴──────────────────────────┘
```
SSR reads: `GET /api/v1/account`, `GET /api/v1/account/orders?per_page=3`. Requires a customer
token; an unauthenticated visit redirects to `/login?next=/account`.
## Account — Orders — `/account/orders`

```
┌───────────────┬──────────────────────────┐
│ AccountNav    │ My orders                 │
│               │ R100000123 · awaiting_ful…│
│               │ 2 items · $57.37 · [Open] │
│               │ Pagination                │
└───────────────┴──────────────────────────┘
```
Read: `GET /api/v1/account/orders?page=&per_page=`. Lists only the caller's store-scoped orders
(`FR-ORD-011`).
## Account — Order Detail — `/account/orders/[id]`

```
┌───────────────┬──────────────────────────┐
│ AccountNav    │ R100000123 · state badge  │
│               │ OrderSummary (lines, totals)│
│               │ Ship-to · Billing · Method│
│               │ Shipment tracking (if any)│
│               │ Payments (state, amount)  │
│               │ [Request return] if eligible│
└───────────────┴──────────────────────────┘
```
Read: `GET /api/v1/account/orders/{id}`. A non-owned id returns not-found/forbidden; the return
entry appears only for a `shipped` or `completed` order (`FR-RET-001`).
## Account — Addresses — `/account/addresses`

```
┌───────────────┬──────────────────────────┐
│ AccountNav    │ My addresses [+ Add]      │
│               │ Home · default shipping   │
│               │ 12 Oak St, Springfield    │
│               │ [Edit][Delete][Make def]  │
└───────────────┴──────────────────────────┘
```
Reads/writes: `GET`/`POST`/`PATCH`/`DELETE /api/v1/account/addresses`. Each address may be marked
default shipping or billing; checkout snapshots the chosen address onto the order (`FR-ACC-007`).
## Account — Wishlists — `/account/wishlists`

```
┌───────────────┬──────────────────────────┐
│ AccountNav    │ Wishlists [Default ▾][+New]│
│               │ ┌──────┐ ┌──────┐         │
│               │ │$24.00│ │$39.00│         │
│               │ │[→Cart]│ │[→Cart]│        │
│               │ │[remove]│ │[remove]│      │
│               │ └──────┘ └──────┘         │
└───────────────┴──────────────────────────┘
```
Reads/writes: `GET /api/v1/account/wishlists`,
`POST`/`DELETE /api/v1/account/wishlists/{id}/items`. Cards show live price and availability; moving
to cart obeys stock rules (`FR-WIS-002`, `FR-WIS-003`).
## Account — Profile — `/account/profile`

```
┌───────────────┬──────────────────────────┐
│ AccountNav    │ First name [________]     │
│               │ Last name  [________]     │
│               │ Email      [________]     │
│               │ Locale [en ▾] Currency [USD ▾]│
│               │ [Save changes]            │
│               │ ── Change password ──     │
│               │ [Current][New][Confirm]   │
└───────────────┴──────────────────────────┘
```
Reads/writes: `GET`/`PATCH /api/v1/account`, `POST /api/v1/account/password`. A customer reads and
updates only their own profile; an email change requires re-verification (`FR-ACC-006`).
## Account — Reviews — `/account/reviews`

```
┌───────────────┬──────────────────────────┐
│ AccountNav    │ Classic Tee · ★★★★☆        │
│               │ status: pending           │
│               │ [Edit] [Delete]           │
└───────────────┴──────────────────────────┘
```
A customer's own reviews with their moderation state; editing returns a published review to
`pending`, and deleting removes it from the aggregate (`FR-REV-005`).
## Auth — `/login` and `/register`

```
┌──────────────────────────────┐
│ Sign in                      │
│ Email    [____________]      │
│ Password [____________]      │
│ [Sign in]                    │
│ New here? Create account     │
│ /register adds name + password│
│ confirm, then signs in.      │
└──────────────────────────────┘
```
Writes: `POST /api/v1/account/login`, `POST /api/v1/account/register`. Invalid credentials return a
generic `401`; a deactivated/locked account returns `423` (`FR-ACC-002`). `?next=` returns the
shopper to the page that required auth.
## Guest Order Lookup — `/orders/lookup`

```
┌──────────────────────────────┐
│ Find your order              │
│ Order number [R100000123____]│
│ Email        [______________]│
│ [Look up]                    │
│ (result: OrderSummary + state│
│  + tracking)                 │
└──────────────────────────────┘
```
Guest lookup by order number plus email. An incorrect pair returns a generic not-found that does
not confirm whether the order exists (`01/04-01` US-014).

---
## Open Questions

1. The mobile filter presentation (drawer vs. inline) is a component choice in `04/01-03`; the page
   always renders the same facet data.
2. Whether the account dashboard shows a wishlist teaser or only recent orders is cosmetic and
   resolved during storefront implementation against `04/01-03`.
3. The exact hero and category-tile content is store branding supplied via Nuxt runtime config; the
   page contract here is content-shape only.
