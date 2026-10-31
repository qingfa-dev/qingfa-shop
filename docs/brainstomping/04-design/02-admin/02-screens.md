# Admin Screens

Screen inventory and layout notes for the Nuxt 4 + PrimeVue back-office: shell, dashboard, catalog, orders, inventory, settings, users, and reports.

**Links:** [Flows](./01-flows.md) · [Components](./03-components.md) · [Storefront Design Tokens](../01-storefront/04-tokens.md) · [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md) · [Multi-store scoping](../../03-architecture/03-data/02-multi-store.md) · [Admin stories](../../01-requirements/04-user-stories/03-admin.md) · [Design index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file fixes the screen inventory and layout skeleton for `web/admin` (Nuxt 4 + PrimeVue 4). Each
screen names the components from `04/02-03` it composes and the `/api/admin` reads in `03/04-02` that
populate it. All screens render inside the shared `AdminShell`, so the layouts below describe the
content region; the shell is shown once. Routes are Nuxt file-based (`pages/orders/[id].vue`).
Tokens come from `04/01-04`, consumed by the shared PrimeVue preset.

Screens follow the SSR-initial-shell decision in `04/02-01`: the shell and the first page read render
on the server; tables, editors, and dialogs hydrate client-side.

---

## Shell Layout

```
┌──────────────────────────────────────────────────────────────────┐
│ Logo │ [Store: Acme US ▾] │ breadcrumb        │ CSRF-safe user ▾ │
├──────────────┬───────────────────────────────────────────────────┤
│ Dashboard    │  Page header (title · primary action)              │
│ Catalog      │  ┌──────────────────────────────────────────────┐  │
│ Orders       │  │  Filters / toolbar                           │  │
│ Inventory    │  ├──────────────────────────────────────────────┤  │
│ Settings     │  │  DataTable (server-paged) or editor form     │  │
│ Users        │  │                                              │  │
│ Reports      │  └──────────────────────────────────────────────┘  │
│              │  Pagination · row actions                         │
└──────────────┴───────────────────────────────────────────────────┘
```

- **Store switcher** (`StoreSwitcher`) is always visible in the top bar. It lists
  `GET /api/admin/stores`; a store admin sees only assigned stores, a platform admin sees all. The
  selection sets `X-Store` on every later call (`03/04-02`). Switching prompts to discard unsaved
  editor state.
- **Navigation** is grouped by feature area and hidden for screens the selected store cannot reach;
  platform-level entries (create store, assign admins) appear only for a platform admin.
- **User menu** shows the admin summary from `GET /api/admin/auth/session` and offers sign-out via
  `POST /api/admin/auth/logout`.
- **Global feedback.** `Toast` for success/error, `ConfirmDialog` for destructive actions, and a
  `ProgressBar` on any in-flight route fetch.

---

## Dashboard — `/`

```
┌──────────────┬──────────────┬──────────────┬──────────────┐
│ Revenue      │ Orders       │ AOV          │ Units sold   │
│ (period)     │ (period)     │ (period)     │ (period)     │
├──────────────┴──────────────┴──────────────┴──────────────┤
│ Sales over time (line/bar chart)                          │
├────────────────────────────┬──────────────────────────────┤
│ Top products (table)       │ Low stock (table)            │
├────────────────────────────┴──────────────────────────────┤
│ Recent orders (table) · Pending returns (list)            │
└────────────────────────────────────────────────────────────┘
```

Reads: `GET /api/admin/reports/sales?from=&to=&granularity=`,
`GET /api/admin/reports/top_products?from=&to=&limit=`,
`GET /api/admin/reports/inventory`, and `GET /api/admin/orders?state=&page=1&per_page=5`. The KPI
cards are client-computed from the sales report, never from cached order totals. Low-stock rows link
to the stock item in `/inventory/stock`.

---

## Catalog

### Product list — `/catalog/products`

```
[Search] [Status ▾] [Category ▾]           [+ New product]
┌────┬──────────────┬──────────┬───────┬────────┬────────┐
│ ☐  │ Name         │ SKU      │ Price │ Stock  │ Status │
├────┼──────────────┼──────────┼───────┼────────┼────────┤
│ ☐  │ Classic Tee  │ TEE-…    │ $24   │ 120    │ Active │
└────┴──────────────┴──────────┴───────┴────────┴────────┘
Row actions: Edit · Duplicate · Archive · Delete
```

Reads: `GET /api/admin/products?status=&q=&page=&per_page=`. Bulk actions run per row and report
partial failures; `DELETE` returns `409` when order history references the product, and the row then
offers **Archive** instead.

### Product editor — `/catalog/products/[id]`

Tabs: **Details** · **Variants & prices** · **Options** · **Images** · **Translations** · **Taxonomy**.

- Details edits master text and status (`PATCH /api/admin/products/{id}`).
- Variants lists variants and lets the admin add/edit one and set per-currency prices
  (`POST .../variants`, `PATCH /api/admin/variants/{id}`, `PUT .../prices`).
- Options manages reusable option types/values (`GET`/`POST`/`PATCH`/`DELETE /api/admin/option_types`).
- Images uploads and reorders (`POST .../images` multipart, `PATCH`/`DELETE .../images/{image_id}`).
- Translations shows one row per store locale with an explicit fallback note (`03/03-03`).
- Taxonomy attaches the product to taxons (`GET /api/admin/taxonomies`).

### Taxonomy — `/catalog/taxonomy`

```
┌───────────────────────────┬──────────────────────────────┐
│ Tree (root per store)     │ Selected taxon editor        │
│ ▸ Apparel                 │  Name · Parent · Position    │
│   ▸ Tops                  │  [Save] [Delete]             │
│   ▸ Bottoms               │  Products in this taxon      │
└───────────────────────────┴──────────────────────────────┘
```

Reads/writes: `GET`/`POST /api/admin/taxonomies`, `POST`/`PATCH`/`DELETE /api/admin/taxons`.
Deleting a category reparents or detaches products and never deletes them.

---

## Orders

### Order list — `/orders`

```
[Search number/email] [State ▾] [From] [To]        [Export]
┌────┬────────────┬──────────────┬────────────┬────────┬─────────┐
│ #  │ Number     │ Customer     │ Total      │ State  │ Placed  │
├────┼────────────┼──────────────┼────────────┼────────┼─────────┤
│    │ R100000123 │ shopper@…    │ $57.37     │ paid   │ Sep 23  │
└────┴────────────┴──────────────┴────────────┴────────┴─────────┘
```

Reads: `GET /api/admin/orders?state=&email=&number=&from=&to=&page=&per_page=`. State is rendered as
a `Tag`; a click opens the detail.

### Order detail — `/orders/[id]`

```
┌───────────────────────────────┬──────────────────────────────┐
│ State timeline / history      │ Customer · addresses         │
│ Line items + adjustments      │ Payments (state, gateway ref)│
│ Shipments + tracking          │ Totals (item/adjust/tax/total)│
├───────────────────────────────┴──────────────────────────────┤
│ Actions: Fulfill · Ship · Refund · Cancel · Return · Adjust  │
└──────────────────────────────────────────────────────────────┘
```

Reads: `GET /api/admin/orders/{id}`. Actions map to
`POST .../fulfill`, `POST .../refund`, `POST .../cancel`, `POST .../adjustments`, and
`POST .../returns`; each destructive action passes through `ConfirmDialog`, and `fulfill`/`cancel`
send `If-Match`. A `409` is surfaced inline without losing the form.

### Returns — `/orders/[id]/returns` and `/returns/[id]`

Authorize from the order detail (`POST /api/admin/orders/{id}/returns`); the return screen shows
items, reason, and status, then approves (`POST /api/admin/returns/{id}/approve`) and receives
(`POST /api/admin/returns/{id}/receive`). Inspection splits restockable and unsellable quantities;
restock and settlement are separate confirmed actions (`US-011`).

---

## Inventory

### Stock — `/inventory/stock`

```
[Variant search] [Location ▾]                     [Adjust stock]
┌──────────────┬──────────┬────────┬──────────┬──────────┬────────┐
│ Variant      │ Location │ On hand│ Reserved │ Available│ Actions│
├──────────────┼──────────┼────────┼──────────┼──────────┼────────┤
│ TEE-M        │ Main     │ 120    │ 8        │ 112      │ History│
└──────────────┴──────────┴────────┴──────────┴──────────┴────────┘
```

Reads: `GET /api/admin/stock_items?variant_id=&location_id=&page=&per_page=`. **History** opens a
drawer with `GET /api/admin/stock_items/{id}/movements`. **Adjust stock** opens a movement form
(`POST /api/admin/stock_items/{id}/movements`); a correction is a new compensating movement, never an
edit. A movement that drives a non-backorder location negative is `409`.

### Locations — `/inventory/locations`

Table plus editor for `GET`/`POST`/`PATCH`/`DELETE /api/admin/stock_locations`, including the
`allow_backorders` and `is_default` flags. Deleting a location referenced by history is `409`.

### Transfers — `/inventory/transfers`

Form for `POST /api/admin/stock_transfers` with variant, source, destination, and quantity; both
locations must belong to the selected store, and an over-transfer is `409`. A transfer list shows
recent transfers as outbound/inbound movement pairs.

---

## Settings

`/settings` is a tabbed area; each tab is its own route.

| Tab | Route | Reads / writes |
|---|---|---|
| Shipping | `/settings/shipping` | `shipping_categories`, `shipping_methods`, `shipping_zones` |
| Tax | `/settings/tax` | `tax_categories`, `jurisdictions`, `tax_rates` |
| Promotions | `/settings/promotions` | `promotions` (+ rule/action editor and preview) |
| Payments | `/settings/payments` | `PUT /api/admin/stores/{id}/settings` |
| Store | `/settings/store` | `PATCH /api/admin/stores/{id}`, `PUT .../domains|currencies|locales` |

- **Shipping** edits categories, methods (with `calculator_key` and its configuration), and zones;
  disabling a method removes it from new quotes only.
- **Tax** edits categories, the jurisdiction tree, and effective-dated rates; the most specific
  jurisdiction wins (`03/02-05`).
- **Promotions** lists and edits promotions with rules/actions/windows/limits and a non-persisting
  preview; a duplicate coupon code is rejected.
- **Payments** enables/disables methods per store; store A's list is independent of store B's.
- **Store** edits name, domain, currencies, and locales; platform admin only, `403` otherwise.

---

## Users

### User list — `/users`

Reads `GET /api/admin/users?role=&q=&page=&per_page=`, showing email, roles, active flag, and store
assignments. It never shows password hashes or unnecessary PII (`US-020`).

### User detail — `/users/[id]`

Reads `GET /api/admin/users/{id}`. Edits roles and active status
(`PATCH /api/admin/users/{id}`) and manages per-store assignments
(`POST`/`DELETE /api/admin/users/{id}/store_assignments[/{store_id}]`). Granting platform-wide
authority and assignment changes require a platform admin and are audited; revoking an assignment
takes effect immediately.

---

## Reports — `/reports`

```
[Date range] [Granularity ▾] [Currency ▾] [All stores ☐]
┌───────────────────────────────┬──────────────────────────────┐
│ Sales over time (chart)       │ Totals by state              │
├───────────────────────────────┴──────────────────────────────┤
│ Top products (table)          │ Inventory report (table)     │
└──────────────────────────────────────────────────────────────┘
```

Reads: `GET /api/admin/reports/sales`, `GET /api/admin/reports/top_products`, and
`GET /api/admin/reports/inventory`. Reports are store-scoped by default; `?all_stores=true` is
available only to a platform admin and is authorized platform-wide (`03/04-02`). Reporting
conversions use the `CurrencyRate` table, never a selling price.

---

## Screen → Route → Primary Read

| Screen | Route | Primary read(s) |
|---|---|---|
| Login | `/login` | `POST /api/admin/auth/login` |
| Dashboard | `/` | `reports/sales`, `reports/top_products`, `reports/inventory`, `orders` |
| Product list | `/catalog/products` | `GET /api/admin/products` |
| Product editor | `/catalog/products/[id]` | `GET /api/admin/products/{id}` (+ variants, options, images) |
| Taxonomy | `/catalog/taxonomy` | `GET /api/admin/taxonomies` |
| Order list | `/orders` | `GET /api/admin/orders` |
| Order detail | `/orders/[id]` | `GET /api/admin/orders/{id}` |
| Return detail | `/returns/[id]` | `GET /api/admin/returns/{id}` |
| Stock | `/inventory/stock` | `GET /api/admin/stock_items` |
| Locations | `/inventory/locations` | `GET /api/admin/stock_locations` |
| Transfers | `/inventory/transfers` | `POST /api/admin/stock_transfers` |
| Shipping | `/settings/shipping` | `shipping_methods`, `shipping_zones`, `shipping_categories` |
| Tax | `/settings/tax` | `tax_rates`, `jurisdictions`, `tax_categories` |
| Promotions | `/settings/promotions` | `GET /api/admin/promotions` |
| Payments | `/settings/payments` | `PUT /api/admin/stores/{id}/settings` |
| Store | `/settings/store` | `GET`/`PATCH /api/admin/stores/{id}` |
| Users | `/users` | `GET /api/admin/users` |
| User detail | `/users/[id]` | `GET /api/admin/users/{id}` |
| Reports | `/reports` | `reports/sales`, `reports/top_products`, `reports/inventory` |

---

## Open Questions

1. Whether the store switcher remembers the last-selected store per admin or resets to the first
   assignment on each boot is owned here and in `04/02-01` OQ1; either is compatible with the shell.
2. The exact dashboard KPI definitions and the low-stock threshold are finalized with the reports in
   `09/01-05`; the screen consumes whatever `reports/sales` and `reports/inventory` return.
3. Whether the product editor is a single page with tabs or a multi-route wizard is a UX detail
   finalized during implementation; the component contracts in `04/02-03` support both.
