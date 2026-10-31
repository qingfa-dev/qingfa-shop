# API Reference — Admin API

Call the `/api/admin` back-office contract: catalog CRUD, orders and returns, inventory, shipping, tax, promotions, stores, users, and reports.

**Links:** [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md) · [Authentication](./01-auth.md) · [Error Reference](./06-errors.md) · [Admin Onboarding](../01-admin-guide/01-onboarding.md) · [User docs index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the published reference for the admin REST surface. It mirrors `03/04-02`; the
token/cookie flow is `03/04-03`. Every route is under `/api/admin`, requires a valid admin access
token, and writes an `AuditLog` row in the same transaction as any mutation. Store-scoped routes
require the selected store in the authorization-checked `X-Store` header; an unassigned store is
`403`. Platform-level routes (creating stores, changing domains, currencies, or locales, and
assigning admins) are reachable only by a platform admin.

Every endpoint below uses the literal token form `METHOD /api/...` so it can be compared verbatim
with the architecture contract. All requests also carry the CSRF header on mutations and may carry
`If-Match` for a row version.

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

Text is written per locale and stored as `Translatable` rows; a missing translation falls back to
the store default. Deleting a product, variant, option type, or taxon referenced by order history is
`409`; archive instead. Price sets are explicit per store and currency and are never converted.

---

## Orders and Returns

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

Order status changes only through the state machine; an illegal transition is `409`. Fulfillment
commits `on_hand` outflows and releases reservations; a manual adjustment is a total-affecting
`Adjustment` with `source = manual` and never tax; refunds call the gateway through `IPaymentGateway`
and never expose PAN.

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

`StockItemDto` reports derived `on_hand`, `reserved`, and `available`; movements are append-only with
reasons receipt/shipment/return/adjustment/transfer. A movement or transfer that would drive a
non-backorder location negative is `409`; a cross-store transfer is `403`.

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
through `IPromotionAction`; an unknown key is `422`. Tax rates are effective-dated; changes affect
new computations only. Deleting a rate, method, or promotion referenced by order history is `409`;
disable instead.

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

`GET /api/admin/stores` backs the store switcher: a store admin sees only assigned stores, a platform
admin sees all. Creating a store and changing domains, currencies, or locales require a platform
admin; a store admin receives `403`. Changing store settings affects new orders only.

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

Assigning or revoking a store admin requires platform admin. `POST /api/admin/users` creates an
account directly from `email`, `password`, `roles`, and `store_assignments` and returns the admin
`UserDto` — never the storefront `AuthTokenDto`; a duplicate email is `409` and validation failure is
`422`. Creating a user with the `admin` role or with store assignments still requires platform admin
(`403` otherwise).

---

## Reports

| Endpoint | Request | Response | Status |
|---|---|---|---|
| `GET /api/admin/reports/sales` | query: `from`, `to`, `granularity`, `currency?` | `SalesReportDto` | 200, 422 |
| `GET /api/admin/reports/top_products` | query: `from`, `to`, `limit` | `TopProductsReportDto` | 200, 422 |
| `GET /api/admin/reports/inventory` | query: `location_id?` | `InventoryReportDto` | 200 |

Reports are store-scoped by default. A platform admin may request cross-store aggregation with
`?all_stores=true`; that path is authorized as platform-wide. Reporting conversions use the global
`CurrencyRate` table and never a selling price.

---

## Example — Create a user

```bash
curl -sS -X POST https://api.example.com/api/admin/users \
  -H 'Authorization: Bearer <admin_access_token>' \
  -H 'X-Store: acme-us' \
  -H 'X-CSRF-Token: b7d1c2e3-4f50-4617-8a92-0b1c2d3e4f50' \
  -H 'Content-Type: application/json' \
  -d '{ "email": "ops@example.com", "password": "s3cret-ops-pass", "roles": ["admin"], "store_assignments": [ "d0d0d0d0-0000-0000-0000-000000000001" ] }'
```

Response `201 Created`:

```json
{
  "id": "3f2e1d0c-aaaa-bbbb-cccc-ddddeeeeffff",
  "email": "ops@example.com",
  "roles": ["admin"],
  "is_active": true,
  "store_assignments": [
    { "store_id": "d0d0d0d0-0000-0000-0000-000000000001", "code": "acme-us" }
  ]
}
```

A duplicate email is `409`; a missing field or weak password is `422`; a caller who is not a platform
admin is `403`.

---

## Example — Fulfill an order

```bash
# POST /api/admin/orders/5e4d3c2b-1111-2222-3333-444455556666/fulfill
curl -sS -X POST https://api.example.com/api/admin/orders/5e4d3c2b-1111-2222-3333-444455556666/fulfill \
  -H 'Authorization: Bearer <admin_access_token>' \
  -H 'X-Store: acme-us' \
  -H 'X-CSRF-Token: b7d1c2e3-4f50-4617-8a92-0b1c2d3e4f50' \
  -H 'If-Match: "AAAAAAAAB9E="' \
  -H 'Content-Type: application/json' \
  -d '{ "shipment_lines": [ { "line_item_id": "9a8b7c6d-1234-5678-9abc-def012345678", "quantity": 2 } ], "shipping_method_id": "3f2e1d0c-aaaa-bbbb-cccc-ddddeeeeffff", "tracking_number": "1Z999AA10123456784" }'
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

In one transaction the call transitions `awaiting_fulfillment → shipped`, writes the shipment and its
lines, commits the `on_hand` outflow and releases the reservation, writes the `OrderShipped` outbox
row, and appends the audit entry. A stale `If-Match` version or an order not in `awaiting_fulfillment`
is `409` and changes nothing.

---

## Status Codes

| Status | Meaning |
|---|---|
| 200 | The read, update, fulfill, refund, or approve succeeded. |
| 201 | A resource was created (product, variant, adjustment, return, transfer, movement, user, store, assignment). |
| 204 | A delete or logout-style removal succeeded. |
| 400 | Malformed request. |
| 401 | Missing or invalid admin token. |
| 403 | Missing role, unassigned store, missing/mismatched CSRF, cross-store transfer, or a platform-only route. |
| 404 | The resource was not found in the resolved store. |
| 409 | State-machine violation, stale `If-Match`, duplicate, or a record referenced by order history. |
| 413 | An uploaded image is too large. |
| 422 | Semantic validation failure (field errors in `errors`). |

Every failure uses the RFC 9457 envelope in `09/03-06`. Admin routes are rate-limited per admin user.

---

## Open Questions

1. The exact default `Priority` values shipped with promotion templates are owned by `03/04-02`; the
   engine's ordering tuple is fixed in `03/02-04`.
2. Whether production exposes the generated OpenAPI documents publicly or behind admin
   authentication is a deployment decision owned by `08/01-01`; the contract itself is unchanged.
3. Whether stock history beyond the stock-item movement list is exposed remains deferred to
   `03/04-04`; the movement list endpoint is fixed here.

## Links

- [REST Admin API](../../03-architecture/04-integration/02-rest-admin.md)
- [Authentication](./01-auth.md)
- [Admin Onboarding](../01-admin-guide/01-onboarding.md)
- [Error Reference](./06-errors.md)
- [Accounts requirements](../../01-requirements/02-functional/10-accounts.md)
- [Stores & i18n requirements](../../01-requirements/02-functional/12-stores-i18n.md)
- [Design spec](../../11-spec/README.md) — §7 (admin API surface),
  §9 (admin front-end).
