# Account — User Stories

Stories for registered customers and guests creating accounts: identity, addresses, order history, returns, wishlists, and reviews.

**Links:** [Accounts](../02-functional/10-accounts.md) · [Wishlists & Reviews](../02-functional/11-wishlists-reviews.md) · [Orders](../02-functional/02-orders.md) · [Returns](../02-functional/09-returns.md) · [Actors](../01-business/02-actors.md) · [Requirements index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

These stories follow the **Customer** actor, with **Guest** entry points where identity is
created. They realize the accounts (`01/02-10`) and wishlists/reviews (`01/02-11`) requirements,
and reuse orders (`01/02-02`), returns (`01/02-09`), payments (`01/02-05`), and stores/i18n
(`01/02-12`). This file is identified as `01/04-02` for cross-reference by the admin design
flows in `04/02-01`.

Account data is the platform's PII, so every story respects the no-PII-in-logs and
server-side-validation constraints in `01/01-03` and the security requirements in `01/03-02`.
Genuinely open details live in `## Open Questions`, never in marker tokens.

Actors and their surfaces are defined in `01/01-02`.

## US-001 — Register an account

- **Persona:** Guest
- **Story:** As a guest, I want to register with my email and a password so that I can save my details and see my orders.
- **FR references:** FR-ACC-001, FR-ACC-005
- **Acceptance Criteria:**
  - Registration requires a unique email and a password meeting the store's policy.
  - Registering with an email already in use is rejected.
  - A weak password is rejected with a field-level validation error.
  - The password is stored hashed and never appears in logs or API responses.

## US-002 — Sign in and sign out

- **Persona:** Customer
- **Story:** As a customer, I want to sign in and sign out so that my account and cart are protected on shared devices.
- **FR references:** FR-ACC-002, FR-ACC-005
- **Acceptance Criteria:**
  - Valid credentials return a short-lived token that is accepted on a subsequent protected request.
  - An expired or malformed token is rejected with an unauthenticated response.
  - Invalid credentials return a generic error that does not reveal whether the account exists.
  - Signing out ends the client session so the token is no longer used.

## US-003 — Keep my account secure

- **Persona:** Customer; Platform Admin
- **Story:** As a customer, I want failed sign-ins throttled and my data kept out of logs so that my account resists abuse.
- **FR references:** FR-ACC-010
- **Acceptance Criteria:**
  - Repeated failed sign-ins trigger throttling or lockout per the auth policy (`03/04-03`).
  - Logs contain no passwords, tokens, or full PII (`01/03-02`).
  - A revoked token is rejected on the next protected request.

## US-004 — Create an account during checkout

- **Persona:** Guest
- **Story:** As a guest at checkout, I want to create an account without losing my order so that I can track it later.
- **FR references:** FR-ACC-005, FR-CHK-005
- **Acceptance Criteria:**
  - Registration during checkout links the resulting order to the new account.
  - Creating the account does not interrupt or duplicate the in-flight order.
  - The cart contents and computed totals are preserved through registration.

## US-005 — Manage my profile

- **Persona:** Customer
- **Story:** As a customer, I want to view and update my name, contact email, and locale preference so that my account stays current.
- **FR references:** FR-ACC-006
- **Acceptance Criteria:**
  - A customer can read and update only their own profile.
  - Changing an email to one already in use is rejected, and email changes require re-verification.
  - Changing a profile never rewrites historical orders or captured addresses.
  - A customer cannot read another customer's profile.

## US-006 — Manage my addresses

- **Persona:** Customer
- **Story:** As a customer, I want to save, edit, and delete addresses and mark defaults so that checkout is faster.
- **FR references:** FR-ACC-007, FR-ORD-003
- **Acceptance Criteria:**
  - A customer may hold multiple addresses with a default shipping and a default billing designation.
  - Checkout snapshots a saved address onto the order, so later edits do not change placed orders.
  - Deleting a saved address does not change an already-placed order.
  - A customer cannot view or modify another customer's addresses.

## US-007 — View my order history

- **Persona:** Customer
- **Story:** As a customer, I want to list and open my orders so that I can review what I bought and its status.
- **FR references:** FR-ORD-011, FR-ACC-008
- **Acceptance Criteria:**
  - The order list contains only the authenticated customer's orders.
  - The order detail shows line items, state, computed total with currency, and shipment tracking when present.
  - Requesting another customer's order returns a not-found or forbidden response.

## US-008 — Claim a guest order

- **Persona:** Customer
- **Story:** As a customer, I want an order I placed as a guest with the same email to appear in my history so that I have one place for my purchases.
- **FR references:** FR-ACC-008, FR-ORD-011
- **Acceptance Criteria:**
  - An order placed as a guest with the same verified email is linked to the account and appears in history.
  - Claiming never changes the order's contents, prices, totals, or state.
  - An order already owned by another account is not claimable.
  - A repeated claim is idempotent and does not duplicate the order.

## US-009 — Track a shipment

- **Persona:** Customer
- **Story:** As a customer, I want to see shipment and tracking status for my order so that I know when it will arrive.
- **FR references:** FR-ORD-010, FR-ORD-011
- **Acceptance Criteria:**
  - The order detail exposes shipment and tracking references when they exist.
  - The displayed state reflects the order's `shipped` or `completed` transitions.
  - No tracking reference is fabricated when the shipment carries none.

## US-010 — Request a return

- **Persona:** Customer
- **Story:** As a customer, I want to request a return for items from a shipped order so that I can send them back.
- **FR references:** FR-RET-001, FR-RET-002, FR-ORD-009
- **Acceptance Criteria:**
  - A customer can request a return only for their own order in `shipped` or `completed`.
  - A requested quantity never exceeds the purchased quantity minus quantities already returned.
  - The return references the original line items without editing them.
  - An unauthorized return cannot advance to reception.

## US-011 — Choose a refund or store credit

- **Persona:** Customer
- **Story:** As a customer, I want to choose a refund or store credit for an accepted return so that I get value back the way I prefer.
- **FR references:** FR-RET-005, FR-PAY-006, FR-PAY-007
- **Acceptance Criteria:**
  - A refund is routed to the gateway that captured the payment and never exceeds the captured amount minus prior refunds.
  - Choosing store credit grants an equal store-scoped ledger credit instead of a gateway refund.
  - The settlement is recorded against the return and the originating payment or ledger.

## US-012 — Manage wishlists

- **Persona:** Customer
- **Story:** As a customer, I want to create, rename, and delete wishlists so that I can organize items I am considering.
- **FR references:** FR-WIS-001, FR-WIS-004
- **Acceptance Criteria:**
  - A wishlist belongs to exactly one customer and one store and is never visible to another customer.
  - A customer may own multiple wishlists, one of which may be marked default.
  - Deleting a wishlist deletes its items and never affects carts or orders.

## US-013 — Save variants to a wishlist

- **Persona:** Customer
- **Story:** As a customer, I want to save a variant to a wishlist so that I can find it again later.
- **FR references:** FR-WIS-002, FR-WIS-004
- **Acceptance Criteria:**
  - Adding the same variant twice to a wishlist is idempotent and yields a single item.
  - Adding an item records no price and reserves no stock.
  - Displaying a wishlist resolves the current price and availability for each item.
  - Only the owning customer may add or remove items.

## US-014 — Move a wishlist item to the cart

- **Persona:** Customer
- **Story:** As a customer, I want to move a saved variant to my cart so that I can buy it.
- **FR references:** FR-WIS-003, FR-ORD-002
- **Acceptance Criteria:**
  - Moving an item adds the variant to the cart under normal catalog and stock rules, then removes it from the wishlist.
  - Adding to the cart without removing leaves the wishlist item intact.
  - An out-of-stock, non-backorder variant cannot be moved and is rejected with a reason.

## US-015 — Submit a product review

- **Persona:** Customer
- **Story:** As a customer, I want to rate and describe a product I bought so that I can share my experience.
- **FR references:** FR-REV-001, FR-REV-002, FR-REV-004
- **Acceptance Criteria:**
  - A review references the product and the submitting customer, with the store derived from the request.
  - A rating outside the allowed integer scale is rejected.
  - At most one active review per product per store exists, and resubmission updates the pending review instead of duplicating it.
  - The verified-purchase flag is computed server-side from qualifying orders and cannot be set by the client.

## US-016 — Edit or withdraw a review

- **Persona:** Customer
- **Story:** As a customer, I want to edit or delete my review so that I can correct or remove my feedback.
- **FR references:** FR-REV-002, FR-REV-005
- **Acceptance Criteria:**
  - Editing a published review returns it to `pending` and removes it from the public list.
  - Deleting a review removes it from publication and from the product's aggregate rating.
  - Moderation state changes are recorded with the acting customer and a timestamp.

---

## Open Questions

1. Whether a customer account and store credit are shared across stores or kept per store is
   resolved with the multi-store model `03/03-02`; these stories assume store-scoped orders
   either way.
2. The exact rating scale (1–5 versus 1–10) is finalized with the catalog and review UI in
   `04/01-03`.
3. Refresh-token lifetime, rotation, and revocation mechanics are finalized in the auth
   integration `03/04-03`.
