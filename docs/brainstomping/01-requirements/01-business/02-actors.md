# Actors

The people and systems that interact with resys-eshop, their responsibilities, and the
surfaces they use.

**Links:** [Goals](./01-goals.md) · [Constraints](./03-constraints.md) · [Requirements index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This document defines every actor that touches the platform. Each functional requirement
and user story is written for one of these actors, and each actor maps to an authentication
or authorization posture described in the spec's §7.

Actors fall into three groups:

- **Customers and guests** — unauthenticated or customer-authenticated users of the public
  storefront (`/api/v1`).
- **Store and platform staff** — authenticated `admin` users of the back-office
  (`/api/admin`), scoped per store or across the platform.
- **External providers** — third-party systems the platform calls or is called by
  (payments, shipping, email).

The user stories that elaborate these actors are planned in `01/04-01` (browse & buy),
`01/04-02` (account), and `01/04-03` (admin).

---

## Customer

An authenticated shopper with an account on a store. A customer may shop across stores.
Whether account data is shared across stores or kept per store is left to the multi-store
data model; orders are always created in the store where checkout occurred.

**Responsibilities**

- Maintain profile, addresses, and preferences.
- Build a cart, complete checkout, and choose delivery and payment options.
- Track orders, request returns, and leave reviews.
- Manage wishlists.

**Surfaces**

- Storefront: home, category/listing, search, product detail, cart, 3-step checkout
  (delivery → payment → confirm), and account (orders, addresses, wishlists, profile).
- API: public `/api/v1` catalog, cart, checkout, account, and reviews endpoints, carrying a
  customer JWT.

**Related**: authentication in `03/04-03`; account flows in `01/04-02`; account API in
`09/03-04`.

---

## Store Admin

A staff member authorized for one or more specific stores. Every admin action is performed
in the context of the currently selected store; the admin shell provides a store switcher.

**Responsibilities**

- Manage the store's catalog: products, variations, options, images, and taxonomy.
- Configure shipping methods, tax rates, and promotions for the store.
- Operate orders: review, fulfill, ship, refund, and process returns.
- Manage inventory: stock items, locations, movements, and transfers.
- Manage store settings such as supported currencies, locales, and checkout options.
- Review reports (sales, top products).

**Surfaces**

- Admin app (`web/admin`, Nuxt 4 + PrimeVue): dashboard, catalog, orders, inventory,
  shipping/tax/promotion configuration, store settings, reports.
- API: `/api/admin` endpoints, requiring the `admin` role plus the target store scope.

**Related**: authorization in `03/04-03`; admin flows in `01/04-03`; admin guide planned in
`09/01`.

---

## Platform Admin

An operator of the deployment itself, with authority across all stores rather than a single
store scope. This is the role that bootstraps stores and their administrators.

**Responsibilities**

- Create and configure stores; assign store administrators.
- Manage the global user base and roles.
- Oversee platform-wide settings, integrations, and operational health.

**Surfaces**

- Admin app, with cross-store visibility where the store scope permits.
- API: `/api/admin` store, user, and role endpoints.
- Operational endpoints `/health/live` and `/health/ready` (see `08/02-01`).

**Related**: roles are defined in the spec §7; the store model is documented in
`03/03-02`; onboarding is planned in `09/01-01`.

---

## Guest

An unauthenticated visitor. A guest is a first-class shopper, not a degraded one: a guest can
do everything up to and including checkout, and only needs an account where identity is
required (order history, wishlists, reviews tied to a user).

**Responsibilities**

- Browse the catalog, search, and filter.
- Add items to a cart and proceed through checkout.
- Optionally register or sign in at the point where an account is required.

**Surfaces**

- Storefront browse, search, and listing pages.
- Cart and checkout pages backed by `/api/v1`.
- Registration and sign-in entry points.

**Related**: cart and checkout flows in `01/04-01`; authentication in `03/04-03`; public API
in `09/03-03`.

---

## Payment/Shipping Providers

External systems integrated behind stable abstractions. The platform integrates them; they
never receive more data than a transaction requires.

**Responsibilities**

- **Payment gateways** (`IPaymentGateway`) — authorize, capture, refund, and void payments;
  used for Stripe and PayPal. The platform stores only gateway references, never card data.
- **Shipping calculators** (`IShippingCalculator`) — return rates and services for a given
  cart and destination; may be internal flat/weight-based rules or carrier-backed.
- **Email delivery** (`IEmailSender`) — deliver transactional messages such as order
  confirmation, payment receipt, shipment, and return status.

**Surfaces**

- Outbound calls from the backend to provider APIs during checkout, fulfillment, and
  notification.
- Provider callbacks/webhooks are recorded against the originating order or payment.
- Configuration and credentials live in gateway adapters registered in DI.

**Related**: gateway adapters in `03/04-05`; extension contracts in `03/02-07`; email/files
in `03/04-06`.

---

## Actor-to-Surface Summary

| Actor | Auth posture | Primary API | Primary app |
|---|---|---|---|
| Guest | None | `/api/v1` | `web/storefront` |
| Customer | Customer JWT | `/api/v1` | `web/storefront` |
| Store Admin | Admin JWT + store scope | `/api/admin` | `web/admin` |
| Platform Admin | Admin JWT (cross-store) | `/api/admin` | `web/admin` |
| Payment/Shipping Providers | Server-to-server / signed callbacks | provider APIs | none |

## Open Questions

1. Whether a customer account is shared across stores or per store is resolved with the
   multi-store data model in `03/03-02`; this document assumes store-scoped orders either
   way.
2. Whether a dedicated support/supervisor role is needed beyond `customer` and `admin` is
   deferred to the accounts requirements in `01/02-10`.
