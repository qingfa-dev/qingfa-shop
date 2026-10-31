# i18n & Money

The `Translatable` per-locale row pattern, the `Money` owned type, per-store price sets, the `CurrencyRate` reference table, and the rounding rules.

**Links:** [Entity model](./01-entities.md) · [Multi-store scoping](./02-multi-store.md) · [Migrations](./04-migrations.md) · [Tax engine](../02-components/05-tax-engine.md) · [Catalog requirements](../../01-requirements/02-functional/01-catalog.md) · [Stores & i18n requirements](../../01-requirements/02-functional/12-stores-i18n.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Two cross-cutting representations sit under the whole model: localized catalog text and typed
money. Localized text uses the **`Translatable`** pattern — one row per entity, field, and locale,
resolved against the store's supported locales with a default-locale fallback (`FR-I18N-001`,
`FR-I18N-002`). Money is always a strongly-typed `(decimal, ISO-4217 currency)` value object; a bare
decimal is never accepted as an amount (spec §6). This file defines both, the per-store price sets
that carry selling prices, the global `CurrencyRate` table used only for reporting conversions, and
the rounding rules every engine shares. The entity tables are `03/03-01`; store resolution is
`03/03-02`; the tax engine's per-line rounding is `03/02-05`.

---

## The `Translatable` Pattern

Catalog presentation text is stored as per-locale rows rather than forking the catalog per language.
A translation row is uniquely keyed by `(Entity, EntityId, Field, Locale)`; the base entity carries
no authoritative display text beyond its default-locale row (`FR-I18N-001`).

**Shared shape**

| Column | Type | Notes |
|---|---|---|
| `Id` | `bigint` | Identity. |
| `EntityId` | typed id | The owning row. |
| `Field` | string | The translatable field name. |
| `Locale` | string (BCP-47) | The locale this row translates. |
| `Value` | string | The translated text. |
| `UpdatedAtUtc` | instant | Written through `IClock`. |

**Concrete tables** (`03/03-01`)

| Table | Key | Fields | Constraints / indexes |
|---|---|---|---|
| `ProductTranslation` | `Id` | `ProductId`, `Locale`, `Name`, `Description` | Unique `(ProductId, Locale)`. |
| `TaxonTranslation` | `Id` | `TaxonId`, `Locale`, `Name`, `Slug` | Unique `(TaxonId, Locale)`; slug unique among siblings per locale. |
| `OptionValueTranslation` | `Id` | `OptionValueId`, `Locale`, `Label` | Unique `(OptionValueId, Locale)`. |

**Resolution rules**

- The effective locale is chosen from the store's supported locales based on the request's locale
  preference; an unsupported locale uses the store's default (`FR-I18N-002`).
- If a supported locale lacks a row for a field, the store's default-locale value is served rather
  than an empty string; the same fallback applies to browse, detail, search, and admin editing
  (`FR-I18N-003`, `FR-I18N-004`).
- Saving one locale updates only that row and never touches other locales or the base entity's
  non-text data (`FR-I18N-001`).
- Translation mutations require `admin` plus store scope and are written to the audit log.

---

## The `Money` Owned Type

`Money` is a value object of exactly two components: a decimal `Amount` and a `Currency` ISO-4217
alphabetic code.

| Member | Type | Mapping |
|---|---|---|
| `Amount` | `decimal` | Column `decimal(18,4)`; never `double`/`float`. |
| `Currency` | `string` (3) | Column `char(3)`, uppercase ISO-4217 (`USD`, `EUR`). |

EF Core configuration (`03/03-01`):

- `Money` is mapped as an **owned type** on every entity that holds an amount. Each such
  property expands to two columns, conventionally `<Property>_Amount` and `<Property>_Currency`
  (for example `UnitPrice_Amount`, `UnitPrice_Currency`).
- No entity stores a raw decimal amount; a bare decimal is rejected at the boundary (`FR-CAT-007`,
  `FR-STO-003`).
- Equality and hashing use both components, so `10 USD` and `10 EUR` are distinct values.
- Arithmetic is permitted only between amounts of the same currency; combining mismatched
  currencies throws, which is how `FR-ORD-012` (an order uses one currency for life) is enforced.
- Orders always use the store's default currency; an attempt to attach a `Money` of a different
  currency is rejected (`FR-STO-003`, `FR-ORD-012`).

