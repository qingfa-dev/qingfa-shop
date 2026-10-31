# REST Admin API — /api/admin

The back-office contract: every `/api/admin` endpoint for catalog CRUD, orders, inventory, configuration, stores, users, and reports, gated by the `admin` role plus store scope.

**Links:** [REST public API](./01-rest-public.md) · [Authentication](./03-auth.md) · [OpenAPI & Scalar](./04-openapi.md) · [Order lifecycle](../02-components/03-order-lifecycle.md) · [Multi-store scoping](../03-data/02-multi-store.md) · [Entity model](../03-data/01-entities.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the authoritative admin REST contract. `web/admin` consumes only these endpoints under
the base path `/api/admin`; the admin is a headless HTTP client, not an embedded UI (ADR 0004). The
public surface is `03/04-01`; the token/cookie flow and authorization model are `03/04-03`; the
generated OpenAPI document is `03/04-04`. The published user reference in `09-user/03-api/` mirrors
this contract endpoint for endpoint.

Each endpoint is rendered as `METHOD /api/admin/...`; a later parity check compares those exact
tokens against the API reference docs. DTO names live in `Resys.Shared`.

---

## Conventions

- **Base path.** Every route is prefixed `/api/admin`.
- **Authentication.** Every endpoint requires a valid admin access token (`admin` role). Public
  endpoints never grant admin access (`03/04-03`).
- **Store scope.** Store-scoped routes require the selected store in the `X-Store` header (id or
  code). The API resolves it, checks it against the caller's `AdminStoreAssignment` through
  `IAdminAccessPolicy.CanManageStoreAsync`, and binds `IStoreContext`; an unassigned store is
  `403`. This header is authorization-checked and therefore distinct from the public dev-only
  `X-Store` override, which stays disabled in production (`03/01-02`, `03/03-02`). A **platform
  admin** spans all stores and is the only caller allowed on platform-level routes.
- **Audit.** Every mutating admin call writes an `AuditLog` row (actor, action, entity, store,
  timestamp) in the same transaction as the change (`03/04-03`).
- **Money and ids.** Amounts are `{ "amount", "currency" }`; ids are UUID strings; paths are
  lowercase with `{id}` placeholders (`03/03-03`).
- **Pagination.** List endpoints accept `?page=` and `?per_page=` (default 1 and 20, max 100) and
  return `{ "data": [...], "meta": { "page", "per_page", "total", "total_pages" } }`.
- **Errors.** Failures use RFC 9457 problem details, matching `03/04-01` (400, 401, 403, 404, 409,
  413, 422). A store-scope violation is `403`; a state-machine violation is `409`; an oversized
  image upload is `413`.
- **Concurrency.** `Order` and `StockItem` mutations accept an `If-Match` row version; a stale
  version is `409` (`03/03-01`).

---

## Catalog CRUD

| Endpoint | Request | Response | Status |
|---|---|---|---|
| `GET /api/admin/products` | query: `status`, `q`, `page`, `per_page` | `ProductPageDto` | 200 |
| `POST /api/admin/products` | `CreateProductRequest` | `ProductDto` | 201, 422 |
| `GET /api/admin/products/{id}` | path id | `ProductDetailDto` | 200, 404 |
| `PATCH /api/admin/products/{id}` | `UpdateProductRequest` | `ProductDto` | 200, 404, 422 |
| `DELETE /api/admin/products/{id}` | — | — | 204, 404, 409 |
| `POST /api/admin/products/{id}/variants` | `CreateVariantRequest` | `VariantDto` | 201, 404, 422 |
| `PATCH /api/admin/variants/{id}` | `UpdateVariantRequest` | `VariantDto` | 200, 404, 422 |
| `DELETE /api/admin/variants/{id}` | — | — | 204, 404, 409 |
| `PUT /api/admin/variants/{id}/prices` | `SetPriceRequest` (`amount`, `currency`) | `VariantDto` | 200, 404, 422 |
| `POST /api/admin/products/{id}/images` | `multipart/form-data` (`file`, `position?`, `variant_id?`) | `ProductImageDto` | 201, 404, 413 |
| `PATCH /api/admin/products/{id}/images/{image_id}` | `UpdateImageRequest` (`position?`, `is_primary?`) | `ProductImageDto` | 200, 404 |
| `DELETE /api/admin/products/{id}/images/{image_id}` | — | — | 204, 404 |
| `GET /api/admin/option_types` | — | `OptionTypeListDto` | 200 |
| `POST /api/admin/option_types` | `CreateOptionTypeRequest` | `OptionTypeDto` | 201, 422 |
| `PATCH /api/admin/option_types/{id}` | `UpdateOptionTypeRequest` | `OptionTypeDto` | 200, 404 |
| `DELETE /api/admin/option_types/{id}` | — | — | 204, 404, 409 |
| `GET /api/admin/taxonomies` | — | `TaxonomyListDto` | 200 |
| `POST /api/admin/taxonomies` | `CreateTaxonomyRequest` | `TaxonomyDto` | 201, 422 |
| `POST /api/admin/taxons` | `CreateTaxonRequest` (`taxonomy_id`, `parent_id?`, `name`) | `TaxonDto` | 201, 422 |
| `PATCH /api/admin/taxons/{id}` | `UpdateTaxonRequest` | `TaxonDto` | 200, 404 |
| `DELETE /api/admin/taxons/{id}` | — | — | 204, 404, 409 |

Product/variant text is written per locale through the translation fields on the create/update
requests and stored as `Translatable` rows; a missing translation falls back to the store default
(`03/03-03`). Deleting a product or variant referenced by order history is `409`; archive instead
(`03/03-01`). Price sets are explicit per store and currency, never converted (`03/03-03`).

---

## Orders

| Endpoint | Request | Response | Status |
|---|---|---|---|
| `GET /api/admin/orders` | query: `state`, `email`, `number`, `from`, `to`, `page`, `per_page` | `OrderPageDto` | 200 |
| `GET /api/admin/orders/{id}` | path id | `OrderDto` | 200, 404 |
| `POST /api/admin/orders/{id}/fulfill` | `FulfillOrderRequest` (`shipment_lines`, `tracking_number?`, `shipping_method_id?`) | `OrderDto` | 200, 404, 409, 422 |
| `POST /api/admin/orders/{id}/cancel` | `CancelOrderRequest` (`reason?`) | `OrderDto` | 200, 404, 409 |
| `POST /api/admin/orders/{id}/adjustments` | `CreateAdjustmentRequest` (`line_item_id?`, `amount`, `label`) | `AdjustmentDto` | 201, 404, 409, 422 |
| `POST /api/admin/orders/{id}/refund` | `RefundRequest` (`amount`, `payment_id`, `reason?`) | `RefundDto` | 200, 404, 409, 422 |
| `POST /api/admin/orders/{id}/returns` | `CreateReturnRequest` (`items`, `reason_id?`) | `ReturnDto` | 201, 404, 409, 422 |
| `GET /api/admin/returns/{id}` | path id | `ReturnDto` | 200, 404 |
| `POST /api/admin/returns/{id}/approve` | `ApproveReturnRequest` (`restock_location_id?`) | `ReturnDto` | 200, 404, 409 |
| `POST /api/admin/returns/{id}/receive` | `ReceiveReturnRequest` (`items`) | `ReturnDto` | 200, 404, 409, 422 |

Order status changes only through the state machine; an illegal transition is `409` (`03/02-03`).
Fulfillment commits `on_hand` outflows and releases reservations (`03/02-06`); refunds call the
gateway through `IPaymentGateway` and never expose PAN (`03/04-05`); a manual adjustment is a
first-class total-affecting `Adjustment` with `source = manual` and never tax (`03/02-03`).

---

## Inventory

| Endpoint | Request | Response | Status |
|---|---|---|---|
| `GET /api/admin/stock_locations` | query: `is_active?` | `StockLocationListDto` | 200 |
| `POST /api/admin/stock_locations` | `CreateStockLocationRequest` (`name`, `allow_backorders`, `is_default?`) | `StockLocationDto` | 201, 422 |
| `PATCH /api/admin/stock_locations/{id}` | `UpdateStockLocationRequest` | `StockLocationDto` | 200, 404, 422 |
| `DELETE /api/admin/stock_locations/{id}` | — | — | 204, 404, 409 |
| `GET /api/admin/stock_items` | query: `variant_id`, `location_id`, `page`, `per_page` | `StockItemPageDto` | 200 |
| `GET /api/admin/stock_items/{id}/movements` | query: `page`, `per_page` | `StockMovementPageDto` | 200, 404 |
| `POST /api/admin/stock_items/{id}/movements` | `CreateStockMovementRequest` (`quantity`, `reason`, `reference?`) | `StockMovementDto` | 201, 404, 409, 422 |
| `POST /api/admin/stock_transfers` | `CreateStockTransferRequest` (`variant_id`, `from_location_id`, `to_location_id`, `quantity`) | `StockTransferDto` | 201, 404, 409, 422 |

`StockItemDto` reports derived `on_hand`, `reserved`, and `available`; movements are append-only
with reasons receipt/shipment/return/adjustment/transfer (`03/02-06`). A movement or transfer that
would drive a non-backorder location negative is `409`; a cross-store transfer is `403`.

---

## Shipping / Tax / Promotion Configuration

| Endpoint | Request | Response | Status |
|---|---|---|---|
| `GET /api/admin/shipping_categories` | — | `ShippingCategoryListDto` | 200 |
| `POST /api/admin/shipping_categories` | `CreateShippingCategoryRequest` (`name`, `position?`) | `ShippingCategoryDto` | 201, 422 |
| `GET /api/admin/shipping_methods` | query: `is_enabled?` | `ShippingMethodListDto` | 200 |
| `POST /api/admin/shipping_methods` | `CreateShippingMethodRequest` (`name`, `calculator_key`, `configuration`) | `ShippingMethodDto` | 201, 422 |
| `PATCH /api/admin/shipping_methods/{id}` | `UpdateShippingMethodRequest` | `ShippingMethodDto` | 200, 404, 422 |
| `DELETE /api/admin/shipping_methods/{id}` | — | — | 204, 404, 409 |
| `GET /api/admin/shipping_zones` | — | `ShippingZoneListDto` | 200 |
| `POST /api/admin/shipping_zones` | `CreateShippingZoneRequest` (`name`, `country_codes`, `region_codes`, `postal_ranges`) | `ShippingZoneDto` | 201, 422 |
| `PATCH /api/admin/shipping_zones/{id}` | `UpdateShippingZoneRequest` | `ShippingZoneDto` | 200, 404, 422 |
| `GET /api/admin/tax_categories` | — | `TaxCategoryListDto` | 200 |
| `POST /api/admin/tax_categories` | `CreateTaxCategoryRequest` (`name`, `is_default?`) | `TaxCategoryDto` | 201, 422 |
| `PATCH /api/admin/tax_categories/{id}` | `UpdateTaxCategoryRequest` | `TaxCategoryDto` | 200, 404 |
| `GET /api/admin/jurisdictions` | query: `level`, `parent_id?` | `JurisdictionListDto` | 200 |
| `POST /api/admin/jurisdictions` | `CreateJurisdictionRequest` (`level`, `parent_id?`, `code`, `name`) | `JurisdictionDto` | 201, 422 |
| `GET /api/admin/tax_rates` | query: `jurisdiction_id?`, `tax_category_id?` | `TaxRateListDto` | 200 |
| `POST /api/admin/tax_rates` | `CreateTaxRateRequest` (`jurisdiction_id`, `tax_category_id`, `rate`, `effective_from`, `effective_to?`) | `TaxRateDto` | 201, 422 |
| `PATCH /api/admin/tax_rates/{id}` | `UpdateTaxRateRequest` | `TaxRateDto` | 200, 404, 422 |
| `DELETE /api/admin/tax_rates/{id}` | — | — | 204, 404, 409 |
| `GET /api/admin/promotions` | query: `is_disabled?`, `active_on?` | `PromotionPageDto` | 200 |
| `POST /api/admin/promotions` | `CreatePromotionRequest` (`name`, `channels`, `starts_at`, `ends_at`, `rules`, `actions`) | `PromotionDto` | 201, 422 |
| `GET /api/admin/promotions/{id}` | path id | `PromotionDto` | 200, 404 |
| `PATCH /api/admin/promotions/{id}` | `UpdatePromotionRequest` | `PromotionDto` | 200, 404, 422 |
| `DELETE /api/admin/promotions/{id}` | — | — | 204, 404, 409 |

`calculator_key` is resolved through the keyed `IShippingCalculator` registry and `actions[].kind`
through `IPromotionAction`; an unknown key is `422` (`03/02-07`). Tax rates are effective-dated;
changes affect new computations only (`03/02-05`). Deleting a rate, method, or promotion referenced
by order history is `409`; disable instead (`03/03-01`).

---

## Stores

| Endpoint | Request | Response | Status |
|---|---|---|---|
| `GET /api/admin/stores` | — | `StoreListDto` | 200 |
| `POST /api/admin/stores` | `CreateStoreRequest` (`name`, `code`, `default_domain`, `default_currency`, `default_locale`) | `StoreDto` | 201, 403, 422 |
| `GET /api/admin/stores/{id}` | path id | `StoreDto` | 200, 403, 404 |
| `PATCH /api/admin/stores/{id}` | `UpdateStoreRequest` | `StoreDto` | 200, 403, 404, 422 |
| `PUT /api/admin/stores/{id}/domains` | `SetStoreDomainsRequest` (`hosts`) | `StoreDto` | 200, 403, 404, 422 |
| `PUT /api/admin/stores/{id}/currencies` | `SetStoreCurrenciesRequest` (`currencies`, `default`) | `StoreDto` | 200, 403, 404, 422 |
| `PUT /api/admin/stores/{id}/locales` | `SetStoreLocalesRequest` (`locales`, `default`) | `StoreDto` | 200, 403, 404, 422 |
| `PUT /api/admin/stores/{id}/settings` | `SetStoreSettingsRequest` (`settings`) | `StoreSettingsDto` | 200, 403, 404, 422 |

`GET /api/admin/stores` backs the store switcher: a store admin sees only assigned stores, a
platform admin sees all. Creating a store and changing domains, currencies, or locales require a
platform admin; a store admin receives `403`. Changing store settings affects new orders only and
never rewrites placed orders (`03/03-02`).

---

## Users / Roles

| Endpoint | Request | Response | Status |
|---|---|---|---|
| `GET /api/admin/users` | query: `role?`, `q`, `page`, `per_page` | `UserPageDto` | 200 |
| `POST /api/admin/users` | `CreateUserRequest` (`email`, `password`, `roles`, `store_assignments`) | `UserDto` | 201, 403, 409, 422 |
| `GET /api/admin/users/{id}` | path id | `UserDto` | 200, 404 |
| `PATCH /api/admin/users/{id}` | `UpdateUserRequest` (`roles?`, `is_active?`) | `UserDto` | 200, 403, 404, 422 |
| `POST /api/admin/users/{id}/store_assignments` | `CreateStoreAssignmentRequest` (`store_id`) | `UserDto` | 201, 403, 404, 409 |
| `DELETE /api/admin/users/{id}/store_assignments/{store_id}` | — | `UserDto` | 200, 403, 404 |

Assigning or revoking a store admin requires platform admin. Granting the `admin` role and each
assignment change is audited; unassigning revokes authority immediately without deleting the user
(`03/03-02`). `POST /api/admin/users` creates an account directly from `email`, `password`, `roles`,
and `store_assignments` and returns the admin `UserDto` — never the storefront `AuthTokenDto`; a
duplicate email is `409` and validation failure is `422`. Creating a user with the `admin` role or
with store assignments still requires platform admin (`403` otherwise).

---

## Reports

| Endpoint | Request | Response | Status |
|---|---|---|---|
| `GET /api/admin/reports/sales` | query: `from`, `to`, `granularity`, `currency?` | `SalesReportDto` | 200, 422 |
| `GET /api/admin/reports/top_products` | query: `from`, `to`, `limit` | `TopProductsReportDto` | 200, 422 |
| `GET /api/admin/reports/inventory` | query: `location_id?` | `InventoryReportDto` | 200 |

Reports are store-scoped by default. A platform admin may request cross-store aggregation with
`?all_stores=true`; that path deliberately bypasses the per-store filter and is itself authorized as
platform-wide (`03/03-02`). Reporting conversions use the global `CurrencyRate` table and never a
selling price (`03/03-03`).

---

## Example — Order Fulfillment

A store admin fulfills part of a paid order and ships it. The order is in `awaiting_fulfillment`.

Request:

```http
POST /api/admin/orders/5e4d3c2b-1111-2222-3333-444455556666/fulfill
X-Store: acme-us
If-Match: "AAAAAAAAB9E="
Content-Type: application/json

{
  "shipment_lines": [
    { "line_item_id": "9a8b7c6d-1234-5678-9abc-def012345678", "quantity": 2 }
  ],
  "shipping_method_id": "3f2e1d0c-aaaa-bbbb-cccc-ddddeeeeffff",
  "tracking_number": "1Z999AA10123456784"
}
```

Response `200 OK`:

```json
{
  "id": "5e4d3c2b-1111-2222-3333-444455556666",
  "number": "R100000123",
  "state": "shipped",
  "currency": "USD",
  "email": "shopper@example.com",
  "line_items": [
    { "id": "9a8b7c6d-1234-5678-9abc-def012345678", "variant_id": "b1a2c3d4-1111-2222-3333-444455556666", "name": "Classic Tee — Medium", "quantity": 2, "unit_price": { "amount": 24.00, "currency": "USD" }, "total": { "amount": 48.00, "currency": "USD" } }
  ],
  "adjustments": [
    { "source": "shipping", "label": "Standard (3–5 days)", "amount": { "amount": 5.00, "currency": "USD" } }
  ],
  "item_total": { "amount": 48.00, "currency": "USD" },
  "adjustment_total": { "amount": 5.00, "currency": "USD" },
  "tax_total": { "amount": 4.37, "currency": "USD" },
  "total": { "amount": 57.37, "currency": "USD" },
  "payments": [
    { "id": "aa11bb22-cc33-dd44-ee55-ff6677889900", "state": "captured", "amount": { "amount": 57.37, "currency": "USD" }, "gateway_reference": "pi_3Qexample" }
  ],
  "shipments": [
    { "id": "77aa88bb-99cc-00dd-11ee-22ff33445566", "state": "shipped", "shipping_method_id": "3f2e1d0c-aaaa-bbbb-cccc-ddddeeeeffff", "tracking_number": "1Z999AA10123456784", "shipped_at": "2026-09-24T09:30:00Z", "shipment_lines": [ { "line_item_id": "9a8b7c6d-1234-5678-9abc-def012345678", "quantity": 2 } ] }
  ],
  "placed_at": "2026-09-23T10:15:00Z"
}
```

In one transaction the call transitions `awaiting_fulfillment → shipped`, writes the shipment and
its lines, commits the `on_hand` outflow and releases the reservation (`03/02-06`), writes the
`OrderShipped` outbox row, and appends the audit entry (`03/02-03`, `03/04-03`). After commit the
outbox relay sends the shipment email. If the `If-Match` version is stale or the order is not in
`awaiting_fulfillment`, the call returns `409` and changes nothing.

---

## Open Questions

1. Whether the admin store selector is the `X-Store` header (chosen here) or a route segment was
   left open by `03/01-02` and `03/03-02`; this file fixes the header form, authorization-checked.
2. The exact default `Priority` values shipped with promotion templates are owned by this API; the
   engine's ordering tuple is fixed in `03/02-04`.
3. Whether stock history is exposed beyond the stock-item movement list is settled here by the
   `GET /api/admin/stock_items/{id}/movements` endpoint; a dedicated report remains deferred to
   `03/04-04`.

## Links

- [REST public API](./01-rest-public.md)
- [Authentication](./03-auth.md)
- [OpenAPI & Scalar](./04-openapi.md)
- [App topology](../01-overview/02-app-topology.md)
- [Order lifecycle](../02-components/03-order-lifecycle.md)
- [Promotion engine](../02-components/04-promotion-engine.md)
- [Tax engine](../02-components/05-tax-engine.md)
- [Inventory model](../02-components/06-inventory-model.md)
- [Extension architecture](../02-components/07-extension-architecture.md)
- [Entity model](../03-data/01-entities.md)
- [Multi-store scoping](../03-data/02-multi-store.md)
- [i18n & money](../03-data/03-i18n-money.md)
- [Accounts requirements](../../01-requirements/02-functional/10-accounts.md)
- [Stores & i18n requirements](../../01-requirements/02-functional/12-stores-i18n.md)
- [ADR 0004 — Headless API and JWT auth](../../00-governance/01-adr/04-headless-jwt.md)
- [Design spec](../../11-spec/README.md) — §7 (admin API
  surface), §9 (admin front-end).
- Public API: `03/04-01`; auth: `03/04-03`; OpenAPI: `03/04-04`; gateways: `03/04-05`; published
  reference: `09/03-01` … `09/03-06`.
