# Security — Non-Functional Requirements

The authentication, authorization, transport, and data-protection controls that every endpoint
and both front-ends must enforce.

**Links:** [Performance](./01-performance.md) · [Availability](./03-availability.md) · [Accounts](../02-functional/10-accounts.md) · [Constraints](../01-business/03-constraints.md) · [Requirements Index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Security is a platform-wide quality attribute. resys-eshop exposes a public commerce API
(`/api/v1`) and an administrative API (`/api/admin`) from one headless service, and both Nuxt
front-ends are separate HTTP clients (spec §3, §7). The controls below are **locked** by the
spec: ASP.NET Identity for the user base, JWT Bearer access tokens, the admin token stored in an
httpOnly cookie, CSRF on state-changing calls, a CORS allow-list for the two front-end origins,
rate limiting on public endpoints, no PAN/PII in logs, and an audit-log table for admin
mutations (spec §7, §10).

The account and identity flows that consume these requirements are defined in
`01/02-10`; the auth integration design is owned by `03/04-03`; the payment-gateway boundary
that keeps card data out of the platform is owned by `03/04-05` and `01/02-05`. Where this file
and the spec disagree, the spec is canonical.

---

## NFR-SEC-001 — Identity and credential storage

- **Description:** The user base is stored with **ASP.NET Identity** (spec §4, §7). Credentials
  and identity data are protected at rest, and no secret is ever logged or returned.
- **Target:** Passwords are stored only as salted hashes via ASP.NET Identity; password policy
  and lockout are enforced server-side; password hashes, tokens, and security stamps never
  appear in API responses, logs, or traces.
- **Measurement:** Unit/integration tests assert hashing and lockout behavior; a log-scrubbing
  test asserts no credential fields are emitted; dependency review confirms the Identity
  configuration in `03/04-03`.

---

## NFR-SEC-002 — JWT Bearer access tokens

- **Description:** Both front-ends and the API authenticate with short-lived **JWT Bearer**
  access tokens (spec §7, `01/02-10`).
- **Target:** Tokens are signed with an asymmetric key held in secret storage, validated on
  every request (issuer, audience, lifetime, signature), and are short-lived; tokens carry no
  PAN or unnecessary PII; a revoked or expired token is rejected.
- **Measurement:** Integration tests exercise valid, expired, tampered, and wrong-audience
  tokens; token lifetimes and signing configuration are reviewed in `03/04-03`.

---

## NFR-SEC-003 — Admin token in an httpOnly cookie

- **Description:** The admin front-end (`web/admin`) stores its JWT in an **httpOnly cookie**
  rather than browser-accessible storage (spec §4, §7).
- **Target:** The admin auth cookie is `HttpOnly`, `Secure`, and `SameSite` (`Lax` or stricter);
  it is never readable from JavaScript; it is scoped to the admin origin and sent only over
  HTTPS; no token is written to `localStorage` or `sessionStorage` in the admin app.
- **Measurement:** Response-header assertions on the auth endpoint; an E2E check (`06/01-04`)
  that `document.cookie` cannot see the token; a source scan of `web/admin` for token storage.

---

## NFR-SEC-004 — CSRF protection on state-changing calls

- **Description:** Cookie-based admin authentication is vulnerable to cross-site request forgery,
  so all state-changing calls must be CSRF-protected (spec §7).
- **Target:** Every non-idempotent `/api/admin` request requires a valid anti-forgery
  token/cookie pair; requests without a valid pair are rejected; the CSRF secret is not exposed
  to third-party origins.
- **Measurement:** Integration tests replay a valid session without the CSRF token and assert
  rejection; the token issuance/validation configuration is reviewed in `03/04-03`.

---

## NFR-SEC-005 — CORS allow-list

- **Description:** The API accepts cross-origin requests only from the two known Nuxt front-end
  origins (spec §7).
- **Target:** CORS is configured with an explicit **allow-list** — the storefront and admin
  origins only — with no wildcard `*`; credentialed requests are permitted only for allow-listed
  origins; unlisted origins receive no `Access-Control-Allow-Origin` for credentialed calls.
- **Measurement:** Integration tests send preflight and simple requests from allowed and
  disallowed origins and assert the response headers; the origin list is environment-configured
  and reviewed in `05/01-02`.

---

## NFR-SEC-006 — Rate limiting on public endpoints

- **Description:** Public endpoints are rate-limited to blunt credential-stuffing, scraping, and
  abuse (spec §7).
- **Target:** Authentication, registration, password-reset, checkout, and review-submission
  endpoints enforce per-client limits and return `429` with `Retry-After` when exceeded; limits
  are configurable per environment and never block normal browsing.
- **Measurement:** Integration tests exceed the limit and assert `429`; the limiter policy table
  and thresholds are documented in `03/04-03`; `429` rate is observable in `08/02-02`.

---

## NFR-SEC-007 — No PAN or PII in logs

- **Description:** The platform must never store, log, or transmit primary account numbers or
  CVV, and customer PII must not appear in logs or traces (spec §7, §10, `01/01-03`).
- **Target:** Card data is captured only by provider-hosted UI (Stripe Elements / PayPal JS
  SDK); the platform persists only gateway tokens/references and payment records. Log and trace
  output is scrubbed of PAN, CVV, passwords, tokens, emails, and other PII; structured logging
  (Serilog) uses redaction, not string concatenation.
- **Measurement:** A redaction test suite feeds representative sensitive payloads and asserts
  they never reach the sink; a repository scan for logging of sensitive field names; gateway
  boundary review in `03/04-05`.

---

## NFR-SEC-008 — Audit log for admin mutations

- **Description:** Admin mutations are recorded in an **audit-log table** (who/what/when) as a
  platform-wide control (spec §10, `03/04-03`).
- **Target:** Every successful `/api/admin` mutation writes an append-only audit row capturing
  the acting identity, the store scope, the action, the target entity, and the timestamp;
  audit rows are never updated or deleted through the application; failed mutations are recorded
  without persisting partial domain state.
- **Measurement:** Integration tests assert one audit row per admin mutation with the correct
  actor and target; the audit table is append-only by permission; audit coverage is reviewed in
  `03/04-03`.

---

## NFR-SEC-009 — Role and store-scope authorization

- **Description:** Authorization enforces the `customer` and `admin` roles plus per-store admin
  assignment (spec §7, `01/02-10`).
- **Target:** `/api/admin` requires the `admin` role **and** the acting admin's store scope;
  cross-store access returns `403`/`404` without leaking another store's data; a `customer`
  token cannot reach admin routes; public routes require no authentication to browse
  (FR-CAT-009).
- **Measurement:** Integration tests attempt cross-store and cross-role access and assert
  denial; authorization policies are reviewed in `03/04-03`.

---

## NFR-SEC-010 — Transport, secrets, and dependency hygiene

- **Description:** All traffic and all secrets are protected in transit and at rest, and
  third-party dependencies are kept current.
- **Target:** HTTPS/TLS is required in every non-development environment (HSTS enabled); secrets
  come from environment/secret storage and are never committed or baked into images; dependencies
  are pinned and scanned for known vulnerabilities on a defined cadence (`10/02`).
- **Measurement:** TLS/HSTS assertions in deployment verification (`07/03`); secret-scanning in
  CI (`05/03-03`); dependency-vulnerability reports per the policy in `10/02`.

---

## Open Questions

1. The concrete token lifetimes, refresh/rotation mechanics, and the rate-limit thresholds are
   finalized in `03/04-03`.
2. The audit-log retention window is an operational policy owned by `08/03-01`; the requirement
   to record the mutation itself is fixed.
3. The precise redaction rule set (which field names are considered PII) is finalized with the
   logging configuration in `08/02-01`.
4. Whether a dedicated WAF or edge rate limiter complements the in-process limiter is an
   infrastructure decision owned by `08/01-01`.
