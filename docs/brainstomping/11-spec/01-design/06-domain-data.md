# Domain & Data Architecture

Core domain concepts and where the data model is specified.

**Links:** [Spec index](../README.md) · [← Prev](05-solution-layout.md) · [Next →](07-api-surface.md) · [Root README](../../../README.md)

---

## Core concepts

- **`Money`** — strongly-typed `(decimal, ISO-4217 currency)` value object; no raw decimals.
  Multi-currency: variants carry per-store price sets; a `CurrencyRate` table feeds
  conversion for reporting; orders always priced in the store currency.
- **Store scoping** — every catalog/inventory/order row is scoped to a `Store`.
  Storefront resolves store from request domain (or `X-Store` header in dev);
  admin has a store switcher.
- **Order aggregate + state machine** — `Order` → `LineItem`s → `ShipAddress`/`BillAddress`.
  States: `cart → awaiting_payment → awaiting_fulfillment → shipped → completed`,
  plus `canceled` / `returnable`. Checkout steps: delivery → payment → confirm.
  State transitions are the only status mutators; each emits a domain event
  (`OrderPlaced`, `PaymentCompleted`, `OrderShipped`, …) via an **outbox table**
  (same transaction → reliable delivery).
- **Adjustments** — line-item and order-level adjustments (promotion discounts,
  shipping discounts, tax) stored as rows referencing the originating rule/promotion.
  Order totals are always computed from source + adjustments + tax (never cached in one column).
- **Inventory** — `StockItem` per (variant, location); `available` vs `on_hand` with
  backorder policy; `StockMovement` ledger. No negative stock unless location allows backorders.
- **Tax engine** — jurisdiction hierarchy (country > region > city); `TaxRate` bound to a
  jurisdiction + `TaxCategory`; inclusive/exclusive per store; computed on post-promotion amounts.
- **Promotion engine** — `Promotion` (channels, window, usage limits) + **rules**
  (coupon, user, product/variant, order total, shipping method) + **actions**
  (percent/amount off order or item, fixed-price item, free shipping, Nth-item free).
  Applied in documented order with per-line clamping; produces adjustments.
- **i18n** — `Translatable` pattern: catalog text in per-locale rows (product
  name/description, categories, option values). Locale from store's supported locales;
  admin falls back to a default locale when a translation is missing.

## Data model

Entity relationships and key constraints are specified in
`docs/03-architecture/03-data/`. Migrations use EF Core; a seed dataset is documented in
`docs/05-development/01-setup/04-seed-data.md`.
