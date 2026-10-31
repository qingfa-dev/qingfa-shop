# Admin Onboarding

Sign in to the back-office, understand the cookie session, switch stores, and learn the role model.

**Links:** [Admin Flows](../../04-design/02-admin/01-flows.md) · [Authentication](../../03-architecture/04-integration/03-auth.md) · [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md) · [Admin Stories](../../01-requirements/04-user-stories/03-admin.md) · [User docs index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This guide is the entry point for staff who use `web/admin`, the Nuxt 4 + PrimeVue back-office. It
covers signing in, how the session is stored, how to pick a store, and what the two roles may do. The
journey is `04/02-01` Journey 0; the screen inventory is `04/02-02`; the HTTP contract is `03/04-02`;
the token and authorization rules are `03/04-03`. The admin is a headless HTTP client of `/api/admin`
(ADR 0004) — there is no embedded UI and no direct database access.

Read this file first, then continue with `09/01-02` (catalog), `09/01-03` (orders), `09/01-04`
(fulfillment and returns), and `09/01-05` (settings).

---

## Signing In

1. Open the admin app and go to `/login` (in development this is the admin origin, for example
   `http://localhost:3001`).
2. Enter the email and password of an account that holds the `admin` role.
3. Submit the form. The app calls `POST /api/admin/auth/login` with `{ "email", "password" }`.
4. On success the API returns `200` with an `AdminSessionDto` **and** sets three cookies; the app
   redirects to the dashboard (`/`). No access token is returned in the response body.

| Outcome | Meaning | What to do |
|---|---|---|
| `200` | Credentials valid and the account may reach the admin. | Continue to the dashboard. |
| `401` | Email or password is wrong. | Re-enter the credentials. |
| `423` | The account is deactivated or locked out after repeated failures. | Ask a platform admin to reactivate it or wait out the lockout. |
| `429` | Too many auth attempts from this address. | Wait for the `Retry-After` window, then retry. |

A `customer`-only account is rejected: `/api/admin` requires the `admin` role plus a store scope
(`03/04-03`). If sign-in succeeds but every store is missing from the switcher, the account has no
`AdminStoreAssignment`; a platform admin must assign one (`09/01-05`).

---

## The Session

The admin stores its JWT in **httpOnly cookies**, never in JavaScript-reachable storage, so a script
on the page cannot read the token (ADR 0004). Three cookies are set at login:

| Cookie | Content | Notes |
|---|---|---|
| `resys_admin_at` | Access JWT, 15-minute lifetime. | Sent automatically on `/api/admin` calls. |
| `resys_admin_rt` | Refresh token, 30-day rotating lifetime. | Only sent to `/api/admin/auth`. |
| `resys_admin_csrf` | Double-submit CSRF token. | Readable by the app; attached as a header on mutations. |

How the session behaves during a working day:

- **Boot.** On every page load the shell calls `GET /api/admin/auth/session`. If there is no valid
  session the request returns `401` and the app redirects to `/login` before any admin chrome
  renders.
- **Silent refresh.** When the access token nears expiry the app calls
  `POST /api/admin/auth/refresh`; the API rotates both tokens and re-sets the cookies. The original
  request is retried once, so a short expiry is invisible.
- **CSRF.** Every state-changing call (`POST`, `PUT`, `PATCH`, `DELETE`) must carry the
  `X-CSRF-Token` header equal to the `resys_admin_csrf` cookie. A missing or mismatched token is
  `403` and the change is not executed. Safe reads (`GET`, `HEAD`) do not need it.
- **Concurrency.** Order and stock mutations also send an `If-Match` row version. A stale version is
  `409` and changes nothing; reload the record and retry.
- **Sign-out.** `POST /api/admin/auth/logout` clears all three cookies, deletes the refresh token,
  and returns `204`. The app returns to `/login`.

Because store authority is **not** carried in the token, it is re-checked from the database on every
request. Unassigning an admin from a store takes effect on their next call, not when the token
expires (`03/03-02`). A password change, role change, or deactivation rotates the security stamp and
ends all sessions at the next validation.

---

## The Store Switcher

The selected store is the scope every admin action runs in. It is not part of the URL and not part of
the token.

1. The shell loads the switcher from `GET /api/admin/stores`. A **store admin** sees only the stores
   they are assigned to; a **platform admin** sees every store (`03/04-02`).
2. Selecting a store writes it to the app's store context and sets the `X-Store` header (store id or
   code) on every later `/api/admin` call.
