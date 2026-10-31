# Admin Settings

Configure shipping, tax, promotions, store currencies and locales, and manage users, roles, and store assignments.

**Links:** [Admin Flows](../../04-design/02-admin/01-flows.md) · [Admin Screens](../../04-design/02-admin/02-screens.md) · [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md) · [Tax Engine](../../03-architecture/02-components/05-tax-engine.md) · [Promotion Engine](../../03-architecture/02-components/04-promotion-engine.md) · [User docs index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

The settings area configures how the storefront behaves for a store and who may change it. It covers
shipping methods, categories, and zones; tax categories, jurisdictions, and rates; promotions and
coupons; payment methods; store currencies and locales; and users, roles, and per-store assignments.
The journey is `04/02-01` Journey 4 and Journey 6; the screens are `04/02-02`; the endpoints are the
configuration, stores, and users sections of `03/04-02`.

Settings are store-scoped unless stated otherwise. Changing a setting affects **new** computations
and orders only; it never rewrites placed orders (`03/03-02`). Every mutation requires the `admin`
role plus store scope and is written to the audit log.

---

## Shipping

Open **Settings → Shipping** (`/settings/shipping`). Three related objects are managed here.

### Categories

Shipping categories group variants into shipping classes. Manage them with
`GET`/`POST /api/admin/shipping_categories` and
`{ "name", "position"? }`. A method belongs to one category.

### Methods

Manage shipping methods with `GET`/`POST`/`PATCH`/`DELETE /api/admin/shipping_methods` and
`{ "name", "calculator_key", "configuration", ... }`.

- The `calculator_key` resolves through the keyed `IShippingCalculator` registry; an unknown key is
  `422` (`03/02-07`).
- Built-in calculators include flat rate and free shipping; a third party adds more by implementing
  the contract and registering it in DI — no core source change (`03/02-07`).
- Disabling a method removes it from new quotes only; in-flight order costs are unchanged (`US-014`).
- Deleting a method referenced by order history is `409`; disable it instead.

### Zones

Manage zones with `GET`/`POST`/`PATCH /api/admin/shipping_zones` and
`{ "name", "country_codes", "region_codes", "postal_ranges" }`.

- A method is offered only when the destination falls in one of its zones.
- When several zones match, the **most specific match wins** (`US-014`).
- A shipping method is never offered in another store.

---

## Tax

Open **Settings → Tax** (`/settings/tax`).

### Categories

Tax categories classify what is taxed. Manage with `GET`/`POST`/`PATCH /api/admin/tax_categories`
and `{ "name", "is_default"? }`. Every taxable line resolves to a category; the default covers
otherwise-uncategorised items.

### Jurisdictions

Jurisdictions form a hierarchy: city > region > country. Manage them with
`GET`/`POST /api/admin/jurisdictions` and `{ "level", "parent_id"?, "code", "name" }`.

- The **most specific jurisdiction wins**, with fallback to broader levels (`03/02-05`).
- A jurisdiction's level and parent define where it sits in the tree; a conflicting level/parent is
  `422`.

### Rates

Tax rates are effective-dated. Manage with `GET`/`POST`/`PATCH`/`DELETE /api/admin/tax_rates` and
`{ "jurisdiction_id", "tax_category_id", "rate", "effective_from", "effective_to"? }`.

- A rate applies only when both the destination jurisdiction and the line's tax category match
  (`US-015`).
- Changes affect **new computations only**; placed orders keep their captured tax (`03/02-05`).
- Negative rates and conflicting rates at the same specificity and category are rejected.
- Deleting a rate referenced by order history is `409`; supersede it with a new effective date
  instead.

---

## Promotions and Coupons

Open **Settings → Promotions** (`/settings/promotions`). The list reads
`GET /api/admin/promotions?is_disabled=&active_on=` and is store-scoped. Create and edit with
`POST`/`PATCH /api/admin/promotions` and
`{ "name", "channels", "starts_at", "ends_at", "rules", "actions", ... }`; open one with
`GET /api/admin/promotions/{id}`.

- A promotion applies only within its window and channels and while under its usage limits
  (`US-016`).
- Each `actions[].kind` resolves through `IPromotionAction`; an unknown kind is `422` (`03/02-07`).
- Promotion effects produce promotion **adjustments only** — never tax rows and never edits to
  captured prices.
- The editor can **preview** the computed adjustment for a sample order without persisting it
  (`04/02-01` Journey 4). Preview never writes.
- Duplicate coupon codes within a store are rejected.
- Disabling a promotion stops it from applying; deleting one referenced by order history is `409`.

---

## Payments

Open **Settings → Payments** (`/settings/payments`) to enable or disable payment methods for the
selected store. The write is part of `PUT /api/admin/stores/{id}/settings`.

- Enabling a method makes it available in new checkouts; disabling removes it from new checkouts only
  (`US-017`).
- Store A's method list is independent of store B's.
- A method identifier is unique within a store.
- Disabling a method never alters in-flight orders.

---

## Store Currencies and Locales

Open **Settings → Store** (`/settings/store`). This screen edits the store's identity and its
language/currency sets.

| Change | Call | Who may do it |
|---|---|---|
| Name, default currency, default locale, settings | `PATCH /api/admin/stores/{id}` | Platform admin |
| Domains | `PUT /api/admin/stores/{id}/domains` | Platform admin |
| Currencies and the default | `PUT /api/admin/stores/{id}/currencies` | Platform admin |
| Locales and the default | `PUT /api/admin/stores/{id}/locales` | Platform admin |
| Checkout and payment settings | `PUT /api/admin/stores/{id}/settings` | Platform admin |

Rules:

- A store declares a default currency and locale plus the sets of enabled currencies and supported
  locales (`US-019`).
- Only currencies enabled for the store are sellable on its storefront; prices are explicit per
  currency and never converted (`03/03-03`).
- Enabling a locale makes it selectable in that store's storefront; a missing translation falls back
  to the store default.
- Empty supported sets and unknown ISO codes are rejected (`422`).
- Store settings, domains, currencies, and locales require a **platform admin**; a store admin
  receives `403` (`03/03-02`).
- Changing store settings affects new orders only and never rewrites placed orders.
- Creating a store uses `POST /api/admin/stores` with
  `{ "name", "code", "default_domain", "default_currency", "default_locale" }` and also requires a
  platform admin.

---

## Users, Roles, and Store Assignments

Open **Users** (`/users`). The list reads
`GET /api/admin/users?role=&q=&page=&per_page=` and shows email, roles, the active flag, and store
assignments. It never exposes password hashes or unnecessary PII (`US-020`).

### Creating a User

Use `POST /api/admin/users` with
`{ "email", "password", "roles", "store_assignments" }`.

- The call creates the account directly and returns the admin `UserDto`; it never returns a
  storefront `AuthTokenDto`.
- A duplicate email is `409`; a validation failure is `422`.
- Creating a user with the `admin` role or with store assignments requires a **platform admin**
  (`403` otherwise).

### Roles and Active Status

Open a user (`GET /api/admin/users/{id}`) and edit roles or the active flag with
`PATCH /api/admin/users/{id}` and `{ "roles"?, "is_active"? }`.

- Granting the `admin` role is audited; a **store admin cannot grant platform-wide authority**
  (`US-020`).
- Deactivating a user blocks login and API access without deleting their history.
- Role changes rotate the user's security stamp, ending outstanding sessions.

### Store Assignments

Manage per-store admin scope from the user detail:

- Assign with `POST /api/admin/users/{id}/store_assignments` and `{ "store_id" }`.
- Revoke with `DELETE /api/admin/users/{id}/store_assignments/{store_id}`.

Rules:

- Assigning or revoking a store admin requires a **platform admin**.
- A duplicate assignment is `409`.
- Unassigning revokes authority **immediately** on the user's next request without deleting the user
  (`03/03-02`).
- Only stores the acting admin is assigned to appear in their store switcher (`US-021`).

---

## Common Tasks

| Task | Where | Call |
|---|---|---|
| Add a shipping method | Settings → Shipping | `POST /api/admin/shipping_methods` |
| Add a shipping zone | Settings → Shipping | `POST /api/admin/shipping_zones` |
| Add a tax rate | Settings → Tax | `POST /api/admin/tax_rates` |
| Create a promotion | Settings → Promotions | `POST /api/admin/promotions` |
| Enable a payment method | Settings → Payments | `PUT /api/admin/stores/{id}/settings` |
| Set store currencies/locales | Settings → Store | `PUT /api/admin/stores/{id}/currencies`, `.../locales` |
| Create a user | Users → Invite | `POST /api/admin/users` |
| Assign a store admin | User detail | `POST /api/admin/users/{id}/store_assignments` |

---

## Open Questions

1. The concrete report set and KPI definitions are finalized with the dashboard design in `04/02-02`
   and `09/01-05`; settings are unaffected.
2. Whether a dedicated support or supervisor role beyond `customer` and `admin` is needed is
   deferred from `01/01-02`; this guide assumes only the two locked roles.
3. The default priority values and tie-breakers for promotion application order are finalized in
   `03/02-04`.

## Links

- [Admin Flows](../../04-design/02-admin/01-flows.md)
- [Admin Screens](../../04-design/02-admin/02-screens.md)
- [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md)
- [Authentication & Authorization](../../03-architecture/04-integration/03-auth.md)
- [Tax Engine](../../03-architecture/02-components/05-tax-engine.md)
- [Promotion Engine](../../03-architecture/02-components/04-promotion-engine.md)
- [Multi-store scoping](../../03-architecture/03-data/02-multi-store.md)
- [Admin stories](../../01-requirements/04-user-stories/03-admin.md)
- [Design spec](../../11-spec/README.md) — §7 (admin configuration
  and users), §9 (admin front-end).
