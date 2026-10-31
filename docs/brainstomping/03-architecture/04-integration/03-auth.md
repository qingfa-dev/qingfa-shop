# Authentication & Authorization

The Identity user model, JWT issuance and refresh, the admin httpOnly-cookie flow, roles and store-scoped authorization, CSRF/CORS/rate-limit rules, and the audit-log fields.

**Links:** [REST public API](./01-rest-public.md) · [REST admin API](./02-rest-admin.md) · [OpenAPI & Scalar](./04-openapi.md) · [Multi-store scoping](../03-data/02-multi-store.md) · [App topology](../01-overview/02-app-topology.md) · [ADR 0004 — Headless API and JWT auth](../../00-governance/01-adr/04-headless-jwt.md) · [Security requirements](../../01-requirements/03-nonfunctional/02-security.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file specifies how resys-eshop authenticates and authorizes callers on both REST surfaces. It
defines the ASP.NET Identity user model, how JWT access and refresh tokens are issued and rotated,
how the admin browser stores its token in an httpOnly cookie without becoming a server session, the
`customer`/`admin` roles plus per-store admin assignment, the CSRF/CORS/rate-limit protections, and
the fields written to the audit log. The public contract is `03/04-01`, the admin contract is
`03/04-02`, and the OpenAPI document is `03/04-04`. The decision record is ADR 0004.

The single rule: **one JWT scheme authenticates both surfaces; the admin cookie is a transport for
the JWT, and store authority is re-checked from the database on every request so revocation is
immediate.**

---

## Identity User Model

Identity is provided by ASP.NET Identity for the whole platform; a user is platform-wide and may
shop in multiple stores (`03/03-02`).

| Member | Source | Meaning |
|---|---|---|
| `Id` | `User` | Primary key; appears as `sub` in the JWT. |
| `Email` | `User` | Unique login identifier; used for account recovery. |
| `UserName` | `User` | Unique; defaults to the email. |
| `PasswordHash` | Identity | PBKDF2 hash managed by Identity; the plaintext is never stored or logged. |
| `SecurityStamp` | Identity | Rotated to invalidate outstanding tokens after a credential or role change. |
| `Locale?` / `Currency?` | `User` | Per-user display preferences; never authoritative for pricing. |
| `IsActive` | `User` | Deactivating blocks login and API access without deleting history. |
| `Roles` | Identity | `customer` and/or `admin` (`UserRole`). |
| `AdminStoreAssignment` | Accounts | `(UserId, StoreId)` rows granting per-store admin scope (`03/03-01`). |
| `RefreshTokens` | Identity `AspNetUserTokens` | Hashed refresh tokens under provider `resys`, name `refresh_token`; no custom table is added. |

Policies: passwords follow Identity's configured length/complexity policy; repeated failures trigger
lockout; a deactivated or locked-out account is rejected with `423`; email confirmation is required
before a `customer` may place an order. Identity tables are platform-wide and exempt from the
per-store query filter (`03/03-02`).

---

## Token Model

Two tokens are issued per successful authentication:

| Token | Form | Lifetime | Storage |
|---|---|---|---|
| Access | JWT, RS256-signed | 15 minutes | Admin: httpOnly cookie. Storefront/shared: held by the client and sent as `Authorization: Bearer`. |
| Refresh | Opaque 256-bit random, stored hashed | 30 days, rotating | Admin: httpOnly cookie. Storefront/shared: returned in the body and sent to the refresh endpoint. |

Access-token claims:

| Claim | Value |
|---|---|
| `sub` | User id. |
| `email` | Account email. |
| `role` | `customer`, `admin`, or both. |
| `jti` | Unique token id (used for denylist and audit correlation). |
| `iss` / `aud` | API issuer and `resys-api` audience. |
| `iat` / `exp` | Issued-at and expiry instants. |

The token deliberately **does not carry store authority**. Store scope is resolved per request and
checked against `AdminStoreAssignment`, so unassigning an admin takes effect on the next request
without waiting for token expiry (`03/03-02`). Tokens are signed with an asymmetric key; the public
key is published at the JWKS endpoint for verification by other services. No PAN or PII is written
to logs at any point (ADR 0004).

---

## JWT Issuance and Refresh

1. **Issuance.** A successful credential check (public login or admin login) mints an access JWT and
   a refresh token. The refresh token is hashed and written to `AspNetUserTokens`; the plaintext is
   returned only once.
2. **Refresh.** The client presents the refresh token. The server hashes it, looks it up, verifies
   the user is active and the security stamp is unchanged, then **rotates**: the presented token is
   deleted and a new refresh token plus a new access token are issued.
3. **Reuse detection.** Presenting a refresh token that was already rotated (not found while the
   user has an active token family) is treated as theft: all of the user's refresh tokens are
   revoked and the security stamp is rotated, invalidating every outstanding access token.
4. **Revocation.** Deactivation, password change, or role change rotates the security stamp and
   deletes refresh tokens, so all sessions end at the next validation.
5. **Validation.** `AddJwtBearer` validates signature, issuer, audience, lifetime, and the security
   stamp; a token past expiry returns `401` and the client refreshes silently.

The public auth endpoints are `POST /api/v1/account/register`, `POST /api/v1/account/login`,
`POST /api/v1/account/refresh`, and `POST /api/v1/account/logout` (`03/04-01`).

---

## Admin httpOnly-Cookie Flow

The admin is a browser application, so it stores the JWT in cookies rather than in JavaScript-reachable
storage (ADR 0004). The bearer handler reads the access token from the cookie, so the server-side
authorization code is identical to the public surface.

**Cookies set at admin login:**

| Cookie | Content | Attributes |
|---|---|---|
| `resys_admin_at` | Access JWT | `HttpOnly; Secure; SameSite=Strict; Path=/api/admin; Max-Age=900` |
| `resys_admin_rt` | Refresh token | `HttpOnly; Secure; SameSite=Strict; Path=/api/admin/auth; Max-Age=2592000` |
| `resys_admin_csrf` | Random double-submit token | `Secure; SameSite=Strict; Path=/` (readable by the app for the CSRF header) |

**Flow:**

1. `POST /api/admin/auth/login` verifies credentials, sets the three cookies, and returns the admin
   summary plus the CSRF token. No token is placed in the response body.
2. The admin Nuxt server (SSR) reads `resys_admin_csrf` server-side and attaches it as the
   `X-CSRF-Token` header on mutating calls; client-side calls read the non-httpOnly cookie and do
   the same. The access JWT is never exposed to JavaScript.
3. `POST /api/admin/auth/refresh` rotates the tokens and re-sets the cookies; it is called
   transparently when an access token expires.
4. `GET /api/admin/auth/session` returns the current admin summary and a fresh CSRF token for page
   loads.
5. `POST /api/admin/auth/logout` clears all three cookies, deletes the refresh token, and returns
   `204`.

Admin auth endpoints:

| Endpoint | Auth | Request | Response | Status |
|---|---|---|---|---|
| `POST /api/admin/auth/login` | public | `AdminLoginRequest` (`email`, `password`) | `AdminSessionDto` + cookies | 200, 401, 423 |
| `POST /api/admin/auth/refresh` | refresh cookie | — | `AdminSessionDto` + rotated cookies | 200, 401 |
| `GET /api/admin/auth/session` | admin cookie | — | `AdminSessionDto` | 200, 401 |
| `POST /api/admin/auth/logout` | admin cookie + CSRF | — | — | 204, 401, 403 |

---

## Roles and Store-Scoped Authorization

- **Roles.** `customer` grants storefront account operations; `admin` grants access to
  `/api/admin`. A user may hold both; effective authority is the union of granted scopes
  (`03/03-02`).
- **Store scope.** Every store-scoped admin route carries the selected store (the `X-Store` header,
  `03/04-02`). The API resolves it, then calls
  `IAdminAccessPolicy.CanManageStoreAsync(userId, storeId)`; a `false` result is `403`.
- **Store Admin.** Assigned to one or more stores through `AdminStoreAssignment`; authorized only
  within those stores.
- **Platform Admin.** Holds the `admin` role without requiring an assignment; spans all stores and
  is the only caller permitted on platform-level routes (creating stores, changing domains,
  assigning admins).
- **Enforcement points.** Role check at the route group; store scope at a resource filter before the
  handler; module queries additionally carry the resolved store so a missing check cannot leak
  another store's rows (`03/03-02`). Authorization tests live in `06/01-01`.

---

## CSRF

Cookie-based admin calls are vulnerable to cross-site request forgery, so every **state-changing**
admin request (`POST`, `PUT`, `PATCH`, `DELETE`) must present `X-CSRF-Token` equal to the
`resys_admin_csrf` cookie (double-submit). A missing or mismatched token is `403` and the call is
not executed. Safe methods (`GET`, `HEAD`) are exempt. The public `/api/v1` surface authenticates
with a bearer token rather than a cookie, so it does not require a CSRF token; the cart token is not
an authentication credential.

---

## CORS

CORS is an allow-list, never a wildcard, and is configured per environment (`03/01-02`):

- Allowed origins: exactly the storefront and admin origins (`http://localhost:3000` /
  `http://localhost:3001` in development; the production hosts in production).
- `Access-Control-Allow-Credentials: true` for the admin origin so cookies are sent; because
  credentials are enabled, no wildcard origin or wildcard method is permitted.
- Allowed request headers include `Authorization`, `Content-Type`, `X-CSRF-Token`, `X-Store`,
  `X-Cart-Token`, `Idempotency-Key`, `If-Match`, and `Accept-Language`.
- Allowed methods are limited to those the front-ends use; preflight responses are cached briefly.

---

## Rate Limiting

Rate limiting applies per client and is partitioned by IP address for anonymous callers and by
authenticated user id once signed in. Exceeding a limit returns `429` with a `Retry-After` header.

| Policy | Scope | Partition | Limit (documented default) |
|---|---|---|---|
| Public read | catalog, search, categories, reviews | IP | 100 requests / minute |
| Public auth | register, login, refresh | IP | 10 requests / minute |
| Cart / checkout | cart and checkout mutations | cart token or user | 20 requests / minute |
| Admin | all `/api/admin` routes | admin user | 600 requests / minute |

Limits are configuration values finalized in `05/01-02`; the partition keys and the `429` contract
are fixed here. Login failures are additionally counted toward Identity lockout.

---

## Audit Log

Every admin mutation writes one append-only `AuditLog` row in the same transaction as the change it
describes (`03/03-01`, `03/04-02`).

| Field | Type | Meaning |
|---|---|---|
| `Id` | `AuditLogId` | Identity. |
| `StoreId?` | `StoreId` | The store scope of the mutation; `null` for platform-level actions. |
| `ActorId` | `UserId` | The authenticated admin who performed the action. |
| `Action` | string | Verb plus entity, for example `product.update`, `order.fulfill`, `store.assign_admin`. |
| `EntityType` | string | The affected aggregate, for example `Product`, `Order`, `Store`. |
| `EntityId` | string | The affected row id. |
| `OccurredAtUtc` | instant | Written through `IClock`. |

The row records who/what/when; secret values and PAN/PII are never written. Auth failures and
lockouts are written to the security log rather than the audit table, and an audit write that fails
rolls back the mutation it accompanies.

## Open Questions

1. The concrete key algorithm and rotation schedule for JWT signing are confirmed with the
   deployment configuration in `05/01-02`; RS256 with a published JWKS is the fixed approach.
2. Whether an admin may impersonate a customer for support is out of scope; if added it must be a
   separately audited action owned by `03/04-02`.
3. The exact rate-limit windows (fixed vs. sliding) are configuration owned by `05/01-02`; the
   partition keys and `429` behavior are fixed here.

## Links

- [REST public API](./01-rest-public.md)
- [REST admin API](./02-rest-admin.md)
- [OpenAPI & Scalar](./04-openapi.md)
- [App topology](../01-overview/02-app-topology.md)
- [Module contracts](../02-components/02-module-contracts.md)
- [Entity model](../03-data/01-entities.md)
- [Multi-store scoping](../03-data/02-multi-store.md)
- [Accounts requirements](../../01-requirements/02-functional/10-accounts.md)
- [Security requirements](../../01-requirements/03-nonfunctional/02-security.md)
- [ADR 0004 — Headless API and JWT auth](../../00-governance/01-adr/04-headless-jwt.md)
- [Design spec](../../11-spec/README.md) — §7 (auth and
  security), §10 (security).
- Public API: `03/04-01`; admin API: `03/04-02`; OpenAPI: `03/04-04`; authorization tests:
  `06/01-01`; environment limits: `05/01-02`; published reference: `09/03-01`.
