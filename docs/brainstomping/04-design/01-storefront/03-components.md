# Storefront Components

Component inventory for the Nuxt 4 storefront: props, events, and the `/api/v1` calls each component owns or triggers.

**Links:** [Flows](./01-flows.md) · [Wireframes & Pages](./02-wireframes-pages.md) · [Design Tokens](./04-tokens.md) · [REST public API](../../03-architecture/04-integration/01-rest-public.md) · [Browse & Buy stories](../../01-requirements/04-user-stories/01-browse-buy.md) · [Design index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the component contract for `web/storefront`. Components live in `web/shared` when they
are reusable across both front-ends and in `web/storefront` otherwise. A **route component** (a
page) fetches data with `useFetch`/`$fetch` and passes plain DTOs down; a **presentational
component** takes props and emits events, and makes no direct HTTP call unless stated. All types
mirror the DTOs in `Resys.Shared` (`03/02-02`) and the shapes in `03/04-01`.

Conventions: props are camelCase and typed; events are kebab-case; a component never posts a
computed amount (prices, tax, totals, stock, and eligibility are server-owned). Money is formatted
by `useMoney`, never by string concatenation (`03/03-03`).

---

## Shell Components

### AppHeader
- **Props:** `storeName: string`, `categories: CategorySummary[]`, `cartCount: number`.
- **Events:** `search(term)`, `open-cart`, `toggle-menu`.
- **API:** none; the layout supplies store config and `GET /api/v1/categories?depth=2`.

### AppFooter
- **Props:** `storeName: string`, `locales: string[]`, `currencies: string[]`.
- **Events:** `select-locale(locale)`, `select-currency(currency)`.
- **API:** none; locale/currency selection updates the `@nuxtjs/i18n` locale and the store's active
  currency used on subsequent reads.

### AccountNav
- **Props:** `active: string`.
- **Events:** none.
- **API:** none; links to the account routes in `04/01-02`.

### CheckoutStepper
- **Props:** `current: 'delivery' | 'payment' | 'confirm'`, `completed: Step[]`.
- **Events:** `navigate(step)`.
- **API:** none; the checkout page enforces step gating from the `CheckoutDto.current_step` returned
  by `GET /api/v1/checkout` (`FR-CHK-001`).

---

## Catalog Components

### ProductCard
- **Props:** `product: ProductSummary`, `currency: string`, `quickAdd?: boolean`.
- **Events:** `select(slug)`, `quick-add(variantId)`.
- **API:** none; renders a `ProductPageDto` item or `SearchResultDto` hit supplied by the page.

### ProductGrid
- **Props:** `products: ProductSummary[]`, `columns?: 2 | 3 | 4`, `loading?: boolean`.
- **Events:** `select(slug)`.
- **API:** none; the page owns the listing read.

### FilterSidebar
- **Props:** `facets: Facet[]`, `selected: SelectedFacets`.
- **Events:** `change(facets)`.
- **API:** none; `change` rewrites the query string, which the route re-reads via
  `GET /api/v1/products` or `GET /api/v1/search` (`FR-CAT-006`).

### Pagination
- **Props:** `page: number`, `totalPages: number`, `perPage: number`.
- **Events:** `change(page)`, `change-per-page(perPage)`.
- **API:** none; the page passes `page`/`per_page` to the listing or search read.

### ProductGallery
- **Props:** `images: ProductImage[]`, `primaryId?: string`.
- **Events:** `select(imageId)`.
- **API:** none; images come from `ProductDetailDto`.

### PriceTag
- **Props:** `money: Money`, `compareAt?: Money`.
- **Events:** none.
- **API:** none; uses `useMoney` for locale-aware formatting.

### RatingStars
- **Props:** `average: number`, `count?: number`, `editable?: boolean`, `modelValue?: number`.
- **Events:** `update:modelValue(rating)`.
- **API:** none; aggregate values come from `ProductDetailDto.rating`.

### StockBadge
- **Props:** `inStock: boolean`, `available?: number`, `backorder?: boolean`.
- **Events:** none.
- **API:** none; reads the variant's server-provided stock state (`FR-INV-009`).

### VariantPicker
- **Props:** `optionTypes: OptionType[]`, `variants: Variant[]`, `modelValue?: variantId`.
- **Events:** `update:modelValue(variantId)`.
- **API:** none; resolves exactly one variant from the detail payload and exposes its `sku`, price,
  and stock. A variant with no price in the store currency is disabled (`FR-CAT-002`, `FR-CAT-007`).

### AddToCartButton
- **Props:** `variantId?: string`, `quantity: number`, `disabled?: boolean`.
- **Events:** `added(cart)`, `error(problem)`.
- **API:** `POST /api/v1/cart/line_items` (and `POST /api/v1/cart` first when no cart token exists);
  delegates to the `useCart` store. Blocks a non-backorder out-of-stock variant (`FR-INV-009`).

### WishlistButton
- **Props:** `variantId: string`, `wishlistId?: string`, `saved?: boolean`.
- **Events:** `saved(item)`, `removed(itemId)`.
- **API:** `POST /api/v1/account/wishlists/{id}/items` and
  `DELETE /api/v1/account/wishlists/{id}/items/{item_id}`; hidden when unauthenticated.

### ReviewList
- **Props:** `reviews: Review[]`, `rating: RatingSummary`, `page: number`, `totalPages: number`.
- **Events:** `change-page(page)`.
- **API:** none; the product page reads `GET /api/v1/products/{slug}/reviews?page=` and shows only
  `published` reviews (`FR-REV-002`).

### ReviewForm
- **Props:** `productSlug: string`, `existing?: Review`.
- **Events:** `submitted(review)`, `error(problem)`.
- **API:** `POST /api/v1/products/{slug}/reviews` with a 1–5 integer rating and optional bounded
  body; a duplicate returns `409` and is surfaced inline (`FR-REV-001`). The verified flag is never
  sent.

---

## Cart & Checkout Components

### CartLine
- **Props:** `line: CartLineDto`, `currency: string`.
- **Events:** `update-quantity(id, quantity)`, `remove(id)`.
- **API:** none; the cart page performs
  `PATCH`/`DELETE /api/v1/cart/line_items/{id}` through the `useCart` store. Quantity `0` removes
  the line.

### CartSummary
- **Props:** `totals: CartTotals`, `currency: string`, `showCheckout?: boolean`.
- **Events:** `checkout`.
- **API:** none; renders `item_total`, `adjustment_total`, `tax_total`, and `total` from the server
  (`FR-CHK-009`).

### CouponForm
- **Props:** `appliedCode?: string`.
- **Events:** `applied(cart)`, `removed(cart)`, `error(problem)`.
- **API:** `POST /api/v1/cart/coupon` and `DELETE /api/v1/cart/coupon`; a rejected code changes no
  total (`FR-CHK-008`).

### AddressForm
- **Props:** `modelValue: Address`, `type: 'ship' | 'bill'`, `saved?: Address[]`.
- **Events:** `update:modelValue(address)`, `select-saved(addressId)`.
- **API:** none; the delivery page persists it with `PUT /api/v1/checkout/delivery`, and the
  account page with `POST`/`PATCH /api/v1/account/addresses`.

### ShippingMethodPicker
- **Props:** `rates: ShippingRate[]`, `modelValue?: methodId`.
- **Events:** `update:modelValue(methodId)`.
- **API:** none; the delivery page reads `GET /api/v1/checkout/shipping_rates`. Changing country
  re-quotes and clears an invalid selection (`FR-CHK-002`).

### PaymentMethodPicker
- **Props:** `methods: PaymentMethod[]`, `modelValue?: methodKey`, `storeCredit?: Money`.
- **Events:** `update:modelValue(methodKey)`, `token(token)`.
- **API:** none directly; provider SDKs (Stripe Elements, PayPal) yield a token that the payment
  page sends via `PUT /api/v1/checkout/payment`. Store credit appears only with a positive balance
  (`FR-PAY-008`).

### OrderSummary
- **Props:** `lines: OrderLine[]`, `totals: OrderTotals`, `currency: string`, `compact?: boolean`.
- **Events:** none.
- **API:** none; renders a `CartDto`, `CheckoutDto`, or `OrderDto` totals block unchanged.

### OrderList
- **Props:** `orders: OrderSummaryDto[]`, `page: number`, `totalPages: number`.
- **Events:** `open(id)`, `change-page(page)`.
- **API:** none; the account page reads `GET /api/v1/account/orders`.

---

## Account & Auth Components

### AuthForm
- **Props:** `mode: 'login' | 'register'`.
- **Events:** `authenticated(auth)`, `error(problem)`.
- **API:** `POST /api/v1/account/login` or `POST /api/v1/account/register`; delegates token storage
  to the `useAuth` store (`FR-ACC-002`).

### ProfileForm
- **Props:** `account: AccountDto`, `locales: string[]`, `currencies: string[]`.
- **Events:** `saved(account)`, `change-password(current, next)`.
- **API:** `GET`/`PATCH /api/v1/account` and `POST /api/v1/account/password` (`FR-ACC-006`).

### AddressList
- **Props:** `addresses: AddressDto[]`.
- **Events:** `edit(address)`, `delete(id)`, `make-default(id, type)`.
- **API:** none; the account addresses page calls
  `POST`/`PATCH`/`DELETE /api/v1/account/addresses` (`FR-ACC-007`).

### WishlistPanel
- **Props:** `wishlists: WishlistDto[]`, `activeId?: string`.
- **Events:** `select(id)`, `move-to-cart(variantId)`, `remove(itemId)`.
- **API:** `GET /api/v1/account/wishlists`; move-to-cart calls `POST /api/v1/cart/line_items` then
  `DELETE /api/v1/account/wishlists/{id}/items/{item_id}` (`FR-WIS-003`).

### OrderLookupForm
- **Props:** none.
- **Events:** `found(order)`, `not-found`.
- **API:** guest lookup via `POST /api/v1/checkout/order_lookup` with the order number and email; a
  wrong pair returns a generic not-found (`01/04-01` US-014).

---

## Pinia Stores & Composables

| Store / composable | Owns | API calls |
|---|---|---|
| `useCart` | cart token, `CartDto`, add/update/remove, coupon | `POST /api/v1/cart`; `GET /api/v1/cart`; `POST`/`PATCH`/`DELETE /api/v1/cart/line_items`; `POST`/`DELETE /api/v1/cart/coupon` |
| `useCheckout` | current step, addresses, selected rates/method | `GET /api/v1/checkout`; `PUT /api/v1/checkout/delivery`; `GET /api/v1/checkout/shipping_rates`; `PUT /api/v1/checkout/payment`; `POST /api/v1/checkout/confirm` |
| `useAuth` | access/refresh tokens, `AccountDto` | `POST /api/v1/account/register|login|refresh|logout`; `GET`/`PATCH /api/v1/account` |
| `useStore` | resolved store, locale, currency, branding | `GET /api/v1/categories` for nav; store config from Nuxt runtime config |
| `useMoney` | locale-aware `Money` formatting | none |
| `useApi` | `$fetch` wrapper adding `X-Cart-Token`, auth header, and `Accept-Language` | wraps every call above |
| `useProblem` | maps RFC 9457 problem details to field/inline errors | none |
| `useToast` | transient success/error notifications | none |

---

## Open Questions

1. The `quick-add` interaction (card-level add vs. detail-only) is a UX choice finalized during
   storefront implementation; the `ProductCard` event exists so either is possible.
2. Whether `ProductGallery` supports zoom/video is out of scope for the initial component set; the
   gallery renders the ordered image list from `ProductDetailDto`.
3. The concrete `Facet` grammar (`facets` parameter encoding) is fixed with the listing design
   `04/01-02` and the API query contract in `03/04-01`.
