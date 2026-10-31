# Business Module Implementation Guide

**Version:** 1.0
**Date:** 2026-10-31
**Author model:** DeepSeek V4 Pro (high effort)
**Scope:** WIP and missing business modules (M1–M9).
**Prerequisite:** `docs/architecture/guides/foundation-implementation.md` (F1–F11) complete. New modules must not inherit Q1–Q32.
**Ground truth:** `docs/architecture/software-quality-audit.pro.md`, `docs/architecture/roadmap-implementation-audit.pro.md`, existing `Payments`/`Todos` modules, `guide/*.md`.

---

## 0. Build order and dependency graph

| Step | Module | Depends on | Phase |
|---|---|---|---|
| 1 | **M9 — Todos cleanup** | Foundation (F4, F8) | 1 |
| 2 | **M2 — Notifications module** | Foundation (F7, F8) | 1–4 |
| 3 | **M3 — Catalog** | Notifications (optional), F4 | 2 |
| 4 | **M4 — Cart** | Catalog read model | 2 |
| 5 | **M6 — Inventory** | Catalog SKU contract | 2 |
| 6 | **M5 — Orders + checkout** | Cart, Catalog, Inventory, durable Payments (F3) | 3 |
| 7 | **M1 — Payments completion** | Orders contract | 3 |
| 8 | **M7 — Search** | Catalog + projections | 4 |
| 9 | **M8 — Reviews/Wishlist** | Catalog + Orders | P2 (deferred) |

```text
M9 ─┐
M2 ─┼─> M3 Catalog ─┬─> M4 Cart ─┐
    │               └─> M6 Inventory ─┼─> M5 Orders ─> M1 Payments completion
    └─────────────────────────────────┘        └─> M7 Search ─> M8 (optional)
```

**Rules:** one schema per module; cross-module communication only through integration events or BuildingBlocks contracts; no direct table writes; no module-to-module project references.

---

## 1. M9 — Todos cleanup (existing WIP)

### Decision: tenant ownership now, user ownership later

`FindOwnedAsync(id, tenantId)` filters `entity.TenantId == tenantId` (`Repository.Ownership.Extension.cs:37`). There is no user-level filter. Do not add one until identity issues a tenant claim (F8) and the product decides whether todos are private or tenant-shared.

**Fixes**

1. **Reference validation.** `CreateTodo`/`AssignTodoToList` must verify the list/category exists, is not deleted, and belongs to the same tenant. Add `ExistsAsync(id, tenantId)` to the read stores; return `TodosOutcome.Failure.ReferenceNotFound(kind)`.
2. **Stale-projection cleanup.** `TodoReferenceCleaner` reads streams from `TodoReadModel`. Change deletion to wait for a projection watermark ≥ the event that added the reference, or maintain an authoritative `todo_list_members` index written in the same transaction as assignment. Prefer the index: it removes eventual-consistency from a correctness path.
3. **Snapshot validation.** Validate identity/version/tenant before restoring a snapshot; fall back to replay on mismatch.
4. **Settable fields.** Make `TodoList`/`TodoCategory` setters private and route mutations through methods.

**Acceptance:** create/assign with nonexistent/deleted/other-tenant ids fails; pausing projections cannot leave a dangling reference; snapshot corruption falls back safely.

---

## 2. M2 — Notifications as a first-class module

### Bounded context

Promote `Application/BuildingBlocks/Ports/Notifications` to a module. Keep the ports (they are good); move delivery orchestration and persistence into `Modules/Notifications`. The foundation guide (F7) fixes the runtime; this section defines the domain and API.

**Aggregate:** `NotificationDelivery` (relational, one row per dedupe key).

```text
NotificationDelivery
  Id, DedupeKey, UseCase, RecipientId, Channel, TemplateKey
  Status: Pending → Sending → Delivered | Failed → DeadLettered
  Attempts, LastError, NextAttemptAt, CreatedAt, DeliveredAt
```

**Preferences:** `NotificationPreference(userId, channel, useCase, enabled)`.

### Layout

