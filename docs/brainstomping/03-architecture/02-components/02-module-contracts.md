# Module Contracts

The rules that keep the twelve commerce modules independent: what a module owns, what it exposes, and how modules communicate without referencing each other's internals.

**Links:** [Solution layout](./01-solution-layout.md) · [Extension architecture](./07-extension-architecture.md) · [C4 model](../01-overview/01-c4.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

resys-eshop is a modular monolith, not a set of microservices. The value of the boundary is that a
module can be understood, changed, and tested on its own. This file states the ownership rules
every module obeys, defines the three tiers of contracts, lists all twelve modules with the
contracts each publishes, and records how the rules are enforced. The physical tree is
`03/02-01`; extension seams are `03/02-07`; the entity model is `03/03-01`.

The central rule: **a module owns its data and behavior, and exposes contracts only.** Other modules
never reach into its entities, its `DbContext`, or its internal services.

---

## What a Module Owns

Each `Resys.Modules.<Name>` project exclusively owns:

1. **Entities** — the domain types in its `Domain/` folder. No other module defines or mutates them.
2. **EF Core configuration** — its `IEntityTypeConfiguration<T>` classes, its `DbContext` (deriving
   from the `Resys.Core` persistence base), and its migrations, under one PostgreSQL schema.
3. **Services** — the internal services implementing its behavior, including its public-contract
   implementations.
4. **Endpoints** — its Minimal API route groups, mapped by the host through the module's
   `Map<Name>Endpoints` hook.

A module is registered with the host through one public entry point, `<Name>Module`, exposing
`Add<Name>(IServiceCollection)` and `Map<Name>Endpoints(RouteGroupBuilder publicApi,
RouteGroupBuilder adminApi)`. Everything else in
the project is `internal`.

---

## Rules

1. **Own your slice.** Entity types, EF configuration, services, and endpoints for a concern live
   in exactly one module.
2. **Expose contracts only.** Cross-module communication uses interfaces and event records declared
   in `Resys.Core`; a module's internals are `internal` and not visible to other modules.
3. **No module-to-module project references.** A `Resys.Modules.*` project never references another
   `Resys.Modules.*` project. The compiler and architecture tests both enforce this.
4. **No foreign `DbContext` access.** A module never injects or queries another module's `DbContext`
   or entity set. Reads cross the boundary through a contract; writes cross through a domain event.
5. **Communicate via Core contracts or events.** For a synchronous read, call another module's
   `Resys.Core` contract (for example `ICatalogPricingQuery`). For a side effect, publish a domain
   event; the interested module subscribes.
6. **Events are durable.** A domain event is written to the outbox in the same transaction as the
   state change it describes, then dispatched after commit (`03/01-03`).
7. **HTTP DTOs are separate.** API request/response records live in `Resys.Shared`, not in module
   entities, so the wire contract never leaks domain types.

A module is allowed to depend on `Resys.Core` (kernel, contracts, events) and `Resys.Shared`
(DTO mapping). Nothing else.

---

## Three Tiers of Contract

| Tier | Assembly | Audience | Example |
|---|---|---|---|
| Cross-module contracts | `Resys.Core` (`Resys.Core.Contracts.<Module>`) | Other modules | `ICatalogPricingQuery` |
| Domain events | `Resys.Core` (`Resys.Core.Events`) | Event subscribers, outbox relay | `OrderPlaced` |
| HTTP contracts | `Resys.Shared` (`Resys.Shared.<Area>`) | API clients (`web/*`) | `ProductDto`, `CreateCartRequest` |

Extension contracts are a fourth, distinct family in `Resys.Core` and are documented in `03/02-07`.
Cross-module contract interfaces are read-oriented and implementation-free; a module's public
service interface is implemented inside that module and injected from the host.

---

## Naming Conventions

- Cross-module query/service contract: `I<Thing>Query`, `I<Thing>Service`, `I<Thing>Registry`.
- Domain event: past-tense record, `<Subject><PastParticiple>` (`OrderPlaced`, `StockReserved`),
  carrying a `StoreId`, an `OccurredAtUtc`, and the minimum identifiers the consumer needs.
- HTTP DTO: `<Thing>Dto` for responses, `<Verb><Thing>Request` for requests, in `Resys.Shared`.

---

## Module Index and Public Contracts

| Module | Project (schema) | Cross-module contract | Key domain events |
|---|---|---|---|
| Catalog | `Resys.Modules.Catalog` (`catalog`) | `ICatalogProductQuery`, `ICatalogPricingQuery`, `ICatalogTaxonomyQuery` | `ProductPublished`, `ProductUnpublished`, `VariantPriceChanged` |
| Orders | `Resys.Modules.Orders` (`orders`) | `IOrderQuery`, `IOrderCheckoutService` | `OrderPlaced`, `OrderCanceled`, `OrderShipped`, `OrderCompleted` |
| Inventory | `Resys.Modules.Inventory` (`inventory`) | `IStockAvailability`, `IStockReservationService` | `StockReserved`, `StockReleased`, `StockDepleted` |
| Shipping | `Resys.Modules.Shipping` (`shipping`) | `IShippingRateService`, `IShippingMethodQuery` | `ShippingRateSelected`, `ShipmentDispatched` |
| Tax | `Resys.Modules.Tax` (`tax`) | `ITaxCalculationService` | `TaxRateChanged` |
| Promotions | `Resys.Modules.Promotions` (`promotions`) | `IPromotionEngine`, `ICouponValidator` | `PromotionApplied`, `PromotionExhausted` |
| Payments | `Resys.Modules.Payments` (`payments`) | `IPaymentService`, `IPaymentGatewayRegistry` | `PaymentAuthorized`, `PaymentCaptured`, `PaymentFailed`, `PaymentRefunded` |
| Returns | `Resys.Modules.Returns` (`returns`) | `IReturnService`, `IReturnQuery` | `ReturnRequested`, `ReturnApproved`, `ReturnReceived` |
| Wishlists | `Resys.Modules.Wishlists` (`wishlists`) | `IWishlistService` | `WishlistItemAdded`, `WishlistItemRemoved` |
| Reviews | `Resys.Modules.Reviews` (`reviews`) | `IReviewService`, `IReviewQuery` | `ReviewSubmitted`, `ReviewApproved`, `ReviewRejected` |
| Accounts | `Resys.Modules.Accounts` (`accounts`) | `IUserDirectory`, `IAddressBook`, `IAdminAccessPolicy` | `UserRegistered`, `UserDeactivated` |
| Stores | `Resys.Modules.Stores` (`stores`) | `IStoreQuery`, `IStoreSettings` | `StoreCreated`, `StoreDomainChanged`, `StoreSuspended` |

The detailed contract surface for each module follows. Return types are `Resys.Core` value types
(`Money`, typed ids, results); they never expose another module's entities.

### Catalog

- `ICatalogProductQuery` — `Task<ProductSummary?> FindAsync(ProductId id, StoreId store)`,
  `Task<IReadOnlyList<ProductSummary>> SearchAsync(CatalogSearch query)`.
- `ICatalogPricingQuery` — `Task<Money> GetPriceAsync(VariantId variant, StoreId store, string currency)`,
  the only source of truthful line-item prices.
- `ICatalogTaxonomyQuery` — `Task<TaxonomyNode?> ResolveTaxonAsync(TaxonId id)`,
  `Task<TaxCategory> GetTaxCategoryAsync(VariantId variant)`.
- Emits: `ProductPublished`, `ProductUnpublished`, `VariantPriceChanged`.
- HTTP: `ProductDto`, `VariantDto`, `CategoryDto`, `OptionTypeDto`; admin write requests.

### Orders

- `IOrderQuery` — `Task<OrderSummary?> FindAsync(OrderId id)`,
  `Task<IReadOnlyList<OrderSummary>> ListForUserAsync(UserId user, StoreId store)`.
- `IOrderCheckoutService` — the only state mutator: `AdvanceAsync(OrderId, CheckoutStep, payload)`.
  Called by the API/checkout flow, not by peer modules.
- Emits: `OrderPlaced`, `OrderCanceled`, `OrderShipped`, `OrderCompleted`; consumes payment,
  inventory, shipping, tax, and promotion contracts to compute totals.
- HTTP: `OrderDto`, `LineItemDto`, `AddressDto`; checkout requests.

### Inventory

- `IStockAvailability` — `Task<StockSnapshot> GetAsync(VariantId variant, LocationId location)` and
  `Task<int> GetAvailableAsync(VariantId variant, StoreId store)`.
- `IStockReservationService` — `ReserveAsync`, `ReleaseAsync`, `CommitAsync` keyed by order id.
- Emits: `StockReserved`, `StockReleased`, `StockDepleted`; consumes `OrderPlaced` to reserve and
  `OrderCanceled`/`ReturnReceived` to release or restock.
- HTTP: `StockItemDto`, `StockMovementDto`, `LocationDto`.

### Shipping

- `IShippingRateService` — `Task<IReadOnlyList<ShippingRate>> GetRatesAsync(ShippingRateRequest request)`,
  delegating to the registered `IShippingCalculator` set (`03/02-07`).
- `IShippingMethodQuery` — `Task<ShippingMethod?> FindAsync(ShippingMethodId id, StoreId store)`.
- Emits: `ShippingRateSelected`, `ShipmentDispatched`.
- HTTP: `ShippingMethodDto`, `ShippingRateDto`.

### Tax

- `ITaxCalculationService` — `Task<TaxBreakdown> CalculateAsync(TaxCalculationRequest request)` where
  the request carries post-promotion amounts. Delegates jurisdiction resolution to
  `ITaxZoneResolver` (`03/02-07`).
- Emits: `TaxRateChanged`.
- HTTP: `TaxRateDto`, `TaxCategoryDto`, `TaxBreakdownDto`.

### Promotions

- `IPromotionEngine` — `Task<PromotionOutcome> EvaluateAsync(PromotionEvaluationContext context)`,
  returning adjustments clamped per line and per order. Delegates each configured action key to an
  `IPromotionAction` (`03/02-07`).
- `ICouponValidator` — `Task<CouponValidation> ValidateAsync(string code, StoreId store)`.
- Emits: `PromotionApplied`, `PromotionExhausted`.
- HTTP: `PromotionDto`, `PromotionRuleDto`, `AdjustmentDto`.

### Payments

- `IPaymentService` — `AuthorizeAsync`, `CaptureAsync`, `RefundAsync`, `GetStatusAsync`, delegating
  to the gateway resolved by key. This is intentionally the checkout-facing subset; the full
  extension surface is `IPaymentGateway` (`03/02-07`), which additionally defines `VoidAsync` and
  `HandleWebhookAsync`.
- `IPaymentGatewayRegistry` — `IPaymentGateway Resolve(string key)`.
- Emits: `PaymentAuthorized`, `PaymentCaptured`, `PaymentFailed`, `PaymentRefunded`;
  `PaymentCaptured` causes Orders to advance the state machine.
- HTTP: `PaymentDto`, `RefundDto`; never exposes PAN or gateway secrets.

### Returns

- `IReturnService` — `RequestAsync`, `ApproveAsync`, `ReceiveAsync` keyed by order and line item.
- `IReturnQuery` — `Task<ReturnSummary?> FindAsync(ReturnId id)`.
- Emits: `ReturnRequested`, `ReturnApproved`, `ReturnReceived`; consumes `PaymentRefunded` and
  triggers restock through the inventory contract.
- HTTP: `ReturnDto`, `ReturnItemDto`, `CreateReturnRequest`.

### Wishlists

- `IWishlistService` — `Task<Wishlist> GetOrCreateAsync(UserId user, StoreId store, string name)` and
  item add/remove operations.
- Emits: `WishlistItemAdded`, `WishlistItemRemoved`; validates variants through `ICatalogProductQuery`.
- HTTP: `WishlistDto`, `WishlistItemDto`.

### Reviews

- `IReviewService` — `SubmitAsync`, `ModerateAsync`.
- `IReviewQuery` — `Task<IReadOnlyList<ReviewSummary>> ListForProductAsync(ProductId, StoreId)`.
- Emits: `ReviewSubmitted`, `ReviewApproved`, `ReviewRejected`.
- HTTP: `ReviewDto`, `SubmitReviewRequest`.

### Accounts

- `IUserDirectory` — `Task<UserSummary?> FindAsync(UserId id)` and email lookup; wraps ASP.NET
  Identity stores supplied by `Resys.Infrastructure`.
- `IAddressBook` — address CRUD for a user.
- `IAdminAccessPolicy` — `Task<bool> CanManageStoreAsync(UserId user, StoreId store)`, the authority
  for `/api/admin` store scoping.
- Emits: `UserRegistered`, `UserDeactivated`.
- HTTP: `UserDto`, `AddressDto`, `RoleDto`.

### Stores

- `IStoreQuery` — `Task<StoreSummary?> ResolveAsync(StoreResolution resolution)` (by id, code, or
  domain) and list access for the admin switcher.
- `IStoreSettings` — supported currencies, locales, checkout options, and tax-inclusive flag for a
  store.
- Emits: `StoreCreated`, `StoreDomainChanged`, `StoreSuspended`.
- HTTP: `StoreDto`, `StoreSettingsDto`, `StoreDomainDto`.

---

## Cross-Cutting Contracts (not a module)

These live in **`BuildingBlocks.Application` area folders** and are implemented by `Resys.Infrastructure`
(placement audit: `05/04-01`, 2026-09-24 — Core is DDD/ES only):

| Contract | Application home | Purpose |
|---|---|---|
| `IDomainEventDispatcher` | `Application/Cqrs` | Dispatch a domain event to in-process handlers after commit, via `MediatorDomainEventDispatcher` (same folder; supersedes the deleted `IEventDispatcher`). |
| `IOutboxWriter` | `Application/Outbox` | Persist an event row in the caller's transaction. |
| `IFileStorage` | `Application/Storage` | Store/retrieve product images and uploaded assets. |
| `INotificationService` | `Application/Notifications` | Send transactional notifications (email/SMS channels; replaces draft `IEmailSender`). |
| `IClock` | `Kernel/Time` | Single testable source of time for `OccurredAtUtc`. |
| `IStoreContext` | `Application/Stores` | The resolved store for the current scope (`03/01-02`). |

---

## Enforcement

- **Compile-time.** Module projects reference only `Resys.Core` and `Resys.Shared`; a forbidden
  project reference fails the build.
- **Architecture tests.** `tests/Resys.UnitTests` asserts no module-to-module reference, no
  dependency from `Resys.Core` outward, and that modules expose only their `<Name>Module` entry
  point. Conventions are elaborated in `05/02-02`.
- **Review.** A change that needs another module's data must add or extend a `Resys.Core` contract
  or event, never a reference to that module's internals.

## Open Questions

1. Whether each module keeps a separate `DbContext` or shares a composed context while retaining its
   own configuration and schema is finalized in `03/03-04` (migrations); the ownership and contract
   rules here are unaffected.
2. The exact contract-name suffix for asynchronous commands (`I...Service` vs. `I...Handler`) is a
   convention decision recorded in `05/02-02`.
3. The outbox dispatch order guarantee (per-store FIFO vs. best-effort parallel) is owned by
   `03/02-03`; consumers must not assume global ordering.

## Links

- [Solution layout](./01-solution-layout.md)
- [Extension architecture](./07-extension-architecture.md)
- [C4 model](../01-overview/01-c4.md)
- [Design spec](../../11-spec/README.md) — §5 (solution layout),
  §6 (domain architecture).
- [ADR 0002 — Modular monolith](../../00-governance/01-adr/02-modular-monolith.md)
- Entity model: `03/03-01`; migrations: `03/03-04`; module conventions: `05/02-02`.
