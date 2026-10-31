# Backlog

Candidate work items per module, prioritized with MoSCoW.

**Links:** [Roadmap](./01-roadmap.md) · [Risks](./03-risks.md) · [Requirements](../01-requirements/README.md) · [Spec](../11-spec/README.md)

---

## Purpose

The backlog is the complete, module-by-module inventory of candidate work for resys-eshop. Each
item is tagged with a MoSCoW priority and traces to a functional requirement (`01/02-*`). The
roadmap (`02/01`) sequences these items into phases; this file does not sequence, it enumerates.

The twelve modules are the ones locked in the spec's Appendix A and `01/01-03`: Catalog, Orders,
Inventory, Shipping, Tax, Promotions, Payments, Returns, Wishlists, Reviews, Accounts, Stores.

---

## By Module

### Catalog

Owning requirements: `01/02-01` (`FR-CAT-001` … `FR-CAT-010`).

- Product master CRUD with slugs, status, and store ownership (`Must`).
- Variants with option types and option values (`Must`).
- Taxonomy tree with manual ordering (`Must`).
- Per-store price sets and enabled currencies using `Money` (`Must`).
- Public catalog API: listing, detail, and store scoping (`Must`).
- Admin catalog management API (`Must`).
- Translatable catalog text via the `Translatable` pattern (`Should`).
- Product images with upload and processing through `IFileStorage` (`Should`).
- Faceted search with filters, sorting, and pagination (`Should`).
- Bulk product import and export (`Could`).
- Product recommendations, which are explicitly out of scope (`Won't`).

### Orders

Owning requirements: `01/02-02` (`FR-ORD-001` … `FR-ORD-012`).

- Cart aggregate that becomes the placed order (`Must`).
- Line item add, update, and remove with captured unit price (`Must`).
- Ship and bill address snapshots (`Must`).
- Order state machine with only-transition status mutation (`Must`).
- Line-item and order-level adjustments (`Must`).
- Computed order totals from source plus adjustments plus tax (`Must`).
- Domain events through the outbox in the same transaction (`Must`).
- Cancel with reserved-stock release and payment reversal (`Must`).
- Returnable orders handed to the returns flow (`Must`).
- Admin order operations via `/api/admin` (`Must`).
- Customer order history via `/api/v1` (`Must`).
- Store scoping and currency integrity enforcement (`Must`).
- Order export and saved list filters (`Could`).

### Inventory

Owning requirements: `01/02-08` (`FR-INV-001` … `FR-INV-010`).

- Stock locations (`Must`).
- `StockItem` per (variant, location) (`Must`).
- `on_hand` vs `available` accounting (`Must`).
- `StockMovement` ledger for every change (`Must`).
- Reservation on checkout confirm (`Must`).
- Shipment and return movements (`Must`).
- Storefront availability endpoint (`Must`).
- Admin inventory management API (`Must`).
- Backorder policy and no-negative-stock enforcement (`Should`).
- Stock transfers between locations (`Should`).
- Low-stock threshold alerts (`Could`).

### Shipping

Owning requirements: `01/02-04` (`FR-SHP-001` … `FR-SHP-007`).

- Shipping methods and categories (`Must`).
- Zones and destination matching (`Must`).
- `IShippingCalculator` with flat, weight, and table calculators (`Must`).
- Rate quoting at the checkout delivery step (`Must`).
- Shipping cost recorded as an adjustment (`Must`).
- Free-shipping interaction with promotions (`Should`).
- Admin shipping configuration API (`Must`).
- Shipment records and tracking references (`Should`).
- Carrier rate integration beyond the built-in calculators (`Could`).

### Tax

Owning requirements: `01/02-06` (`FR-TAX-001` … `FR-TAX-008`).

- Tax jurisdiction hierarchy (country > region > city) (`Must`).
- `TaxRate` bound to a jurisdiction and `TaxCategory` (`Must`).
- Inclusive vs exclusive tax per store (`Must`).
- Tax computed on post-promotion amounts (`Must`).
- Tax stored as its own computed component, per line and per order (`Must`).
- Pluggable `ITaxZoneResolver` extension point (`Must`).
- Admin tax configuration API (`Must`).
- Tax reporting by jurisdiction (`Could`).
- External tax-service integration (`Could`).

### Promotions

Owning requirements: `01/02-07` (`FR-PRM-001` … `FR-PRM-010`).

- Promotion definition with channels, window, and usage limits (`Must`).
- Rules: coupon, user, product/variant, order total, shipping method (`Must`).
- Actions: percent/amount off order or item, fixed-price item, free shipping, Nth-item free
  (`Must`).
- Documented application order (`Must`).
- Per-line clamping so discounts never go below zero (`Must`).
- Promotions mutating orders only through adjustments (`Must`).
- Coupon code entry and validation (`Must`).
- Usage limits and eligibility accounting (`Must`).
- `IPromotionAction` extension point (`Must`).
- Admin promotion management API (`Must`).
- Promotion simulator and stacking preview in admin (`Should`).
- Automatic campaign scheduling (`Could`).

### Payments

Owning requirements: `01/02-05` (`FR-PAY-001` … `FR-PAY-010`).