```text
src/Core/Modules/Notifications/Delivery/NotificationDelivery.cs
src/Core/Modules/Notifications/Delivery/DeliveryStatus.cs
src/Core/Modules/Notifications/Delivery/NotificationDelivery.Validator.cs
src/Core/Modules/Notifications/Preferences/NotificationPreference.cs
src/Core/Modules/Notifications/NotificationsOutcome.cs

src/Application/Modules/Notifications/Commands/SendNotification.cs
src/Application/Modules/Notifications/Commands/RetryFailedDelivery.cs
src/Application/Modules/Notifications/Queries/ListDeliveries.cs
src/Application/Modules/Notifications/Events/NotificationDelivered.Integration.cs
src/Application/Modules/Notifications/Persistence/INotificationDeliveryStore.Interface.cs
src/Application/Modules/Notifications/Permissions/NotificationPermissionCatalog.cs

src/Infrastructure/Modules/Notifications/Persistence/NotificationDelivery.Configuration.cs
src/Infrastructure/Modules/Notifications/Persistence/NotificationDelivery.Store.cs
src/Infrastructure/Modules/Notifications/Workers/NotificationRetry.Worker.cs
src/Infrastructure/Modules/Notifications/Adapters/Providers/**   # moved provider adapters

app/Web.Api/Endpoints/Notifications/NotificationEndpoint.cs
```

### Schema

```sql
CREATE SCHEMA notification;
CREATE TABLE notification.deliveries (
    id UUID PRIMARY KEY,
    dedupe_key TEXT NOT NULL,
    use_case TEXT NOT NULL,
    recipient_id TEXT NOT NULL,
    channel TEXT NOT NULL,
    template_key TEXT NOT NULL,
    status TEXT NOT NULL,
    attempts INT NOT NULL DEFAULT 0,
    last_error TEXT,
    next_attempt_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    delivered_at TIMESTAMPTZ
);
CREATE UNIQUE INDEX ix_deliveries_dedupe ON notification.deliveries (dedupe_key);
CREATE INDEX ix_deliveries_retry ON notification.deliveries (status, next_attempt_at)
    WHERE status IN ('Pending','Failed');

CREATE TABLE notification.preferences (
    user_id TEXT NOT NULL,
    channel TEXT NOT NULL,
    use_case TEXT NOT NULL,
    enabled BOOLEAN NOT NULL DEFAULT true,
    PRIMARY KEY (user_id, channel, use_case)
);
```

### Contracts

- `SendNotificationUseCase(useCase, recipientId, channel, parameters, dedupeKey)` — dedupe key is derived by the caller from business identity (e.g. `order-confirmed:{orderId}`), not random.
- `NotificationDeliveredIntegration`, `NotificationFailedIntegration`.
- Permissions: `Notifications.Read`, `Notifications.Manage`.

### API

```text
GET    /api/notifications/preferences
PUT    /api/notifications/preferences
GET    /api/admin/notifications/deliveries?status=&channel=
POST   /api/admin/notifications/deliveries/{id}/retry
```

### Failure handling

| Failure | Action |
|---|---|
| Provider 5xx/timeout | `Failed`, attempts++, exponential `next_attempt_at` |
| Provider 4xx/permanent | `DeadLettered` immediately, alert |
| Opt-out | Skip send, status `Delivered`, metadata `skipped_opt_out` |
| Duplicate enqueue | Return existing row, no provider call |
| Template missing | `DeadLettered`, alert (configuration error) |

**Acceptance:** duplicate enqueue → one send; provider outage → retries then DLQ; opt-out respected; admin can retry a delivery.

---

## 3. M3 — Catalog

### Bounded context and aggregate

`Product` (relational), `Category` (relational). SKU and slug are value objects with validators. Inventory status is **not** a product property; it is projected from Inventory events.

```text
Product
  Id: ProductId (Guid/Ulid)
  Name, Slug, Description
  BasePrice: Money, CompareAtPrice: Money?
  Status: Draft | Published | Archived
  Variants: [ProductVariant { Id, Sku, Name, PriceAdjustment, Attributes, SortOrder }]
  Media:    [ProductMedia { Id, Url, AltText, Type, SortOrder }]
  Categories: [CategoryId]
  Tags: [string]
  CreatedAt, UpdatedAt, PublishedAt, ArchivedAt, Version
```

**Invariants**

- `BasePrice > 0`; `CompareAtPrice >= BasePrice` when set.
- `Sku` unique globally; `Slug` unique globally.
- `Draft → Published` requires name, price, ≥ 1 variant/SKU, ≥ 1 media item.
- `Published → Archived` always allowed; `Archived → Draft` allowed.
- Price/description edits on a published product are allowed; the storefront shows the latest.
- Deletion is soft (`SoftDeletable` concern) and only for drafts or archived products.

