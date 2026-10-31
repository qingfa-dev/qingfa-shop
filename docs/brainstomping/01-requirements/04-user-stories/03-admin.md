# Admin — User Stories

Back-office stories for store and platform administrators: catalog, orders, inventory, configuration, stores, users, and reports.

**Links:** [Catalog](../02-functional/01-catalog.md) · [Orders](../02-functional/02-orders.md) · [Shipping](../02-functional/04-shipping.md) · [Payments](../02-functional/05-payments.md) · [Tax](../02-functional/06-tax.md) · [Promotions](../02-functional/07-promotions.md) · [Actors](../01-business/02-actors.md) · [Requirements index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

These stories follow the **Store Admin** and **Platform Admin** actors (`01/01-02`) through the
admin app and `/api/admin`. They realize orders (`01/02-02`), shipping (`01/02-04`), payments
(`01/02-05`), tax (`01/02-06`), and promotions (`01/02-07`), and reuse catalog (`01/02-01`),
inventory (`01/02-08`), returns (`01/02-09`), accounts (`01/02-10`), wishlists/reviews
(`01/02-11`), and stores/i18n (`01/02-12`). This file is identified as `01/04-03` for
cross-reference by the admin design flows in `04/02-01`.

Every admin mutation requires the `admin` role plus a store scope, and is written to the audit
log (`03/04-03`), per the constraints in `01/01-03`. Genuinely open details live in
`## Open Questions`, never in marker tokens. Actors and their surfaces are defined in
`01/01-02`.

## US-001 — Manage product masters

- **Persona:** Store Admin
- **Story:** As a store admin, I want to create, edit, and archive products in my store so that the storefront reflects what we sell.
- **FR references:** FR-CAT-001, FR-CAT-010
- **Acceptance Criteria:**
  - A product belongs to exactly one store and is invisible to another store's catalog.
  - Draft products never appear in public listing or search; only active products do.
  - A slug is unique within the store, and archiving hides a product without deleting order history.
  - Every mutation requires the `admin` role plus store scope and is written to the audit log.

## US-002 — Manage variants and options

- **Persona:** Store Admin
- **Story:** As a store admin, I want to define option types and values and generate variants so that shoppers can buy the right SKU.
- **FR references:** FR-CAT-002, FR-CAT-003, FR-CAT-010
- **Acceptance Criteria:**
  - Creating a master creates its default variant in the same transaction.
  - SKU is unique across the platform, and duplicate SKUs are rejected.
  - Option types and values are store-scoped and reusable across products.
  - Removing an option value never orphans an existing variant; the variant is reassigned or archived.

## US-003 — Manage the category tree

- **Persona:** Store Admin
- **Story:** As a store admin, I want to maintain a hierarchical category tree and attach products so that shoppers can browse by taxonomy.
- **FR references:** FR-CAT-004, FR-CAT-010
- **Acceptance Criteria:**
  - Categories form a tree with a single root per store and one parent per category.
  - A product may belong to multiple categories.
  - Moving a category under a new parent preserves existing product links.
  - Deleting a category reparents or detaches its products and never deletes products.

## US-004 — Manage product images

- **Persona:** Store Admin
- **Story:** As a store admin, I want to upload and order product images and mark a primary so that listings show the right visual.
- **FR references:** FR-CAT-005, FR-CAT-010
- **Acceptance Criteria:**
  - An uploaded image is retrievable through a stable URL and derives a thumbnail rendition.
  - Exactly one primary image per product drives product listings.
  - A variant without its own image falls back to the master's primary image.
  - Removing an image does not delete past order records that referenced the product.

## US-005 — Set per-store prices

- **Persona:** Store Admin; Platform Admin
- **Story:** As an admin, I want to set prices per variant, store, and currency so that each storefront sells at the correct amount.
- **FR references:** FR-CAT-007, FR-STO-003
- **Acceptance Criteria:**
  - A price is keyed by (variant, store, currency) and stored as a `Money` value with an ISO-4217 code.
  - Only currencies enabled for the store are sellable on its storefront.
  - Changing a price never rewrites the price captured on existing orders.

## US-006 — Manage translations

- **Persona:** Store Admin
- **Story:** As a store admin, I want to edit product, category, and option-value text per locale so that a multi-locale store reads correctly.
- **FR references:** FR-I18N-004, FR-CAT-008
- **Acceptance Criteria:**
  - Adding a locale row leaves other locale rows and the base entity's non-text data untouched.
  - A missing translation is shown as an empty locale row that falls back at render time, not as an error.
  - Every translation mutation requires the `admin` role plus store scope and is written to the audit log.

## US-007 — Review and fulfill orders

- **Persona:** Store Admin; Platform Admin
- **Story:** As a store admin, I want to list, filter, and inspect orders and move them through fulfillment so that customers receive their purchases.
- **FR references:** FR-ORD-004, FR-ORD-010
- **Acceptance Criteria:**
  - The order list supports filtering by state, number, and date.
  - The order detail returns line items, addresses, adjustments, payments, shipments, and state history.
  - State-changing operations go through the state machine, never through direct status writes.
  - An admin scoped to store A cannot view store B's orders.

## US-008 — Ship an order

- **Persona:** Store Admin
- **Story:** As a store admin, I want to ship a fulfilled order so that stock leaves inventory and the customer is notified.
- **FR references:** FR-ORD-007, FR-ORD-010, FR-INV-006
- **Acceptance Criteria:**
  - Shipping writes a shipment movement that reduces `on_hand` and decrements the reservation by the same quantity.
  - The order moves to `shipped` and emits the `OrderShipped` event through the outbox.
  - A shipped order's recorded costs and line items are not altered.

## US-009 — Capture and refund payments

- **Persona:** Store Admin; Platform Admin
- **Story:** As a store admin, I want to capture, void, refund, and mark offline payments paid so that money movement matches the order.
- **FR references:** FR-PAY-004, FR-PAY-006, FR-PAY-009
- **Acceptance Criteria:**
  - A capture moves the payment to `captured` and advances the order to `awaiting_fulfillment`.
  - A refund is routed to the gateway that captured the payment and never exceeds the captured amount minus prior refunds.
  - Marking an offline payment paid advances its payment state and the order.
  - Every action records a transaction row and an audit entry; an admin scoped to store A cannot view store B's payments.

## US-010 — Cancel an order

- **Persona:** Store Admin; Customer
- **Story:** As a store admin, I want to cancel an unshipped order so that reserved stock and any payment are released correctly.
- **FR references:** FR-ORD-008, FR-INV-005
- **Acceptance Criteria:**
  - Cancellation is allowed from `cart`, `awaiting_payment`, and `awaiting_fulfillment`.
  - Reserved stock is released so the quantity becomes available again.
  - A captured payment is refunded or voided through the originating gateway.
  - Canceling a `shipped` order is rejected because that path is a return.

## US-011 — Process a return

- **Persona:** Store Admin
- **Story:** As a store admin, I want to authorize, receive, inspect, restock, and settle returns so that returned goods and money are handled consistently.
- **FR references:** FR-RET-004, FR-RET-005, FR-RET-007, FR-RET-010
- **Acceptance Criteria:**
  - Reception records received quantities and moves the return to `received`; inspection splits restockable and unsellable quantities.
  - Restocking writes a return movement and is idempotent per accepted return item.
  - Settlement records a refund transaction or a store-credit grant plus an audit entry.
  - An admin scoped to store A cannot view or settle store B's returns.

## US-012 — Adjust inventory

- **Persona:** Store Admin
- **Story:** As a store admin, I want to record receipts, adjustments, and corrections as stock movements so that on-hand stock stays accurate and auditable.
- **FR references:** FR-INV-004, FR-INV-006, FR-INV-010
- **Acceptance Criteria:**
  - Every quantity change writes an append-only `StockMovement`, and `on_hand` equals the sum of a stock item's movement deltas.
  - Corrections are new compensating movements, never edits or deletes.
  - Stock history is queryable by variant, location, reason, and date range.
  - Mutations require the `admin` role plus store scope and are written to the audit log.

## US-013 — Transfer stock between locations

- **Persona:** Store Admin
- **Story:** As a store admin, I want to transfer stock between locations of my store so that inventory is available where it is needed.
- **FR references:** FR-INV-008, FR-INV-010
- **Acceptance Criteria:**
  - A transfer writes an outbound movement at the source and an inbound movement at the destination.
  - A transfer exceeding the source `on_hand` is rejected.
  - Both locations must belong to the same store; a cross-store transfer is rejected.

## US-014 — Configure shipping methods and zones

- **Persona:** Store Admin
- **Story:** As a store admin, I want to define shipping methods, categories, zones, and calculators so that checkout quotes the right rates.
- **FR references:** FR-SHP-001, FR-SHP-002, FR-SHP-007
- **Acceptance Criteria:**
  - A shipping method belongs to one store and one category and is never offered in another store.
  - A method is available only when the destination falls in one of its zones, with the most specific match winning.
  - Disabling a method removes it from all quotes without changing in-flight order costs.
  - Every mutation requires the `admin` role plus store scope and is written to the audit log.

## US-015 — Configure tax

- **Persona:** Store Admin
- **Story:** As a store admin, I want to define jurisdictions, rates, and categories and choose inclusive or exclusive pricing so that orders are taxed correctly.
- **FR references:** FR-TAX-001, FR-TAX-002, FR-TAX-003, FR-TAX-008
- **Acceptance Criteria:**
  - The most specific jurisdiction wins (city over region over country), with fallback to broader levels.
  - A rate applies only when the destination jurisdiction and the line's tax category both match.
  - Switching inclusive or exclusive mode affects new computations only, never placed orders.
  - Negative rates and conflicting rates at the same specificity and category are rejected.

## US-016 — Configure promotions and coupons

- **Persona:** Store Admin
- **Story:** As a store admin, I want to create promotions with rules, actions, windows, limits, and coupon codes so that I can run discounts safely.
- **FR references:** FR-PRM-001, FR-PRM-002, FR-PRM-003, FR-PRM-010
- **Acceptance Criteria:**
  - A promotion applies only within its window and channel and while under its usage limits.
  - Promotion actions produce promotion adjustments only, never tax rows or edits to captured prices.
  - A preview shows the computed adjustment for a sample order without persisting it.
  - Duplicate coupon codes within a store are rejected.

## US-017 — Manage payment methods

- **Persona:** Store Admin; Platform Admin
- **Story:** As a store admin, I want to enable and disable payment methods per store so that checkout offers only what we accept.
- **FR references:** FR-PAY-001, FR-PAY-009
- **Acceptance Criteria:**
  - Enabling a method makes it available in that store's new checkouts.
  - Disabling a method removes it from new checkouts without altering in-flight orders.
  - A method identifier is unique within a store.
  - Store A's method list is independent of store B's.

## US-018 — Moderate reviews

- **Persona:** Store Admin; Platform Admin
- **Story:** As a store admin, I want to approve, reject, or hide submitted reviews so that the storefront stays trustworthy.
- **FR references:** FR-REV-003, FR-REV-006
- **Acceptance Criteria:**
  - Only `published` reviews appear on the storefront; `pending`, `rejected`, and `hidden` reviews do not.
  - Each moderation decision records the acting admin and a timestamp and is written to the audit log.
  - A rejected or hidden review is retained for audit but is never publicly readable.
  - An admin scoped to store A cannot moderate store B's reviews.

## US-019 — Manage store settings

- **Persona:** Store Admin; Platform Admin
- **Story:** As an admin, I want to manage currencies, locales, and checkout options for a store so that the storefront matches its market.
- **FR references:** FR-STO-003, FR-STO-004, FR-STO-005
- **Acceptance Criteria:**
  - A store declares a default currency and locale plus the sets of enabled currencies and supported locales.
  - Enabling a locale makes it selectable in that store's storefront.
  - Empty supported sets and unknown ISO codes are rejected.
  - Only a Platform Admin may create stores or change a store's domain and defaults.

## US-020 — Manage users and roles

- **Persona:** Platform Admin; Store Admin
- **Story:** As a platform admin, I want to list users and manage roles and per-store admin assignments so that staff have exactly the access they need.
- **FR references:** FR-ACC-003, FR-ACC-004, FR-ACC-009
- **Acceptance Criteria:**
  - An admin without a store scope cannot mutate store-scoped resources.
  - A Store Admin cannot grant platform-wide admin authority.
  - User listings never expose password hashes or full PII beyond the administrative need.
  - Role and assignment changes appear in the audit log.

## US-021 — Create stores and assign administrators

- **Persona:** Platform Admin
- **Story:** As a platform admin, I want to create stores and assign their administrators so that a new store can operate independently.
- **FR references:** FR-STO-001, FR-ACC-004
- **Acceptance Criteria:**
  - A new store has a name, default domain, default currency, default locale, and active flag.
  - Deactivating a store blocks new storefront traffic and new orders without deleting its data.
  - Only stores the acting admin is assigned to appear in the store switcher.
  - Store data is never visible through another store's scope.

## US-022 — View sales and product reports

- **Persona:** Store Admin; Platform Admin
- **Story:** As a store admin, I want sales and top-product reports for my store so that I can make merchandising decisions.
- **FR references:** FR-ORD-010, FR-INV-010, FR-RET-003
- **Acceptance Criteria:**
  - Reports are scoped to the selected store and never mix another store's data.
  - Order and return reporting can group by state and by return reason.
  - Stock reporting reflects the movement ledger and reconciles with reported `on_hand`.
  - A reporting conversion uses the `CurrencyRate` table and never becomes a selling price.

---

## Open Questions

1. The concrete report set and KPI definitions are finalized with the admin dashboard design in
   `04/02-02` and the admin guide `09/01-05`.
2. Whether a dedicated support or supervisor role beyond `customer` and `admin` is needed is
   deferred from `01/01-02`; the current requirement set defines only the two locked roles.
3. The default priority values and tie-breakers for promotion application order are finalized in
   the promotion engine `03/02-04`.
