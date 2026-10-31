# Multi-Store Scoping

How every commerce row binds to one `Store`, how a request resolves its store, how admin authority is scoped per store, and how query filters enforce isolation.

**Links:** [Entity model](./01-entities.md) · [i18n & money](./03-i18n-money.md) · [Migrations](./04-migrations.md) · [App topology](../01-overview/02-app-topology.md) · [Module contracts](../02-components/02-module-contracts.md) · [Stores & i18n requirements](../../01-requirements/02-functional/12-stores-i18n.md) · [Accounts requirements](../../01-requirements/02-functional/10-accounts.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Store scoping is the root isolation rule of resys-eshop: catalog, inventory, order, promotion,
payment, and return data all belong to exactly one `Store` and are never visible through another
store's scope (spec §6, `FR-STO-001`). This file states that rule, defines the `Store` entity and
its settings, fixes how a request resolves its store on each surface, describes per-store admin
assignment, and lists the data-isolation implications and EF Core query filters that make the rule
real rather than conventional. Request-time resolution in the deployment is `03/01-02`; the entity
tables are `03/03-01`; store-owned currency and locale data is `03/03-03`.

---

## The Scoping Rule

1. **One store per commerce row.** Every store-scoped entity carries a non-nullable `StoreId` FK
   and is queried only within the resolved store. This covers products, variants, options,
   taxonomy, prices, stock, orders, payments, promotions, tax, returns, wishlists, and reviews.
2. **No cross-store references.** A row never points at another store's row. A shipping location,
   tax category, promotion, or payment method from store A is never selectable for store B's
   operations (`FR-STO-001`, `FR-INV-001`).
3. **Store is immutable once set.** An order cannot move between stores, and its currency is fixed
   when the cart is created and kept for the order's whole lifecycle (`FR-ORD-012`).
4. **Global versus store-scoped data.** Identity users are platform-wide and may shop in multiple
   stores (`FR-ACC-001`); `CurrencyRate` is global reference data (`03/03-03`). Everything a
   shopper buys, and everything an admin configures, is store-scoped.
5. **Deactivation, not deletion.** A deactivated store blocks new storefront traffic and new orders
   while retaining all its data (`FR-STO-001`).

---

## The `Store` Entity

| Field | Type | Meaning |
|---|---|---|
| `Id` | `StoreId` | Identity; the key on every store-scoped FK. |
| `Name` | string | Display name. |
| `Code` | string | Stable human key used by the `X-Store` dev override; unique. |
| `DefaultDomain` | string | Primary host mapped to this store; unique. |
| `DefaultCurrency` | ISO-4217 | Currency every order in the store is priced in. |
| `DefaultLocale` | string | Fallback locale when a translation is missing. |
| `IsActive` | bool | Deactivating blocks new traffic and orders without deleting data. |

Related child tables (`03/03-01`):

- `StoreDomain` — every host that resolves to the store, including the default; unique `Host`.
- `StoreCurrency` — enabled currencies with exactly one default; a variant priced only in a
  disabled currency is not sellable (`FR-STO-003`).
- `StoreLocale` — supported locales with exactly one default; a requested unsupported locale falls
  back to the default (`FR-I18N-002`).
- `StoreSetting` — key/value settings: guest-checkout availability, enabled payment methods,
  tax-inclusive flag, reservation-expiry window, review-eligibility mode.

Changing a store's currencies, locales, or settings affects new requests and orders only; it never
rewrites already-placed orders (`FR-STO-004`, `FR-ORD-012`).

---

## Store Resolution

A request is mapped to exactly one store before any store-scoped query runs. Resolution is
deterministic and is implemented in middleware that attaches the resolved store to the request
scope as `IStoreContext` (`03/02-02`); the ordering and deployment context are `03/01-02`.

**Resolution order (storefront and API):**

1. **Explicit store id** — an authenticated admin request may name a store directly; the admin
   store switcher sends the store id. Admin access is additionally checked against the caller's
   per-store assignment (below).
2. **`X-Store` header (development only)** — in non-production environments a client may name a
   store by code or id. This exists to test multi-store behavior without DNS and is disabled in
   production (`FR-STO-002`).
3. **Request domain** — the storefront resolves the store from the request `Host` against
   `StoreDomain`; the first matching active store wins.
4. **Default store (development only)** — when no mapping matches in a development environment, a
   configured default store is used. In production an unmapped domain is rejected rather than
   defaulting to an arbitrary store (`FR-STO-002`).

Admin requests resolve the store from the authenticated admin's selected scope, not from the public
domain. An unknown or inactive store returns a not-found error in production.

---

## Per-Store Admin Assignment

Admin authority is two-dimensional: the `admin` role grants access to `/api/admin`, and a per-store
assignment grants authority within specific stores (spec §7, `FR-ACC-004`).

- `AdminStoreAssignment` is a join row `(UserId, StoreId)` with a composite PK (`03/03-01`). It is
  the authority for store scope.
- **Store Admin** — assigned to one or more stores; authorized only within those stores.
- **Platform Admin** — spans all stores, creates stores, and changes a store's domain and defaults
  (`FR-STO-005`).
- `IAdminAccessPolicy.CanManageStoreAsync(user, store)` is the single check the admin surface calls
  before a store-scoped mutation (`03/02-02`).
- The store switcher exposes only the stores the acting admin is assigned to; changing stores
  re-resolves the scope per request and re-checks authorization without requiring re-authentication
  (`03/01-02`).
- Unassigning an admin revokes authority in that store immediately, without deleting the user;
  every assignment and removal is written to the audit log (`FR-ACC-004`, `03/04-03`).

A user may hold both the `customer` and `admin` roles; effective authority is the union of granted
scopes (`FR-ACC-003`).

---

## Data-Isolation Implications

- **Query filters.** Each module's `DbContext` applies a global query filter on every store-scoped
  entity, for example `modelBuilder.Entity<Order>().HasQueryFilter(o => o.StoreId == _storeContext.StoreId)`.
  The filter reads the resolved store from the scoped `IStoreContext`, so a query can never return
  another store's rows even if an endpoint omits an explicit `Where`.
- **Writes carry the scope.** Inserts set `StoreId` from `IStoreContext`, never from a client
  field; a client-supplied store is ignored or rejected (`FR-CAT-010`, `FR-WIS-004`).
- **Cross-store reads go through contracts.** A module that needs another module's store-scoped
  data calls a `Resys.Core` contract that takes a `StoreId` argument; it never joins across
  module tables (`03/02-02`).
- **Admin cross-store by exception only.** Platform-admin reporting may aggregate across stores;
  that path bypasses the per-store filter deliberately and is itself authorized as platform-wide.
- **Physical separation.** Each module owns a PostgreSQL schema (`catalog`, `orders`, `inventory`,
  …) in one database (`03/02-01`); schema separation is logical, not a substitute for the store
  filter.
- **Unfiltered tables.** `Store` itself, the global `CurrencyRate` table, Identity tables, and
  `OutboxMessage`/`AuditLog` (which carry an optional `StoreId`) are not subject to the per-store
  filter; access to them is constrained by authorization instead.

Isolation is verified by integration tests that assert a store-A query never returns store-B rows
and that a store-A admin receives a forbidden response for store-B resources (`06/01-01`).

## Open Questions

1. Whether customer accounts and store credit are shared across a store's sibling stores or kept
   strictly per store is settled by the store-scoped `StoreCreditEntry` table here; a shared-wallet
   model would require a schema change.
2. The exact admin store-switcher transport (header versus route segment) is finalized in the admin
   API contract `03/04-02`.
3. Whether platform-level catalog or promotion templates may be shared across stores is deferred to
   the admin catalog API `03/04-02`; the default is per-store ownership.

## Links

- [Entity model](./01-entities.md)
- [i18n & money](./03-i18n-money.md)
- [Migrations](./04-migrations.md)
- [App topology](../01-overview/02-app-topology.md)
- [Module contracts](../02-components/02-module-contracts.md)
- [Stores & i18n requirements](../../01-requirements/02-functional/12-stores-i18n.md)
- [Accounts requirements](../../01-requirements/02-functional/10-accounts.md)
- [Design spec](../../11-spec/README.md) — §6 (store scoping), §7
  (auth and store scope).
- Admin API: `03/04-02`; auth integration: `03/04-03`; test strategy: `06/01-01`.