**Why relational:** admin-maintained content is mutable CRUD; event sourcing adds replay cost without an audit requirement. If price history is needed, emit `ProductPriceChanged` into an append-only `catalog.price_history` table, not as primary persistence.

### Layout

```text
src/Core/Modules/Catalog/Products/{Product,Product.Constant,Product.Validator,Product.Outcome,Product.Factory,ProductVariant,ProductMedia,PublicationStatus}.cs
src/Core/Modules/Catalog/Categories/{Category,Category.Constant,Category.Validator,Category.Outcome}.cs
src/Core/Modules/Catalog/Shared/{ProductId,CategoryId,Sku,Slug}.cs

src/Application/Modules/Catalog/Products/Commands/{CreateProduct,UpdateProduct,PublishProduct,ArchiveProduct,AddProductVariant,RemoveProductVariant,AddProductMedia}.cs
src/Application/Modules/Catalog/Products/Queries/{GetProduct,ListProducts}.cs
src/Application/Modules/Catalog/Categories/Commands/{CreateCategory,UpdateCategory,DeleteCategory}.cs
src/Application/Modules/Catalog/Categories/Queries/{GetCategory,ListCategories,GetCategoryTree}.cs
src/Application/Modules/Catalog/Permissions/CatalogPermissionCatalog.cs

src/Infrastructure/Modules/Catalog/Persistence/{Product,ProductVariant,ProductMedia,Category,CategoryAssignment}.Configuration.cs
src/Infrastructure/Modules/Catalog/Persistence/{EfCoreProduct,EfCoreCategory}.Store.cs
app/Web.Api/Endpoints/Catalog/{ProductAdminEndpoint,ProductStorefrontEndpoint,CategoryEndpoint}.cs
```

### Schema

```sql
CREATE SCHEMA catalog;
CREATE TABLE catalog.products (
    id UUID PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    slug VARCHAR(250) NOT NULL,
    description TEXT,
    base_price_amount NUMERIC(19,4) NOT NULL,
    base_price_currency VARCHAR(3) NOT NULL,
    compare_at_price_amount NUMERIC(19,4),
    compare_at_price_currency VARCHAR(3),
    status VARCHAR(20) NOT NULL,
    published_at TIMESTAMPTZ,
    archived_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    xmin BIGINT NOT NULL DEFAULT 0,
    search_vector TSVECTOR
);
CREATE UNIQUE INDEX ix_products_slug ON catalog.products (slug);
CREATE INDEX ix_products_published ON catalog.products (status) WHERE status = 'Published';
CREATE INDEX ix_products_search ON catalog.products USING GIN (search_vector);

CREATE TABLE catalog.product_variants (
    id UUID PRIMARY KEY,
    product_id UUID NOT NULL REFERENCES catalog.products(id) ON DELETE CASCADE,
    sku VARCHAR(100) NOT NULL,
    name VARCHAR(200) NOT NULL,
    price_adjustment_amount NUMERIC(19,4),
    price_adjustment_currency VARCHAR(3),
    attributes JSONB NOT NULL DEFAULT '{}',
    sort_order INT NOT NULL DEFAULT 0
);
CREATE UNIQUE INDEX ix_variants_sku ON catalog.product_variants (sku);

CREATE TABLE catalog.categories (
    id UUID PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    slug VARCHAR(250) NOT NULL UNIQUE,
    parent_category_id UUID REFERENCES catalog.categories(id),
    description TEXT,
    sort_order INT NOT NULL DEFAULT 0
);

CREATE TABLE catalog.category_assignments (
    product_id UUID NOT NULL REFERENCES catalog.products(id) ON DELETE CASCADE,
    category_id UUID NOT NULL REFERENCES catalog.categories(id) ON DELETE CASCADE,
    PRIMARY KEY (product_id, category_id)
);

CREATE TABLE catalog.product_media (
    id UUID PRIMARY KEY,
    product_id UUID NOT NULL REFERENCES catalog.products(id) ON DELETE CASCADE,
    url TEXT NOT NULL,
    alt_text VARCHAR(250) NOT NULL,
    media_type VARCHAR(20) NOT NULL,
    sort_order INT NOT NULL DEFAULT 0
);
```

### API

