# API Reference — Authentication

Register and sign in a customer, refresh and revoke tokens, and open an admin cookie session, with the exact `curl` calls and JSON bodies the platform accepts.

**Links:** [REST Public API](../../03-architecture/04-integration/01-rest-public.md) · [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md) · [Authentication](../../03-architecture/04-integration/03-auth.md) · [Catalog API](./02-catalog.md) · [Error Reference](./06-errors.md) · [User docs index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the published reference for the authentication endpoints on both REST surfaces. It
mirrors `03/04-01` (public account/auth), `03/04-02` (admin), and `03/04-03` (the token model). The
base path is `/api/v1` for the storefront and shared client, and `/api/admin` for the back-office.
Every endpoint below uses the literal token form `METHOD /api/...` so it can be compared verbatim
with the architecture contract.

The single rule: **one JWT scheme authenticates both surfaces; the admin cookie is only a transport
for that JWT, and store authority is re-checked from the database on every request.**

---

## Bearer Header Format

Storefront and shared-client calls carry the short-lived access token as a bearer credential:

```http
Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6InJlc3lzLWFwaSJ9.eyJzdWIiOiI3ZjNjMmE5ZS0wZDUxLTRjOGItOWY2MC0yYjFhNGU2ZDhjMDEiLCJlbWFpbCI6InNob3BwZXJAZXhhbXBsZS5jb20iLCJyb2xlIjoiY3VzdG9tZXIiLCJqdGkiOiJhYTEiLCJpc3MiOiJodHRwczovL2FwaS5leGFtcGxlLmNvbSIsImF1ZCI6InJlc3lzLWFwaSIsImlhdCI6MTc1ODYxOTIwMCwiZXhwIjoxNzU4NjIwMTAwfQ.c2lnbmF0dXJl
Content-Type: application/json
```

The access token is a 15-minute RS256 JWT; its claims are `sub`, `email`, `role`, `jti`, `iss`,
`aud`, `iat`, and `exp`. It never carries store authority. The refresh token is an opaque 256-bit
value valid for 30 days and is rotated on every use (`03/04-03`).

---

## Public Auth Endpoints

| Endpoint | Auth | Request | Response | Status |
|---|---|---|---|---|
| `POST /api/v1/account/register` | public | `RegisterRequest` (`email`, `password`, `first_name?`, `last_name?`) | `AuthTokenDto` | 201, 409, 422 |
| `POST /api/v1/account/login` | public | `LoginRequest` (`email`, `password`) | `AuthTokenDto` | 200, 401, 423 |
| `POST /api/v1/account/refresh` | public | `RefreshRequest` (`refresh_token`) | `AuthTokenDto` | 200, 401 |
| `POST /api/v1/account/logout` | customer | `RefreshRequest` (`refresh_token`) | — | 204, 401 |

`AuthTokenDto` carries `access_token`, `expires_in`, `refresh_token`, and the account summary. A
duplicate email on register is `409`; malformed or weak input is `422`; a deactivated or locked-out
account receives `423` at login. Refresh rotates the presented token and returns a new pair; a token
that was already rotated is treated as theft and revokes the whole token family (`03/04-03`).

---

## Admin Auth Endpoints

The admin is a browser client: the API sets httpOnly cookies and never returns the access token in
the response body. Mutating admin calls must add the double-submit `X-CSRF-Token` header equal to
the `resys_admin_csrf` cookie.

| Endpoint | Auth | Request | Response | Status |
|---|---|---|---|---|
| `POST /api/admin/auth/login` | public | `AdminLoginRequest` (`email`, `password`) | `AdminSessionDto` + cookies | 200, 401, 423 |
| `POST /api/admin/auth/refresh` | refresh cookie | — | `AdminSessionDto` + rotated cookies | 200, 401 |
| `GET /api/admin/auth/session` | admin cookie | — | `AdminSessionDto` | 200, 401 |
| `POST /api/admin/auth/logout` | admin cookie + CSRF | — | — | 204, 401, 403 |

**Cookies set at admin login:**

| Cookie | Content | Attributes |
|---|---|---|
| `resys_admin_at` | Access JWT | `HttpOnly; Secure; SameSite=Strict; Path=/api/admin; Max-Age=900` |
| `resys_admin_rt` | Refresh token | `HttpOnly; Secure; SameSite=Strict; Path=/api/admin/auth; Max-Age=2592000` |
| `resys_admin_csrf` | Double-submit CSRF token | `Secure; SameSite=Strict; Path=/` (readable by the app) |

---

## Example 1 — Register a customer

```bash
curl -sS -X POST https://api.example.com/api/v1/account/register \
  -H 'Content-Type: application/json' \
  -H 'Accept-Language: en-US' \
  -d '{ "email": "shopper@example.com", "password": "correct-horse-battery", "first_name": "Ada", "last_name": "Lovelace" }'
```

Response `201 Created`:

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiI3ZjNjMmE5ZS0wZDUxLTRjOGItOWY2MC0yYjFhNGU2ZDhjMDEiLCJlbWFpbCI6InNob3BwZXJAZXhhbXBsZS5jb20iLCJyb2xlIjoiY3VzdG9tZXIifQ.c2ln",
  "expires_in": 900,
  "refresh_token": "0f6f5e4d-aaaa-bbbb-cccc-ddddeeeeffff",
  "account": {
    "id": "7f3c2a9e-0d51-4c8b-9f60-2b1a4e6d8c01",
    "email": "shopper@example.com",
    "first_name": "Ada",
    "last_name": "Lovelace"
  }
}
```

A duplicate email returns `409`; a missing field or a password below policy returns `422`.

---

## Example 2 — Log in

```bash
curl -sS -X POST https://api.example.com/api/v1/account/login \
  -H 'Content-Type: application/json' \
  -d '{ "email": "shopper@example.com", "password": "correct-horse-battery" }'
