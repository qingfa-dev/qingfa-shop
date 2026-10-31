# Module Conventions

How to add a module to resys-eshop: the standard folder shape, where EF configuration and migrations live, how endpoints and DI registration hook into the host, the dependency rules, and the contract-only boundary rule.

**Links:** [.NET style](./01-dotnet-style.md) · [Naming](./04-naming.md) · [Solution layout](../../03-architecture/02-components/01-solution-layout.md) · [Module contracts](../../03-architecture/02-components/02-module-contracts.md) · [Extension architecture](../../03-architecture/02-components/07-extension-architecture.md) · [Development index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the step-by-step procedure and the boundary law for the twelve `Resys.Modules.*`
projects. It expands the standard module anatomy in `03/02-01` into concrete rules: what to create,
where each file goes, which types are public, and how `Resys.Api` composes the module at startup.
The logical ownership rules and the contract tiers are `03/02-02`; the extension seams a module
must keep open are `03/02-07`; identifiers are `05/02-04`.

The central invariant: **a module owns its data and behavior and exposes contracts only. No other
module references its project, its entities, or its `DbContext`.**

---

## When to Add a Module

Add a module only when a bounded domain slice needs its own entities, schema, and services. The
twelve modules are fixed by `03/02-02` (Catalog, Orders, Inventory, Shipping, Tax, Promotions,
Payments, Returns, Wishlists, Reviews, Accounts, Stores). Do not create a module for:

- a cross-cutting concern (events, storage, email, outbox) — these are `Resys.Core` contracts
  implemented by `Resys.Infrastructure`;
- a thin grouping of endpoints over an existing module's data — extend that module instead;
- a third-party extension — an extension is a separate library implementing a `Resys.Core`
  contract and registering in DI (`03/02-07`), never a new core module.

---

## Folder Shape

Every module uses the same layout so navigating any one of the twelve is identical.

```
src/Resys.Modules.<Name>/
├─ Resys.Modules.<Name>.csproj   # references Resys.Core and Resys.Shared only
├─ Domain/                       # entities and value objects this module owns
├─ Persistence/                  # <Name>DbContext + IEntityTypeConfiguration<T> + Migrations/
├─ Services/                     # internal behavior + public-contract implementations
├─ Contracts/                    # records other modules consume via Resys.Core abstractions
├─ Endpoints/                    # Minimal API route groups for this module's HTTP surface
└─ <Name>Module.cs               # Add<Name>(IServiceCollection) + Map<Name>Endpoints(publicApi, adminApi)
```

- **`Domain/`** holds entities as classes with private setters and behavior methods, and value
  objects as records. No EF attributes on these types; mapping is fluent (`05/02-01`).
- **`Persistence/`** holds the module's `DbContext` (deriving from the `Resys.Core` persistence
  base), one `IEntityTypeConfiguration<T>` per entity, and the `Migrations/` folder. See
  "EF Core Configuration" below.
- **`Services/`** holds internal services and the implementations of the public contracts the
  module publishes. Implementation classes are `internal sealed`.
- **`Contracts/`** holds only records that other modules may consume through this module's
  `Resys.Core` abstractions; the module's internals stay `internal`. Client-facing HTTP records
  live in `Resys.Shared`, not here (`03/02-01`, `03/02-02`).
- **`Endpoints/`** holds static classes with a `Map(RouteGroupBuilder publicApi,
  RouteGroupBuilder adminApi)` method that adds this module's routes to the host's two base groups.
- **`<Name>Module.cs`** is the single public entry point and the only type `Resys.Api` names.

Everything except `<Name>Module` is `internal` unless it is a `Resys.Core` contract implementation
(which is public because it satisfies a public interface) or a public contract interface declared
in `Resys.Core`.

---

## EF Core Configuration

- **One `DbContext` per module**, named `<Name>DbContext`, with its own PostgreSQL schema
  (lowercase, singular: `catalog`, `orders`, `inventory`, `shipping`, `tax`, `promotions`,
  `payments`, `returns`, `wishlists`, `reviews`, `accounts`, `stores`).
