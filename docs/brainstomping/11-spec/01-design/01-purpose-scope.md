# Purpose & Scope

What resys-eshop is, and the scope of the design spec and the documentation deliverable.

**Links:** [Spec index](../README.md) · [Next →](02-decisions.md) · [Root README](../../../README.md)

---

resys-eshop is a **Spree-aspired multi-store e-commerce platform** on the .NET stack. This
specification is the **canonical design** for the system *and* the **contract for the full
documentation set** that will be written into the numbered `docs/00`–`docs/11` areas.

The immediate deliverable of the implementation effort is **documentation only** (no code).
Code comes later, driven by the plans generated from this spec.

**In scope**

- A Spree-parity e-commerce domain (catalog, orders, shipping, payments, tax, promotions,
  inventory, returns, wishlists, reviews, accounts, multi-store, i18n/multi-currency).
- A headless .NET Minimal API service (modular monolith).
- Two Nuxt 4 (Vue 3) front-ends: a public storefront and an admin back-office.
- A complete, implementation-grade documentation set across `docs/00`–`docs/11`.

**Out of scope**

- CMS (pages, page builder, blog, content blocks) — explicitly excluded.
- Recommendation / personalization engine — excluded; "resys" is only the project name.
- Native mobile apps.
- Production code in this phase (docs-first; code follows the implementation plans).
