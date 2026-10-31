# 03 — Architecture

How resys-eshop is structured: system overview, component design, data architecture, and integration design.

**Links:** [C4 model](./01-overview/01-c4.md) · [App topology](./01-overview/02-app-topology.md) · [Data flow](./01-overview/03-data-flow.md) · [Spec](../11-spec/README.md)

---

## Purpose

This area is the technical reference for resys-eshop: the modular-monolith .NET service, the two
Nuxt front-ends that consume it, the data model behind it, and the contracts at its edges. It
turns the locked decisions in the [design spec](../11-spec/README.md)
and [area `00`](../00-governance/README.md) into implementation-grade detail.

The area is split into four concern subfolders, each with numbered files (~100–300 lines). Start
with `01-overview` for orientation, then descend into whichever concern you need. When a statement
here conflicts with the spec, the spec wins and the architecture file is corrected.

---

## Subfolder Index

| Subfolder | Purpose | Files |
|---|---|---|
| `01-overview` | Orientation: C4 levels, deployment topology, and the end-to-end data flow. | [01-c4.md](./01-overview/01-c4.md) · [02-app-topology.md](./01-overview/02-app-topology.md) · [03-data-flow.md](./01-overview/03-data-flow.md) |
| `02-components` | The project/module layout, module contracts, and the core domain engines (order lifecycle, promotions, tax, inventory) plus extension architecture and the BuildingBlocks kit. | [01-solution-layout.md](./02-components/01-solution-layout.md) · [02-module-contracts.md](./02-components/02-module-contracts.md) · [03-order-lifecycle.md](./02-components/03-order-lifecycle.md) · [04-promotion-engine.md](./02-components/04-promotion-engine.md) · [05-tax-engine.md](./02-components/05-tax-engine.md) · [06-inventory-model.md](./02-components/06-inventory-model.md) · [07-extension-architecture.md](./02-components/07-extension-architecture.md) · [08-building-blocks.md](./02-components/08-building-blocks.md) |
| `03-data` | The EF Core entity model, multi-store scoping, i18n/money representation, and the migration process. | [01-entities.md](./03-data/01-entities.md) · [02-multi-store.md](./03-data/02-multi-store.md) · [03-i18n-money.md](./03-data/03-i18n-money.md) · [04-migrations.md](./03-data/04-migrations.md) |
| `04-integration` | The REST contracts for both surfaces, auth, OpenAPI/Scalar, payment gateways, and email/files. | [01-rest-public.md](./04-integration/01-rest-public.md) · [02-rest-admin.md](./04-integration/02-rest-admin.md) · [03-auth.md](./04-integration/03-auth.md) · [04-openapi.md](./04-integration/04-openapi.md) · [05-payment-gateways.md](./04-integration/05-payment-gateways.md) · [06-email-files.md](./04-integration/06-email-files.md) |

Every file in this area is linked from the subfolder index above; prose elsewhere cites it by its
identifier (for example `03/02-01`, `03/03-01`, `03/04-01`) and links it when a jump is useful.

---

## How to Read This Area

1. **Orient** with `01-overview`: `03/01-01` (C4 context → containers → components), `03/01-02`
   (deployables, ports, CORS, store resolution), and `03/01-03` (the annotated purchase flow).
2. **Learn the boundaries** in `02-components`: the project tree (`03/02-01`), the rules a module
   must follow (`03/02-02`), and the extension seams (`03/02-07`).
3. **Follow the domain engines**: orders/checkout (`03/02-03`), promotions (`03/02-04`), tax
   (`03/02-05`), and inventory (`03/02-06`).
4. **Read the data model** in `03-data`: entities (`03/03-01`), multi-store scoping
   (`03/03-02`), i18n/money (`03/03-03`), and migrations (`03/03-04`).
5. **Integrate** against `04-integration`: the public REST surface (`03/04-01`), the admin REST
   surface (`03/04-02`), auth (`03/04-03`), OpenAPI (`03/04-04`), gateways (`03/04-05`), and
   email/files (`03/04-06`).
6. Cite a file by its identifier (`AA/BB-CC`) in prose and link it from the index above when a
   reader should jump to it.

---

## Architectural Stance