- **Fluent configuration, one file per entity.** An
  `IEntityTypeConfiguration<TEntity>` class named `<Entity>Configuration` lives in
  `Persistence/`. Entities carry no EF attributes. The configuration sets the schema, table name,
  key, indexes, value conversions, and owned types.
- **Migrations are owned per module** and live in `Persistence/Migrations/`. The module's history
  table is its own `__EFMigrationsHistory` per schema. Naming, seed strategy, and rollback rules
  are `03/03-04`.
- **No cross-module foreign keys.** A module references another module's aggregate by typed id
  only; integrity across the boundary is carried by contracts and domain events (`03/02-02`).
  Within the module, FKs and cascades are declared normally.
- **Timestamps use the `Resys.Core` clock.** `CreatedAtUtc`/`UpdatedAtUtc` are written through
  `IClock`; the configuration maps them to `timestamptz` (`03/03-01`).

```csharp
namespace Resys.Modules.Catalog.Persistence;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", "catalog");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Slug).HasMaxLength(160).IsRequired();
        builder.HasIndex(product => new { product.StoreId, product.Slug }).IsUnique();
    }
}
```

The `DbContext` applies configurations from its own assembly:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
    => modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
```

---

## Services and Contracts

- **Public contracts live in `Resys.Core`**, in `Resys.Core.Contracts.<Module>` (queries, services,
  registries). A module implements its own contracts in `Services/`; the host injects the
  implementation (`03/02-02`).
- **Read across the boundary through a contract**, never through another module's `DbContext` or
  entity. A write across the boundary is a domain event, not a direct call.
- **Domain events** are `sealed record`s in `Resys.Core.Events`, published through the outbox in
  the same transaction as the state change (`03/01-03`). A module that needs another module's data
  adds or extends a `Resys.Core` contract or event rather than a project reference.
- **DTO mapping stays in the module.** Entities never leave the module; the endpoint maps an entity
  to a `Resys.Shared` record. A DTO never exposes a module entity (`03/02-02`).

---

## Endpoint Registration

- **The module registers into two distinct route groups.** `Resys.Api` creates
  `app.MapGroup("/api/v1")` and `app.MapGroup("/api/admin")`, then passes both to the module's
  `Map<Name>Endpoints` entry point. The module adds its relative paths to the group it belongs to,
  so it never hard-codes `/api/v1` or `/api/admin` and the public and admin surfaces can never
  collide. The base paths and versioning policy are owned by `Resys.Api` (`03/04-01`, `03/04-02`).

```csharp
// src/Resys.Modules.Catalog/Endpoints/CatalogEndpoints.cs
namespace Resys.Modules.Catalog.Endpoints;

internal static class CatalogEndpoints
{
    internal static void Map(RouteGroupBuilder publicApi, RouteGroupBuilder adminApi)
    {
        var products = publicApi.MapGroup("/products").WithTags("Catalog");
        products.MapGet("/", ProductHandlers.ListAsync).AllowAnonymous().WithName("ListProducts");
        products.MapGet("/{slug}", ProductHandlers.GetBySlugAsync).AllowAnonymous().WithName("GetProduct");

        var adminProducts = adminApi.MapGroup("/products").RequireAuthorization("admin");
        adminProducts.MapPost("/", ProductHandlers.CreateAsync).WithName("CreateProduct");
        adminProducts.MapPatch("/{id}", ProductHandlers.UpdateAsync).WithName("UpdateProduct");
    }
}
```

The module entry point forwards the two groups to that class, so `<Name>Module` stays the only
public entry point `Resys.Api` names:

```csharp
// src/Resys.Modules.Catalog/CatalogModule.cs
namespace Resys.Modules.Catalog;

