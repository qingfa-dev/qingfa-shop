# Tech Stack & Versions

The researched backend, frontend, storefront, and admin technology choices.

**Links:** [Spec index](../README.md) · [← Prev](03-system-overview.md) · [Next →](05-solution-layout.md) · [Root README](../../../README.md)

---

## Backend

| Concern | Choice | Note |
|---|---|---|
| Runtime | .NET 10.0 LTS (10.0.12) | LTS to 2028-11-14. .NET 11 is RC/STS (2026-11). |
| Web | ASP.NET Core **Minimal API** | REST only. |
| ORM | Entity Framework Core 10 | Ships with .NET 10. |
| DB provider | Npgsql | PostgreSQL. |
| Database | PostgreSQL 18 | Current stable. |
| OpenAPI | Built-in `Microsoft.AspNetCore.OpenApi` + Scalar | Generated from the Minimal API. |
| Payments | Stripe.net / PayPal .NET SDKs | Behind `IPaymentGateway`. |
| Auth | ASP.NET Identity (users) + JWT Bearer tokens | Admin uses JWT in httpOnly cookie. |
| Testing | xUnit, Testcontainers (.NET, Postgres), Playwright | See 06-verification. |

## Frontend (shared)

| Concern | Choice | Note |
|---|---|---|
| Framework | **Nuxt 4.5.x** | Nuxt 3 is **EOL 2026-07-31**; target v4. |
| Vue | **Vue 3.5.43** (stable) | 3.6 is RC; pin to stable 3.5. |
| Language | TypeScript (strict) | |
| State | Pinia | Cart, auth, UI state. |
| Fetch | `ofetch` / Nuxt `useFetch` | $fetch typed via shared contracts. |

## Storefront (`web/storefront`)

| Concern | Choice | Note |
|---|---|---|
| Rendering | Nuxt **SSR** | SEO + fast first paint. |
| Styling | **Tailwind CSS v4** | Design tokens in 04-design. |
| i18n | `@nuxtjs/i18n` | Per-store supported locales. |
| Payments UI | Stripe Elements, PayPal JS SDK | Embedded in checkout. |

## Admin (`web/admin`)

| Concern | Choice | Note |
|---|---|---|
| Component lib | **PrimeVue 4** | Data-dense tables/forms for back-office. |
| Rendering | Nuxt (SSR or SPA acceptable) | Authenticated app; SSR for initial shell. |
| Auth | JWT in **httpOnly cookie** | Same `/api/admin` as a headless client. |