```text
GET    /api/catalog/products?category=&minPrice=&maxPrice=&sort=&cursor=
GET    /api/catalog/products/{slug}
GET    /api/catalog/categories
GET    /api/catalog/categories/{slug}/products

POST   /api/admin/catalog/products                 Catalog.Manage
PUT    /api/admin/catalog/products/{id}            Catalog.Manage
POST   /api/admin/catalog/products/{id}/publish    Catalog.Manage
POST   /api/admin/catalog/products/{id}/archive    Catalog.Manage
POST   /api/admin/catalog/products/{id}/variants   Catalog.Manage
POST   /api/admin/catalog/products/{id}/media      Catalog.Manage
```

### Publication sketch

```csharp
public Result Publish(DateTimeOffset nowUtc)
{
    if (Status is PublicationStatus.Archived)
        return Result.Fail(ProductOutcome.Failure.CannotPublishArchived());
    if (_variants.Count == 0)
        return Result.Fail(ProductOutcome.Failure.PublishRequiresVariant());
    if (_media.Count == 0)
        return Result.Fail(ProductOutcome.Failure.PublishRequiresMedia());

    Status = PublicationStatus.Published;
    PublishedAt = nowUtc;
    UpdatedAt = nowUtc;
    RaiseDomainEvent(new ProductPublished(Id, Slug, nowUtc));
    return Result.Success();
}
```

### Cross-module

- **→ Inventory:** consume `StockLevelChangedIntegration` to update `catalog.product_read_model.inventory_status`.
- **→ Search:** `ProductPublishedDomain` handler publishes `ProductPublishedIntegration`; the search adapter consumes it.
- **→ Notifications:** back-in-stock/price-drop use cases call `INotificationService`, never the reverse.

**Acceptance:** invalid price/publication rejected; only published products visible publicly; SKU/slug uniqueness enforced; admin CRUD authorized.

---

## 4. M4 — Cart

### Aggregate

`Cart` owns `CartLine` snapshots. It never references the Catalog aggregate; it copies name, SKU, price, and media URL at add time.

```text
Cart
  Id, CustomerId?, SessionId, Currency
  Lines: [CartLine { ProductId, VariantId?, Sku, ProductName, UnitPrice, Quantity, MediaUrl, AddedAt }]
  ExpiresAt (guest: +7 days; authenticated: none)
  Version
```

**Invariants:** quantity ≥ 1; currency fixed at creation; price snapshot immutable after add; guest cart has a session id; authenticated cart has a unique customer id.

### Merge-on-login

1. Lock the authenticated cart row (`FOR UPDATE`) and load the guest cart.
2. If no authenticated cart: transfer ownership of the guest cart, clear `SessionId`.
3. Else merge: authenticated line wins on price; duplicate product/variant increases quantity by the guest quantity (cap at a configured maximum); append unique lines.
4. Delete the guest cart in the same transaction.
5. Publish `CartMergedDomain` for analytics; no cross-module writes.

### Layout

```text
src/Core/Modules/Cart/Carts/{Cart,Cart.Constant,Cart.Validator,CartLine,CartLine.Validator}.cs
src/Core/Modules/Cart/CartOutcome.cs
src/Application/Modules/Cart/Commands/{AddToCart,UpdateCartLine,RemoveFromCart,MergeCartOnLogin}.cs
src/Application/Modules/Cart/Queries/GetCart.cs
src/Application/Modules/Cart/Persistence/ICartStore.Interface.cs
src/Infrastructure/Modules/Cart/Persistence/{Cart,CartLine}.Configuration.cs
src/Infrastructure/Modules/Cart/Persistence/EfCoreCart.Store.cs
app/Web.Api/Endpoints/Cart/CartEndpoint.cs
```

### Schema

```sql
CREATE SCHEMA cart;
CREATE TABLE cart.carts (
    id UUID PRIMARY KEY,
    customer_id UUID,
    session_id VARCHAR(64),
    currency VARCHAR(3) NOT NULL,
    expires_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    xmin BIGINT NOT NULL DEFAULT 0
);
CREATE UNIQUE INDEX ix_carts_customer ON cart.carts (customer_id) WHERE customer_id IS NOT NULL;
CREATE UNIQUE INDEX ix_carts_session ON cart.carts (session_id) WHERE customer_id IS NULL;

CREATE TABLE cart.cart_lines (
    id UUID PRIMARY KEY,
    cart_id UUID NOT NULL REFERENCES cart.carts(id) ON DELETE CASCADE,
    product_id UUID NOT NULL,
    variant_id UUID,
    sku VARCHAR(100) NOT NULL,
    product_name VARCHAR(200) NOT NULL,
    unit_price_amount NUMERIC(19,4) NOT NULL,
    unit_price_currency VARCHAR(3) NOT NULL,
    quantity INT NOT NULL CHECK (quantity > 0),
    media_url TEXT,
    added_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX ix_cart_lines_cart ON cart.cart_lines (cart_id);
```