public static class CatalogModule
{
    public static void MapCatalogEndpoints(RouteGroupBuilder publicApi, RouteGroupBuilder adminApi)
        => Endpoints.CatalogEndpoints.Map(publicApi, adminApi);
}
```

- **Handlers are `static` methods** with dependency injection through parameters, returning
  `IResult`; they accept the request `CancellationToken` and forward it (`05/02-01`). A handler
  never contains domain logic; it validates, calls a service or contract, and maps the result.
- **Authorization is declared at the group or endpoint** (`RequireAuthorization`), never checked
  imperatively inside a handler. Admin endpoints require the `admin` role and store scope
  (`03/04-03`).
- **Every endpoint has a `.WithName(...)` and a response type annotation** so the OpenAPI document
  is complete (`03/04-04`).
- **The public and admin surfaces stay separate**; never expose a route on both without review.

---

## DI Registration and Composition

`Resys.Api/Program.cs` is the only composition root. It composes the kit first, then calls each
module once, in a fixed order (`03/02-01`):

```csharp
// src/Resys.Api/Program.cs
builder.Services.AddMediator(options => { /* the edge recipe below, BEFORE the kit */ });
builder.Services.AddBuildingBlocks();             // kit: buses, behaviors, in-memory defaults (03/02-08)
builder.Services.AddInfrastructure(builder.Configuration);   // swaps the durability ports to EF

builder.Services.AddCatalog();
builder.Services.AddOrders();
builder.Services.AddInventory();
builder.Services.AddShipping();
builder.Services.AddTax();
builder.Services.AddPromotions();
builder.Services.AddPayments();
builder.Services.AddReturns();
builder.Services.AddWishlists();
builder.Services.AddReviews();
builder.Services.AddAccounts();
builder.Services.AddStores();

// extension registrations go last, after every module (03/02-07)
builder.Services.AddFlatRateShipping(builder.Configuration);

