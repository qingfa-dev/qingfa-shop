# System Overview

How the modular-monolith service and the two Nuxt front-ends fit together.

**Links:** [Spec index](../README.md) · [← Prev](02-decisions.md) · [Next →](04-tech-stack.md) · [Root README](../../../README.md)

---

A **modular-monolith** .NET service exposes the commerce domain over REST. Two independent
Nuxt front-ends consume it over HTTP:

```
                    ┌────────────────────────────┐
   browser ───────► │  web/storefront (Nuxt 4 SSR) │  /api/v1  ──►
   browser ───────► │  web/admin      (Nuxt 4)     │  /api/admin ─► ┌──────────────────────────┐
                    └────────────────────────────┘                │  resys.api (Minimal API)   │
                                                                 │  modular monolith, headless │
                                                                 │  ├ Catalog  Orders  Shipping │
                                                                 │  ├ Payments Tax Promo Inv.   │
                                                                 │  ├ Returns Wishlists Reviews │
                                                                 │  └ Accounts Stores i18n       │
                                                                 │      │                        │
                                                                 │      ▼                        │
                                                                 │  EF Core 10 → PostgreSQL 18   │
                                                                 └──────────────────────────┘
```

Key properties:

- **Headless API** — no in-process UI in the service; both front-ends are separate HTTP clients.
- **Modular monolith** — one deployable .NET service, internally split into module projects
  with clean contracts (not microservices).
- **Domain-accurate** — entities and flows mirror Spree so the mapping is 1:1 for the team.