### API

```text
GET    /api/cart
POST   /api/cart/items          { productId, variantId?, quantity }
PUT    /api/cart/items/{lineId} { quantity }
DELETE /api/cart/items/{lineId}
POST   /api/cart/merge          (called after login if a guest cart exists)
```

**Acceptance:** price snapshot survives a catalog price change; currency mismatch rejected; guest→authenticated merge is atomic and creates one cart.

---

## 5. M6 — Inventory

### Model

Per-SKU row plus reservations. `available = on_hand - reserved`, never negative.

```text
StockLevel { Sku (PK), OnHand, Reserved, Version }
Reservation { Id, Sku, OrderId, Quantity, ReservedAt, ExpiresAt, Status }
```

### Oversell prevention

```sql
WITH locked AS (
    SELECT sku, on_hand, reserved
    FROM inventory.stock_levels
    WHERE sku = @sku
    FOR UPDATE
)
UPDATE inventory.stock_levels sl
SET reserved = sl.reserved + @quantity, updated_at = now()
FROM locked
WHERE sl.sku = locked.sku
  AND locked.on_hand - locked.reserved >= @quantity
RETURNING locked.on_hand - locked.reserved - @quantity AS remaining;
```

Zero rows → insufficient stock or unknown SKU; distinguish with a follow-up read.

### Expiry

A `ReservationExpiry.Worker` runs every 60 s: `FOR UPDATE SKIP LOCKED` expired active reservations, decrement `reserved`, mark `Released`, publish `ReservationExpiredIntegration`. Orders consumes it to cancel a still-pending order.

### Layout

```text
src/Core/Modules/Inventory/Stock/{StockLevel,StockLevel.Validator,Reservation,ReservationStatus}.cs
src/Core/Modules/Inventory/InventoryOutcome.cs
src/Application/Modules/Inventory/Commands/{AdjustStock,ReserveStock,ReleaseReservation,ConsumeReservation}.cs
src/Application/Modules/Inventory/Queries/{GetStockLevel,ListLowStock}.cs
src/Application/Modules/Inventory/Events/{StockReserved,StockLevelChanged,ReservationExpired}.Integration.cs
src/Infrastructure/Modules/Inventory/Persistence/{StockLevel,Reservation}.Configuration.cs
src/Infrastructure/Modules/Inventory/Persistence/EfCoreInventory.Store.cs
src/Infrastructure/Modules/Inventory/Workers/ReservationExpiry.Worker.cs
app/Web.Api/Endpoints/Inventory/InventoryAdminEndpoint.cs
```

### Schema

```sql
CREATE SCHEMA inventory;
CREATE TABLE inventory.stock_levels (
    sku VARCHAR(100) PRIMARY KEY,
    on_hand INT NOT NULL CHECK (on_hand >= 0),
    reserved INT NOT NULL CHECK (reserved >= 0),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    xmin BIGINT NOT NULL DEFAULT 0
);
CREATE TABLE inventory.reservations (
    id UUID PRIMARY KEY,
    sku VARCHAR(100) NOT NULL REFERENCES inventory.stock_levels(sku),
    order_id UUID NOT NULL,
    quantity INT NOT NULL CHECK (quantity > 0),
    reserved_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    expires_at TIMESTAMPTZ NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Active'
);
CREATE INDEX ix_reservations_sku ON inventory.reservations (sku, status);
CREATE INDEX ix_reservations_expiry ON inventory.reservations (expires_at, status)
    WHERE status = 'Active';
```

**Acceptance:** two concurrent reservations for the last unit → exactly one succeeds; expiry releases stock; consume on ship; release on cancel.

---

## 6. M5 — Orders and checkout

### Aggregate (relational, not event-sourced)

**Why relational:** Orders is the most business-critical aggregate and the event-store projection cursor is being repaired (F4). A relational status column with optimistic concurrency is simpler and avoids projection drift. Revisit event sourcing per module via ADR.