```

Response `200 OK`: an `AuthTokenDto` identical in shape to the register response. Wrong credentials
return `401`; a deactivated or locked-out account returns `423`.

---

## Example 3 — Refresh and log out

```bash
curl -sS -X POST https://api.example.com/api/v1/account/refresh \
  -H 'Content-Type: application/json' \
  -d '{ "refresh_token": "0f6f5e4d-aaaa-bbbb-cccc-ddddeeeeffff" }'

curl -sS -X POST https://api.example.com/api/v1/account/logout \
  -H 'Authorization: Bearer <access_token>' \
  -H 'Content-Type: application/json' \
  -d '{ "refresh_token": "0f6f5e4d-aaaa-bbbb-cccc-ddddeeeeffff" }'
```

`refresh` returns `200` with a rotated pair. `logout` returns `204 No Content` and deletes the
presented refresh token. A token that was already rotated returns `401` and revokes the family.

---

## Example 4 — Open an admin session

```bash
curl -sS -i -X POST https://api.example.com/api/admin/auth/login \
  -H 'Content-Type: application/json' \
  -d '{ "email": "admin@example.com", "password": "s3cret-admin-pass" }'
```

Response `200 OK` with `Set-Cookie` headers and a body (no access token):

```json
{
  "admin": {
    "id": "3f2e1d0c-aaaa-bbbb-cccc-ddddeeeeffff",
    "email": "admin@example.com",
    "roles": ["admin"],
    "stores": [
      { "id": "d0d0d0d0-0000-0000-0000-000000000001", "code": "acme-us", "name": "Acme US" }
    ]
  },
  "csrf_token": "b7d1c2e3-4f50-4617-8a92-0b1c2d3e4f50"
}
```

Then confirm the session and close it:

```bash
curl -sS https://api.example.com/api/admin/auth/session --cookie 'resys_admin_at=<jwt>'

curl -sS -i -X POST https://api.example.com/api/admin/auth/logout \
  --cookie 'resys_admin_at=<jwt>; resys_admin_csrf=b7d1c2e3-4f50-4617-8a92-0b1c2d3e4f50' \
  -H 'X-CSRF-Token: b7d1c2e3-4f50-4617-8a92-0b1c2d3e4f50'
```

`session` returns `200` with an `AdminSessionDto`. `logout` returns `204`, clears all three cookies,
and deletes the refresh token. A missing or mismatched CSRF header is `403`.

---

## Status Codes

| Status | Meaning |
|---|---|
| 200 | Login, refresh, or admin session succeeded. |
| 201 | Registration created the account. |
| 204 | Logout revoked the refresh token. |
| 401 | Missing/invalid token or wrong credentials. |
| 403 | Missing or mismatched `X-CSRF-Token` on a mutating admin call. |
| 409 | Registering an email that already exists. |
| 422 | Request body failed validation. |
| 423 | Account locked out or deactivated. |
| 429 | Too many auth attempts from this client. |

Failures use the RFC 9457 envelope described in `09/03-06`. Rate limits are per IP for anonymous
callers and per user once authenticated (`03/04-03`).

---

## Open Questions

1. The concrete JWT signing key algorithm and rotation schedule are confirmed with deployment
   configuration in `05/01-02`; RS256 with a published JWKS is the fixed approach.
2. Admin impersonation of a customer for support is out of scope; if added it must be a separately
   audited action owned by `03/04-02`.
3. The exact rate-limit windows (fixed versus sliding) are configuration owned by `05/01-02`; the
   partition keys and the `429` contract are fixed in `03/04-03`.

## Links

- [REST Public API](../../03-architecture/04-integration/01-rest-public.md)
- [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md)
- [Authentication & Authorization](../../03-architecture/04-integration/03-auth.md)
- [Catalog API](./02-catalog.md)
- [Cart & Checkout API](./03-cart-checkout.md)
- [Account API](./04-account.md)
- [Error Reference](./06-errors.md)
- [Security requirements](../../01-requirements/03-nonfunctional/02-security.md)
- [Design spec](../../11-spec/README.md) — §7 (auth and security),
  §10 (security).