3. The current screen re-reads its data under the new scope. Switching away from an editor with
   unsaved changes prompts for confirmation before discarding them (`04/02-02`).
4. If the caller is not assigned to the selected store the API returns `403` and the action is not
   performed.

The selected store is the only scope an admin can act in. There is no route-segment store id; the
`X-Store` header is the single mechanism, and it is authorization-checked on the server. A store
admin who needs another store's data must be assigned to it by a platform admin.

Platform-level routes — creating a store, changing its domains, currencies, or locales, and
assigning admins — are reachable **only** by a platform admin. A store admin receives `403` on those
calls even for a store they manage (`03/03-02`).

---

## Roles and Authority

Two roles exist, and a user may hold both:

| Role | Grants | Notes |
|---|---|---|
| `customer` | Storefront account operations under `/api/v1`. | Not sufficient for the admin app. |
| `admin` | Access to `/api/admin`. | Must also be scoped to a store to act on store data. |

Effective authority is the union of granted scopes. Two admin flavours are common:

- **Store Admin.** Holds `admin` and is assigned to one or more stores through
  `AdminStoreAssignment`. Authorized only inside those stores. Cannot create stores or change store
  domains, currencies, locales, or grant platform-wide authority.
- **Platform Admin.** Holds `admin` without requiring an assignment. Spans all stores and is the
  only caller allowed on platform-level routes, including `POST /api/admin/users` with the `admin`
  role or store assignments.

Every mutation writes an append-only `AuditLog` row in the same transaction as the change — actor,
action, entity, store, and timestamp (`03/04-03`). There is no separate approval step; the audit
trail is the record of who did what.

---

## Reading Errors

Failures use RFC 9457 problem details (`03/04-02`). The status codes an admin sees most:

| Status | Typical cause | What to do |
|---|---|---|
| `401` | Session expired or token invalid. | The app refreshes; if it still fails, sign in again. |
| `403` | Missing role, unassigned store, missing/mismatched CSRF, or a platform-only route. | Check the store scope and that the action is permitted for your role. |
| `409` | Illegal state transition, stale `If-Match`, duplicate assignment, or a record referenced by order history. | Reload and retry; for a referenced record, archive or disable instead of deleting. |
| `422` | Validation failure; maps to field errors in the editor. | Correct the highlighted fields. |
| `413` | An uploaded image is too large. | Resize or compress, then upload again. |

The app surfaces `422` on the affected fields and `403`/`409` as an inline message or toast, without
losing the form.

---

## First-Run Checklist

1. Sign in at `/login` and confirm the dashboard renders.
2. Open the store switcher and confirm the stores you expect are listed.
3. Open each feature area in the left navigation to confirm the role-gated entries appear correctly.
4. Confirm the user menu shows your email from `GET /api/admin/auth/session`.
5. Continue to `09/01-02` to start on catalog work.

---

## Open Questions

1. Whether the store switcher remembers the last-selected store per admin (a server-side preference)
   or resets to the first assignment on each boot is a UX detail owned by `04/02-01` and `04/02-02`;
   onboarding works either way.
2. Whether a dedicated support or supervisor role beyond `customer` and `admin` is needed is
   deferred from `01/01-02`; this guide assumes only the two locked roles.
3. Admin impersonation of a customer for support is out of scope; if added it must be a separately
   audited action owned by `03/04-02`.

## Links

- [Admin Flows](../../04-design/02-admin/01-flows.md)
- [Admin Screens](../../04-design/02-admin/02-screens.md)
- [Authentication & Authorization](../../03-architecture/04-integration/03-auth.md)
- [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md)
- [Multi-store scoping](../../03-architecture/03-data/02-multi-store.md)
- [Admin stories](../../01-requirements/04-user-stories/03-admin.md)
- [ADR 0004 — Headless API and JWT auth](../../00-governance/01-adr/04-headless-jwt.md)
- [Design spec](../../11-spec/README.md) — §7 (auth and security),
  §9 (admin front-end).
