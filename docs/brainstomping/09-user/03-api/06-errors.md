# API Reference — Errors

The RFC 9457 problem-details envelope, the shared status-code table, common error codes, validation field errors, and rate-limit responses for both REST surfaces.

**Links:** [REST Public API](../../03-architecture/04-integration/01-rest-public.md) · [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md) · [Authentication](../../03-architecture/04-integration/03-auth.md) · [OpenAPI & Scalar](../../03-architecture/04-integration/04-openapi.md) · [Auth API](./01-auth.md) · [User docs index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the published reference for how resys-eshop reports failures. Every failure on both
`/api/v1` and `/api/admin` uses the same RFC 9457 `application/problem+json` envelope defined in
`03/04-01` and `03/04-02`. A client can parse one shape regardless of surface; the `status` member
repeats the HTTP status code and the `errors` member carries field-level validation messages.

The single rule: **no endpoint returns a bare error string; every failure is a problem document,
and a state change that fails changes nothing.**

---

## The Error Envelope

```json
{
  "type": "https://docs.resys-eshop.example/errors/validation",
  "title": "One or more validation errors occurred.",
  "status": 422,
  "detail": "The request body failed validation.",
  "instance": "/api/v1/cart/line_items",
  "errors": { "quantity": ["must be greater than 0"] }
}
```

| Member | Type | Meaning |
|---|---|---|
| `type` | string (URI) | Stable identifier for the error category. |
| `title` | string | Short, human-readable summary. |
| `status` | integer | The HTTP status code, repeated. |
| `detail` | string | Explanation specific to this occurrence. |
| `instance` | string | The request path that produced the failure. |
| `errors` | object | Field name to array of messages; present on `422`. |

The `Content-Type` is `application/problem+json`. Sensitive values — passwords, PAN, and PII — are
never included in any member (`03/04-03`).

---

## Status Codes

| Status | Meaning | Where it appears |
|---|---|---|
| 400 | Malformed request or missing required header (for example `Idempotency-Key`). | Public and admin. |
| 401 | Missing or invalid access token on an authenticated endpoint. | Both surfaces. |
| 402 | Payment required or payment authorization declined. | Checkout. |
| 403 | Authenticated but not permitted: another account's resource, an unassigned store, missing/mismatched CSRF, a cross-store transfer, or a platform-only route. | Both surfaces. |
| 404 | Resource not found in the resolved store. | Both surfaces. |
| 409 | Conflict: duplicate review or email, unavailable stock, stale `If-Match`, or an invalid state transition. | Both surfaces. |
| 413 | An uploaded image exceeds the size limit. | Admin image upload. |
| 422 | Semantic validation failure; field errors in `errors`. | Both surfaces. |
| 423 | Account locked out or deactivated; login is rejected. | Public and admin login. |
| 429 | Rate limit exceeded. | Both surfaces. |

The same status table is stated per surface in `03/04-01` and `03/04-02`; this page is the combined
reference.

---

## Common Error Codes

`type` values are stable and safe to branch on. The common ones:

| Error type suffix | Status | Typical trigger |
|---|---|---|
| `/errors/validation` | 422 | A field fails a rule; `errors` names it. |
| `/errors/not-found` | 404 | The id or slug does not exist in the resolved store. |
| `/errors/conflict` | 409 | Duplicate or illegal state transition. |
| `/errors/forbidden` | 403 | Role, store scope, CSRF, or platform-only violation. |
| `/errors/unauthorized` | 401 | Token missing, expired, or invalid. |
| `/errors/payment` | 402 | Authorization declined or payment required. |
| `/errors/locked` | 423 | Account deactivated or locked out. |
| `/errors/rate-limited` | 429 | A rate-limit policy was exceeded. |
| `/errors/payload-too-large` | 413 | The uploaded file exceeds the limit. |

A client should treat an unknown type as a generic failure and show `detail`.

---

## Validation Errors

`422` is the only status that populates `errors`. Each key is the request field, each value is the
list of failed rules:

```json
{
  "type": "https://docs.resys-eshop.example/errors/validation",
  "title": "One or more validation errors occurred.",
  "status": 422,
  "detail": "The request body failed validation.",
  "instance": "/api/v1/account/register",
  "errors": {
    "email": ["must be a valid email address", "is already registered"],
    "password": ["must be at least 12 characters"]
  }
}
```

The admin editors map `errors` to the affected fields and keep the rest of the form intact.

---

## Rate-Limit Responses

Rate limiting is partitioned by IP for anonymous callers and by user id once signed in. Exceeding a
limit returns `429` with a `Retry-After` header (seconds) so a client can back off (`03/04-03`).

```http
HTTP/1.1 429 Too Many Requests
Content-Type: application/problem+json
Retry-After: 30

{ "type": "https://docs.resys-eshop.example/errors/rate-limited", "title": "Too many requests.", "status": 429, "detail": "Rate limit exceeded; retry after the indicated delay.", "instance": "/api/v1/account/login" }
```

| Policy | Scope | Partition | Documented default |
|---|---|---|---|
| Public read | catalog, search, categories, reviews | IP | 100 requests / minute |
| Public auth | register, login, refresh | IP | 10 requests / minute |
| Cart / checkout | cart and checkout mutations | cart token or user | 20 requests / minute |
| Admin | all `/api/admin` routes | admin user | 600 requests / minute |

Login failures are additionally counted toward Identity lockout; after repeated failures the account
returns `423` rather than `401`.

---

## Handling Failures

1. Read `status`; it repeats the HTTP code for clients that only parse the body.
2. On `401`, refresh the access token once and retry; if refresh fails, sign in again.
3. On `403`, check the store scope, the CSRF header on admin mutations, and whether the route is
   platform-only.
4. On `409`, reload the resource, discard the stale `If-Match`, and retry if the transition is still
   legal; for a record referenced by order history, archive or disable instead of deleting.
5. On `422`, bind `errors` to the offending form fields.
6. On `429`, honor `Retry-After` before retrying.
7. On `413`, resize or compress the image and upload it again.

A failed mutation is atomic: the server rolls back the change and any audit row written with it
(`03/04-03`).

---

## Open Questions

1. Whether error `type` URIs are versioned alongside `/api/v2` if and when that surface is created is
   deferred to ADR 0004's evolution rule; the identifiers are stable for `/api/v1` and
   `/api/admin`.
2. The exact numeric rate-limit values are configuration owned by `05/01-02`; the partition keys and
   the `429` envelope are fixed in `03/04-03`.

## Links

- [REST Public API](../../03-architecture/04-integration/01-rest-public.md)
- [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md)
- [Authentication & Authorization](../../03-architecture/04-integration/03-auth.md)
- [OpenAPI & Scalar](../../03-architecture/04-integration/04-openapi.md)
- [Auth API](./01-auth.md)
- [Catalog API](./02-catalog.md)
- [Cart & Checkout API](./03-cart-checkout.md)
- [Account API](./04-account.md)
- [Admin API](./05-admin-api.md)
- [Troubleshooting](../04-support/02-troubleshooting.md)
- [Security requirements](../../01-requirements/03-nonfunctional/02-security.md)
- [Design spec](../../11-spec/README.md) — §7 (contracts), §12
  (API parity verification).