var app = builder.Build();
var publicApi = app.MapGroup("/api/v1");
var adminApi = app.MapGroup("/api/admin");
```

Each module is then mapped through its `Map<Name>Endpoints(publicApi, adminApi)` entry point in the
same registration order (`CatalogModule.MapCatalogEndpoints(publicApi, adminApi)`, and so on).

The registration half of the module entry point is:

```csharp
// src/Resys.Modules.Catalog/CatalogModule.cs
namespace Resys.Modules.Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalog(this IServiceCollection services)
    {
        services.AddDbContext<CatalogDbContext>((provider, options) =>
            options.UseNpgsql(
                provider.GetRequiredService<IConfiguration>().GetConnectionString("ResysDb"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "catalog")));
        services.AddScoped<ICatalogProductQuery, CatalogProductQuery>();
        services.AddScoped<ICatalogPricingQuery, CatalogPricingQuery>();
        services.AddScoped<ICatalogTaxonomyQuery, CatalogTaxonomyQuery>();
        return services;
    }
}
```

- **`AddBuildingBlocks()` runs before `AddInfrastructure()`.** The kit registers the dispatch
  stack and the in-memory durability defaults; the infrastructure registration then replaces those
  defaults with the EF implementations (outbox writer/reader, inbox store, event store,
  specification executor, repository, unit of work) and the relay hosted service (`03/02-08`).
- **The composition root registers the clock.** `IClock` is registered as `SystemClock`
  (`BuildingBlocks.Infrastructure.Time`); the outbox, the unit of work, the aggregate store, and
  the relay all read time through it.
- **Lifetimes.** Buses, `IDomainEventDispatcher`, pipeline behaviors, `IUnitOfWork`, `IRepository`
  and `ISpecificationExecutor<T>` are **Scoped**; the in-memory stores, `IEventTypeNameRegistry`
  and `IEventSerializer` are **Singleton** (their scoped facades forward to the singleton).
- **Behavior order is fixed — Logging → Validation → ExceptionMapping** — and registered exactly
  once by `AddBuildingBlocks()`; an edge never lists the behaviors in
  `MediatorOptions.PipelineBehaviors`.
- **The edge owns `AddMediator` — with both load-bearing options.** The host executable (and edge
  test projects) references `Mediator.SourceGenerator` and calls `AddMediator(...)` **before**
  `AddBuildingBlocks()`; kit libraries reference `Mediator.Abstractions` only and never call it.

  ```csharp
  // src/Resys.Api/Program.cs — the edge recipe; both options are load-bearing
  builder.Services.AddMediator(options =>
  {
      options.ServiceLifetime = ServiceLifetime.Scoped;
      options.NotificationPublisherType = typeof(InboxAwareNotificationPublisher);
  });
  builder.Services.AddBuildingBlocks();
  ```

  - `ServiceLifetime.Scoped` — Mediator's default is `Singleton`; a singleton mediator would
    resolve the kit's **Scoped** buses, behaviors and unit of work from the root provider (captive
    dependencies once a provider is built with `ValidateScopes`).
  - `NotificationPublisherType = typeof(InboxAwareNotificationPublisher)` — the inbox gate.
    Mediator's default (`ForeachAwaitPublisher`) never consults the inbox, so omitting this option
    silently disables dedupe and every relay replay is handled a second time.
- **One `Add<Name>` extension** registers the module's `DbContext`, its services, and its contract
  implementations. Registration order among modules does not create a dependency; only contracts
  and events cross modules.
- **One `Map<Name>Endpoints` entry point**, taking the host's public and admin `RouteGroupBuilder`s,
  maps the module's routes (see Endpoint Registration above).
- **Extensions register after all modules** so last-registered-wins override semantics hold
  (`03/02-07`).
- **`Resys.Api` references the module**; the module never references `Resys.Api`.

---

## Dependency Rules

| From | May reference | Never references |
|---|---|---|
| `Resys.Modules.<Name>` | `Resys.Core`, `Resys.Shared` | another `Resys.Modules.*`, `Resys.Infrastructure`, `Resys.Api` |
| `Resys.Api` | every module, `Resys.Infrastructure`, `Resys.Core`, `Resys.Shared` | — |

- **No module-to-module project reference.** The compiler fails the build and architecture tests
  assert it (`03/02-01`).
- **No foreign `DbContext` access.** A module never injects another module's `DbContext` or entity
  set. Reads cross through a `Resys.Core` contract; writes cross through a domain event.
- **`Resys.Infrastructure` never references a module.** It implements `Resys.Core` abstractions
  only (`03/02-01`).
- **The contract-only rule.** Cross-module communication uses interfaces and event records declared
  in `Resys.Core`; a module's internals are `internal` and invisible to peers. A change that needs
  another module's data extends a contract or event, never adds a reference (`03/02-02`).

---

## Enforcement Checklist

Before committing a module change, confirm:

- [ ] The project references only `Resys.Core` and `Resys.Shared`.
- [ ] `IEntityTypeConfiguration<T>` classes are in `Persistence/`; entities have no EF attributes.
- [ ] The schema name is the lowercase singular module name; migrations live under
      `Persistence/Migrations/` and are committed with the entity change (`03/03-04`).
- [ ] New cross-module reads go through a `Resys.Core` contract; new cross-module writes emit a
      domain event through the outbox.
- [ ] Endpoints are declared relative to the host's route group and carry authorization and a name.
- [ ] The only public entry point added is `<Name>Module`; other types are `internal`.
- [ ] Architecture tests in `tests/Resys.UnitTests` still pass (`06/01-01`).

## Open Questions

1. Whether each module keeps a separate `DbContext` or shares a composed context while retaining
   its own configuration and schema is finalized in `03/03-04`; the ownership and contract rules
   here hold either way.
2. The exact contract-name suffix for asynchronous commands (`I...Service` vs. `I...Handler`) is a
   convention recorded here; `I...Service` is used in `03/02-02` and is the default.
3. Whether extension option sections are namespaced by extension id or by module is confirmed in
   this file's implementation guidance; the section name is owned by the registering extension.

## Links

- [.NET style](./01-dotnet-style.md)
- [Naming](./04-naming.md)
- [Solution layout](../../03-architecture/02-components/01-solution-layout.md)
- [Module contracts](../../03-architecture/02-components/02-module-contracts.md)
- [Extension architecture](../../03-architecture/02-components/07-extension-architecture.md)
- [Design spec](../../11-spec/README.md) — §5 (solution layout),
  §8 (extension architecture).
- Migrations: `03/03-04`; REST public: `03/04-01`; REST admin: `03/04-02`; auth: `03/04-03`;
  test strategy: `06/01-01`; build and run: `05/03-01`.
