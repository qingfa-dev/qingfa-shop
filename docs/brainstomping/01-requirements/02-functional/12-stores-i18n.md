# Stores & i18n — Functional Requirements

What the stores and i18n concerns must do: the store entity, request-domain resolution, enabled
currencies, supported locales, and the `Translatable` catalog-text pattern with default-locale
fallback.

**Links:** [Catalog](./01-catalog.md) · [Checkout](./03-checkout.md) · [Accounts](./10-accounts.md) · [Requirements index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Stores is the root scoping concern of the platform. Every catalog, inventory, and order row
belongs to a `Store` (spec §6), and the store determines which currencies, locales, and
checkout and payment options apply. The storefront resolves the active store from the **request
domain** (with an `X-Store` header in development), while the admin shell exposes a **store
switcher** and every admin request carries a store scope (spec §7).

i18n is a cross-cutting property of catalog text. Product names/descriptions, category names,
and option-value labels are stored as per-locale rows — the **`Translatable`** pattern — and
resolved against the store's supported locales, falling back to a **default locale** when a
translation is missing (spec §6, FR-CAT-008). Money is always a strongly-typed
`(decimal, ISO-4217 currency)` value object, never a raw decimal; orders are priced in the
store's currency and conversions for reporting come from the `CurrencyRate` table (spec §6).

The multi-store data model is planned in `03/03-02`; the i18n and money model is planned in
`03/03-03`; resolved locale/currency behavior in checkout is consumed by `03/03-02` and
`03/03-03`.

Actors are defined in `01/01-02`.

---

## FR-STO-001 — Store entity

- **Actor:** Platform Admin
- **Description:** Create and manage stores, each a first-class entity that owns a domain, its
  settings, and all store-scoped data.
- **Rules:**
  - A store has a name, a default domain, a default currency, a default locale, and an
    active/inactive flag.
  - Every store-scoped entity (catalog, inventory, orders, promotions, and so on) references
    exactly one store.
  - Deactivating a store blocks new storefront traffic and new orders without deleting its data.
  - Creating stores and assigning their administrators is a Platform Admin authority.
- **Acceptance Criteria:**
  - A Platform Admin can create a store and see it in the store switcher.
  - A deactivated store returns no storefront catalog to public requests.
  - Store data is not visible through another store's scope.

---

## FR-STO-002 — Domain resolution

- **Actor:** Guest; Customer; System
- **Description:** Resolve the active store for each request from the request domain, with a
  development override header, and fail clearly when no store matches.
- **Rules:**
  - The storefront resolves the store from the request `Host`/domain against the store's
    configured domain(s).
  - In development only, an `X-Store` header may override the resolved store (spec §6).
  - A request whose domain matches no active store is rejected rather than defaulting to an
    arbitrary store.
  - Admin requests resolve the store from the authenticated admin's selected scope, not from the
    public domain.
- **Acceptance Criteria:**
  - Requests to store A's domain return store A's catalog.
  - An `X-Store` override selects the named store in development.
  - An unknown domain is rejected with a store-resolution error.

---

## FR-STO-003 — Enabled currencies

- **Actor:** Store Admin; Platform Admin
- **Description:** Declare the currencies a store accepts, and price and charge orders in the
  store's currency.
- **Rules:**
  - A store declares one default currency and a set of enabled currencies.
  - Every price and monetary amount is a `Money` value of `(decimal, ISO-4217 currency)`
    (spec §6); a bare decimal is never accepted.
  - Orders are created in the store default currency and keep that currency for their whole
    lifecycle (FR-ORD-012).
  - Reporting conversions use the `CurrencyRate` table; automatic conversion is never the source
    of a selling price (FR-CAT-007).
- **Acceptance Criteria:**
  - A variant priced in an enabled currency is sellable; one in a disabled currency is not.
  - An order whose payments mix currencies is rejected.
  - Every stored price carries an ISO-4217 currency code.

---

## FR-STO-004 — Supported locales and store settings

- **Actor:** Store Admin; Platform Admin
- **Description:** Declare the locales a store supports and expose the store settings that
  govern locale, currency, and checkout behavior.
- **Rules:**
  - A store declares a default locale and a set of supported locales.
  - Locale-sensitive text is served from the `Translatable` rows for the active locale
    (FR-I18N-002).
  - Checkout options such as guest-checkout availability and enabled payment methods are store
    settings (FR-CHK-011).
  - Changing a store's supported locales or currencies affects new requests and orders, not
    already-placed orders.
- **Acceptance Criteria:**
  - A request in a supported locale renders that locale's text where available.
  - A request in an unsupported locale falls back to the default locale.
  - Changing the store currency does not rewrite existing orders.

---

## FR-STO-005 — Admin store settings API

- **Actor:** Store Admin; Platform Admin
- **Description:** Read and update store settings (currencies, locales, checkout options) over
  `/api/admin`, scoped to the selected store.
- **Rules:**
  - Every mutation requires the `admin` role plus store scope and is written to the audit log
    (`03/04-03`).
  - Only a Platform Admin may create stores or change a store's domain and defaults.
  - Validation rejects an empty supported-currency or supported-locale set and unknown ISO
    codes.
  - The store switcher exposes only the stores the acting admin is assigned to (FR-ACC-004).
- **Acceptance Criteria:**
  - An admin scoped to store A cannot edit store B's settings.
  - Enabling a locale makes it selectable in that store's storefront.
  - A successful settings change appears in the audit log.

---

## FR-I18N-001 — `Translatable` pattern

- **Actor:** Store Admin; System
- **Description:** Store catalog presentation text as per-locale rows keyed by entity, field,
  and locale, rather than forking the catalog per language.
- **Rules:**
  - Translatable fields are product name and description, category name/slug, and option-value
    label (FR-CAT-003, FR-CAT-004, FR-CAT-008).
  - A translation row is uniquely keyed by (entity, field, locale); saving a locale updates only
    that row.
  - The base entity carries no authoritative display text beyond its default-locale row.
  - Adding or editing a translation never modifies other locale rows or the base entity's
    non-text data.
- **Acceptance Criteria:**
  - A product can hold distinct name/description rows per locale.
  - Editing the French name leaves the English row unchanged.
  - Option-value labels resolve per locale.

---

## FR-I18N-002 — Locale selection and fallback

- **Actor:** Guest; Customer; Store Admin
- **Description:** Resolve the effective locale for a request from the store's supported locales
  and fall back to the default locale when a translation is missing.
- **Rules:**
  - The effective locale is chosen from the store's supported locales based on the request's
    locale preference.
  - If a requested locale is unsupported, the store's default locale is used.
  - If a supported locale lacks a translation for a field, the default-locale value is served
    rather than an empty string.
  - Admin content editing also falls back to the default locale when a translation is missing
    (spec §6).
- **Acceptance Criteria:**
  - A French request on a store supporting French renders French text.
  - A product without a French translation renders its default-locale text.
  - An unsupported locale falls back to the store default rather than erroring.

---

## FR-I18N-003 — Localized storefront API responses

- **Actor:** Guest; Customer
- **Description:** Serve catalog and account responses in the resolved locale and currency so
  the storefront needs no client-side language logic.
- **Rules:**
  - Public `/api/v1` responses resolve translatable text to the effective locale
    (FR-I18N-002).
  - Prices are returned as `Money` in the resolved currency with its ISO-4217 code.
  - Locale and currency resolution are server-side and never trusted from a client parameter
    that conflicts with the store configuration.
  - Fallback behavior is identical between browse, detail, and search responses
    (FR-CAT-006).
- **Acceptance Criteria:**
  - A detail response carries text in the effective locale and price with a currency code.
  - Search results use the same translation fallback as detail pages.
  - The response locale matches the store's supported set.

---

## FR-I18N-004 — Admin translation management

- **Actor:** Store Admin
- **Description:** Edit translatable catalog text per locale through the admin app, with clear
  indication when a locale is missing a translation.
- **Rules:**
  - Admin APIs expose translatable fields per supported locale for products, categories, and
    option values.
  - A missing translation is shown as an empty locale row that falls back at render time, not as
    an error.
  - Effective-date and publish rules on the base entity apply to all locale rows alike.
  - Every translation mutation requires the `admin` role plus store scope and is written to the
    audit log.
- **Acceptance Criteria:**
  - An admin can add a French name to a product without touching the English row.
  - The admin UI marks fields that fall back to the default locale.
  - A translation mutation appears in the audit log.

---

## Open Questions

1. Whether customer accounts and store credit are shared across stores or kept per store is
   resolved with the multi-store model `03/03-02`.
2. The exact locale-matching algorithm (Accept-Language parsing and quality weights) and the
   fallback chain are finalized in `03/03-03`.
3. Whether a third currency mode beyond "enabled" and "default" is needed is deferred; the
   current requirement set defines only those two.
