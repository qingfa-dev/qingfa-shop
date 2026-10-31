# Solution Layout

The physical project tree of resys-eshop, the responsibility of each project, and the one-way dependency direction every project must obey.

**Links:** [Module contracts](./02-module-contracts.md) · [C4 model](../01-overview/01-c4.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the physical map of the repository. It expands Spec §5 into an annotated project
tree, states what every `src/`, `web/`, and `tests/` project is responsible for, and fixes the
allowed dependency direction so the modular-monolith boundary stays intact. The logical component
view is `03/01-01`; the rules a module must follow once you are inside one of these projects are
`03/02-02`; extension seams are `03/02-07`.

The layout is deliberate: `Resys.Core` is a shared kernel with no outward dependencies, the twelve
`Resys.Modules.*` projects each own a bounded slice of the domain, `Resys.Infrastructure` supplies
technical adapters, and `Resys.Api` is the only executable that composes them. The two Nuxt apps
are decoupled HTTP clients and share no source with the service.

---

## Repository Tree

```
resys-eshop/
├─ Resys.sln
├─ Directory.Build.props            # shared C# settings: nullable, analyzers, LangVersion
├─ Directory.Packages.props         # central package management (one version per package)
├─ src/
│  ├─ Resys.Core/                   # shared kernel: Money, identities, store scoping,
│  │                                #   domain-event base types, extension contracts
│  ├─ Resys.Shared/                 # client-facing DTOs and HTTP contracts; leaf project
│  ├─ Resys.Modules.Catalog/        # products (master + variations), options, taxonomy,
│  │                                #   images, per-locale text, price sets
│  ├─ Resys.Modules.Orders/         # order aggregate, line items, addresses, adjustments,
│  │                                #   state machine, checkout orchestration
│  ├─ Resys.Modules.Inventory/      # stock items per (variant, location), reservations,
│  │                                #   stock movement ledger, backorder policy
│  ├─ Resys.Modules.Shipping/       # shipping methods, zones, rate quoting, delivery selection
│  ├─ Resys.Modules.Tax/            # jurisdiction hierarchy, tax rates/categories, computation
│  ├─ Resys.Modules.Promotions/     # promotions, rules, actions, adjustment output
│  ├─ Resys.Modules.Payments/       # payment records/attempts, gateway orchestration, refunds
│  ├─ Resys.Modules.Returns/        # return authorizations, return items, refund/restock linkage
│  ├─ Resys.Modules.Wishlists/      # named wishlists and their items per customer
│  ├─ Resys.Modules.Reviews/        # product reviews, ratings, moderation state
│  ├─ Resys.Modules.Accounts/       # Identity users, roles, addresses, per-store admin assignment
│  ├─ Resys.Modules.Stores/         # store records, domain mapping, currencies, locales, options
│  ├─ Resys.Infrastructure/         # EF Core + Postgres primitives, Identity stores, file
│  │                                #   storage, email, gateway adapters, outbox relay
│  └─ Resys.Api/                    # Minimal API host: /api/v1 + /api/admin, OpenAPI, health
├─ web/
│  ├─ storefront/                   # Nuxt 4 SSR storefront
│  ├─ admin/                        # Nuxt 4 + PrimeVue admin
│  └─ shared/                       # shared TypeScript: generated API client, types, formatting
├─ tests/
│  ├─ Resys.UnitTests/              # domain units, engines, architecture-boundary tests
│  ├─ Resys.IntegrationTests/       # Testcontainers (Postgres) + API host integration
│  └─ Resys.E2E/                    # Playwright for storefront and admin
└─ docs/                            # the deliverable (spec + 00–10 areas)
```

`Resys.Modules.*` is shorthand for the twelve module projects listed in the subtree; each is a
separate `.csproj` so the compiler enforces the boundary. `Resys.Modules.Catalog` is shown first
only as an example of the standard module anatomy below.

---

## Project Responsibilities

| Project | Kind | One-line responsibility | May reference |
|---|---|---|---|
| `Resys.Core` | Library | Shared kernel: `Money`, identities, store scoping, domain-event base types, and the extension contracts. | nothing |
| `Resys.Shared` | Library | Client-facing DTOs and HTTP contract records shared with the front-ends. | nothing |
| `Resys.Modules.<Name>` (×12) | Library | Own one bounded domain slice: entities, EF configuration, services, endpoints. | `Core`, `Shared` |
| `Resys.Infrastructure` | Library | Technical adapters: EF Core/Npgsql, Identity stores, storage, email, gateways, outbox relay. | `Core`, `Shared` |
| `Resys.Api` | Executable | Minimal API host: composes modules + infrastructure, exposes both REST surfaces. | `Core`, `Shared`, `Modules.*`, `Infrastructure` |
| `web/*` | Nuxt apps / TS lib | HTTP clients and shared TS; no C# source coupling. | via HTTP only |
| `tests/*` | Test projects | Exercise units, integration (real Postgres), and end-to-end browser flows. | any project under test |

`Resys.Api` is the only project with a `Program.cs` and the only deployable on the backend. No
module, and not `Resys.Infrastructure`, is independently runnable.

---

## Dependency Direction

```
                         ┌──────────────────────────────────────────┐
                         │                Resys.Api                  │
                         └───────┬───────────────────────┬──────────┘
                                 │                       │
                 ┌───────────────▼──────────┐   ┌────────▼─────────────────┐
                 │      Resys.Modules.*      │   │   Resys.Infrastructure   │
                 │  (12 projects, siblings   │   │   adapters only          │
                 │   never reference each    │   └────────┬─────────────────┘
                 │   other)                  │            │
                 └───────────────┬──────────┘             │
                                 │                        │
                                 ▼                        ▼
                         ┌──────────────────────────────────────────┐
                         │                Resys.Core                 │
                         │            (depends on nothing)           │
                         └──────────────────────────────────────────┘

     Resys.Shared ── referenced by modules, infrastructure, api; depends on nothing
```

**Direction rules**

1. **`Resys.Core` depends on nothing.** It references no other project and no infrastructure
   package beyond the BCL. Domain primitives and extension contracts live here so modules can share
   them without referencing each other.
2. **`Resys.Shared` depends on nothing.** It is a leaf DTO assembly so a client can consume the HTTP
   contract without pulling in domain types. Modules map their entities to these records.
3. **Modules → Core.** Every `Resys.Modules.*` project references `Resys.Core` (and may reference
   `Resys.Shared`). No module references another module's project, entities, or `DbContext`.
4. **Infrastructure → Core.** `Resys.Infrastructure` implements abstractions declared in
   `Resys.Core` (storage, email, event dispatch, outbox, identity stores) and never references a
   module.
5. **Api → Modules + Infrastructure.** `Resys.Api` is the composition root: it references every
   module and the infrastructure library, calls each module's registration hook, and maps its
   endpoint groups.
6. **Tests → anything.** Test projects may reference any project under test; this is not a
   production edge.
7. **`web/*` → HTTP only.** The front-ends share no source with `src/`; the TypeScript API client is
   generated from the service's OpenAPI document, keeping the contract one-way.

The four required edges are `Modules → Core`, `Infrastructure → Core`, `Api → Modules +
Infrastructure`, and `Core → nothing`. The `→ Shared` edges in the table and diagram (`Modules →
Shared`, `Infrastructure → Shared`, `Api → Shared`) plus `Api → Core` are an explicit addition to
that required set: they all point at the dependency-free DTO leaf, add no cycles, and let any
project name the client-facing records. They are listed separately here so this file is not read as
misquoting the required direction.

The graph is acyclic. A pull request that adds an edge forbidden by the table above is a
correctness defect, not a style preference; architecture tests in `tests/Resys.UnitTests` fail the
build when such an edge appears (see `03/02-02`).

---

## Standard Module Anatomy

Every module follows the same folder shape so a reader can navigate any of the twelve the same way.

```
src/Resys.Modules.<Name>/
├─ Domain/            # entities and value objects owned by this module
├─ Persistence/       # IEntityTypeConfiguration<T> + EF migrations for this module's schema
├─ Services/          # internal services and public-contract implementations
├─ Endpoints/         # Minimal API route groups for this module's HTTP surface
├─ Contracts/         # records other modules consume via Resys.Core; HTTP records live in Resys.Shared
└─ <Name>Module.cs    # Add<Name>(IServiceCollection) + Map<Name>Endpoints(RouteGroupBuilder publicApi, RouteGroupBuilder adminApi)
```

The `<Name>Module` type is the only public entry point `Resys.Api` calls. Each module registers its
own `DbContext` and owns a dedicated PostgreSQL schema (`catalog`, `orders`, `inventory`, …) with
its own migrations history table, so module data is physically co-located but logically separated.
A module's `Contracts/` folder contains only records other modules may consume through `Resys.Core`
abstractions; internals stay `internal`.

---

## Front-End Layout

```
web/
├─ storefront/                 # public SSR storefront (Tailwind v4, @nuxtjs/i18n)
│  ├─ app/                     # pages, components, layouts, Pinia stores
│  ├─ server/                  # Nitro routes / SSR data loaders that call /api/v1
│  ├─ nuxt.config.ts
│  └─ package.json
├─ admin/                      # back-office (PrimeVue 4), JWT in httpOnly cookie
│  ├─ app/                     # pages, tables, forms, admin shell with store switcher
│  ├─ nuxt.config.ts
│  └─ package.json
└─ shared/                     # workspace TS package consumed by both apps
   ├─ src/api/                 # generated OpenAPI client and typed wrappers
   ├─ src/types/               # DTO mirrors of Resys.Shared
   ├─ src/format/              # money/currency and i18n formatting helpers
   └─ package.json
```

The storefront calls `/api/v1`; the admin calls `/api/admin`. Neither app holds database
credentials or provider secrets. `web/shared` is a build-time TypeScript library and is not a
deployable.

---

## Tests Layout

| Test project | Scope | Key tooling |
|---|---|---|
| `Resys.UnitTests` | Domain units, promotion/tax/shipping engines, and architecture-boundary assertions. | xUnit |
| `Resys.IntegrationTests` | Module persistence, outbox relay, API endpoints against a real database. | xUnit + Testcontainers (Postgres) |
| `Resys.E2E` | Storefront and admin journeys through a real browser. | Playwright |

Architecture-boundary tests live with the unit tests and assert the dependency direction in this
file (for example, that no module project references another module). The full test strategy is
`06/01-01`.

---

## Build and Composition Order

1. `Resys.Core` and `Resys.Shared` build first; they are leaves.
2. The twelve `Resys.Modules.*` projects build in parallel against the leaves.
3. `Resys.Infrastructure` builds against `Resys.Core`.
4. `Resys.Api` builds last, composing modules and infrastructure at startup.
5. `web/*` builds independently and depends only on the generated OpenAPI document.

`Resys.Api` startup order is: register infrastructure primitives → call each module's `Add<Name>`
hook → register extension implementations (see `03/02-07`) → map module endpoints → start the outbox
relay. Central package management (`Directory.Packages.props`) keeps every project on the same
package version.

---

## Open Questions

1. Whether the twelve module projects consolidate into solution folders or remain a flat `src/`
   listing is a build-convenience choice owned by `05/03-01`; project count and boundaries are
   unchanged either way.
2. The exact `.NET` namespace prefix for module internals (`Resys.Modules.Catalog.Domain.*` vs. a
   shorter alias) is recorded in `05/02-02` (module conventions) and does not affect this tree.
3. Whether `web/shared` is published as a workspace package or vendored by path alias is confirmed
   in `05/03-01`; both preserve the decoupling shown here.

## Links

- [Module contracts](./02-module-contracts.md)
- [C4 model](../01-overview/01-c4.md)
- [App topology](../01-overview/02-app-topology.md)
- [Design spec](../../11-spec/README.md) — §5 (solution layout),
  §9 (front-ends).
- [ADR 0002 — Modular monolith](../../00-governance/01-adr/02-modular-monolith.md)
- Extension architecture: `03/02-07`; module conventions: `05/02-02`; test strategy: `06/01-01`.
