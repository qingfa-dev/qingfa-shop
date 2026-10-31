# Wishlists & Reviews — Functional Requirements

What the wishlists and reviews modules must do: customer wishlists with saved items, and product
reviews with a rating and moderation.

**Links:** [Catalog](./01-catalog.md) · [Accounts](./10-accounts.md) · [Stores & i18n](./12-stores-i18n.md) · [Requirements index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file covers two lightweight customer-engagement modules that both hang off the catalog.

**Wishlists** let an authenticated customer save variants for later without adding them to a
cart. A wishlist is a personal collection, not a cart: adding a variant reserves no stock and
creates no line item. A wishlist is owned by a customer and scoped to a store; its **wishlist
items** reference buyable variants and re-resolve current pricing and availability when
displayed (FR-CAT-007, FR-INV-009). A customer may keep more than one wishlist and move an item
from a wishlist to the cart explicitly.

**Reviews** let customers rate and describe a product. A review carries an integer **rating**,
a body, and a state; new submissions are **moderated** by an admin before appearing on the
storefront, so the storefront is not a spam or abuse channel. Product detail pages show the
average rating and published reviews. Reviews are store-scoped and visible only for active
products. Tests are exercised through the customer flows planned in `06/02-01`; the
customer-facing guide is `09/02-01`.

Both modules operate on `/api/v1` for customers and `/api/admin` for staff, require an
authenticated customer for writes, and never let a client dictate which store or product a
saved item or review belongs to.

Actors are defined in `01/01-02`.

---

## FR-WIS-001 — Wishlist management

- **Actor:** Customer
- **Description:** Create, rename, list, and delete personal wishlists.
- **Rules:**
  - A wishlist belongs to exactly one customer and one store and is never visible to another
    customer.
  - A customer may own multiple wishlists; one may be marked as the default.
  - A wishlist name is required and need not be unique.
  - Deleting a wishlist deletes its items and never affects carts or orders.
- **Acceptance Criteria:**
  - A customer can create a wishlist and retrieve it in their own list.
  - A customer cannot view another customer's wishlist.
  - Deleting a wishlist removes its items without touching the cart.

---

## FR-WIS-002 — Wishlist items

- **Actor:** Customer
- **Description:** Add and remove variants in a wishlist, avoiding duplicates within the same
  wishlist.
- **Rules:**
  - A wishlist item references a buyable variant; adding the same variant twice to the same
    wishlist is idempotent and does not create a duplicate.
  - Adding an item records no price and no stock reservation; price and availability are
    resolved at display time.
  - A variant that is inactive or unavailable may remain saved but is shown as unavailable
    (FR-INV-009).
  - Only the owning customer may add or remove items.
- **Acceptance Criteria:**
  - Adding a variant twice yields a single wishlist item.
  - Displaying a wishlist shows current price and availability for each item.
  - A customer cannot modify another customer's wishlist items.

---

## FR-WIS-003 — Move wishlist item to cart

- **Actor:** Customer
- **Description:** Move or add a wishlist item to the active cart, removing it from the wishlist
  when the customer chooses "move".
- **Rules:**
  - Moving an item adds the variant to the cart under normal catalog and stock rules
    (FR-ORD-002) and then removes it from the wishlist.
  - Adding to cart without removing leaves the wishlist item intact.
  - A variant that is out of stock and not backorderable cannot be moved to the cart.
  - The cart is store-scoped and must match the wishlist's store.
- **Acceptance Criteria:**
  - Moving an available item creates or updates a cart line and removes the wishlist item.
  - Adding without removing keeps the wishlist item.
  - Moving an out-of-stock, non-backorder item is rejected with a reason.

---

## FR-WIS-004 — Customer wishlist API

- **Actor:** Customer; Guest
- **Description:** Expose wishlists and their items under `/api/v1` account endpoints for the
  authenticated customer only.
- **Rules:**
  - Every endpoint requires the `customer` role and scopes results to the authenticated customer.
  - A guest is redirected to register or sign in; anonymous wishlists are not persisted.
  - Responses include the resolved current price as `Money` and an availability flag per item.
  - Writes are validated server-side; a client cannot set the owning customer or store.
- **Acceptance Criteria:**
  - An authenticated customer can list, add, and remove wishlist items.
  - An unauthenticated request is rejected with an unauthenticated response.
  - A client-supplied owner or store is ignored or rejected.

---

## FR-REV-001 — Submit a product review

- **Actor:** Customer
- **Description:** Submit a review for a product with an integer rating and an optional body.
- **Rules:**
  - A review references a product and the submitting customer; the store is derived from the
    request, not the client.
  - Rating is an integer on a documented scale (for example 1–5); a value outside the scale is
    rejected.
  - A customer may have at most one active review per product per store; resubmission edits the
    existing pending review rather than creating a second.
  - A review body is optional but bounded by a documented maximum length.
- **Acceptance Criteria:**
  - A valid submission creates a pending review for the product.
  - A rating outside the allowed scale is rejected.
  - Submitting twice for the same product updates the pending review instead of duplicating it.

---

## FR-REV-002 — Rating and aggregation

- **Actor:** Guest; Customer; System
- **Description:** Compute and expose an aggregate rating per product from its published
  reviews.
- **Rules:**
  - The aggregate is derived from published reviews only; pending or rejected reviews do not
    contribute.
  - The aggregate includes the average rating and the review count, recalculated on
    publication changes.
  - A product with no published reviews reports no rating rather than a zero rating.
  - Aggregation is store-scoped and never mixes reviews across stores.
- **Acceptance Criteria:**
  - Publishing a review changes the product's average and count.
  - A product with only pending reviews shows no rating.
  - Reviews from another store do not affect the aggregate.

---

## FR-REV-003 — Review moderation

- **Actor:** Store Admin; Platform Admin
- **Description:** Approve, reject, or hide submitted reviews through a moderation queue.
- **Rules:**
  - A review state is `pending`, `published`, `rejected`, or `hidden`.
  - Only `published` reviews appear on the storefront; `pending`, `rejected`, and `hidden` do
    not.
  - Moderation decisions are recorded with the acting admin and timestamp and written to the
    audit log (`03/04-03`).
  - A rejected or hidden review retains its body for audit but is never publicly readable.
- **Acceptance Criteria:**
  - An admin can publish a pending review, after which it appears on the storefront.
  - A rejected review is absent from the public product response.
  - The moderation decision is present in the audit log.

---

## FR-REV-004 — Verified purchase and eligibility

- **Actor:** System; Store Admin
- **Description:** Mark a review as a verified purchase when the submitting customer bought the
  reviewed product, and enforce any store review eligibility rules.
- **Rules:**
  - A review is verified when the customer has a completed or shipped order containing the
    product.
  - A store may require a verified purchase before a review is accepted, or allow open reviews;
    the setting is store-scoped (FR-STO-004).
  - Verification is computed server-side and cannot be set by the client.
  - A review's verified flag is shown alongside its published rating.
- **Acceptance Criteria:**
  - A customer with a qualifying order has their review marked verified.
  - When a store requires a purchase, a review from a non-buyer is rejected.
  - A client attempting to set the verified flag directly is ignored.

---

## FR-REV-005 — Edit and remove a review

- **Actor:** Customer; Store Admin
- **Description:** Let a customer edit or withdraw their own review, and let an admin remove a
  published review that violates policy.
- **Rules:**
  - Editing a published review returns it to `pending` for re-moderation.
  - A customer may delete their own review; deletion removes it from publication and aggregation.
  - An admin may set a review to `hidden` without deleting the record, preserving audit history.
  - Moderation state changes are recorded with actor and timestamp.
- **Acceptance Criteria:**
  - Editing a published review moves it to `pending` and removes it from the public list.
  - Deleting a review updates the product's aggregate rating.
  - A hidden review is no longer publicly visible but remains for audit.

---

## FR-REV-006 — Public and admin review API

- **Actor:** Guest; Customer; Store Admin
- **Description:** Expose published reviews publicly and moderation operations to admins.
- **Rules:**
  - Public `/api/v1` endpoints return published reviews and the aggregate rating for an active
    product; writes require the `customer` role.
  - Admin `/api/admin` endpoints list the moderation queue and apply state changes, scoped to
    the store.
  - Every admin operation requires the `admin` role plus store scope and is written to the audit
    log.
  - A guest cannot submit, edit, or moderate reviews.
- **Acceptance Criteria:**
  - A guest can read published reviews and the aggregate.
  - A guest write attempt is rejected with an unauthenticated response.
  - An admin scoped to store A cannot moderate store B's reviews.

---

## Open Questions

1. Whether wishlists can be shared via a public link is not required by the spec; if added, it
   is recorded here as an open question for a later phase.
2. The exact rating scale (1–5 vs. 1–10) is finalized with the catalog/review UI in `04/01`.
3. Whether a customer may include images with a review is deferred; the spec's review scope is
   rating plus text.
