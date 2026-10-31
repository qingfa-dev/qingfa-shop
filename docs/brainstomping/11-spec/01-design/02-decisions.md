# Decisions Summary

The locked architectural and scope decisions.

**Links:** [Spec index](../README.md) · [← Prev](01-purpose-scope.md) · [Next →](03-system-overview.md) · [Root README](../../../README.md)

---

| # | Decision | Choice |
|---|----------|--------|
| 1 | Front-ends | Two Nuxt 4 (Vue 3) apps: `web/storefront` (SSR) + `web/admin` (PrimeVue). **No Blazor.** |
| 2 | API | **.NET Minimal API, REST only. No GraphQL.** |
| 3 | Back-end topology | Modular-monolith, headless. Admin consumes `/api/admin` over HTTP. |
| 4 | Runtime | **.NET 10 LTS** (10.0.12, supported to 2028-11-14). |
| 5 | ORM / DB | **EF Core 10 + Npgsql / PostgreSQL 18** (current stable). |
| 6 | Feature scope | Full Spree parity, minus CMS. |
| 7 | Docs depth | All areas `00`–`11`, implementation-grade. |
| 8 | Docs approach | **Hub spec + per-area elaboration** (single source of truth = the design spec). |
| 9 | Docs structure | Per-concern **subfolders** + **numbered files**, ~100–300 lines each, `README.md` index per area. |
| 10 | Recommendation engine | Excluded. |