```text
Order
  Id, OrderNumber, CustomerId
  Status: Pending → Confirmed → Processing → Shipped → Delivered
           ↘ Cancelled
           Confirmed → PartiallyReturned → Returned
  Lines: [OrderLine { ProductId, VariantId?, Sku, ProductName, UnitPrice, Quantity, LineTotal }]
  ShippingAddress, BillingAddress (existing Address VO)
  Subtotal, TaxTotal, ShippingTotal, OrderTotal
  PaymentId?, PaymentStatus?
  IdempotencyKey, PlacedAt, CancelledAt, CancellationReason
  Version
```

**Invariants:** totals are computed server-side from line snapshots (`line = unitPrice × quantity`, `total = sum + tax + shipping`); currency is single per order; transitions are closed; a cancelled/delivered/returned order cannot be cancelled again; a line's price is immutable after placement.

### Checkout orchestration — one transaction, then outbox

The payment provider is the only external system. Orders and Inventory share PostgreSQL, so a saga would add latency for no benefit.

```text
1. Begin transaction
2. Load cart (server-side prices), reject empty/expired
3. Reserve inventory per line (conditional UPDATE; all-or-nothing)
4. Create Order (Pending), copy line snapshots, compute totals
5. Stage OrderPlacedIntegration in the outbox
6. Commit
7. After commit: outbox dispatcher publishes OrderPlacedIntegration
8. Payments handler creates a durable payment operation (F3)
9. PaymentSucceeded → Order.Confirm; PaymentFailed → Order.Cancel + release inventory
10. ReservationExpired (no payment within TTL) → Order.Cancel + release
```

```csharp
await using var tx = await unitOfWork.BeginTransactionAsync(ct);

Cart cart = await carts.FindByCustomerAsync(command.CustomerId, ct);
if (cart is null || cart.Lines.Count == 0)
    return Result<OrderResponseModel>.Fail(OrderOutcome.Failure.CartEmpty());

Result reserved = await inventory.ReserveAsync(cart.Lines, orderId, ttl, ct);
if (reserved.IsFailure) return Result<OrderResponseModel>.Fail(reserved.Errors);

Order order = Order.Place(cart, command.IdempotencyKey, clock.GetUtcNow());
orders.Add(order);
await unitOfWork.SaveChangesAsync(ct);
await tx.CommitAsync(ct);
```

**Compensation:** a plain handler, not an orchestrator. `PaymentFailed` and `ReservationExpired` both call `ReleaseReservation` and `Order.Cancel`.

### Idempotency

Unique `(customer_id, idempotency_key)`. A replay returns the stored order with `Idempotent-Replay: true`. Never create a second order.

### Layout

```text
src/Core/Modules/Orders/Orders/{Order,Order.Constant,Order.Validator,Order.Factory,OrderLine,OrderLine.Validator,OrderStatus}.cs
src/Core/Modules/Orders/Shared/{OrderId,OrderNumber}.cs
src/Core/Modules/Orders/OrderOutcome.cs
src/Application/Modules/Orders/Commands/{PlaceOrder,CancelOrder,StartProcessing,ShipOrder,DeliverOrder,ReturnItems}.cs
src/Application/Modules/Orders/Queries/{GetOrder,ListOrders}.cs
src/Application/Modules/Orders/Events/{OrderPlaced,OrderCancelled,OrderDelivered}.Integration.cs
src/Application/Modules/Orders/Events/{PaymentSucceeded,PaymentFailed,ReservationExpired}.Integration.Handler.cs
src/Application/Modules/Orders/Permissions/OrderPermissionCatalog.cs
src/Infrastructure/Modules/Orders/Persistence/{Order,OrderLine}.Configuration.cs
src/Infrastructure/Modules/Orders/Persistence/EfCoreOrder.Store.cs
app/Web.Api/Endpoints/Orders/{OrderEndpoint,OrderAdminEndpoint}.cs
```

### Schema

