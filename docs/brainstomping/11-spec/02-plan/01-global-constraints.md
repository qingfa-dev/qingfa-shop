# Plan Overview & Global Constraints

The plan's goal plus the project-wide constraints every documentation task inherits.

**Links:** [Spec index](../README.md) · [Design spec](../01-design/01-purpose-scope.md) · [Next →](02-verification-helpers.md) · [Root README](../../../README.md)

---

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce the complete implementation-grade documentation set (numbered markdown files across `docs/00`–`docs/11`) for a Spree-like .NET e-commerce platform, exactly as specified in the design spec.

**Architecture:** Docs-only deliverable — no code is produced. Each task writes one concern subfolder (or a small group of files), verifies it against placeholder/link/consistency checks, and commits. Task order follows the dependency graph: governance → requirements → planning → architecture → design → development → verification → release → operations → user docs → maintenance → root index + final audit.

**Tech Stack (documented, not built):** .NET 10.0 LTS (10.0.12) · ASP.NET Core Minimal API (REST only) · EF Core 10 + Npgsql + PostgreSQL 18 · built-in `Microsoft.AspNetCore.OpenApi` + Scalar · ASP.NET Identity + JWT Bearer · Nuxt 4.5.x / Vue 3.5.43 / Tailwind CSS v4 / Pinia / PrimeVue 4 · Stripe.net / PayPal .NET SDK.

**Spec:** `docs/11-spec/README.md`


### Global Constraints


Every task's documentation implicitly includes these project-wide rules. Exact values are copied from the spec; do not restate them differently.

- **Versions (verbatim):** .NET 10.0 LTS (10.0.12, supported to 2028-11-14); EF Core 10; Npgsql; PostgreSQL 18; Nuxt 4.5.x; Vue 3.5.43 (3.6 is RC — pin 3.5); Tailwind CSS v4; Pinia; PrimeVue 4; built-in `Microsoft.AspNetCore.OpenApi` + Scalar; Stripe.net / PayPal .NET SDKs.
- **No Blazor. No GraphQL.** REST only (`/api/v1` public, `/api/admin` admin).
- **No CMS** (pages, page builder, blog, content blocks) and **no recommendation engine**.
- **Topology:** modular monolith, headless; modules `Catalog`, `Inventory`, `Shipping`, `Tax`, `Promotions`, `Payments`, `Returns`, `Wishlists`, `Reviews`, `Accounts`, `Stores`.
- **Front-ends:** `web/storefront` (Nuxt 4 SSR, Tailwind v4, Pinia, `@nuxtjs/i18n`), `web/admin` (Nuxt 4 + PrimeVue, JWT in httpOnly cookie), `web/shared`.
- **Auth:** ASP.NET Identity; JWT Bearer; roles `customer`, `admin`, plus per-store admin assignment.
- **Money:** strongly-typed `(decimal, ISO-4217 currency)`; no raw decimals anywhere.
- **Doc file format — every new file MUST start with:**
  ```markdown
  # <Title>

  <One-line purpose.>

  **Links:** [Area index](../README.md) · [Spec](../../11-spec/README.md)
  ```
  (Adjust the `../` depth so the spec path resolves from the file's location. For a file at
  `docs/AA/sub/file.md`, the spec link is `../../11-spec/README.md`.
  For an area README at `docs/AA/README.md`, it is `../11-spec/README.md`.)
- **Doc size:** one concern per file, 100–300 lines.
- **No placeholders:** never write `TBD`, `TODO`, `FIXME`, `PLACEHOLDER`, "implement later", or
  "fill in the details". If a detail is genuinely undecided, add an explicit `## Open Questions`
  section stating the decision needed — never a placeholder token.
- **Cross-linking:** every file links back to its area `README.md` and to the spec; READMEs link to
  every child file.
- **Citations:** reference another doc as `AA/BB-CC` (area/`<subfolder>-<number>`), e.g. `03/03-01`.
