# Admin Catalog

Create and edit products, variants, options, images, and taxonomy, and set per-store pricing.

**Links:** [Admin Flows](../../04-design/02-admin/01-flows.md) · [Admin Screens](../../04-design/02-admin/02-screens.md) · [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md) · [Catalog Requirements](../../01-requirements/02-functional/01-catalog.md) · [User docs index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

The catalog area is where a store admin builds what the storefront sells. It covers the product
list, the product editor with its variants, options, images, translations, and taxonomy tabs, and
the per-store prices that make a variant sellable. The journey is `04/02-01` Journey 1; the screens
are `04/02-02`; the endpoints are the catalog section of `03/04-02`; the stories are `US-001` …
`US-006` in `01/04-03`.

Everything here is store-scoped: a product belongs to exactly one store and is invisible to another
store's catalog. Switch to the correct store before creating anything.

---

## The Product List

Open **Catalog → Products** (`/catalog/products`). The list reads
`GET /api/admin/products?status=&q=&page=&per_page=` and includes draft and archived products.

- **Search** by name or SKU (`q`).
- **Filter** by status — `draft`, `active`, or `archived`.
- **Paginate** with the table controls; pages are server-paged (default 20 rows, max 100).
- **Row actions** are Edit, Duplicate, Archive, and Delete.
- **New product** opens the editor for a fresh master.

A `DELETE /api/admin/products/{id}` that would break order history returns `409`; the row then offers
**Archive** instead. Archive hides the product from listing and search while keeping historical
orders intact. Deleting a product never deletes the order lines that reference it.

---

## Creating and Editing a Product

1. Click **New product** and enter the master fields: name, slug, description, and status.
   Submit `POST /api/admin/products`; the API returns `201` with the product and creates a **default
   variant in the same transaction**.
2. Open **Details** on an existing product and edit the master text or status with
   `PATCH /api/admin/products/{id}`.
3. Validation failures come back as `422` and are shown on the affected fields.

Rules to keep in mind:

- A **slug is unique within the store**; a duplicate is rejected.
- **Draft** products never appear in public listing or search — only **active** ones do.
- A product must belong to exactly one store and cannot be shared across stores.
- Archiving hides a product without deleting order history; deleting a referenced product is `409`.

### Translations

The **Translations** tab shows one row per locale enabled for the store. Product, category, and
option-value text is written per locale through the translation fields on the create/update requests
(`03/03-03`). A locale row left empty is not an error: the storefront falls back to the store's
default locale at render time. Adding a locale row never touches the other locale rows or the
entity's non-text data (`US-006`).

---

## Variants and Options

A product's **Variants & prices** tab lists its variants and lets you add, edit, and price them.

- Add a variant with `POST /api/admin/products/{id}/variants`; edit it with
  `PATCH /api/admin/variants/{id}`.
- **SKU is unique across the platform.** A duplicate SKU is rejected; fix the value and resubmit.
- **Option types and values** are reusable, store-scoped definitions managed on the **Options** tab
  with `GET`/`POST`/`PATCH`/`DELETE /api/admin/option_types`. Examples: Size (S, M, L), Colour
  (Black, White).
- Removing an option value never orphans a variant: the variant is reassigned or archived rather
  than left pointing at a missing value (`US-002`).
- A product with no explicit variant still has the default variant created at product creation.

### Setting Prices

Prices are set per variant, per store, and per currency:

1. Open the variant and choose the currency.
2. Enter the amount and save with `PUT /api/admin/variants/{id}/prices` and a body of
   `{ "amount", "currency" }`.

Important properties of prices:

- A price is keyed by (variant, store, currency) and stored as a `Money` value with an ISO-4217
  code (`03/03-03`).
- Only currencies enabled for the store are sellable on its storefront (`09/01-05`).
- Prices are **explicit per store and currency and are never converted** from another price.
- Changing a price never rewrites the price captured on existing orders (`US-005`).

A variant with no price in the store currency cannot be sold in that store; add the price before
activating the product.

---

## Images

Upload and order images on the **Images** tab. Images are attached to the product and optionally to
a specific variant.

- Upload with `POST /api/admin/products/{id}/images` as `multipart/form-data`, including `file` and
  optionally `position` and `variant_id`.
- Reorder or mark the primary with `PATCH /api/admin/products/{id}/images/{image_id}`
  (`position`, `is_primary`).
- Remove an image with `DELETE /api/admin/products/{id}/images/{image_id}`.

Rules:

- **Exactly one primary image per product** drives product listings.
- A variant without its own image falls back to the master's primary image.
- An uploaded image is retrievable through a stable URL and derives a thumbnail rendition.
- An oversized upload is `413`; resize or compress and try again.
- Removing an image does not delete past order records that referenced the product (`US-004`).

---

## Taxonomy

Open **Catalog → Taxonomy** (`/catalog/taxonomy`) to maintain the category tree. The left pane is the
tree; the right pane edits the selected category.

- List and create taxonomies with `GET`/`POST /api/admin/taxonomies`.
- Create, rename, move, and delete categories with `POST`/`PATCH`/`DELETE /api/admin/taxons`.
- Attach products to categories from the product editor's **Taxonomy** tab, which reads
  `GET /api/admin/taxonomies`.

Rules:

- Categories form a tree with a **single root per store** and one parent per category.
- A product may belong to **multiple** categories.
- Moving a category under a new parent preserves existing product links.
- Deleting a category reparents or detaches its products and **never deletes products** (`US-003`).
- Deleting a taxonomy or category referenced by order history is `409`; detach or rename instead.

---

## Archive vs Delete

Prefer archiving over deleting. The API refuses a hard delete (`409`) whenever a product, variant,
category, or option type is referenced by order history, because the historical order must stay
readable (`03/03-01`). Archiving hides the record from new activity while preserving the reference.
Only delete records that have never been used in an order.

---

## Common Tasks

| Task | Where | Call |
|---|---|---|
| Create a product | Products → New product | `POST /api/admin/products` |
| Edit master fields | Product editor → Details | `PATCH /api/admin/products/{id}` |
| Add a variant | Product editor → Variants & prices | `POST /api/admin/products/{id}/variants` |
| Set a price | Variant → currency + amount | `PUT /api/admin/variants/{id}/prices` |
| Manage option types | Product editor → Options | `GET`/`POST`/`PATCH`/`DELETE /api/admin/option_types` |
| Upload an image | Product editor → Images | `POST /api/admin/products/{id}/images` |
| Build the category tree | Catalog → Taxonomy | `GET`/`POST /api/admin/taxonomies`, `POST`/`PATCH`/`DELETE /api/admin/taxons` |
| Archive a product | Products → row action | `DELETE /api/admin/products/{id}` (falls back to archive on `409`) |

---

## Open Questions

1. Whether the product editor is a single page with tabs or a multi-route wizard is a UX detail
   finalized during implementation; the component contracts in `04/02-03` support both.
2. The concrete default `Priority` values shipped with promotion templates are owned by `03/04-02`;
   the promotion engine's ordering tuple is fixed in `03/02-04`.
3. The exact image size limit behind the `413` response is a configuration value finalized in
   `05/01-02`; the status contract is fixed here.

## Links

- [Admin Flows](../../04-design/02-admin/01-flows.md)
- [Admin Screens](../../04-design/02-admin/02-screens.md)
- [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md)
- [Entity model](../../03-architecture/03-data/01-entities.md)
- [i18n & money](../../03-architecture/03-data/03-i18n-money.md)
- [Catalog requirements](../../01-requirements/02-functional/01-catalog.md)
- [Admin stories](../../01-requirements/04-user-stories/03-admin.md)
- [Design spec](../../11-spec/README.md) — §7 (admin catalog CRUD),
  §9 (admin front-end).