```sql
CREATE SCHEMA orders;
CREATE TABLE orders.orders (
    id UUID PRIMARY KEY,
    order_number VARCHAR(30) NOT NULL UNIQUE,
    customer_id UUID NOT NULL,
    status VARCHAR(20) NOT NULL,
    payment_id UUID,
    payment_status VARCHAR(20),
    shipping_address JSONB NOT NULL,
    billing_address JSONB NOT NULL,
    subtotal_amount NUMERIC(19,4) NOT NULL,
    subtotal_currency VARCHAR(3) NOT NULL,
    tax_total_amount NUMERIC(19,4) NOT NULL,
    tax_total_currency VARCHAR(3) NOT NULL,
    shipping_total_amount NUMERIC(19,4) NOT NULL,
    order_total_amount NUMERIC(19,4) NOT NULL,
    order_total_currency VARCHAR(3) NOT NULL,
    idempotency_key VARCHAR(128) NOT NULL,
    placed_at TIMESTAMPTZ,
    cancelled_at TIMESTAMPTZ,
    cancellation_reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    xmin BIGINT NOT NULL DEFAULT 0
);
CREATE UNIQUE INDEX ix_orders_idempotency ON orders.orders (customer_id, idempotency_key);
CREATE INDEX ix_orders_customer ON orders.orders (customer_id, created_at DESC);

CREATE TABLE orders.order_lines (
    id UUID PRIMARY KEY,
    order_id UUID NOT NULL REFERENCES orders.orders(id) ON DELETE CASCADE,
    product_id UUID NOT NULL,
    variant_id UUID,
    sku VARCHAR(100) NOT NULL,
    product_name VARCHAR(200) NOT NULL,
    unit_price_amount NUMERIC(19,4) NOT NULL,
    unit_price_currency VARCHAR(3) NOT NULL,
    quantity INT NOT NULL CHECK (quantity > 0),
    line_total_amount NUMERIC(19,4) NOT NULL,
    line_total_currency VARCHAR(3) NOT NULL
);
CREATE INDEX ix_order_lines_order ON orders.order_lines (order_id);
```

### API

```text
POST   /api/orders/checkout                  Idempotency-Key required
GET    /api/orders
GET    /api/orders/{orderNumber}
POST   /api/orders/{orderNumber}/cancel
POST   /api/orders/{orderNumber}/return

GET    /api/admin/orders                     Orders.Read
POST   /api/admin/orders/{orderNumber}/process   Orders.Manage
POST   /api/admin/orders/{orderNumber}/ship      Orders.Manage
POST   /api/admin/orders/{orderNumber}/deliver   Orders.Manage
```

### Cross-module

- Consume `PaymentSucceededIntegration` → `Confirm`; `PaymentFailedIntegration` → `Cancel` + release.
- Consume `ReservationExpiredIntegration` → `Cancel`.
- Publish `OrderPlaced/Confirmed/Shipped/Delivered` for Notifications and Reviews.
- Never call Payment/Inventory tables directly.

**Acceptance:** price tampering fails (server reads catalog price at checkout); duplicate checkout returns the same order; payment failure releases stock; reservation expiry cancels; delivery enables returns.

---

## 7. M1 — Payments completion at module level

Foundation F3 fixes durability. This section completes the business surface.

### Add

1. **Disputes:** extend `PaymentObservation` and the Stripe adapter; add `DisputeChallenged`, `DisputeChargebackReversed`; admin endpoints `challenge`/`accept`; project dispute read models.
2. **Provider refund reconciliation:** consume provider-initiated refunds; match by provider reference; never create a local refund without an observation or a local command.
3. **Precision:** `IPaymentPolicy.GetDecimalPlaces(provider, currency)`; reject unrepresentable amounts before I/O; record the rounded amount.
4. **Partial capture:** aggregate stays `Authorized` until fully captured; projection mirrors it; validator uses the authorized remainder.
5. **Payment↔Order contract:** consume `OrderPlacedIntegration` into `payment.order_references(order_id UNIQUE, payment_id, amount, currency)`; reject unknown references and amounts above the order total.
6. **Admin:** reconciliation dashboard (unknown/oldest age), manual replay of unknown operations, dispute queue.

**Schema**

