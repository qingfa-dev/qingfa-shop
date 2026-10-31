# ADR 0001: Application Stack

Records the locked runtime, API, persistence, and front-end stack for resys-eshop.

**Links:** [Governance index](../README.md) · [Spec](../../11-spec/README.md)

---

## Status

Accepted — 2026-09-23. Locked by the design spec (§2, §4) and binding on all
subsequent documentation and implementation plans.

## Context

resys-eshop is a Spree-aspired, multi-store e-commerce platform built on the .NET
stack with a headless REST API and two independent Nuxt front-ends. The effort is
documentation-first: this ADR fixes the stack so that every later area (`03`–`10`)
can cite one set of versions without re-litigating them.

Forces that shaped the decision:

- **Long support window.** The platform is expected to live for years; the runtime
  must be a Long Term Support release with a published end-of-support date.
- **Server-rendered storefront.** The public storefront needs SEO and fast first
  paint, which rules out a purely client-rendered SPA for that surface.
- **Data-dense back office.** The admin needs mature data tables, forms, and
  theming, favouring a component library rather than bespoke widgets.
- **Team familiarity.** The team already works in C#/ASP.NET and Vue, so the stack
  should stay inside those ecosystems instead of introducing a second runtime.
- **Relational integrity.** Commerce data (orders, stock, adjustments, tax) is
  inherently relational and benefits from a mature ORM and PostgreSQL.
- **One deployable service.** Operationally simple hosting; the topology decision is
  recorded separately in [ADR 0002](./02-modular-monolith.md).

## Decision

Adopt the following stack. Versions are stated verbatim and are authoritative.

### Backend

| Concern | Choice | Note |
|---|---|---|
| Runtime | .NET 10.0 LTS (10.0.12) | Supported to 2028-11-14. .NET 11 is RC/STS (2026-11). |
| Web | ASP.NET Core **Minimal API** | REST only; no GraphQL. |
| ORM | Entity Framework Core 10 | Ships with .NET 10. |
| DB provider | Npgsql | PostgreSQL driver for EF Core. |
| Database | PostgreSQL 18 | Current stable. |
| OpenAPI | Built-in `Microsoft.AspNetCore.OpenApi` + Scalar | Generated from the Minimal API; browsable in Scalar. |
| Payments | Stripe.net / PayPal .NET SDKs | Behind an `IPaymentGateway` contract. |
| Auth | ASP.NET Identity (users) + JWT Bearer tokens | Admin stores the JWT in an httpOnly cookie. |
| Testing | xUnit, Testcontainers (.NET, Postgres), Playwright | Strategy documented in area `06`. |

### Front-end (shared)

| Concern | Choice | Note |
|---|---|---|
| Framework | **Nuxt 4.5.x** | Nuxt 3 is EOL 2026-07-31; target v4. |
| Vue | **Vue 3.5.43** | 3.6 is RC; pin to stable 3.5. |
| Language | TypeScript (strict) | Shared contracts across both apps. |
| State | Pinia | Cart, auth, and UI state. |
| Fetch | `ofetch` / Nuxt `useFetch` | `$fetch` typed via shared contracts. |

### Storefront (`web/storefront`)

| Concern | Choice | Note |
|---|---|---|
| Rendering | Nuxt **SSR** | SEO and fast first paint. |
| Styling | **Tailwind CSS v4** | Design tokens owned by area `04`. |
| i18n | `@nuxtjs/i18n` | Per-store supported locales. |
| Payments UI | Stripe Elements, PayPal JS SDK | Embedded in checkout. |

### Admin (`web/admin`)

| Concern | Choice | Note |
|---|---|---|
| Component library | **PrimeVue 4** | Data-dense tables and forms for the back office. |
| Rendering | Nuxt (SSR or SPA acceptable) | Authenticated app; SSR for the initial shell. |
| Auth | JWT in **httpOnly cookie** | Calls the same `/api/admin` as any headless client. |

## Consequences

**Positive**

- One language and toolchain for the back end; one framework family for both front-ends.
- REST + OpenAPI yields generated, typed clients and a browsable contract with no
  schema-server to operate.
- EF Core 10 + Npgsql + PostgreSQL 18 gives migrations, relational integrity, and a
  provider aligned with the .NET 10 release cadence.
- Nuxt SSR satisfies SEO, while the admin can choose SSR or SPA per the open question
  in the spec (§13.1, resolved later in area `04`).
- A single .NET process keeps local development and deployment simple.

**Negative / costs**

- REST forces explicit endpoint design where GraphQL would have offered flexible
  querying; the team accepts this in exchange for cacheability and simplicity.
- Two Nuxt applications duplicate some conventions; `web/shared` exists to reduce the
  duplication.
- Minimal API endpoints are less structured than MVC controllers, so the project must
  enforce module conventions (see area `05`) to keep the surface organised.
- PrimeVue 4 and Tailwind v4 are pinned; upgrades must be validated per the
  dependency policy in area `10`.

**Neutral**

- The stack intentionally mirrors the Spree service boundaries so that a Spree
  developer can map concepts 1:1 (see the spec's Appendix A).

## Alternatives Considered

- **Blazor (server or WebAssembly) for the front-ends.** Rejected: it would replace
  the Vue ecosystem the team uses, is weaker for the SEO-critical public storefront,
  and conflicts with the locked "no Blazor" decision. Listed here because it is the
  most plausible .NET-native alternative.
- **GraphQL for the API.** Rejected: REST with generated OpenAPI clients covers the
  known consumers (storefront, admin, shared client) and adds no runtime schema
  server. See [ADR 0003](./03-no-cms-no-gql.md).
- **Microservices instead of a modular monolith.** Rejected: the domain is cohesive,
  the team is small, and distributed transactions across orders, inventory, payments,
  and tax would add cost without a demonstrated scaling need. See
  [ADR 0002](./02-modular-monolith.md).
- **.NET 8 (LTS).** Rejected: .NET 8 leaves the maintenance window before the planned
  implementation horizon; .NET 10 LTS is supported to 2028-11-14. .NET 11 is RC/STS at
  the plan date and is not a stable target.
- **A single universal front-end app.** Rejected: the storefront (public, SSR,
  Tailwind) and admin (authenticated, PrimeVue) have different rendering, styling, and
  security needs; separating them keeps each bundle lean.
- **A different relational database (e.g. SQL Server).** Rejected: PostgreSQL 18 is
  the documented target and pairs with Npgsql; no requirement favours an alternative.

## Open Questions

None for the stack itself. The spec's open questions (admin SSR vs SPA, object-storage
default, payment-gateway ordering) do not change the stack choice and are resolved in
areas `04`, `05`, and `03` respectively.

## Links

- [Governance index](../README.md)
- [Design spec](../../11-spec/README.md) — §2 (locked
  decisions), §4 (tech stack and versions).
- [ADR 0002 — Modular monolith](./02-modular-monolith.md)
- [ADR 0003 — No CMS, no GraphQL](./03-no-cms-no-gql.md)
- [ADR 0004 — Headless API and JWT auth](./04-headless-jwt.md)
