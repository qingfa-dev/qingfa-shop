# Front-ends

The storefront, admin, and shared Nuxt applications.

**Links:** [Spec index](../README.md) · [← Prev](08-extension-architecture.md) · [Next →](10-cross-cutting.md) · [Root README](../../../README.md)

---

## `web/storefront` (Nuxt 4 SSR)

Screens: home; category/listing (filters + pagination); product detail (variations, reviews,
stock state); search; cart; 3-step checkout (delivery → payment → confirm, Stripe/PayPal UI);
account (orders, addresses, wishlists, profile); review submission. Talks to the platform via
REST only.

## `web/admin` (Nuxt 4 + PrimeVue)

Feature areas: dashboard (KPIs); catalog (products/variations/options/images, taxonomy);
orders (list → detail → fulfill/ship/refund/return); inventory (stock, locations, movements,
transfers); shipping/tax/promotion configuration; store settings; users/roles; reports.
Multi-store switcher in the shell. Calls `/api/admin` over HTTP.

## `web/shared`

Shared TypeScript: API client (typed via generated OpenAPI types), DTO mirrors,
common components, auth helpers, money/currency formatting.
