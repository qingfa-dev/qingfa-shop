# Accounts — Functional Requirements

What the accounts module must do: ASP.NET Identity users, authentication, the `customer` and
`admin` roles with per-store admin assignment, customer profiles and addresses, and guest-order
claiming.

**Links:** [Orders](./02-orders.md) · [Checkout](./03-checkout.md) · [Stores & i18n](./12-stores-i18n.md) · [Requirements index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Accounts owns identity and access for the whole platform. The user base is stored with
**ASP.NET Identity** and authenticated with **JWT Bearer** access tokens; the admin front-end
keeps its JWT in an **httpOnly cookie** (spec §7). Authorization recognizes two roles —
**`customer`** and **`admin`** — and admin authority is further **scoped to specific stores**
through per-store assignment (spec §7, §4 of `01/01-02`).

Customers own a **profile** and a set of saved **addresses** that checkout and account screens
reuse. A guest who places an order may later create an account and **claim** that order, which
is the mechanism behind customer order history (FR-ORD-011). Registration, sign-in, and token
issuance are planned in `03/04-03` (auth integration); account user flows are planned in
`01/04-02`.

Accounts carries the platform's PII, so no password, token, or personal data is written to logs
(`01/01-03`). Authentication and authorization failures return generic responses that do not
disclose whether an account exists.

Actors are defined in `01/01-02`.

---

## FR-ACC-001 — Identity user store

- **Actor:** Platform Admin; System
- **Description:** Maintain the platform user base with ASP.NET Identity, storing credentials
  and identity data but never storing card data or secrets in plain text.
- **Rules:**
  - A user has a unique email/username, a securely hashed password, and identity metadata
    (created, last login, lockout).
  - Passwords are never stored or logged in plain text; hashing and verification use the
    ASP.NET Identity facilities.
  - One user may shop in multiple stores; orders are always created in the store of checkout
    (`01/01-02`).
  - Deleting or disabling a user does not delete their orders, which remain for history and
    audit.
- **Acceptance Criteria:**
  - A user can be created and later authenticated with the same credentials.
  - No plaintext password or password hash appears in logs or API responses.
  - Disabling a user blocks new sign-ins without removing their order history.

---

## FR-ACC-002 — Authentication and tokens

- **Actor:** Guest; Customer; Store Admin; Platform Admin
- **Description:** Authenticate users and issue short-lived JWT Bearer access tokens used by
  both front-ends and the API.
- **Rules:**
  - Sign-in returns a short-lived access token; refresh/rotation behavior is defined in
    `03/04-03`.
  - The admin front-end stores its JWT in an httpOnly, secure cookie, never in browser
    local storage.
  - State-changing public and admin calls are protected against CSRF per spec §7.
  - Token validation is server-side on every protected request; an expired or malformed token is
    rejected with an unauthenticated response.
- **Acceptance Criteria:**
  - Valid credentials return a token accepted on a subsequent protected request.
  - An expired token is rejected.
  - The admin cookie is httpOnly and not readable from client script.

---

## FR-ACC-003 — Roles `customer` and `admin`

- **Actor:** Platform Admin; Store Admin
- **Description:** Grant each user one or both of the `customer` and `admin` roles and enforce
  authorization by role on the public and admin surfaces.
- **Rules:**
  - The `customer` role grants access to customer-scoped `/api/v1` account endpoints.
  - The `admin` role is required for every `/api/admin` endpoint and must be combined with a
    store scope for store-scoped operations (spec §7).
  - Role assignment is an admin-only operation recorded in the audit log (`03/04-03`).
  - A user may hold both roles; the effective authority is the union of their granted scopes.
- **Acceptance Criteria:**
  - A `customer` cannot call `/api/admin`.
  - An `admin` without a store scope cannot mutate store-scoped resources.
  - Granting a role is recorded in the audit log.

---

## FR-ACC-004 — Per-store admin assignment

- **Actor:** Platform Admin; Store Admin
- **Description:** Assign an `admin` user to one or more specific stores so each admin acts only
  within the stores they are authorized for.
- **Rules:**
  - An admin assignment binds a user to a store; a store-scoped request is authorized only when
    the acting admin is assigned to that store.
  - Platform Admin authority spans stores; a Store Admin is limited to the stores assigned to
    them.
  - Unassigning an admin from a store immediately removes their authority there without deleting
    their user.
  - Every assignment or removal is recorded in the audit log.
- **Acceptance Criteria:**
  - An admin assigned only to store A receives a forbidden response for store B.
  - An admin assigned to both A and B can operate in either.
  - Removing the assignment revokes store B access while leaving store A access intact.

---

## FR-ACC-005 — Registration and sign-in

- **Actor:** Guest; Customer
- **Description:** Allow a guest to register a customer account and sign in, including during
  checkout, without interrupting an in-flight order.
- **Rules:**
  - Registration requires a unique email and a password meeting the store's policy.
  - A guest may register during or after checkout; the resulting order is linked to the new
    account without duplicating it (FR-CHK-005).
  - Signing in with an existing account while holding an anonymous cart associates that cart
    with the customer (FR-ORD-001).
  - A sign-in attempt with invalid credentials returns a generic error that does not reveal
    whether the account exists.
- **Acceptance Criteria:**
  - Registering with an email already in use is rejected.
  - Registering during checkout links the placed order to the new account.
  - A weak password is rejected with a field-level validation error.

---

## FR-ACC-006 — Customer profile

- **Actor:** Customer
- **Description:** Let a customer view and update their own profile, including name, contact
  email, and locale/currency preferences where the store supports them.
- **Rules:**
  - A customer can read and update only their own profile.
  - Email changes require uniqueness and re-verification per the auth policy in `03/04-03`.
  - Changing a profile never rewrites historical orders or captured addresses.
  - Profile data is PII and is redacted from logs.
- **Acceptance Criteria:**
  - A customer can update their name and see it on the next request.
  - Changing an email to one already in use is rejected.
  - A customer cannot read another customer's profile.

---

## FR-ACC-007 — Customer addresses

- **Actor:** Customer; Guest
- **Description:** Manage a reusable address book for a customer and select a saved address
  during checkout.
- **Rules:**
  - A customer may have multiple saved addresses with a default shipping and default billing
    designation.
  - Addresses are owned by the customer and are never readable by other customers.
  - Checkout snapshots a saved address onto the order so later edits to the address book do not
    change placed orders (FR-ORD-003).
  - A guest may enter an address without an account; it is not persisted until an account
    exists.
- **Acceptance Criteria:**
  - A customer can create, edit, and delete their own addresses.
  - Deleting a saved address does not change an already-placed order.
  - A customer cannot view another customer's addresses.

---

## FR-ACC-008 — Guest order claim

- **Actor:** Customer; System
- **Description:** Link a guest order to a customer account when the customer registers or signs
  in with the order's email, so the order appears in their history.
- **Rules:**
  - An order is claimable when it was placed as a guest with the same verified email as the now
    authenticated customer.
  - Claiming links the order to the account and makes it visible in order history
    (FR-ORD-011); it never changes the order's contents, prices, or totals.
  - An order already linked to a different account is not claimable by another account.
  - Claim attempts are recorded so duplicate claims are idempotent.
- **Acceptance Criteria:**
  - A customer signing in with the order email sees the previously guest-placed order.
  - Claiming does not alter the order's totals or state.
  - An order owned by another account is not claimable.

---

## FR-ACC-009 — Admin user and role management API

- **Actor:** Platform Admin; Store Admin
- **Description:** List users, inspect and update roles, and manage per-store admin assignments
  over `/api/admin`.
- **Rules:**
  - Every operation requires the `admin` role and is written to the audit log.
  - A Store Admin may manage assignments only within their own store scope; platform-wide user
    and role management is a Platform Admin authority.
  - User listings never expose password hashes or full PII beyond the administrative need.
  - Validation rejects unknown roles and assignments to nonexistent stores.
- **Acceptance Criteria:**
  - A Platform Admin can assign an existing user as admin for a store.
  - A Store Admin cannot grant platform-wide admin authority.
  - A change of role or assignment appears in the audit log.

---

## FR-ACC-010 — Account security

- **Actor:** System; Platform Admin
- **Description:** Protect account access through lockout, rate limiting, and secure handling of
  credentials and tokens.
- **Rules:**
  - Repeated failed sign-ins trigger lockout or throttling per the auth policy in `03/04-03`.
  - Public authentication endpoints are rate limited; PII and secrets are redacted from logs
    (`01/01-03`).
  - Tokens are short-lived and validated server-side; revocation takes effect for new requests.
  - Account enumeration is avoided by returning generic authentication errors.
- **Acceptance Criteria:**
  - Repeated failed sign-ins result in throttling or lockout.
  - Logs contain no passwords, tokens, or full PII.
  - A revoked token is rejected on the next protected request.

---

## Open Questions

1. Whether a customer account is shared across stores or kept per store is resolved with the
   multi-store model `03/03-02`; this file assumes store-scoped orders either way.
2. Refresh-token lifetime, rotation, and revocation mechanics are finalized in the auth
   integration `03/04-03`.
3. Whether a dedicated support or supervisor role beyond `customer` and `admin` is needed is
   deferred from `01/01-02`; the current requirement set defines only the two locked roles.