```sql
CREATE TABLE payment.order_references (
    order_id UUID PRIMARY KEY,
    payment_id UUID NOT NULL,
    reserved_amount NUMERIC(19,4) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

**Acceptance:** external reference must map to an order; amount cannot exceed order total; dispute lifecycle works end to end; partial capture status agrees between aggregate and projection.

---

## 8. M7 — Search

- **Port:** `Application/Modules/Catalog/Ports/IProductSearch.Interface.cs` with `SearchAsync`, `SuggestAsync`, `GetFacetsAsync`.
- **Adapter:** `Infrastructure/Modules/Catalog/Adapters/Search/PostgresFullText.Search.cs` using `websearch_to_tsquery`, `ts_rank`, weights A=name, B=description, C=tags.
- **Index:** `tsvector` column + GIN; trigger or application maintenance. Only `Published` rows are searchable.
- **Filters/facets:** category, price, inventory status (from the projection), tags.
- **Content updates:** consume `ProductPublishedIntegration`/`ProductUpdatedIntegration` (projection), or maintain the vector in the same write for simplicity.
- **Optional ML:** `IEmbeddingProvider` port + HTTP adapter behind the standard resilience handler; if unavailable, FTS-only; never block browse/checkout. No pgvector until volume justifies it.
- **Benchmark:** seed 50k products; assert p95 < 300 ms local; record in `docs/architecture/slo.md`.

**Acceptance:** published-only results; search-as-you-type under 300 ms; ML down degrades gracefully; no unindexed `LIKE '%…%'` on catalog.

---

## 9. M8 — Reviews and Wishlist (deferred, P2)

Design intent only; do not build before Catalog/Orders/Storefront are live.

- **Review:** `ReviewId, ProductId, CustomerId, Rating (1–5), Title, Body, Status (PendingModeration|Published|Rejected)`. Only verified buyers (`OrderDeliveredIntegration`) may review; admin moderation.
- **Wishlist:** per-customer product list; no reservation; reads Catalog read model.
- **Trigger:** after checkout is stable and moderation capacity exists.

---

## 10. Shared infrastructure for new modules

### Outbox for relational aggregates

Use the existing `OutboxInterceptor`. Aggregates raise domain events; the interceptor stages rows in the same `SaveChanges`. A same-module handler converts the domain event into an integration event via `IIntegrationEventPublisher` (see `Application/Modules/Todos/Events/*.Integration.Handler.cs` for the pattern).

### Permissions

| Module | Permissions |
|---|---|
| Catalog | `Catalog.Read`, `Catalog.Manage` |
| Orders | `Orders.Read`, `Orders.Manage` |
| Inventory | `Inventory.Read`, `Inventory.Manage` |
| Notifications | `Notifications.Read`, `Notifications.Manage` |
| Cart | none (user-owned) |

Add each catalog to `IPermissionCatalogContributor`; register in `ApplicationPermissionCatalog`.

### Schemas

| Module | Schema |
|---|---|
| Catalog | `catalog` |
| Cart | `cart` |
| Inventory | `inventory` |
| Orders | `orders` |
| Notifications | `notification` |
| Payments (existing) | `payment` |
| Todos (existing) | `todo` |

### Conventions checklist per module

- One schema, one `IEntityTypeConfiguration` per table, one migration.
- `*Outcome.cs` per module; no inline `Error.*`.
- Commands/queries co-located with handlers; validators beside them.
- Read models only for storefront/list queries; write models stay relational.
- Public storefront endpoints `AllowAnonymous`; admin endpoints `HasPermission`.
- Every integration event versioned; no PII in payloads.

---

## 11. Demo script (end-to-end acceptance)

1. Admin creates a product (M3), adds a variant and media, publishes.
2. Admin sets stock to 10 (M6).
3. Shopper lists products, opens the slug, adds to cart (M4).
4. Shopper checks out with an idempotency key → order `Pending`, stock `on_hand=10, reserved=1` (M5).
5. Payments creates a durable operation and a provider intent; sandbox payment succeeds.
6. Webhook settles; `PaymentSucceeded` → order `Confirmed`; notification delivery recorded and sent (M2).
7. Admin ships → `Shipped`; notification sent.
8. Customer returns one item → `PartiallyReturned`; stock released/reserved accordingly.
9. Admin refunds 50% with an idempotency key; retry returns the same refund, one provider operation (M1).
10. Kill the API between webhook record and settle → restart → retry worker settles exactly once.

**Exit:** all steps pass through the gateway in a Playwright smoke, with zero infrastructure skips in CI.

---

## 12. Explicit non-goals

- No saga/process manager: only payment crosses a system boundary; inventory/orders are same-database.
- No CDC, sharding, separate search service, BFF, micro-frontends, or worker extraction without the triggers in the master roadmap.
- No event sourcing for Catalog/Cart/Inventory/Orders unless an ADR justifies it.
- No multi-currency conversion, marketplace, or international tax before the first market is safe.
