# Entity Model

The persisted model: one table per aggregate with its key columns, relationships, and constraints, the EF Core mapping conventions, and the domain-event catalogue.

**Links:** [Multi-store scoping](./02-multi-store.md) · [i18n & money](./03-i18n-money.md) · [Migrations](./04-migrations.md) · [Module contracts](../02-components/02-module-contracts.md) · [Order lifecycle](../02-components/03-order-lifecycle.md) · [Inventory model](../02-components/06-inventory-model.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the canonical entity model for resys-eshop. It lists every aggregate and its persisted
shape — key columns, relationships, and the constraints and indexes that protect them — states the
EF Core mapping conventions shared by all twelve modules, and carries the domain-event catalogue
that `03/01-03` attributes here. The store-scoping rules these tables obey are `03/03-02`; the
translatable-row and money shapes are `03/03-03`; the migration process is `03/03-04`. Behavior
lives in the component docs (`03/02-03` orders, `03/02-04` promotions, `03/02-05` tax, `03/02-06`
inventory).

Each module owns its entities, EF Core configuration, and PostgreSQL schema under one physical
database (`03/02-01`, `03/02-02`); the tables below are grouped by owning module. Every
store-scoped table carries a `StoreId` foreign key, and the mapping conventions state how that key
and the `Money` value object are configured.

---

## Mapping Conventions

- **Money is an owned value.** Every monetary amount is mapped as a `Money` owned type with
  `Amount decimal(18,4)` and `Currency char(3)` (ISO-4217). No raw decimal column is
  authoritative (`03/03-03`).
- **Typed identifiers.** `StoreId`, `OrderId`, `VariantId`, and peers are strongly-typed ids stored
  as `uuid`/`bigint` via value converters; an entity never stores a bare `Guid` for another
  aggregate's id.
- **Required foreign keys.** Every store-scoped entity has a non-nullable `StoreId` FK to `Store`;
  child FKs (`LineItem.OrderId`, `Adjustment.OrderId`, `StockMovement.StockItemId`) are required.
- **Delete behavior.** Catalog parents cascade to their children; anything referenced by order
  history (`Variant`, `PaymentMethod`, `TaxRate`) uses `Restrict` and is archived instead of
  deleted (`FR-CAT-010`).
- **Concurrency tokens.** `Order` and `StockItem` carry a `RowVersion` concurrency token; stock
  reservation and shipment updates are guarded by it (`03/02-06`).
- **Timestamps.** All `*AtUtc` columns are `timestamptz`, written through `IClock`.
- **Unique indexes.** Enumerated per table below; a store-scoped uniqueness claim always includes
  `StoreId`. The one deliberate exception is `Variant.Sku`, which is unique platform-wide rather
  than per store (`FR-CAT-002`).
- **Owned addresses.** `Order.ShipAddress` and `Order.BillAddress` are owned types mapped to columns
  on `orders.Orders`; the saved `Address` entity is a separate table. Both snapshot at set-time, so
  a later edit never rewrites an order (`FR-ORD-003`).

---

## Catalog

### Product

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `Product` | `Id`, `StoreId`, `Slug`, `Status`, `CreatedAtUtc` | Store (required); has many `Variant`, `ProductImage`, `Translation`; many-to-many `Taxon` | Unique `(StoreId, Slug)`; index `(StoreId, Status)`. |

### Variant

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `Variant` | `Id`, `StoreId`, `ProductId`, `Sku`, `IsDefault`, `Weight?` | Product (required, restrict); has many `ProductPrice`, `StockItem`; many-to-many `OptionValue` | Unique `Sku` platform-wide (explicit exception to the store-scoped rule, `FR-CAT-002`); unique `(ProductId, IsDefault)` filtered where default. |

### OptionType / OptionValue

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `OptionType` | `Id`, `StoreId`, `Name`, `Position` | Store (required); has many `OptionValue` | Unique `(StoreId, Name)`. |
| `OptionValue` | `Id`, `OptionTypeId`, `Position` | OptionType (required); has many `Translation`; many-to-many `Variant` | Unique `(OptionTypeId, Position)`. |

### Taxonomy / Taxon

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `Taxonomy` | `Id`, `StoreId`, `Name` | Store (required); has one root `Taxon` | Unique `(StoreId, Name)`. |
| `Taxon` | `Id`, `TaxonomyId`, `ParentId?`, `Position`, `Slug` | Taxonomy (required); self-referencing parent; many-to-many `Product` | Unique `(TaxonomyId, ParentId, Slug)`; index `(ParentId, Position)`. |

### ProductImage

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `ProductImage` | `Id`, `ProductId`, `VariantId?`, `Url`, `Position`, `IsPrimary` | Product (required); Variant (optional override) | Unique `(ProductId, IsPrimary)` filtered where primary; one primary per product (`FR-CAT-005`). |

---

## Stores

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `Store` | `Id`, `Name`, `Code`, `DefaultDomain`, `DefaultCurrency`, `DefaultLocale`, `IsActive` | Has many of every store-scoped entity | Unique `Code`; unique `DefaultDomain`. |
| `StoreDomain` | `Id`, `StoreId`, `Host` | Store (required) | Unique `Host`. |
| `StoreCurrency` | `StoreId`, `Currency`, `IsDefault` | Store (required) | PK `(StoreId, Currency)`; exactly one default per store. |
| `StoreLocale` | `StoreId`, `Locale`, `IsDefault` | Store (required) | PK `(StoreId, Locale)`; exactly one default per store. |
| `StoreSetting` | `StoreId`, `Key`, `Value` | Store (required) | PK `(StoreId, Key)`; holds checkout options and the tax-inclusive flag (`FR-STO-004`). |

---

## Orders

### Order

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `Order` | `Id`, `StoreId`, `Number`, `State`, `Currency`, `UserId?`, `Email`, `RowVersion` | Store (required); User (optional, guest); owns `LineItem`, `Adjustment`, `ShipAddress`, `BillAddress`, `Shipment`, `Payment`, `Return` | Unique `(StoreId, Number)`; index `(StoreId, State)`, `(UserId)`; `Currency` fixed for life (`FR-ORD-012`). |

### LineItem

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `LineItem` | `Id`, `OrderId`, `VariantId`, `Quantity`, `UnitPrice` (Money) | Order (required); Variant (required, restrict); has many line-scoped `Adjustment`, `ReturnItem` | Unique `(OrderId, VariantId)`; check `Quantity > 0`; total is computed, never stored (`FR-ORD-002`). |

### Adjustment

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `Adjustment` | `Id`, `OrderId`, `LineItemId?`, `Amount` (signed Money), `Source`, `SourceReference`, `Label`, `CreatedAtUtc` | Order (required); LineItem (optional) | Index `(OrderId)`; check `Source IN (promotion, shipping, manual)`; tax is never a source (`03/02-03`). |

### Address

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `Address` | `Id`, `UserId`, `StoreId`, `FirstName`, `LastName`, `Line1`, `Line2?`, `City`, `Region`, `PostalCode`, `CountryCode`, `Phone?` | User (required); Store (required) | Index `(UserId, StoreId)`; editable saved address, never referenced by a placed order (`FR-ACC-007`). |
| `Order.ShipAddress` / `Order.BillAddress` | owned columns on `Order` | Owned by Order | Value snapshot; billing defaults to shipping unless overridden (`FR-ORD-003`). |

### Shipment

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `Shipment` | `Id`, `OrderId`, `StoreId`, `ShippingMethodId`, `TrackingNumber?`, `State`, `ShippedAtUtc?` | Order (required); has many `ShipmentLine` | Index `(OrderId)`, `(StoreId, State)`; partial shipments allowed (`03/02-06`). |
| `ShipmentLine` | `Id`, `ShipmentId`, `LineItemId`, `Quantity` | Shipment (required); LineItem (required) | Unique `(ShipmentId, LineItemId)`; check `Quantity > 0`. |

---

## Shipping

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `ShippingCategory` | `Id`, `StoreId`, `Name`, `Position` | Store (required); has many `ShippingMethod` | Unique `(StoreId, Name)`. |
| `ShippingMethod` | `Id`, `StoreId`, `ShippingCategoryId`, `Name`, `CalculatorKey`, `Configuration`, `IsEnabled` | Store, ShippingCategory (required) | Unique `(StoreId, Name)`. |
| `ShippingZone` | `Id`, `StoreId`, `Name`, `CountryCodes`, `RegionCodes`, `PostalRanges` | Store (required); many-to-many `ShippingMethod` | Index `(StoreId)`; most-specific match wins (`FR-SHP-002`). |

---

## Payments

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `PaymentMethod` | `Id`, `StoreId`, `Key`, `DisplayName`, `GatewayKey`, `IsEnabled`, `Configuration` | Store (required); has many `Payment` | Unique `(StoreId, Key)`; enabled methods only are offered (`FR-PAY-001`). |
| `Payment` | `Id`, `OrderId`, `StoreId`, `PaymentMethodId`, `Amount` (Money), `State`, `GatewayReference?`, `IdempotencyKey` | Order (required); PaymentMethod (required); has many `PaymentTransaction` | Unique `(StoreId, IdempotencyKey)`; index `(OrderId, State)`; states `pending/authorized/captured/failed/voided/refunded/partially_refunded`. |
| `PaymentTransaction` | `Id`, `PaymentId`, `Kind`, `Amount` (Money), `Outcome`, `GatewayReference?`, `OccurredAtUtc` | Payment (required) | Index `(PaymentId)`; append-only, a correction is a new row (`FR-PAY-006`). |
| `StoreCreditEntry` | `Id`, `StoreId`, `UserId`, `Amount` (signed Money), `Reason`, `OrderId?`, `OccurredAtUtc` | Store, User (required); Order (optional) | Index `(StoreId, UserId)`; balance is derived as the ledger sum, never a column (`FR-PAY-007`). |

---

## Tax

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `TaxCategory` | `Id`, `StoreId`, `Name`, `IsDefault` | Store (required); has many `TaxRate` | Unique `(StoreId, Name)`; unique `(StoreId, IsDefault)` filtered where default. |
| `Jurisdiction` | `Id`, `StoreId`, `Level`, `ParentId?`, `Code`, `Name` | Store (required); self-referencing parent (`City → Region → Country`) | Unique `(StoreId, Level, ParentId, Code)`. |
| `TaxRate` | `Id`, `StoreId`, `JurisdictionId`, `TaxCategoryId`, `Rate`, `EffectiveFrom`, `EffectiveTo?` | Store, Jurisdiction, TaxCategory (required) | Index `(StoreId, JurisdictionId, TaxCategoryId, EffectiveFrom)`; effective-dated, new computations only (`FR-TAX-002`). |

---

## Promotions

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `Promotion` | `Id`, `StoreId`, `Name`, `Channels`, `StartsAtUtc`, `EndsAtUtc`, `Priority`, `TotalUsageLimit?`, `PerUserLimit?`, `IsDisabled` | Store (required); has many `PromotionRule`, `PromotionAction`, `PromotionUsage` | Index `(StoreId, IsDisabled, StartsAtUtc)`; coupon code unique per store (`FR-PRM-007`). |
| `PromotionRule` | `Id`, `PromotionId`, `Kind`, `Configuration` | Promotion (required) | Index `(PromotionId)`; rules combine with AND (`03/02-04`). |
| `PromotionAction` | `Id`, `PromotionId`, `Kind`, `Position`, `Configuration` | Promotion (required) | Index `(PromotionId, Position)`; authoritative action model — `ActionKey`/`Parameters` in `03/02-04` are the logical view of these rows, and the promotion stores no duplicate action columns. |
| `PromotionUsage` | `Id`, `PromotionId`, `OrderId`, `UserId?`, `Email?` | Promotion, Order (required) | Unique `(PromotionId, OrderId)`; counted at placement, atomically guarded (`FR-PRM-008`). |

---

## Inventory

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `StockLocation` | `Id`, `StoreId`, `Name`, `IsDefault`, `AllowBackorders`, `IsActive` | Store (required); has many `StockItem` | Unique `(StoreId, Name)`; unique `(StoreId, IsDefault)` filtered where default. |
| `StockItem` | `Id`, `StoreId`, `VariantId`, `LocationId`, `Reserved`, `RowVersion` | Store, Variant, StockLocation (required); has many `StockMovement` | Unique `(VariantId, LocationId)`; `on_hand` and `available` are derived, not stored (`03/02-06`). |
| `StockMovement` | `Id`, `StockItemId`, `Quantity` (signed), `Reason`, `ReferenceType`, `ReferenceId?`, `ActorId?`, `OccurredAtUtc` | StockItem (required) | Index `(StockItemId, OccurredAtUtc)`; append-only ledger (`FR-INV-004`). |

Inventory quantity rules (matching `03/02-06` and `01/02-08`):

- **Derived quantities.** `on_hand` is the sum of the stock item's `StockMovement.Quantity` deltas
  and is never a mutable balance column; `available = on_hand − reserved`, where `reserved` is the
  counter column on `StockItem`. `available` is computed on read, not stored.
- **Backorder policy.** `on_hand` may not go negative unless the owning `StockLocation` has
  `AllowBackorders = true`; otherwise any operation that would drive `available` (or `on_hand`)
  below zero is rejected (`FR-INV-007`).
- **Movement reasons.** `StockMovement.Reason` is exactly `receipt`/`restock` (positive delta),
  `shipment` (negative delta), `return` restock (positive delta), `adjustment` (signed recount or
  write-off), and `transfer` (negative at the source, positive at the destination). Reservations
  and releases change `reserved`, not `on_hand`, and therefore write no `StockMovement` row
  (`FR-INV-004`).

---

## Returns

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `Return` | `Id`, `StoreId`, `OrderId`, `State`, `SettlementKind?`, `RefundAmount?` (Money), `CreatedAtUtc` | Order (required); has many `ReturnItem` | Index `(OrderId)`, `(StoreId, State)`; states `requested → authorized → received → inspected → completed` plus `canceled`/`rejected` (`FR-RET-008`). |
| `ReturnItem` | `Id`, `ReturnId`, `LineItemId`, `Quantity`, `ReasonId`, `RestockableQuantity`, `UnsellableQuantity` | Return, LineItem, ReturnReason (required) | Unique `(ReturnId, LineItemId)`; check `Quantity > 0`; sum never exceeds purchased minus returned (`FR-RET-002`). |
| `ReturnReason` | `Id`, `StoreId`, `Name`, `Fault`, `IsActive` | Store (required) | Unique `(StoreId, Name)`; deactivating keeps historical references (`FR-RET-003`). |

---

## Wishlists

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `Wishlist` | `Id`, `StoreId`, `UserId`, `Name`, `IsDefault` | Store, User (required); has many `WishlistItem` | Index `(UserId, StoreId)`; unique `(UserId, StoreId, IsDefault)` filtered where default. |
| `WishlistItem` | `Id`, `WishlistId`, `VariantId`, `AddedAtUtc` | Wishlist, Variant (required) | Unique `(WishlistId, VariantId)`; no price or reservation stored (`FR-WIS-002`). |

---

## Reviews

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `Review` | `Id`, `StoreId`, `ProductId`, `UserId`, `Rating`, `Body?`, `State`, `IsVerifiedPurchase`, `CreatedAtUtc` | Store, Product, User (required) | Unique `(ProductId, UserId)`; index `(StoreId, ProductId, State)`; states `pending/published/rejected/hidden` (`FR-REV-003`). |

---

## Accounts

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `User` | `Id`, `Email`, `UserName`, `PasswordHash`, `Locale?`, `Currency?`, `IsActive` | Identity user; has many `Address`, `Order`, `Review`, `Wishlist` | Unique `Email`; unique `UserName`; credentials via ASP.NET Identity (`03/04-03`). |
| `Role` / `UserRole` | Identity tables | User | Roles `customer`, `admin` (`FR-ACC-003`). |
| `AdminStoreAssignment` | `UserId`, `StoreId` | User, Store (required) | PK `(UserId, StoreId)`; the authority for per-store admin scope (`03/03-02`). |

---

## Cross-Cutting Tables

| Name | Key columns | Relationships | Key constraints / indexes |
|---|---|---|---|
| `OutboxMessage` | `Id`, `StoreId?`, `Type`, `Payload`, `OccurredAtUtc`, `DeliveredAtUtc?` | none (no FK, to keep the write cheap) | Index `(DeliveredAtUtc, OccurredAtUtc)`; written in the state-change transaction (`03/02-03`). |
| `AuditLog` | `Id`, `StoreId?`, `ActorId`, `Action`, `EntityType`, `EntityId`, `OccurredAtUtc` | none | Index `(StoreId, OccurredAtUtc)`, `(EntityType, EntityId)`; admin mutations only (`03/04-03`). |

---

## Domain-Event Catalogue

Every event is persisted to `OutboxMessage` in the same transaction as the state change it
records, then dispatched after commit (`03/01-03`). All events carry `StoreId` and `OccurredAtUtc`.

| Event | Emitted by | Primary consumers |
|---|---|---|
| `ProductPublished`, `ProductUnpublished`, `VariantPriceChanged` | Catalog | Search index, analytics |
| `OrderPlaced`, `OrderCanceled`, `OrderShipped`, `OrderCompleted` | Orders | Email, analytics, Inventory |
| `PaymentAuthorized`, `PaymentCaptured`, `PaymentFailed`, `PaymentRefunded` | Payments | Orders, email |
| `StockReserved`, `StockReleased`, `StockDepleted` | Inventory | Admin notification |
| `ShippingRateSelected`, `ShipmentDispatched` | Shipping | Email, analytics |
| `TaxRateChanged` | Tax | Analytics |
| `PromotionApplied`, `PromotionExhausted` | Promotions | Analytics |
| `ReturnRequested`, `ReturnApproved`, `ReturnReceived`, `ReturnStatusChanged` | Returns | Email, Inventory |
| `WishlistItemAdded`, `WishlistItemRemoved` | Wishlists | Analytics |
| `ReviewSubmitted`, `ReviewApproved`, `ReviewRejected` | Reviews | Catalog rating cache |
| `UserRegistered`, `UserDeactivated` | Accounts | Email, audit |
| `StoreCreated`, `StoreDomainChanged`, `StoreSuspended` | Stores | Cache invalidation |

## Open Questions

1. Whether each module keeps a separate `DbContext`/migrations history or a composed context with
   per-module schemas is finalized in `03/03-04`; table ownership here is unaffected.
2. Whether stock history is exposed as its own admin report, beyond the `StockMovement` table, is
   owned by `03/04-02`.
3. The concrete rating scale bounds and review-body maximum are finalized with the review UI
   (`04/01`) and the admin API (`03/04-02`).

## Links

- [Multi-store scoping](./02-multi-store.md)
- [i18n & money](./03-i18n-money.md)
- [Migrations](./04-migrations.md)
- [Module contracts](../02-components/02-module-contracts.md)
- [Order lifecycle](../02-components/03-order-lifecycle.md)
- [Promotion engine](../02-components/04-promotion-engine.md)
- [Tax engine](../02-components/05-tax-engine.md)
- [Inventory model](../02-components/06-inventory-model.md)
- [Design spec](../../11-spec/README.md) — §6 (domain and data
  architecture).
- Migrations: `03/03-04`; admin API: `03/04-02`; auth integration: `03/04-03`.