---

## Per-Store Price Sets

Selling prices are explicit, per store, per variant, per currency. They are never derived from an
automatic conversion (`FR-CAT-007`).

| Table | Key columns | Relationships | Constraints / indexes |
|---|---|---|---|
| `ProductPrice` | `Id`, `VariantId`, `StoreId`, `Price` (Money) | Variant, Store (required) | Unique `(VariantId, StoreId, Price_Currency)`; check `Price_Amount >= 0`. |

There is no standalone `Currency` column: the currency is owned by `Price`, so it is stored once as
`Price_Currency` and the uniqueness key is that same column.

- A variant is purchasable in a store only when an active `ProductPrice` exists for the store's
  currency; otherwise add-to-cart is rejected (`FR-CAT-002`, `FR-CAT-009`).
- The same variant may carry distinct prices in two stores and two currencies (`FR-CAT-007`).
- `ICatalogPricingQuery.GetPriceAsync(variant, store, currency)` is the only truthful source of a
  line-item price (`03/02-02`); the price is captured onto the `LineItem` at add-time and never
  rewritten by a later price change (`FR-ORD-002`).
- The storefront shows a price only for a currency in `StoreCurrency`; an unsupported currency is
  not offered.

---

## `CurrencyRate`

`CurrencyRate` is **global reference data**, not store-scoped. It exists solely to convert amounts
for reporting and analytics; it is never the source of a selling price or a charge.

| Column | Type | Notes |
|---|---|---|
| `Id` | `bigint` | Identity. |
| `BaseCurrency` | `char(3)` | Source ISO-4217 code. |
| `QuoteCurrency` | `char(3)` | Target ISO-4217 code. |
| `Rate` | `decimal(18,8)` | 1 `BaseCurrency` = `Rate` `QuoteCurrency`. |
| `EffectiveFrom` | date | Start of validity. |
| `EffectiveTo` | date? | Optional end of validity. |

- Unique `(BaseCurrency, QuoteCurrency, EffectiveFrom)`.
- The conversion rate effective at a report's as-of date is used; a missing pair means no reporting
  conversion rather than a silent identity rate.
- Because the table is not store-scoped, it is exempt from the per-store query filter
  (`03/03-02`).

---

## Rounding Rules

Rounding is centralized so every engine produces identical results for identical inputs.

- **Minor units.** Each currency's scale comes from ISO-4217: 0 decimals (JPY), 2 (USD, EUR), or 3
  (BHD). `Money.Round()` rounds to the currency's minor units.
- **Mode.** Rounding is **half-up, midpoint away from zero** (`2.5 → 3`, `−2.5 → −3`). This matches
  the tax engine's per-line rule (`03/02-05`).
- **Per line, then sum.** Tax is rounded per line and the rounded values are summed to the order
  tax, so repeated computation does not drift (`FR-TAX-006`).
- **Promotion allocation.** An order-scoped discount is allocated across lines with half-up
  rounding; any remainder is assigned to the line with the largest remaining amount so the shares
  sum exactly to the applied total (`03/02-04`).
- **Storage versus display.** Amounts are stored at `decimal(18,4)` and rounded to minor units at
  the point a charge or an order total is computed. The front-end formats `(amount, currency)` via
  `web/shared`'s money helper and never re-rounds (`03/02-01`).

## Open Questions

1. The exact locale-matching algorithm (Accept-Language parsing and quality weights) and the
   fallback chain are finalized with the storefront i18n integration; the row model here is fixed.
2. Whether the platform ships exchange-rate ingestion or `CurrencyRate` is populated manually is an
   operations concern owned by `08/01-01`.
3. The concrete set of currency scales beyond the ISO-4217 defaults is read from the runtime
   globalization data; no custom scale table is planned.

## Links

- [Entity model](./01-entities.md)
- [Multi-store scoping](./02-multi-store.md)
- [Migrations](./04-migrations.md)
- [Tax engine](../02-components/05-tax-engine.md)
- [Catalog requirements](../../01-requirements/02-functional/01-catalog.md)
- [Stores & i18n requirements](../../01-requirements/02-functional/12-stores-i18n.md)
- [Design spec](../../11-spec/README.md) — §6 (money, i18n).
- Module contracts: `03/02-02`; operations topology: `08/01-01`.
