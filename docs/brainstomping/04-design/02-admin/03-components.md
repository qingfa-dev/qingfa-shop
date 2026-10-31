# Admin Components

PrimeVue 4 component inventory for the back-office: shell, `DataTable` wrappers, form-layout patterns, and `ConfirmDialog` usage, with props, events, and the `/api/admin` calls each makes.

**Links:** [Flows](./01-flows.md) · [Screens](./02-screens.md) · [Storefront Design Tokens](../01-storefront/04-tokens.md) · [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md) · [Authentication](../../03-architecture/04-integration/03-auth.md) · [Admin stories](../../01-requirements/04-user-stories/03-admin.md) · [Design index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the component contract for `web/admin` (Nuxt 4 + PrimeVue 4). Components live in
`web/shared` when reusable across both front-ends and in `web/admin` otherwise. A **route component**
(a page) fetches data with `useFetch`/`$fetch` and passes plain DTOs down; a **presentational
component** takes props and emits events, and makes no direct HTTP call unless stated. Types mirror
the DTOs in `Resys.Shared` (`03/02-02`) and the shapes in `03/04-02`.

Conventions: props are camelCase and typed; events are kebab-case; a component never posts a computed
amount (prices, tax, totals, stock, and state transitions are server-owned). Money is formatted by
`useMoney`, never by string concatenation (`03/03-03`). PrimeVue theming uses the shared token preset
from `04/01-04`.

---

## Shell Components

### AdminShell
- **Props:** `session: AdminSession`, `stores: StoreSummary[]`, `activeStoreId: string`.
- **Events:** `select-store(id)`, `sign-out`.
- **API:** wraps the layout; the page supplies `GET /api/admin/auth/session` and
  `GET /api/admin/stores`. Renders `AdminTopbar`, `AdminNav`, and the content slot.
- **SSR:** renders the shell server-side per `04/02-01`.

### AdminTopbar
- **Props:** `session: AdminSession`, `breadcrumb: Crumb[]`.
- **Events:** `open-user-menu`, `open-switcher`.
- **API:** none; the layout owns the session read.

### StoreSwitcher
- **Props:** `stores: StoreSummary[]`, `modelValue: string`.
- **Events:** `update:modelValue(storeId)`, `discard-unsaved`.
- **API:** `GET /api/admin/stores` (layout); the selection sets `X-Store` on later calls and never
  issues a store route segment (`03/04-02` OQ1). Uses PrimeVue `Select` with a search filter.

### AdminNav
- **Props:** `active: string`, `isPlatformAdmin: boolean`.
- **Events:** none.
- **API:** none; platform-level entries (create store, assign admins) render only when
  `isPlatformAdmin`.

### UserMenu
- **Props:** `session: AdminSession`.
- **Events:** `sign-out`.
- **API:** `POST /api/admin/auth/logout`; the layout clears cookies and returns to `/login`.

---

## DataTable Wrappers

All table wrappers build on PrimeVue `DataTable` in **lazy** (server-side) mode and share the same
contract. Specialized wrappers fix the columns and the endpoint; the base `AdminDataTable` owns
pagination, sorting, filtering, selection, empty state, and the loading skeleton.

### AdminDataTable (base)
- **Props:** `value: unknown[]`, `columns: ColumnDef[]`, `totalRecords: number`, `loading?: boolean`,
  `page: number`, `perPage: number`, `sortField?: string`, `sortOrder?: 1 | -1`,
  `selectable?: boolean`, `rowKey: string`, `emptyMessage?: string`.
- **Events:** `page-change(page)`, `sort-change(field, order)`, `filter-change(filters)`,
  `row-select(rows)`, `row-action(action, row)`.
- **API:** none directly; it emits the query it needs and the page re-reads the endpoint. Never sorts
  or filters client-side over a server page.

### ProductTable
- **Props:** `products: ProductSummary[]`, `totalRecords`, `page`, `perPage`, `filters`.
- **Events:** `open(id)`, `archive(id)`, `delete(id)`, `bulk-action(action, ids)`.
- **API:** `GET /api/admin/products?status=&q=&page=&per_page=`; `DELETE /api/admin/products/{id}`
  (a `409` switches the row to **Archive**), `PATCH /api/admin/products/{id}` for archive.

### OrderTable
- **Props:** `orders: OrderSummary[]`, `totalRecords`, `page`, `perPage`, `filters`.
- **Events:** `open(id)`.
- **API:** `GET /api/admin/orders?state=&email=&number=&from=&to=&page=&per_page=`. State renders as
  a PrimeVue `Tag`.

### StockItemTable
- **Props:** `items: StockItemDto[]`, `totalRecords`, `page`, `perPage`, `filters`.
- **Events:** `open-history(id)`, `adjust(id)`.
- **API:** `GET /api/admin/stock_items?variant_id=&location_id=&page=&per_page=`; row actions call
  `GET /api/admin/stock_items/{id}/movements` and open the movement form.

### StockLocationTable
- **Props:** `locations: StockLocationDto[]`.
- **Events:** `edit(id)`, `delete(id)`, `make-default(id)`.
- **API:** `GET`/`POST`/`PATCH`/`DELETE /api/admin/stock_locations`.

### UserTable
- **Props:** `users: UserSummary[]`, `totalRecords`, `page`, `perPage`, `filters`.
- **Events:** `open(id)`, `toggle-active(id)`.
- **API:** `GET /api/admin/users?role=&q=&page=&per_page=`; never renders password hashes or
  unnecessary PII (`US-020`).

### PromotionTable
- **Props:** `promotions: PromotionSummary[]`, `totalRecords`, `page`, `perPage`, `filters`.
- **Events:** `open(id)`, `toggle-disabled(id)`, `delete(id)`.
- **API:** `GET /api/admin/promotions?is_disabled=&active_on=`; `PATCH`/`DELETE
  /api/admin/promotions/{id}`.

### TaxRateTable / ShippingMethodTable / ReturnTable
- **Props:** their DTO page plus `page`/`perPage`.
- **Events:** `edit(id)`, `delete(id)`, `open(id)`; `ReturnTable` emits only `open(id)`.
- **API:** `tax_rates`, `shipping_methods`, and `returns` respectively (`03/04-02`). Deleting a rate
  or method referenced by order history is `409`; the wrapper surfaces the problem and offers
  **Disable** instead. `ReturnTable` has no delete event because the contract exposes no
  `DELETE /api/admin/returns/{id}`; a return is processed through approve and receive, never removed.

---

## Form-Layout Patterns

Forms use PrimeVue 4 `Form` + `FormField` with a shared `AdminForm` layout. `AdminForm` is a
two-column grid above `md` and one column below; `FormField` binds the label, help text, and the
server-side error for one control.

### AdminForm (base)
- **Props:** `modelValue: object`, `schema: FieldSchema[]`, `submitting?: boolean`,
  `columns?: 1 | 2`.
- **Events:** `update:modelValue(model)`, `submit(model)`, `cancel`.
- **API:** none; the page supplies the submit handler and maps a `422` problem's field errors onto
  the matching `FormField` via `useProblem`.

### FormField
- **Props:** `name: string`, `label: string`, `required?: boolean`, `help?: string`,
  `error?: string`.
- **Events:** none.
- **API:** none; slots the control (`InputText`, `InputNumber`, `Select`, `MultiSelect`,
  `Textarea`, `Calendar`, `ToggleSwitch`, `Checkbox`).

### ProductForm
- **Props:** `product?: ProductDetail`, `locales: string[]`, `currencies: string[]`.
- **Events:** `saved(product)`, `error(problem)`.
- **API:** `POST /api/admin/products` or `PATCH /api/admin/products/{id}`; per-locale text is written
  through translation fields, with an explicit fallback note when a locale is empty (`03/03-03`).

### VariantForm
- **Props:** `variant?: VariantDto`, `optionTypes: OptionType[]`, `currencies: string[]`.
- **Events:** `saved(variant)`, `error(problem)`.
- **API:** `POST /api/admin/products/{id}/variants`, `PATCH /api/admin/variants/{id}`, and
  `PUT /api/admin/variants/{id}/prices`; a duplicate SKU or an unknown currency is `422`.

### OrderActionForms (fulfill / refund / cancel / adjust)
- **Props:** `order: OrderDto`, `payments: PaymentDto[]`, `locations: StockLocationDto[]`.
- **Events:** `done(order)`, `error(problem)`.
- **API:** `POST /api/admin/orders/{id}/fulfill|cancel|refund|adjustments`. Fulfill and cancel send
  `If-Match`; refund never exceeds captured minus prior refunds and never exposes PAN.

### MovementForm
- **Props:** `stockItem: StockItemDto`, `reasons: string[]`.
- **Events:** `saved(movement)`, `error(problem)`.
- **API:** `POST /api/admin/stock_items/{id}/movements`; a correction is a new compensating movement,
  and a movement that drives a non-backorder location negative is `409`.

### TransferForm
- **Props:** `variants: VariantSummary[]`, `locations: StockLocationDto[]`.
- **Events:** `saved(transfer)`, `error(problem)`.
- **API:** `POST /api/admin/stock_transfers`; both locations must belong to the selected store.

### PromotionEditor
- **Props:** `promotion?: PromotionDto`, `ruleKinds: string[]`, `actionKinds: string[]`.
- **Events:** `saved(promotion)`, `preview(adjustment)`, `error(problem)`.
- **API:** `POST`/`PATCH /api/admin/promotions`; `actions[].kind` resolves through
  `IPromotionAction`, and an unknown kind is `422`. Preview is client-side and never persists.

### StoreSettingsForm
- **Props:** `store: StoreDto`, `isPlatformAdmin: boolean`.
- **Events:** `saved(store)`, `error(problem)`.
- **API:** `PATCH /api/admin/stores/{id}` and `PUT /api/admin/stores/{id}/domains|currencies|locales`;
  platform admin only, `403` otherwise.

### UserDetailForm
- **Props:** `user: UserDto`, `stores: StoreSummary[]`, `isPlatformAdmin: boolean`.
- **Events:** `saved(user)`, `error(problem)`.
- **API:** `PATCH /api/admin/users/{id}` and
  `POST`/`DELETE /api/admin/users/{id}/store_assignments[/{store_id}]`.

### ImageUploader
- **Props:** `productId: string`, `images: ProductImage[]`, `maxBytes: number`.
- **Events:** `uploaded(image)`, `reordered(images)`, `error(problem)`.
- **API:** `POST /api/admin/products/{id}/images` (multipart, `file`, `position?`, `variant_id?`),
  `PATCH`/`DELETE .../images/{image_id}`; an oversized file is `413`.

---

## ConfirmDialog Usage

Destructive and irreversible actions never fire directly; they route through a `ConfirmDialog` driven
by a shared `useConfirmAction` composable. The dialog states the consequence, the affected entity, and
the scope (selected store).

```
useConfirmAction({ header, message, accept, reject })
```

- **Required for:** delete/archive product, variant, taxon, option type, tax rate, shipping method,
  promotion, or stock location; disable a method or promotion; refund a payment; cancel an order;
  approve/receive a return; revoke a store assignment; switch store with unsaved changes.
- **Pattern:** the confirm callback runs the mutation with `X-CSRF-Token` and, for `Order` and
  `StockItem`, the row's `If-Match`; a `409` keeps the dialog open and shows the problem, and only a
  `2xx` closes it and raises a success `Toast`.
- **Not required for:** non-destructive reads, creating a draft, or saving a form that can be edited
  again.

### ConfirmDialogHost
- **Props:** none.
- **Events:** none.
- **API:** none; a single host instance is mounted in `AdminShell` and consumed via PrimeVue
  `useConfirm`. `useConfirmAction` composes it with `useApiAdmin` so every accepted action carries
  CSRF and concurrency headers.

---

## Pinia Stores & Composables

| Store / composable | Owns | API calls |
|---|---|---|
| `useAdminAuth` | session summary, CSRF token, silent refresh | `POST /api/admin/auth/login|refresh|logout`; `GET /api/admin/auth/session` |
| `useStoreContext` | selected store id, `X-Store` header value, store list | `GET /api/admin/stores` |
| `useApiAdmin` | `$fetch` wrapper adding credentials, `X-Store`, `X-CSRF-Token`, `If-Match` | wraps every call above |
| `useProblem` | maps RFC 9457 problem details to field/inline errors | none |
| `useConfirmAction` | destructive-action confirmation and retry-on-409 | none |
| `useToast` | transient success/error notifications | none |
| `useMoney` | locale-aware `Money` formatting | none |

---

## Open Questions

1. Whether `AdminDataTable` exposes column visibility and density preferences per admin is a UX
   detail finalized during implementation; the base contract supports adding it without a breaking
   prop change.
2. The exact `ColumnDef` shape (field, header, width, formatter, sortable) is shared with the
   storefront tables in `04/01-03` where they overlap; a shared type is expected but not yet fixed.
3. Whether promotion preview is a dedicated endpoint or a purely client-side rule evaluation is owned
   by the promotion engine `03/02-04`; the editor emits a `preview` event either way and never
   persists it.
