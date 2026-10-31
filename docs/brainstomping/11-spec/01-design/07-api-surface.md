# API Surface

The public and admin REST surfaces, authentication, and security.

**Links:** [Spec index](../README.md) · [← Prev](06-domain-data.md) · [Next →](08-extension-architecture.md) · [Root README](../../../README.md)

---

## Public — `/api/v1`

- Catalog: products, categories, search with facets.
- Cart: create/add/update/remove.
- Checkout: delivery → payment → confirm.
- Account: orders, addresses, wishlists, profile.
- Reviews.

## Admin — `/api/admin`

- Catalog CRUD (products, variations, options, images, taxonomy).
- Orders (list/detail, fulfill/ship, refund, return).
- Inventory (stock items, locations, movements, transfers).
- Shipping / tax / promotion configuration.
- Store settings (currencies, locales, checkout options).
- Users / roles.
- Reports (sales, top products).

## Auth & security

- **AuthN** — ASP.NET Identity for the user base. Public + admin: **JWT Bearer**
  (short-lived access tokens). Admin front-end stores the JWT in an **httpOnly cookie**.
- **AuthZ** — roles: `customer`, `admin`, plus per-store admin assignment.
  `/api/admin` requires `admin` role + store scope.
- **Protection** — CSRF on state-changing calls, CORS allow-list for the two front-end origins,
  rate limiting on public endpoints, no PAN/PII in logs.
- **Contracts** — OpenAPI generated via built-in `Microsoft.AspNetCore.OpenApi`, browsable in
  Scalar. Full reference lives in `docs/09-user/03-api/`.
- **Versioning** — additive `/api/v1` until a breaking change forces `/api/v2`.

> GraphQL is **not** part of this system (explicit decision).
