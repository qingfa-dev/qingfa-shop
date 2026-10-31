# Admin Flows

Journey maps for the Nuxt 4 + PrimeVue back-office: sign in and select a store, manage the catalog, fulfill orders, adjust inventory, configure shipping/tax/promotions, and administer users.

**Links:** [Screens](./02-screens.md) · [Components](./03-components.md) · [Storefront Design Tokens](../01-storefront/04-tokens.md) · [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md) · [Authentication](../../03-architecture/04-integration/03-auth.md) · [Admin stories](../../01-requirements/04-user-stories/03-admin.md) · [Design index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file maps the end-to-end admin journeys to the screens in `04/02-02`, the components in
`04/02-03`, and the `/api/admin` calls in `03/04-02`. It realizes the admin stories `01/04-03` and is
the journey source cited by the E2E plan `06/01-04` and the admin guide `09/01-*`.

The admin is a Nuxt 4 + PrimeVue HTTP client of the headless API (ADR 0004). It authenticates with
the `admin` role plus per-store assignment, stores the JWT in an httpOnly cookie, and sends the
selected store in the `X-Store` header. Prices, tax, totals, stock, and state transitions are always
server-owned; the client never posts a computed amount (`03/04-02`).

Every mutating call (`POST`, `PUT`, `PATCH`, `DELETE`) carries the `X-CSRF-Token` double-submit
header and, where the contract requires it, an `If-Match` row version. A stale version or an illegal
state transition is `409`; a store-scope violation is `403` (`03/04-03`).

---

## Rendering Decision — SSR Initial Shell (resolves Spec §13 Open Question 1)

**Decision: `web/admin` renders a server-side (SSR) initial shell and hydrates client-side after
login. It is not a full SPA.**

Spec §13 OQ1 left the admin rendering mode tentatively at "SSR for the initial shell" and required
this file to confirm it. This file confirms that choice and fixes the boundary:

- **SSR scope.** Nuxt renders the app shell (navigation, store switcher, breadcrumbs, page frame)
  and the first page read on the server. Route-level middleware validates the session with
  `GET /api/admin/auth/session` before rendering, so an unauthenticated request is redirected to
  `/login` without flashing admin chrome.
- **CSR scope.** Data-dense regions — `DataTable` pages, editors, dialogs, charts — run as client
  components after hydration and page their own `/api/admin` reads. A filter, sort, or page change is
  a client fetch, never a full document navigation.
- **Rationale.** The admin access token lives in an httpOnly cookie scoped to `/api/admin`
  (`03/04-03`); SSR lets the Nuxt server attach the session and the `resys_admin_csrf` cookie
  server-side, so the JWT is never exposed to JavaScript. SSR also gives a fast, permission-gated
  first paint of the shell and keeps the default Nuxt 4 behaviour. A pure SPA would disable SSR per
  route, add a client-side auth round-trip before any chrome appears, and gain nothing for an
  authenticated internal tool.
- **Consequence.** No admin route is prerendered or statically generated; the shell is dynamic and
  store-aware. Components in `04/02-03` state which parts render SSR and which are client-only.

---

## Journey 0 — Session Boot, Store Switch, and Navigation

| # | Screen | Admin action | API call(s) | Result / state |
|---|---|---|---|---|
| 1 | `/login` | Submits email and password | `POST /api/admin/auth/login` | `AdminSessionDto`; three cookies set; redirect to dashboard |
| 2 | shell | Shell boots on every load | `GET /api/admin/auth/session` | Session summary + fresh CSRF token; no session → `/login` |
| 3 | shell | Access token nears expiry | `POST /api/admin/auth/refresh` | Cookies rotated transparently; the request is retried once |
| 4 | shell | Opens the store switcher | `GET /api/admin/stores` | A store admin sees assigned stores; a platform admin sees all |
| 5 | shell | Selects a store | none (client state) | `X-Store` is set for every later call; screens reload |
| 6 | shell | Signs out | `POST /api/admin/auth/logout` | Cookies cleared; return to `/login` |

**Store scope.** The selected store is the only scope an admin can act in. It is not carried in the
token; the API re-checks `IAdminAccessPolicy.CanManageStoreAsync` on every store-scoped request, so
unassigning an admin takes effect on the next call (`03/03-02`). Platform-level routes (creating a
store, changing domains, assigning admins) are reachable only by a platform admin (`03/04-02`).

---

## Journey 1 — Product Create and Edit

| # | Screen | Admin action | API call(s) | Result / state |
|---|---|---|---|---|
| 1 | `/catalog/products` | Lists and filters products | `GET /api/admin/products?status=&q=&page=&per_page=` | Store-scoped page; draft and archived products included |
| 2 | `/catalog/products/new` | Enters name, slug, description, status | `POST /api/admin/products` | `201` with `ProductDto`; a default variant is created in the same transaction |
| 3 | `/catalog/products/{id}` | Edits master fields and translations | `GET`/`PATCH /api/admin/products/{id}` | `422` maps to field errors; per-locale text written through translation fields |
| 4 | product editor | Adds option types and values | `GET`/`POST /api/admin/option_types` | Store-scoped and reusable across products (`FR-CAT-003`) |
| 5 | product editor | Adds or edits a variant | `POST /api/admin/products/{id}/variants`; `PATCH /api/admin/variants/{id}` | Duplicate SKU is rejected; removing an option value never orphans a variant |
| 6 | product editor | Sets a price per currency | `PUT /api/admin/variants/{id}/prices` | Explicit `{ amount, currency }` per store; never converted (`03/03-03`) |
| 7 | product editor | Uploads and orders images | `POST /api/admin/products/{id}/images`; `PATCH`/`DELETE .../images/{image_id}` | Multipart upload; exactly one primary; oversized file is `413` |
| 8 | `/catalog/taxonomy` | Maintains the category tree | `GET`/`POST /api/admin/taxonomies`; `POST`/`PATCH`/`DELETE /api/admin/taxons` | Single root per store; deleting a category never deletes products |
| 9 | `/catalog/products/{id}` | Archives or deletes | `DELETE /api/admin/products/{id}` | `409` when order history references it; archive instead (`US-001`) |

---

## Journey 2 — Order Fulfillment: List → Detail → Fulfill/Ship → Refund/Return

| # | Screen | Admin action | API call(s) | Result / state |
|---|---|---|---|---|
| 1 | `/orders` | Lists and filters orders | `GET /api/admin/orders?state=&email=&number=&from=&to=&page=&per_page=` | Store-scoped; filters by state, number, and date (`US-007`) |
| 2 | `/orders/{id}` | Opens one order | `GET /api/admin/orders/{id}` | Lines, addresses, adjustments, payments, shipments, and state history |
| 3 | order detail | Fulfills selected lines | `POST /api/admin/orders/{id}/fulfill` with `If-Match` | Commits `on_hand` outflow and releases the reservation (`03/02-06`) |
| 4 | order detail | Adds tracking / chooses a method | same `fulfill` call (`tracking_number`, `shipping_method_id`) | State → `shipped`; `OrderShipped` outbox row; shipment email queued |
| 5 | order detail | Refunds a captured payment | `POST /api/admin/orders/{id}/refund` | Routed to the capturing gateway; never exceeds captured minus prior refunds |
| 6 | order detail | Adds a manual adjustment | `POST /api/admin/orders/{id}/adjustments` | First-class total-affecting adjustment, `source = manual`, never tax |
| 7 | order detail | Cancels an unshipped order | `POST /api/admin/orders/{id}/cancel` | Releases reserved stock; captured payment refunded/voided; `shipped` is `409` |
| 8 | `/orders/{id}/returns` | Authorizes a return | `POST /api/admin/orders/{id}/returns` | `201` with `ReturnDto`; `409` for an ineligible order |
| 9 | `/returns/{id}` | Approves and receives | `POST /api/admin/returns/{id}/approve`; `POST /api/admin/returns/{id}/receive` | Reception records received quantities and moves the return to `received` |
| 10 | return detail | Restocks and settles | restock via `receive` + `POST /api/admin/stock_items/{id}/movements`; settle via `refund` | Restock is idempotent per accepted item; settlement refunds or grants store credit (`US-011`) |

**State machine.** Order status changes only through these transitions; an illegal transition is
`409` and changes nothing (`03/02-03`). Fulfillment and cancellation both accept `If-Match`, and a
stale row version is `409`.

---

## Journey 3 — Inventory Adjustment

| # | Screen | Admin action | API call(s) | Result / state |
|---|---|---|---|---|
| 1 | `/inventory/stock` | Lists stock by variant and location | `GET /api/admin/stock_items?variant_id=&location_id=&page=&per_page=` | Reports derived `on_hand`, `reserved`, and `available` |
| 2 | stock item | Opens movement history | `GET /api/admin/stock_items/{id}/movements` | Append-only ledger filtered by reason and date (`US-012`) |
| 3 | stock item | Records a receipt, adjustment, or correction | `POST /api/admin/stock_items/{id}/movements` | A compensating movement, never an edit; `on_hand` equals the sum of deltas |
| 4 | stock item | Corrects a prior entry | new `POST .../movements` with the opposite quantity | The original row is retained; corrections are new movements |
| 5 | `/inventory/transfers` | Transfers stock between locations | `POST /api/admin/stock_transfers` | Outbound + inbound movements; same store only; over-transfer is `409` |
| 6 | `/inventory/locations` | Manages locations and backorder policy | `GET`/`POST`/`PATCH`/`DELETE /api/admin/stock_locations` | A movement that drives a non-backorder location negative is `409` |

---

## Journey 4 — Shipping, Tax, and Promotion Configuration

| # | Screen | Admin action | API call(s) | Result / state |
|---|---|---|---|---|
| 1 | `/settings/shipping` | Manages categories and methods | `GET`/`POST /api/admin/shipping_categories`; `GET`/`POST`/`PATCH`/`DELETE /api/admin/shipping_methods` | `calculator_key` resolves through the keyed `IShippingCalculator`; unknown key is `422` |
| 2 | `/settings/shipping` | Manages zones | `GET`/`POST`/`PATCH /api/admin/shipping_zones` | Country/region/postal ranges; most specific match wins (`US-014`) |
| 3 | `/settings/tax` | Manages categories and jurisdictions | `GET`/`POST`/`PATCH /api/admin/tax_categories`; `GET`/`POST /api/admin/jurisdictions` | Jurisdiction hierarchy: city > region > country (`03/02-05`) |
| 4 | `/settings/tax` | Manages effective-dated rates | `GET`/`POST`/`PATCH`/`DELETE /api/admin/tax_rates` | Changes affect new computations only; deleting a referenced rate is `409` |
| 5 | `/settings/promotions` | Lists and opens promotions | `GET /api/admin/promotions?is_disabled=&active_on=`; `GET /api/admin/promotions/{id}` | Store-scoped; shows rules, actions, window, and limits |
| 6 | promotion editor | Creates or edits a promotion | `POST`/`PATCH /api/admin/promotions` | `actions[].kind` resolves through `IPromotionAction`; unknown kind is `422` |
| 7 | promotion editor | Previews an adjustment | client-side rule/action preview, then save | Preview never persists; a duplicate coupon code within a store is rejected |
| 8 | `/settings/payments` | Enables or disables methods | `PUT /api/admin/stores/{id}/settings` | Disabling removes it from new checkouts only (`US-017`) |

---

## Journey 5 — Store Switch

1. The shell loads the switcher with `GET /api/admin/stores`; a store admin sees only assigned
   stores, a platform admin sees all (`03/04-02`).
2. Selecting a store writes the choice to the `useStoreContext` Pinia store and to the `X-Store`
   header on every subsequent `/api/admin` call; there is no route-segment store id (`03/04-02` OQ1).
3. The current screen re-reads its endpoint under the new scope; unsaved editor state is discarded
   after a confirmation prompt (`04/02-03` `ConfirmDialog`).
4. Store settings, domains, currencies, and locales are edited under `/settings/store` with
   `PATCH /api/admin/stores/{id}` and `PUT /api/admin/stores/{id}/domains|currencies|locales`; these
   require a platform admin and return `403` for a store admin.
5. Changing store settings affects new orders only and never rewrites placed orders (`03/03-02`).

---

## Journey 6 — User Management

| # | Screen | Admin action | API call(s) | Result / state |
|---|---|---|---|---|
| 1 | `/users` | Lists and filters users | `GET /api/admin/users?role=&q=&page=&per_page=` | Never exposes password hashes or unnecessary PII (`US-020`) |
| 2 | `/users/{id}` | Opens a user | `GET /api/admin/users/{id}` | Roles, active flag, and store assignments |
| 3 | user detail | Grants or revokes the `admin` role / deactivates | `PATCH /api/admin/users/{id}` | Platform admin only for platform-wide authority; audited |
| 4 | user detail | Assigns a store admin | `POST /api/admin/users/{id}/store_assignments` | Platform admin only; duplicate assignment is `409` |
| 5 | user detail | Revokes an assignment | `DELETE /api/admin/users/{id}/store_assignments/{store_id}` | Authority revoked immediately; the user is never deleted (`03/03-02`) |
| 6 | `/users/invite` | Creates an admin or customer | `POST /api/admin/users` (`email`, `password`, `roles`, `store_assignments`) | `201` with the admin `UserDto`; duplicate email is `409`; no storefront token is returned |

---

## Cross-Journey Rules

- **Store resolution is server-side.** The selected store travels in `X-Store`; the API resolves and
  authorizes it before the handler (`03/04-02`).
- **Money is typed.** Every rendered amount is a `Money` value in the store currency; the admin
  formats but never converts (`03/03-03`). Reports convert with `CurrencyRate`, never a selling
  price.
- **CSRF and concurrency.** Mutations carry `X-CSRF-Token`; `Order` and `StockItem` mutations carry
  `If-Match`; a mismatch is `403` or `409` respectively (`03/04-03`).
- **Audit.** Every mutating admin call writes an `AuditLog` row in the same transaction as the change
  (`03/04-03`).
- **Errors.** Failures use RFC 9457 problem details; `422` maps to field errors, `403`/`409` to
  inline or toast messages (`03/04-02`).

---

## Open Questions

1. Whether the store switcher persists the last-selected store per admin (server-side preference) or
   defaults to the first assignment on each boot is a UX detail owned by `04/02-02`; the flow works
   either way.
2. The concrete KPI set on the dashboard and the report list are finalized with the screen inventory
   `04/02-02` and the admin guide `09/01-05`; `01/04-03` OQ1 owns the definitions.
3. Whether a dedicated support or supervisor role beyond `customer` and `admin` is needed remains
   deferred from `01/01-02`; the flows assume only the two locked roles.
