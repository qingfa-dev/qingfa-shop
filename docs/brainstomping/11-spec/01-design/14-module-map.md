# Spree → resys-eshop Module Map

Appendix mapping Spree concepts to resys-eshop modules.

**Links:** [Spec index](../README.md) · [← Prev](13-open-questions.md) · [Root README](../../../README.md)

---

| Spree area | resys-eshop module |
|---|---|
| Products / Variations / Options | **Catalog** |
| Categories (taxons) | **Catalog** (taxonomy tree) |
| Stores (4.x multi-store) | **Stores** |
| Orders / Checkout | **Orders** |
| Shipping | **Shipping** |
| Payments (gateways) | **Payments** |
| Taxes | **Tax** |
| Promotions | **Promotions** |
| Stock / Inventory | **Inventory** |
| Returns / Exchanges | **Returns** |
| Wishlists | **Wishlists** |
| Reviews | **Reviews** |
| Users / Roles | **Accounts** |
| i18n / multi-currency | Cross-cutting (Stores + Catalog) |
| Core::Extension | **Extension architecture** (DI contracts) |
| REST API | Minimal API `/api/v1` + `/api/admin` |
| Back office | `web/admin` (Nuxt + PrimeVue) |
| Storefront themes | `web/storefront` (Nuxt SSR) |
