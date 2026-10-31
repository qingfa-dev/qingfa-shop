# Browse & Buy — User Stories

End-to-end shopping stories for guests and customers: discover, evaluate, cart, checkout, pay, and confirm.

**Links:** [Catalog](../02-functional/01-catalog.md) · [Orders](../02-functional/02-orders.md) · [Checkout](../02-functional/03-checkout.md) · [Actors](../01-business/02-actors.md) · [Requirements index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

These stories follow the **Guest** and **Customer** actors (`01/01-02`) through the public
storefront and `/api/v1`. They realize the catalog (`01/02-01`), orders (`01/02-02`), and
checkout (`01/02-03`) requirements, plus the shipping (`01/02-04`), payments (`01/02-05`), tax
(`01/02-06`), promotions (`01/02-07`), inventory (`01/02-08`), and stores/i18n (`01/02-12`)
behavior those flows depend on. This file is identified as `01/04-01` for cross-reference by
the storefront design flows in `04/01-01`.

Every story is written as `As a… I want… so that…`, names the functional requirements it
realizes, and lists acceptance criteria that can be verified against the API. Genuinely open
details live in `## Open Questions`, never in marker tokens.

Actors and their surfaces are defined in `01/01-02`; the money, store-scoping, and security
rules that apply to every price and request are in `01/01-03`.

## US-001 — Browse products by category

- **Persona:** Guest; Customer
- **Story:** As a guest, I want to browse products by category so that I can discover what a store sells without knowing a product name.
- **FR references:** FR-CAT-004, FR-CAT-009, FR-I18N-003
- **Acceptance Criteria:**
  - The storefront lists only active products belonging to the store resolved from the request domain or the dev `X-Store` header (`01/02-12`).
  - Selecting a category returns that category's products in a stable, admin-defined order with pagination.
  - Products from another store never appear in the listing.

## US-002 — Search and filter the catalog

- **Persona:** Guest; Customer
- **Story:** As a shopper, I want to search by term and refine with facets so that I can find a matching product quickly.
- **FR references:** FR-CAT-006, FR-CAT-009, FR-I18N-003
- **Acceptance Criteria:**
  - A query matches product name, description, and variant SKU in the active locale.
  - Applying a category, option, price, or availability facet narrows the result set; facets reflect only active products visible in the current store.
  - Paging returns disjoint, non-duplicated result sets with the documented page size.

## US-003 — View a product and its availability

- **Persona:** Guest; Customer
- **Story:** As a shopper, I want to view a product's detail, images, price, and stock state so that I can decide whether to buy it.
- **FR references:** FR-CAT-005, FR-CAT-007, FR-CAT-009, FR-INV-009, FR-I18N-002
- **Acceptance Criteria:**
  - The detail response includes variants, option values, images, categories, and a `Money` price with an ISO-4217 currency code.
  - Availability shows in stock, backordered, or out of stock from server data, never from a client value.
  - A missing translation falls back to the store's default locale instead of rendering an empty field.

## US-004 — Choose a variant

- **Persona:** Guest; Customer
- **Story:** As a shopper, I want to pick the option values that describe the variant I want so that I add exactly the right SKU to my cart.
- **FR references:** FR-CAT-002, FR-CAT-003, FR-CAT-007
- **Acceptance Criteria:**
  - Selecting option values resolves exactly one buyable variant and shows its SKU.
  - A variant without an active price in the store currency is not purchasable.
  - A variant with no available stock and no backorder policy blocks add-to-cart.

## US-005 — Add items to the cart

- **Persona:** Guest; Customer
- **Story:** As a shopper, I want to add a variant and set its quantity so that I can build an order before checking out.
- **FR references:** FR-ORD-001, FR-ORD-002, FR-INV-009
- **Acceptance Criteria:**
  - Adding a variant creates a cart in the `cart` state for the resolved store and captures the unit price at add-time.
  - Adding the same variant again increases the existing line's quantity instead of creating a duplicate line.
  - Setting a line quantity to zero removes that line from the cart.

## US-006 — Review cart totals

- **Persona:** Guest; Customer
- **Story:** As a shopper, I want to see the item total, discounts, tax, shipping, and order total so that I know what I will pay before payment.
- **FR references:** FR-ORD-006, FR-ORD-012, FR-TAX-005, FR-CHK-009
- **Acceptance Criteria:**
  - Totals are computed server-side from line items, adjustments, and tax, never read from a cached total column.
  - Tax appears as its own component and is never written as a promotion or shipping adjustment.
  - Every amount is a `Money` value in the order currency, and a currency mismatch is rejected.

## US-007 — Enter a shipping address and choose delivery

- **Persona:** Guest; Customer
- **Story:** As a shopper, I want to provide a shipping address and pick a shipping method so that my order can be delivered at a known cost.
- **FR references:** FR-CHK-001, FR-CHK-002, FR-ORD-003, FR-SHP-004
- **Acceptance Criteria:**
  - The delivery step requires a valid shipping address and a selected shipping method before advancing.
  - Available methods are quoted for the destination, and a served destination offers at least one priced method.
  - Changing the country re-quotes methods and clears a selection that is no longer valid.

## US-008 — Apply a coupon code

- **Persona:** Guest; Customer
- **Story:** As a shopper, I want to enter a coupon code so that an eligible promotion reduces my total.
- **FR references:** FR-CHK-008, FR-PRM-007
- **Acceptance Criteria:**
  - A valid code applies its promotion as an adjustment and recomputes tax and total server-side.
  - An expired, ineligible, or usage-exhausted code is rejected with a reason and no change to the total.
  - Removing the code removes its adjustment and restores the pre-coupon total.

## US-009 — Pay for the order

- **Persona:** Guest; Customer
- **Story:** As a shopper, I want to pay with an available method so that I can complete my purchase securely.
- **FR references:** FR-CHK-003, FR-PAY-004, FR-PAY-008, FR-PAY-010
- **Acceptance Criteria:**
  - Checkout offers only methods enabled for the store and eligible for the shopper; store credit appears only for a customer with a positive balance.
  - Card data is captured in provider-hosted UI, and the platform stores only a gateway reference and outcome.
  - The amount charged equals the server-computed order total in the order currency, and a client-supplied amount is rejected.

## US-010 — Confirm and place the order

- **Persona:** Guest; Customer
- **Story:** As a shopper, I want to confirm my order once delivery and payment are complete so that it is placed and stock is committed.
- **FR references:** FR-CHK-004, FR-CHK-006, FR-ORD-004, FR-ORD-007
- **Acceptance Criteria:**
  - Confirm is rejected until both the delivery and payment steps are complete.
  - The state change to `awaiting_payment`, the stock reservation, and the `OrderPlaced` outbox row are written in a single transaction.
  - Repeating the same confirm request does not create a second order or a double charge.

## US-011 — Recover from a failed payment

- **Persona:** Guest; Customer
- **Story:** As a shopper, I want to retry payment after a decline without losing my cart so that I can still complete the order.
- **FR references:** FR-CHK-007, FR-PAY-004
- **Acceptance Criteria:**
  - A decline leaves the cart, addresses, and line items intact and returns the shopper to the payment step.
  - The failure reason is recorded against the failed payment attempt, and a retry creates a new attempt rather than mutating the failed one.
  - The order is not placed while every payment attempt has failed.

## US-012 — Receive order confirmation

- **Persona:** Guest; Customer
- **Story:** As a shopper, I want a confirmation with my order number and total so that I have a record of the purchase.
- **FR references:** FR-ORD-007, FR-CHK-005
- **Acceptance Criteria:**
  - Placing the order emits the order-placed event that queues a confirmation to the order's email address.
  - The confirmation names the order number, line items, and computed total in the order currency.
  - A notification failure does not roll back the placed order or its state change.

## US-013 — Shop in a preferred locale and currency

- **Persona:** Guest; Customer
- **Story:** As a shopper, I want the storefront in my language and the store's currency so that prices and text are understandable.
- **FR references:** FR-I18N-002, FR-I18N-003, FR-STO-003
- **Acceptance Criteria:**
  - A request in a supported locale renders translated catalog text, while an unsupported locale falls back to the store default.
  - Prices are returned as `Money` with the store's currency and its ISO-4217 code.
  - Locale and currency are resolved server-side from store configuration and are never taken from a conflicting client value.

## US-014 — Look up a guest order

- **Persona:** Guest
- **Story:** As a guest, I want to retrieve my placed order with its number and email so that I can check its status without an account.
- **FR references:** FR-CHK-005, FR-ORD-011
- **Acceptance Criteria:**
  - A guest lookup requires the order number plus the email used at checkout.
  - The response shows the order state, line items, and computed total without exposing other orders.
  - An incorrect pair returns a not-found response that does not confirm whether the order exists.

---

## Open Questions

1. The concrete storefront filter set beyond category, option, price, and availability is
   finalized with the design flows in `04/01-01`.
2. Whether a store may require account creation before the payment step rather than at confirm is
   a checkout-options detail finalized in `04/01-01` and `01/02-03`.
3. The exact confirmation and receipt email templates are owned by the email/files integration
   `03/04-06`.