- **Modular monolith, headless.** One deployable .NET service (`Resys.Api`) composed of internal
  module projects; two decoupled Nuxt 4 clients over REST. Not microservices (ADR 0002).
- **REST only.** Public `/api/v1`, admin `/api/admin`. No GraphQL, no Blazor, no CMS, no
  recommendation engine (ADR 0003).
- **Contracts, not hooks.** Modules communicate through `Resys.Core` contracts and domain events;
  third parties extend via DI registrations with last-registered-wins semantics (`03/02-07`).
- **Money is typed.** Every amount is a `(decimal, ISO-4217 currency)` value object; there are no
  raw decimals in the model.
- **Store-scoped everything.** Catalog, inventory, payment, and order rows belong to a `Store`;
  store resolution is described in `03/01-02`.
- **Outbox for reliable events.** State transitions persist a domain event in the same
  transaction, dispatched after commit (`03/01-03`, `03/03-01`).

---

## Module Index

The platform has **twelve modules**. Each owns its entities, EF Core configuration, services, and
endpoints, and exposes contracts only. Functional requirements live in `01/02-01` … `01/02-12`;
component detail lives in `02-components`.

| Module | Project | Primary architecture reference |
|---|---|---|
| Catalog | `Resys.Modules.Catalog` | `03/02-02`, `03/03-01` |
| Orders | `Resys.Modules.Orders` | `03/02-03` |
| Inventory | `Resys.Modules.Inventory` | `03/02-06` |
| Shipping | `Resys.Modules.Shipping` | `03/02-02`, `03/04-01` |
| Tax | `Resys.Modules.Tax` | `03/02-05` |
| Promotions | `Resys.Modules.Promotions` | `03/02-04` |
| Payments | `Resys.Modules.Payments` | `03/04-05` |
| Returns | `Resys.Modules.Returns` | `03/02-03`, `03/04-01` |
| Wishlists | `Resys.Modules.Wishlists` | `03/02-02`, `03/04-01` |
| Reviews | `Resys.Modules.Reviews` | `03/02-02`, `03/04-01` |
| Accounts | `Resys.Modules.Accounts` | `03/04-03` |
| Stores | `Resys.Modules.Stores` | `03/03-02`, `03/01-02` |

Supporting projects (legacy wording from ADR 0002; kit tree for new work is
`03/02-08` + [ADR 0005](../00-governance/01-adr/05-building-blocks.md)): `Resys.Core` →
`BuildingBlocks.Core` (**shared domain DDD bases + commerce + ports**), `Resys.Infrastructure` →
`BuildingBlocks.Infrastructure`, `Resys.Shared` → `BuildingBlocks.Shared`, plus
`BuildingBlocks.Kernel` (pure primitives, no DDD), `BuildingBlocks.Application`, and
`BuildingBlocks.Hosting`. Host remains `Resys.Api`. See `03/02-01` and `03/02-08`.

---

## Related Areas

- `00` (governance) — ADRs lock the stack, topology, scope, and auth decisions this area elaborates.
- `01` (requirements) — the behavior each module and flow must satisfy; `01/02-*` per module.
- `04` (design) — the storefront and admin screens that consume these contracts.
- `05` (development) — environment, conventions, and the build/run workflow for the layout here.
- `06` (verification) — test strategy for the module boundaries and flows documented here.
- `08` (operations) — deployment topology and runbooks that realize `03/01-02`.
- `09` (user) — the published API reference must match `04-integration` exactly.

---

## Keeping This Area Current

- Any change to a locked decision is recorded as an ADR in `00/01`, not edited into a child file.
- Update `03/01-01` when a container or module is added or removed, and `03/01-02` when a
  deployable, port, CORS origin, or store-resolution rule changes.
- Update `03/01-03` when a step, state transition, or one of the four cross-cutting mechanisms
  (tax, promotion, inventory, outbox) changes position in the flow.
- Keep the `04-integration` REST contracts and `09-user/03-api` in lockstep; the parity check is
  part of verification.
- Keep `03/03-01` and the migrations in `03/03-04` consistent with any entity change.

## Open Questions

None at the area level. Unresolved details are stated in the owning file under its
`## Open Questions` section; the ones that affect this index are the admin rendering mode and the
object-storage default, both referenced from `03/01-01`.
