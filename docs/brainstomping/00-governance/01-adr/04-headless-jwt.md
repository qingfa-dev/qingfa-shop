# ADR 0004: Headless API, JWT Bearer, and Role-Based Authorization

Records the locked posture for API delivery and authentication: a headless REST
service with JWT Bearer tokens and role plus store-scoped authorization.

**Links:** [Governance index](../README.md) · [Spec](../../11-spec/README.md)

---

## Status

Accepted — 2026-09-23. Locked by the design spec (§2 decision 3, §7, §9).

## Context

resys-eshop serves two independently built front-ends plus a shared typed client. The
back end must expose commerce capabilities without embedding any UI, and it must
authorize two very different audiences — anonymous/customer shoppers and privileged
store administrators — across a multi-store deployment.

Forces:

- **Headless by design.** The service must have no in-process UI; both front-ends are
  separate HTTP clients of the same API.
- **Two trust levels.** Public catalog browsing is anonymous; admin operations mutate
  catalog, orders, inventory, and settings and must be tightly scoped.
- **Multi-store.** Admin authority is per store: an administrator of one store must not
  administer another unless explicitly assigned.
- **Token handling in a browser.** The admin is a browser application; the access token
  must not be readable by JavaScript to reduce XSS token theft.
- **Uniform contract.** Both front-ends should speak the same REST style, differing only
  in the endpoint surface they call.

## Decision

### Headless API

The system is **headless**. There is no in-process UI in `Resys.Api`. The admin is just
another HTTP client: it consumes `/api/admin` over HTTP exactly as the storefront
consumes `/api/v1`, rather than being served or embedded by the .NET service.

- Public surface: `/api/v1` — catalog, cart, checkout, account, reviews.
- Admin surface: `/api/admin` — catalog CRUD, orders, inventory, shipping/tax/promotion
  configuration, store settings, users/roles, reports.

### Authentication

- **Identity** is provided by ASP.NET Identity for the user base.
- **JWT Bearer** is the API authentication scheme for both public and admin calls, using
  short-lived access tokens.
- The **admin front-end stores its JWT in an httpOnly cookie**; JavaScript cannot read
  the token, which mitigates token exfiltration via XSS. The storefront and shared
  client follow the same bearer-token contract.
- **CSRF** protection applies to state-changing calls from the cookie-based admin;
  **CORS** is restricted to an allow-list of the two front-end origins; **rate limiting**
  applies to public endpoints; **no PAN or PII is written to logs**.
- OpenAPI is generated with the built-in `Microsoft.AspNetCore.OpenApi` and browsable in
  Scalar.

### Authorization

- Roles are **`customer`** and **`admin`**.
- On top of the `admin` role, authorization enforces **per-store admin assignment**: an
  admin acts only within the store(s) they are assigned to.
- `/api/admin` requires the `admin` role **and** a valid store scope on the request.
- Every admin mutation is recorded in an **audit-log table** (who / what / when).

### Versioning

`/api/v1` is evolved additively; a breaking change is what triggers `/api/v2`.

## Consequences

**Positive**

- Any client (Nuxt storefront, Nuxt admin, future mobile) can consume the same REST
  contract; the back end has no rendering concerns.
- httpOnly-cookie storage keeps the admin token out of JavaScript reach, lowering XSS
  impact.
- Role + per-store scoping models real multi-store operations and prevents cross-store
  administration by default.
- One authorization model, audited, across all admin mutations.

**Negative / costs**

- Cookie-based admin auth requires explicit CSRF handling on state-changing requests;
  this is mitigated but must be maintained.
- Short-lived tokens imply a refresh flow on the client; the admin must handle silent
  renewal (detailed in area `03` / area `09`).
- Store scoping must be applied on every admin query and mutation; missing a scope
  filter is a real authorization bug and must be covered by tests (area `06`).
- Audit logging adds a write on each admin mutation; acceptable for the volume.

**Neutral**

- The public and admin surfaces share the JWT scheme but have distinct authorization
  requirements; this is intentional and documented in the auth API reference.

## Alternatives Considered

- **Server-rendered admin inside the .NET service.** Rejected: it violates the headless
  decision and couples admin rendering to the API process.
- **Cookie session authentication with server-side sessions for all clients.**
  Rejected: bearer tokens suit stateless API hosts and multiple clients better; the
  admin still uses a cookie, but as a transport for the JWT rather than a session.
- **Storing the admin JWT in `localStorage`.** Rejected: readable by JavaScript and thus
  exposed to XSS; httpOnly cookie is safer.
- **Single `admin` role with global authority.** Rejected: it cannot express multi-store
  operations; per-store assignment is required.
- **Separate admin API host or admin-specific authentication.** Rejected: one service
  and one auth scheme keep operations and contracts uniform; only the endpoint surface
  and authorization differ.
- **Long-lived access tokens to avoid refresh.** Rejected: it widens the damage window
  of a leaked token; short-lived tokens with refresh are preferred.

## Open Questions

None that affect this decision. The concrete token lifetimes, refresh mechanism, and
CSRF token exchange are specified in the auth architecture and API reference in area
`03`/`09` without changing the choices recorded here.

## Links

- [Governance index](../README.md)
- [Design spec](../../11-spec/README.md) — §2
  (decisions), §7 (API surface, auth and security), §9 (front-ends).
- [ADR 0001 — Application stack](./01-stack.md)
- [ADR 0002 — Modular monolith](./02-modular-monolith.md)
- [ADR 0003 — No CMS, no GraphQL](./03-no-cms-no-gql.md)
- Auth architecture (Identity model, JWT issuance, cookie flow, store-scoped
  authorization): `03/04-03`.
