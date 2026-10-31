# Environment Configuration

The API `appsettings` and environment variables, the Nuxt runtime config, and the resolved default for object storage.

**Links:** [Prerequisites](./01-prerequisites.md) · [Docker Compose](./03-docker-compose.md) · [Seed data](./04-seed-data.md) · [App topology](../../03-architecture/01-overview/02-app-topology.md) · [Auth](../../03-architecture/04-integration/03-auth.md) · [Email & files](../../03-architecture/04-integration/06-email-files.md) · [Development index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the contract for how resys-eshop is configured per environment. It defines the API's
`appsettings` shape and the environment variables that override it, the Nuxt runtime config for both
front-ends, and **it resolves Spec §13 Open Question 2**: development uses **local disk** storage
and production uses an **S3-compatible** backend.

Configuration follows the options pattern (§10 of the [spec](../../11-spec/README.md)):
strongly-typed options objects are bound from `appsettings.{Environment}.json` and then overridden by
environment variables. The provider abstractions being configured are `IEmailSender` and
`IFileStorage` (`03/04-06`), the JWT scheme (`03/04-03`), and the payment gateways (`03/04-05`).
The database these settings point at is started by `05/01-03`; the seed data is applied per
`05/01-04`.

**Secrets are never committed.** Every key marked *secret* below is supplied through environment
variables, `dotnet user-secrets` in development, or the deployment secret store — never through a
committed JSON file. The repository ships empty placeholders in `appsettings.Development.json` only
for non-secret local values.

---

## API Configuration Model

- Base file `src/Resys.Api/appsettings.json` holds non-secret defaults shared by all environments.
- `appsettings.Development.json` holds local-only non-secret values, such as the local database host
  and the `local` file-storage root.
- Environment variables override both. The .NET convention uses a double underscore for nesting:
  `Jwt__SigningKey`, `Storage__Provider`, `ConnectionStrings__ResysDb`.
- No production secret is read from a file; the deployment injects the environment variables
  defined in `08/01-02`.

---

## API Settings

| Setting | Environment variable | Dev example | Secret | Notes |
|---|---|---|---|---|
| `ConnectionStrings:ResysDb` | `ConnectionStrings__ResysDb` | `Host=localhost;Port=5432;Database=resys_eshop;Username=resys;Password=resys` | yes (prod) | Points at the container from `05/01-03`. |
| `Jwt:Issuer` | `Jwt__Issuer` | `https://localhost:8443` | no | Validated on every token (`03/04-03`). |
| `Jwt:Audience` | `Jwt__Audience` | `resys` | no | Public and admin share one scheme. |
| `Jwt:SigningKey` | `Jwt__SigningKey` | from user-secrets | **yes** | RS256 private key; never committed. |
| `Jwt:AccessTokenMinutes` | `Jwt__AccessTokenMinutes` | `15` | no | Matches the httpOnly access cookie lifetime. |
| `Jwt:RefreshTokenDays` | `Jwt__RefreshTokenDays` | `30` | no | Matches the refresh cookie lifetime. |
| `Cors:AllowedOrigins:0` | `Cors__AllowedOrigins__0` | `http://localhost:3000` | no | Storefront origin (`03/01-02`). |
| `Cors:AllowedOrigins:1` | `Cors__AllowedOrigins__1` | `http://localhost:3001` | no | Admin origin. |
| `Stores:HeaderName` | `Stores__HeaderName` | `X-Store` | no | Dev store override; domain resolution wins in production. |
| `Payments:DefaultProvider` | `Payments__DefaultProvider` | `stripe` | no | Stripe first, PayPal second (`03/04-05`). |
| `Payments:Stripe:SecretKey` | `Payments__Stripe__SecretKey` | `sk_test_…` | **yes** | Never logged (`03/04-05`). |
| `Payments:Stripe:PublishableKey` | `Payments__Stripe__PublishableKey` | `pk_test_…` | no | Returned to the storefront checkout. |
| `Payments:Stripe:WebhookSecret` | `Payments__Stripe__WebhookSecret` | `whsec_…` | **yes** | Verifies inbound webhooks. |
| `Payments:PayPal:ClientId` | `Payments__PayPal__ClientId` | `…` | no | Second gateway. |
| `Payments:PayPal:ClientSecret` | `Payments__PayPal__ClientSecret` | `…` | **yes** | |
| `Payments:PayPal:WebhookId` | `Payments__PayPal__WebhookId` | `…` | **yes** | |
| `Email:Provider` | `Email__Provider` | `smtp` | no | `smtp` for dev mailbox, `ses` in production (`03/04-06`). |
| `Email:FromAddress` | `Email__FromAddress` | `no-reply@resys.test` | no | Default sender; per-store override allowed. |
| `Email:Smtp:Host` | `Email__Smtp__Host` | `localhost` | no | Mail catcher from `05/01-03`. |
| `Email:Smtp:Port` | `Email__Smtp__Port` | `1025` | no | Port `1025` per `03/01-02`. |
| `Email:Smtp:UserName` | `Email__Smtp__UserName` | *(empty)* | no | Empty for a local catcher. |
| `Email:Smtp:Password` | `Email__Smtp__Password` | *(empty)* | **yes** | |
| `Email:Smtp:UseStartTls` | `Email__Smtp__UseStartTls` | `false` | no | Plain transport on localhost only. |
| `Email:Ses:Region` | `Email__Ses__Region` | `eu-west-1` | no | Used when `Email:Provider=ses`. |
| `Storage:Provider` | `Storage__Provider` | `local` | no | **Resolved below**: `local` in dev, `s3` in production. |
| `Storage:PublicBaseUrl` | `Storage__PublicBaseUrl` | `http://localhost:8080` | no | Base for generated asset URLs (`03/04-06`). |
| `Storage:Local:RootPath` | `Storage__Local__RootPath` | `./.data/storage` | no | Local-disk backend root; API serves it. |
| `Storage:S3:Endpoint` | `Storage__S3__Endpoint` | `https://s3.example.com` | no | S3-compatible endpoint (AWS or MinIO). |
| `Storage:S3:Region` | `Storage__S3__Region` | `eu-west-1` | no | |
| `Storage:S3:Bucket` | `Storage__S3__Bucket` | `resys-assets` | no | Per-store keys isolate tenants. |
| `Storage:S3:AccessKey` | `Storage__S3__AccessKey` | `…` | **yes** | |
| `Storage:S3:SecretKey` | `Storage__S3__SecretKey` | `…` | **yes** | |
| `Storage:S3:UsePathStyle` | `Storage__S3__UsePathStyle` | `false` | no | `true` for MinIO-style endpoints. |
| `Observability:OtlpEndpoint` | `Observability__OtlpEndpoint` | `http://localhost:4317` | no | OpenTelemetry export; optional locally. |

`appsettings.Development.json` therefore looks like this, with secrets supplied out of band:

```json
{
  "ConnectionStrings": {
    "ResysDb": "Host=localhost;Port=5432;Database=resys_eshop;Username=resys;Password=resys"
  },
  "Jwt": { "Issuer": "https://localhost:8443", "Audience": "resys", "AccessTokenMinutes": 15, "RefreshTokenDays": 30 },
  "Cors": { "AllowedOrigins": ["http://localhost:3000", "http://localhost:3001"] },
  "Stores": { "HeaderName": "X-Store" },
  "Payments": { "DefaultProvider": "stripe" },
  "Email": { "Provider": "smtp", "FromAddress": "no-reply@resys.test", "Smtp": { "Host": "localhost", "Port": 1025, "UseStartTls": false } },
  "Storage": {
    "Provider": "local",
    "PublicBaseUrl": "http://localhost:8080",
    "Local": { "RootPath": "./.data/storage" }
  }
}
```

In production the same tree is populated with `ConnectionStrings__ResysDb`, `Jwt__SigningKey`,
`Payments__*`, `Email__*`, and `Storage__S3__*`; the `Storage:Provider` flips to `s3`. The deployment
topology that injects these is `08/01-01` and `08/01-02`.

---

## Object Storage — Resolving Spec §13 Open Question 2

**Decision (fixed):** the default storage backend is **`local` (local disk) in development and
`Storage:Provider=s3` (S3-compatible) in production.** This closes the open question left in §13 of
the [spec](../../11-spec/README.md); the abstraction it configures
is the `IFileStorage` contract in `03/04-06`.

| Environment | `Storage:Provider` | Implementation | What `GetUrlAsync` returns |
|---|---|---|---|
| Local dev / test | `local` | Files under `Storage:Local:RootPath`, streamed by `Resys.Api`. | `{Storage:PublicBaseUrl}/api/v1/files/{key}` |
| Staging / production | `s3` | S3-compatible bucket (AWS SDK / MinIO), credentials from options. | Public base (CDN or bucket host) for public keys; short-lived signed URLs for private keys. |

Rationale:

- **Zero-dependency local start.** A new contributor needs only Docker (for PostgreSQL) and the
  toolchain; there is no object-storage account, bucket, or credential to obtain before the first
  run. The API serves local files, so product images render in dev exactly as they do through the
  CDN in production.
- **One code path in production.** Production uses the same `IFileStorage` abstraction backed by
  S3, so the key scheme (`stores/{storeId}/products/{productId}/{imageId}/{size}{ext}`), the
  public/private split, and the key-not-URL rule in the database are identical across environments.
- **Swappable without code.** Moving from disk to S3 is a configuration change and, if existing
  objects must move, a one-time copy of the local root into the bucket; no application code changes.
- **The database stores keys, never absolute URLs.** A provider change does not rewrite rows; the
  URL is regenerated on read through `GetUrlAsync` (`03/04-06`).

Rules that follow from the decision:

- `Storage:Provider=local` is rejected outside `Development` and `Test`. A production host with a
  `local` value fails fast at startup rather than serving files from a container filesystem.
- `Storage:Local:RootPath` is a writable path excluded from source control (`.gitignore`); the
  Compose volume in `05/01-03` persists it only if a developer mounts it, which is optional.
- Private assets (originals) always use signed access; the `local` backend implements the same
  short-lived-token behavior so the access split is exercised in dev too.
- The exact production bucket, CDN host, and credential source are deployment configuration owned
  by `08/01-01`; this file fixes only that production is S3-compatible.

---

## Nuxt Runtime Config

Both Nuxt apps read configuration through `runtimeConfig`, which is the only supported way to vary
values per environment. `runtimeConfig.public.*` is available to the browser; everything else is
server-only and must never be exposed to the client.

### Storefront (`web/storefront`)

| Runtime config | Environment variable | Dev default | Purpose |
|---|---|---|---|
| `runtimeConfig.apiBaseUrl` (private) | `NUXT_API_BASE_URL` | `http://localhost:8080` | Server-side SSR calls to `/api/v1`. |
| `runtimeConfig.public.apiBaseUrl` | `NUXT_PUBLIC_API_BASE_URL` | `http://localhost:8080` | Browser calls to `/api/v1`. |
| `runtimeConfig.public.siteUrl` | `NUXT_PUBLIC_SITE_URL` | `http://localhost:3000` | Canonical/SEO base. |
| `runtimeConfig.public.defaultLocale` | `NUXT_PUBLIC_DEFAULT_LOCALE` | `en` | Fallback locale (`03/03-03`). |
| `runtimeConfig.public.branding` | `NUXT_PUBLIC_BRANDING_JSON` | see below | Per-store branding defaults. |
| `runtimeConfig.public.storeHeader` | `NUXT_PUBLIC_STORE_HEADER` | `X-Store` | Dev-only store override header. |

Per-store branding is a single JSON object bound to `runtimeConfig.public.branding`; it is a
**fallback for SSR before store resolution** and is overridden at runtime by the store record
returned from the API. The shape is:

```json
{
  "name": "Resys",
  "logoUrl": "/branding/resys-logo.svg",
  "primaryColor": "#0f766e",
  "faviconUrl": "/favicon.ico"
}
```

Store resolution rules:

- In production, the storefront resolves the store from the **request domain** (mapped to `Store`
  in the database); branding then comes from the store record, not from this fallback.
- In development, a developer selects a store with the `X-Store` header (or its configured name
  from `Stores:HeaderName`), so two seeded stores (`05/01-04`) can be exercised from one host.
- The `branding` object is a compile-time-safe default only; it never overrides a resolved store.

### Admin (`web/admin`)

| Runtime config | Environment variable | Dev default | Purpose |
|---|---|---|---|
| `runtimeConfig.apiBaseUrl` (private) | `NUXT_API_BASE_URL` | `http://localhost:8080` | SSR calls to `/api/admin`; attaches cookies/CSRF server-side. |
| `runtimeConfig.public.apiBaseUrl` | `NUXT_PUBLIC_API_BASE_URL` | `http://localhost:8080` | Browser calls to `/api/admin`. |
| `runtimeConfig.public.adminApiBase` | `NUXT_PUBLIC_ADMIN_API_BASE` | `/api/admin` | Route prefix for the admin surface. |
| `runtimeConfig.public.defaultStore` | `NUXT_PUBLIC_DEFAULT_STORE` | `resys-main` | Initial store in the multi-store switcher. |
| `runtimeConfig.public.branding` | `NUXT_PUBLIC_BRANDING_JSON` | admin branding object | Back-office title/logo. |

The admin JWT lives in an httpOnly cookie scoped to `/api/admin`, so the Nuxt server reads it and
attaches the CSRF header during SSR (`03/04-03`). No token is placed in `runtimeConfig.public`.

---

## Development Defaults Summary

| Concern | Value | Set in |
|---|---|---|
| API HTTP port | `8080` | `ASPNETCORE_URLS` (`05/03-01`) |
| Storefront port | `3000` | Nuxt dev server |
| Admin port | `3001` | Nuxt dev server |
| Database | `localhost:5432/resys_eshop` | `ConnectionStrings__ResysDb` |
| Object storage | `local`, `./.data/storage` | `Storage__Provider`, `Storage__Local__RootPath` |
| Email | SMTP catcher on `localhost:1025` | `Email__Provider=smtp` |
| Store selection | `X-Store` header | `Stores__HeaderName` |

`ASPNETCORE_ENVIRONMENT=Development` enables the runtime demo seeder and the `local` storage
backend; neither is enabled in `Production` (`05/01-04`).

## Open Questions

1. The production secret store and the exact variables injected at deploy time are owned by
   `08/01-02`; the variable names above are the contract the deployment must satisfy.
2. The production bucket, CDN host, and signing key rotation schedule are owned by `08/01-01` and
   `03/04-03`; this file fixes the `s3` provider, the key scheme, and the public/private split.
3. Whether per-store branding is delivered as a fallback JSON object or fetched on first render is
   finalized with the storefront theming in `04/01-04`; the override-by-store-record rule is fixed.