- Payment method registry (`Must`).
- `IPaymentGateway` contract (`Must`).
- Stripe adapter via Stripe.net (`Must`).
- PayPal adapter via the PayPal .NET SDK (`Must`).
- Payment state lifecycle (authorize, capture, void, refund) (`Must`).
- Idempotent charges (`Must`).
- Refunds and voids through the capturing gateway (`Must`).
- Payment method availability at checkout (`Must`).
- Admin payment operations with audit (`Must`).
- No card data stored, logged, or transmitted (`Must`).
- Store-credit ledger (`Should`).
- Gateway webhook handling and reconciliation (`Should`).
- Additional gateways beyond Stripe and PayPal (`Could`).

### Returns

Owning requirements: `01/02-09` (`FR-RET-001` … `FR-RET-010`).

- Return authorization (RMA) (`Must`).
- Return items and quantities referencing original line items (`Must`).
- Return reasons (`Must`).
- Reception and inspection (`Must`).
- Refund vs store credit (`Must`).
- Restocking accepted items through inventory movements (`Must`).
- Return lifecycle and its interaction with the order (`Must`).
- Admin returns API (`Must`).
- Return status notifications (`Should`).
- Exchanges that create new order lines (`Should`).
- Customer self-service return portal (`Could`).

### Wishlists

Owning requirements: `01/02-11` (`FR-WIS-001` … `FR-WIS-004`).

- Wishlist management per customer (`Should`).
- Wishlist items referencing variants (`Should`).
- Move a wishlist item to the cart (`Should`).
- Customer wishlist API (`Should`).
- Multiple named wishlists (`Could`).
- Public or shared wishlist links (`Could`).

### Reviews

Owning requirements: `01/02-11` (`FR-REV-001` … `FR-REV-006`).

- Submit a product review with rating and body (`Should`).
- Rating aggregation and display (`Should`).
- Review moderation queue (`Should`).
- Edit and remove a review (`Should`).
- Public and admin review API (`Should`).
- Verified-purchase eligibility (`Could`).
- Helpful-vote and review sorting (`Could`).

### Accounts

Owning requirements: `01/02-10` (`FR-ACC-001` … `FR-ACC-010`).

- ASP.NET Identity user store (`Must`).
- Authentication with JWT Bearer access tokens (`Must`).
- Roles `customer` and `admin` (`Must`).
- Per-store admin assignment (`Must`).
- Registration and sign-in flows (`Must`).
- Customer profile management (`Must`).
- Customer address book (`Must`).
- Admin user and role management API (`Must`).
- Account security: password policy, lockout, reset (`Must`).
- Guest order claim on sign-in (`Should`).
- Two-factor authentication for admins (`Could`).
- Social sign-in providers (`Could`).

### Stores

Owning requirements: `01/02-12` (`FR-STO-001` … `FR-STO-005`, `FR-I18N-001` … `FR-I18N-004`).

- Store entity with ownership and status (`Must`).
- Domain resolution in production and `X-Store` header in development (`Must`).
- Enabled currencies and the `CurrencyRate` table (`Must`).
- Supported locales and per-store checkout settings (`Must`).
- Admin store settings API (`Must`).
- `Translatable` pattern for catalog text (`Must`).
- Locale selection with default-locale fallback (`Must`).
- Localized storefront API responses (`Must`).
- Admin translation management (`Should`).
- Right-to-left layout support (`Could`).
- Content pages and a CMS, which are explicitly out of scope (`Won't`).

---

## Prioritization

Priorities use MoSCoW, applied to the release cycle defined by the roadmap (`02/01`).

| Class | Meaning | Examples |
|---|---|---|
| **Must** | Required for a working store; the release cannot ship without it. | Catalog, orders, checkout, shipping, tax, payments, inventory, accounts, stores, i18n basics. |
| **Should** | Important and expected, but a first release can ship without it if a workaround exists. | Wishlists, reviews, images, faceted search, backorders, transfers, store credit, webhooks, shipment tracking, translation admin. |
| **Could** | Desirable; included only when the Must/Should work is on track. | Bulk import/export, tax reporting, external tax service, campaign scheduling, multiple wishlists, verified purchases, 2FA, social sign-in, RTL, paid-carrier rates. |
| **Won't** | Explicitly out of scope for this cycle, per the spec's non-goals. | CMS and content pages, GraphQL, Blazor, recommendation engine, native mobile apps. |

Bulk triage rules:

- An item is **Must** if a customer cannot complete a purchase, an admin cannot fulfil and
  refund one, or a locked constraint (`01/01-03`) would be violated without it.
- An item is **Won't** if the spec's out-of-scope list (`01/01-01`) names it; it is not a
  candidate for this cycle at any priority.
- Everything else defaults to **Should**, and is demoted to **Could** only when a documented
  workaround or manual process covers the first release.

The Must set spans phases P1–P5 of the roadmap. The Should set is scheduled inside P2–P6 as
capacity allows. Could items are candidate debt reductions (`10/01`) once the release ships.

---

## Open Questions

1. Whether wishlists and reviews ship in the first release or the immediately following cycle
   is fixed when release scope is frozen in `07/01`.
2. The required depth of exchanges (`FR-RET-006`) is confirmed with the returns design in `04`.
3. The exact set of admin reports is bounded by the admin user stories (`01/04-03`) and the
   operations dashboards (`08/02`).
