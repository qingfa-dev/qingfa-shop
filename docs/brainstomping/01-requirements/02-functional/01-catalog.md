# Catalog — Functional Requirements

What the catalog module must do: products, variants, options, taxonomy, images, search, and
per-store pricing.

**Links:** [Orders](./02-orders.md) · [Checkout](./03-checkout.md) · [Requirements index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

The catalog is the source of everything a shopper can find and buy. It owns the **product
master** and its **variants**, the **option types/values** that distinguish variants, the
**taxonomy** used to organize and browse, product **images**, product **search with facets**,
and **per-store price sets** expressed as `Money`.

Every catalog row is scoped to a `Store` (spec §6). Pricing and text are not duplicated per
store; instead, price sets and translatable rows attach to a product/variant and resolve per
store and locale. The archived entity and relationship detail lives in the planned data doc
`03/03-01`; module contracts live in `03/02-01`.

Actors are defined in `01/01-02`. Prices are always a strongly-typed
`(decimal, ISO-4217 currency)` value object — never a raw decimal (`01/01-03`).

---

## FR-CAT-001 — Manage product masters

- **Actor:** Store Admin
- **Description:** Create, read, update, and archive a product master in the admin catalog.
  A master holds the shared presentation data (name, description, slug, status) and is the
  parent of one or more variants.
- **Rules:**
  - A product belongs to exactly one `Store` and is never visible in another store's catalog.
  - A product has a lifecycle status of draft or active; only active products are exposed
    through the public `/api/v1` catalog.
  - Slug is unique within a store; names may repeat.
  - Archiving a product hides it from the storefront without deleting order history.
- **Acceptance Criteria:**
  - An admin can create a product and retrieve it by id within the same store.
  - A draft product does not appear in public listing or search results.
  - The same slug cannot be created twice in one store; the second attempt is rejected with a
    validation error.

---

## FR-CAT-002 — Manage variants under a master

- **Actor:** Store Admin
- **Description:** Every product master exposes at least one buyable variant. A variant is the
  unit that carries a SKU, price set, stock relationship, and option-value combination.
- **Rules:**
  - A master always has a **default variant**, so a product with no real options is still
    purchasable.
  - SKU is unique across the platform; duplicate SKUs are rejected.
  - Each variant references its master and a set of option values.
  - A variant without an active price in the current store currency is not purchasable.
- **Acceptance Criteria:**
  - Creating a master creates its default variant in the same transaction.
  - Two variants with the same SKU cannot coexist.
  - The storefront rejects add-to-cart for a variant with no active price.

---

## FR-CAT-003 — Option types and option values

- **Actor:** Store Admin
- **Description:** Define reusable option types (for example Size, Color), their values, and
  the combinations that produce variants.
- **Rules:**
  - Option types and values are scoped to a store and reused across products.
  - A product references a subset of option types; its variants enumerate option-value
    combinations.
  - Option values carry translatable presentation labels (`01/01-03`).
  - Removing an option value from a product must not orphan an existing variant; the variant
    is either reassigned or archived as part of the change.
- **Acceptance Criteria:**
  - An admin can add a Size option type with values S/M/L and apply it to a product.
  - The catalog generates distinct variants for each selected combination.
  - A variant always resolves the option values that describe it.

---

## FR-CAT-004 — Taxonomy (categories)

- **Actor:** Store Admin; Guest; Customer
- **Description:** Maintain a hierarchical category tree (taxonomy) and associate products
  with one or more categories for browsing and filtering.
- **Rules:**
  - Categories form a tree with a single root per store; a category has one parent.
  - A product may belong to multiple categories.
  - Category names and slugs are translatable and unique among siblings.
  - Deleting a category reparents or detaches its products; it never deletes products.
- **Acceptance Criteria:**
  - An admin can move a category under a new parent without breaking existing product links.
  - The storefront can render a category tree and list products for a selected category.
  - A category's children are returned in a stable, admin-defined order.

---

## FR-CAT-005 — Product images

- **Actor:** Store Admin
- **Description:** Upload and order the images shown for a product and, where relevant, a
  variant. Media is stored through the file-storage abstraction and served by URL.
- **Rules:**
  - Images are attached to a master and may optionally override per variant.
  - Exactly one image per product is the primary image used in listings.
  - The system accepts documented image formats and derives a thumbnail rendition on upload.
  - Removing an image does not delete past order records that referenced the product.
- **Acceptance Criteria:**
  - An uploaded image is retrievable through a stable URL.
  - Marking an image primary changes the image shown in product listings.
  - A variant without its own image falls back to the master's primary image.

---

## FR-CAT-006 — Product search with facets

- **Actor:** Guest; Customer
- **Description:** Search the public catalog and refine results through facets such as
  category, option value, price range, and availability, all scoped to the current store.
- **Rules:**
  - Search matches on product name, description, and variant SKU in the active locale.
  - Facets reflect only active products that are visible in the current store.
  - Results are paginated with a documented page size and deterministic ordering.
  - Search never returns products from another store.
- **Acceptance Criteria:**
  - A query matches a product by a term present in its name.
  - Applying a category facet narrows the result set to that category.
  - Paging through results returns disjoint, non-duplicated product sets.

---

## FR-CAT-007 — Per-store price sets and currency

- **Actor:** Store Admin; Platform Admin
- **Description:** Define prices per store, per variant, per currency. Prices are `Money`
  values and are the authoritative selling prices; orders are always priced in the store
  currency.
- **Rules:**
  - A price set is keyed by (variant, store, currency) and stores a `Money` amount.
  - Every price is `(decimal, ISO-4217 currency)`; a bare decimal is never accepted as a
    price.
  - The store declares its supported currencies; the storefront shows a price only for a
    supported currency.
  - Reporting conversions use the `CurrencyRate` table, but selling prices are never derived
    from an automatic conversion.
- **Acceptance Criteria:**
  - An admin can set distinct prices for the same variant in two stores.
  - A variant priced in EUR but not in USD is not sold in a USD-only storefront context.
  - Changing a price does not rewrite the price captured on existing orders.

---

## FR-CAT-008 — Translatable catalog text

- **Actor:** Store Admin; Guest; Customer
- **Description:** Store product, category, and option-value presentation text as
  per-locale rows so a multi-locale store serves translated content without forking the
  catalog.
- **Rules:**
  - Translatable fields are name and description (product), name (category), and label
    (option value).
  - Locale is selected from the store's supported locales; the admin falls back to the
    default locale when a translation is missing.
  - A missing storefront translation falls back to the store's default locale rather than
    rendering an empty field.
- **Acceptance Criteria:**
  - A product with English and French names renders the French name in a French locale.
  - A product missing a French translation falls back to its default-locale text.
  - Adding a locale translation does not modify other locale rows.

---

## FR-CAT-009 — Store-scoped public catalog API

- **Actor:** Guest; Customer
- **Description:** Expose the catalog through `/api/v1` (listing, detail, categories, search),
  resolving the store from the request domain or the dev `X-Store` header.
- **Rules:**
  - Only active products of the resolved store are returned.
  - Detail responses include variants, option values, images, categories, and the resolved
    price in the active currency.
  - Endpoints are read-only on the public surface; all writes go through `/api/admin`.
  - Brief responses remain cacheable; authorization headers are not required to browse.
- **Acceptance Criteria:**
  - A request without an auth token can list and view active products.
  - A product from store A is absent from store B's listing response.
  - The detail response includes a `Money` price with its currency code.

---

## FR-CAT-010 — Admin catalog management API

- **Actor:** Store Admin; Platform Admin
- **Description:** Expose catalog CRUD over `/api/admin` for products, variants, options,
  images, and taxonomy, scoped to the selected store.
- **Rules:**
  - Every admin mutation requires the `admin` role plus the target store scope.
  - Mutations are recorded in the audit log (who/what/when) per `03/04-03`.
  - Validation failures return structured, field-level errors.
  - Deletes that would break order history are rejected in favor of archive.
- **Acceptance Criteria:**
  - An admin scoped to store A receives an authorization error when targeting store B.
  - A successful product update appears in the audit log.
  - A request with an invalid price format is rejected without persisting partial data.

---

## Open Questions

1. The concrete search backend (database full-text vs. an external search service) is an
   architecture decision owned by `03/02-01`; the requirement above is backend-neutral.
2. The exact set of supported image formats and derived rendition sizes is finalized with the
   file-storage documentation in `03/04-06`.
3. Whether option types may be shared across stores via a platform catalog is deferred to the
   multi-store model in `03/03-02`.
