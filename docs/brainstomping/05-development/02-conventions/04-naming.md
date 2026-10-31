# Naming

The canonical names for projects, namespaces, modules, API routes, database tables and columns, Vue components, Pinia stores, and documentation files, with the `AA/BB-CC` citation rule.

**Links:** [.NET style](./01-dotnet-style.md) · [Module conventions](./02-module-conventions.md) · [Vue and Nuxt style](./03-vue-nuxt-style.md) · [Solution layout](../../03-architecture/02-components/01-solution-layout.md) · [Entity model](../../03-architecture/03-data/01-entities.md) · [Contributing](../../00-governance/02-policies/01-contributing.md) · [Development index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the single naming reference for the repository. A name is chosen once here and reused
everywhere it appears — a project folder, a namespace, a route, a table, a component, a store, and
a document filename. Consistent names are what let a reader predict where something lives and what
it is called. Identifier spelling at the C# language level is `05/02-01`; module structure is
`05/02-02`; front-end structure is `05/02-03`.

The guiding rule: **one concept, one name.** If the same concept is a module, a route segment, a
schema, and a store, all four derive from the same root word.

---

## Projects and Assemblies

| Project | Root namespace | Kind |
|---|---|---|
| `Resys.Core` | `Resys.Core` | Library — shared kernel |
| `Resys.Shared` | `Resys.Shared` | Library — client-facing DTOs |
| `Resys.Modules.<Name>` | `Resys.Modules.<Name>` | Library — one per module |
| `Resys.Infrastructure` | `Resys.Infrastructure` | Library — technical adapters |
| `Resys.Api` | `Resys.Api` | Executable — Minimal API host |
| `Resys.UnitTests` | `Resys.UnitTests` | Test project |
| `Resys.IntegrationTests` | `Resys.IntegrationTests` | Test project |
| `Resys.E2E` | `Resys.E2E` | Test project |

- **PascalCase, `Resys` prefix.** Every .NET project starts with `Resys.` so the solution is
  identifiable in a shared package feed.
- **One project per module**, named `Resys.Modules.<Name>` where `<Name>` is the singular module
  name (`Resys.Modules.Catalog`, not `Catalogs`).
- **Test projects are named for their layer**, not their subject: `Resys.UnitTests`,
  `Resys.IntegrationTests`, `Resys.E2E`.
- **Folders match project names.** `src/Resys.Modules.Catalog/` and
  `tests/Resys.UnitTests/` (`03/02-01`).

---

## Namespaces

- **Namespace mirrors folder path**, rooted at the project root namespace. A type in
  `src/Resys.Modules.Catalog/Persistence/` is in `Resys.Modules.Catalog.Persistence`.
- **Contract tiers have fixed namespaces** (`03/02-02`):

  | Tier | Namespace |
  |---|---|
  | Cross-module contract | `Resys.Core.Contracts.<Module>` |
  | Domain event | `Resys.Core.Events` |
  | Extension contract | `Resys.Core.Shipping` / `.Payments` / `.Promotions` / `.Tax` / `.Orders` |
  | HTTP DTO | `Resys.Shared.<Area>` |
  | Module internals | `Resys.Modules.<Name>.<Folder>` |

- **`<Module>` in a contract namespace is the singular module name** (`Resys.Core.Contracts.Catalog`).
- **`<Area>` in `Resys.Shared` is the API area** (`Catalog`, `Orders`, `Checkout`, `Accounts`), not
  necessarily the module name; an area groups the DTOs its endpoints share (`03/02-02`).
- **File-scoped namespaces only** (`05/02-01`).

---

## Modules

- **Singular PascalCase.** The twelve modules are `Catalog`, `Orders`, `Inventory`, `Shipping`,
  `Tax`, `Promotions`, `Payments`, `Returns`, `Wishlists`, `Reviews`, `Accounts`, `Stores`
  (`03/02-02`).
- **The module entry type is `<Name>Module`** (`CatalogModule`) with `Add<Name>` and
  `Map<Name>Endpoints` extensions (`05/02-02`).
- **The module `DbContext` is `<Name>DbContext`** (`CatalogDbContext`).
- **The database schema is the lowercase singular module name** (`catalog`, `orders`, `tax`), so
  the module, its schema, and its namespace share one root word (`05/02-02`).
- **A module name is never abbreviated** in a type or route; use `Promotions`, not `Promos`.

---

## Domain Types

- **Entities**: singular PascalCase noun (`Order`, `LineItem`, `StockItem`, `PromotionAction`).
- **Value objects**: descriptive noun; `Money` and typed ids are the canonical examples (`Money`,
  `VariantId`, `StoreId`, `OrderId`).
- **Typed ids are `<Entity>Id`** and are distinct types, not `Guid` (`03/03-03`).
- **Domain events**: past-tense record `<Subject><PastParticiple>` (`OrderPlaced`, `StockReserved`,
  `PaymentCaptured`), carrying `StoreId` and `OccurredAtUtc` (`03/02-02`).
- **Cross-module contracts**: interface prefixed `I` with a role suffix — `I<Thing>Query`,
  `I<Thing>Service`, `I<Thing>Registry` (`ICatalogPricingQuery`, `IPaymentService`)
  (`03/02-02`).
- **Extension contracts**: the fixed five — `IShippingCalculator`, `IPaymentGateway`,
  `IPromotionAction`, `ITaxZoneResolver`, `OrderStateHooks` (`03/02-07`).
- **DTOs**: `<Thing>Dto` for responses, `<Verb><Thing>Request` for requests
  (`ProductDto`, `CreateProductRequest`), in `Resys.Shared` (`03/02-02`).
- **Options types**: `<Feature>Options` bound from configuration (`FlatRateOptions`).

---

## API Routes

- **Base paths are fixed**: public is `/api/v1`, admin is `/api/admin`. A breaking change opens
  `/api/v2` (`03/04-01`, `03/04-02`).
- **Segments are lowercase `snake_case` plurals** for resources: `/api/admin/stock_items`,
  `/api/admin/option_types`, `/api/v1/line_items` is nested under `/api/v1/cart/line_items`.
- **One resource per path level**, with sub-resources nested: `/api/admin/products/{id}/variants`,
  `/api/v1/products/{slug}/variants`.
- **Path parameters are lowercase `snake_case`.** A resource's own member is `{id}` or `{slug}`;
  a parameter that identifies another entity is `{<entity>_id}`, matching the authoritative route
  tables in `03/04-01` and `03/04-02` (`{item_id}`, `{image_id}`, `{store_id}`). A parameter is
  never camelCase (`/api/admin/orders/{id}`, not `{orderId}`).
- **Query parameters are lowercase `snake_case`**: `page`, `per_page`, `variant_id`, `location_id`,
  `is_disabled`, `active_on`.
- **The HTTP method expresses the verb**, not the path: `POST` creates, `PATCH` partially updates,
  `PUT` replaces a sub-collection (`PUT /api/admin/variants/{id}/prices`), `DELETE` removes.
- **An action that is not CRUD is a `snake_case` sub-path**: `/api/admin/orders/{id}/fulfill`,
  `/api/admin/stock_transfers`.
- **Store scoping is never a path segment**; it is resolved from the request (public) or the
  `X-Store` header (admin) (`03/04-02`).
- **Money crosses the wire as `{ amount, currency }`**, never a bare number (`03/04-01`).

The endpoint tables in `03/04-01` and `03/04-02` are the authoritative lists; this file fixes the
naming grammar they obey.

---

## Database Tables and Columns

- **Schema**: lowercase singular module name (`catalog`, `orders`, `accounts`).
- **Entity class**: singular PascalCase (`Order`, `StockItem`, `PromotionAction`); `03/03-01` lists
  the entities by this class name.
- **Table**: PascalCase **plural**, one table per entity (`Order` → `Orders`, `StockItem` →
  `StockItems`, `PromotionAction` → `PromotionActions`), mapped with `ToTable` in the module's
  configuration (`03/03-01`).
- **Column**: PascalCase (`FirstName`, `Line1`, `PostalCode`, `OccurredAtUtc`), matching the
  property name on the entity.
- **Primary key**: `Id`. **Foreign key**: `<ReferencedEntity>Id` (`StoreId`, `UserId`,
  `VariantId`, `PromotionId`).
- **Timestamps**: `<Verb>edAtUtc` / `<Verb>AtUtc` (`CreatedAtUtc`, `OccurredAtUtc`), mapped to
  `timestamptz` and written through `IClock` (`03/03-01`).
- **Booleans**: `Is`/`Has`/`Allows`/`Can` prefix (`IsActive`, `IsDefault`, `AllowsBackorder`,
  `IsTaxInclusive`).
- **Money**: stored as the two parts `Amount decimal(18,4)` and `Currency char(3)`; there is no
  single floating-point money column and no raw-decimal public type (`03/03-03`).
- **Indexes**: `IX_<Table>_<Columns>` (`IX_Orders_StoreId_Number`); **unique indexes** keep the
  same prefix and are declared unique in the configuration (`03/03-01`).
- **Indexes are named by EF convention** unless the generated name would exceed the PostgreSQL
  identifier limit, in which case the explicit `IX_...` name is shorter but keeps the prefix.
- **No reserved-word table or column names**; if a concept collides with SQL, rename the column
  explicitly in configuration.
- **Migration names**: `Initial<Module>` then a short PascalCase description
  (`AddVariantWeight`, `AddPromotionUsageLimit`) (`03/03-04`).

---

## Vue Components

- **Files are PascalCase `.vue`** and multi-word: `ProductCard.vue`, `AdminDataTable.vue`,
  `StoreSwitcher.vue` (`04/01-03`, `04/02-03`).
- **The component name matches the filename**; the template tag is PascalCase
  (`<ProductCard />`), keeping kebab-case only where an HTML element must be disambiguated.
- **Base and shared components** carry the `App`/`Admin` prefix when they are shell-level
  (`AppHeader`, `AdminShell`, `AdminDataTable`).
- **A route component is a page** under `app/pages/` and is named for its route, not its data
  (`pages/products/[slug].vue`), while the components it composes are named for their role.
- **Composables are `use*` functions**: `useMoney`, `useCart`, `useApiV1`, `useProblem`
  (`04/01-03`, `04/02-03`).
- **Props are camelCase; events are kebab-case** (`05/02-03`).

---

## Pinia Stores

- **A store is named for the state it owns**, `use<Thing>`: `useCart`, `useAdminAuth`,
  `useStoreContext` (`05/02-03`).
- **The file is `use<Thing>.ts`** in `app/stores/`, so the file name and the exported store
  function agree (`stores/useCart.ts` exports `useCart`).
- **A store id string is the lowerCamel of the thing** (`defineStore('cart', …)`,
  `defineStore('adminAuth', …)`).
- **A store is not named after a page or a component** it happens to serve; state ownership, not
  the consumer, decides the name.

---

## Documentation Files

- **Numbered, kebab-case filenames**: `AA-bb-cc.md`, for example `01-dotnet-style.md`,
  `04-seed-data.md`, `03-review-policy.md` (`00/02-01`, spec §11).
- **Numbering is stable**. Once a file has a number, its number is not reused for a different
  subject; a removed file leaves its number retired.
- **The area and subfolder numbers form the file's identity**; the title inside the file is
  human-readable (`01-dotnet-style.md` → `# .NET and C# Style`).
- **`README.md` indexes each area** and lists its children (`03/02-01`).

### The `AA/BB-CC` Citation Rule

Every cross-reference between documentation files uses a numeric identifier of the form
`AA/BB-CC`, where:

- `AA` is the area number (`00` governance through `10` maintenance);
- `BB` is the subfolder number inside the area;
- `CC` is the file number inside the subfolder.

| Reference | Resolves to |
|---|---|
| `03/02-01` | `docs/03-architecture/02-components/01-solution-layout.md` |
| `05/02-01` | `docs/05-development/02-conventions/01-dotnet-style.md` |
| `06/01-01` | `docs/06-verification/01-test-strategy/01-matrix.md` |

Rules:

1. **Cite future or unlinked files in plain text** using the `AA/BB-CC` token, so the reference
   survives until the file exists and the link check stays green (`00/02-01`).
2. **Use a markdown link only when the file already exists**, with the identifier or a short label
   as the link text. Never link a file that is not committed.
3. **Cite the area and subfolder numbers, not the title.** "See `05/02-02`" is correct;
   "see the module conventions file" is not.
4. **A citation points at one concern.** If a fact spans two files, cite both.
5. **When a file moves, its identifier changes and every citation is updated** in the same change;
   a stale identifier is a broken reference caught by review (`00/02-03`).

---

## Quick Reference

| Subject | Convention | Example |
|---|---|---|
| .NET project | `Resys.<Kind>` | `Resys.Modules.Orders` |
| Namespace | project root + folder | `Resys.Modules.Orders.Domain` |
| Module | singular PascalCase | `Promotions` |
| Database schema | lowercase singular module | `promotions` |
| Table | PascalCase plural | `PromotionActions` |
| Column | PascalCase | `OccurredAtUtc` |
| Foreign key | `<Entity>Id` | `PromotionId` |
| API route | lower `snake_case` plural | `/api/admin/stock_items` |
| Query parameter | lower `snake_case` | `variant_id` |
| Domain event | past-tense record | `PaymentCaptured` |
| Contract interface | `I<Thing><Role>` | `ICatalogPricingQuery` |
| DTO | `<Thing>Dto` / `<Verb><Thing>Request` | `ProductDto` |
| Vue component | PascalCase multi-word | `ProductCard.vue` |
| Composable | `use<Thing>` | `useMoney` |
| Pinia store | `use<Thing>` in `use<Thing>.ts` | `useCart` |
| Doc file | `AA-bb-cc.md`, cite `AA/BB-CC` | `05/02-04` |

## Open Questions

1. Whether a shared `web/shared` DTO type is re-exported under a single barrel or imported by path
   is a scaffold decision; the exported type names above are fixed either way.
2. The exact abbreviation policy for very long entities (`StoreCreditEntry`) is settled when the
   entity set is implemented; no abbreviation is introduced without recording it here.
3. Whether documentation file numbers are ever renumbered during a reorganization is a governance
   decision owned by `00/02-01`; the stable-number rule holds by default.

## Links

- [.NET style](./01-dotnet-style.md)
- [Module conventions](./02-module-conventions.md)
- [Vue and Nuxt style](./03-vue-nuxt-style.md)
- [Solution layout](../../03-architecture/02-components/01-solution-layout.md)
- [Entity model](../../03-architecture/03-data/01-entities.md)
- [Contributing](../../00-governance/02-policies/01-contributing.md)
- [Design spec](../../11-spec/README.md) — §11 (documentation
  map).
- Module contracts: `03/02-02`; extension architecture: `03/02-07`; migrations: `03/03-04`;
  review policy: `00/02-03`; test strategy: `06/01-01`.
