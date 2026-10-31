# Plan — Architecture

Tasks 10–15: system overview, component engines, data, and integration.

**Links:** [Spec index](../README.md) · [Design spec](../01-design/01-purpose-scope.md) · [← Prev](05-planning.md) · [Next →](07-design.md) · [Root README](../../../README.md)

---

### Task 10: Architecture — overview and area index


**Files:**
- Modify: `docs/03-architecture/README.md`
- Create: `docs/03-architecture/01-overview/01-c4.md`
- Create: `docs/03-architecture/01-overview/02-app-topology.md`
- Create: `docs/03-architecture/01-overview/03-data-flow.md`

**Interfaces:**
- Consumes: Tasks 1–9; Spec §3, §5.
- Produces: `03/01-01` … `03/01-03`, cited by every architecture child and `08/01-01`.

- [ ] **Step 1: Write the three overview files and the index**

- `01-c4.md` — C4 Levels 1–3: context (customer, admin, providers), containers (API service, two Nuxt apps, PostgreSQL, object storage, email), components (modules). Link `03/02-01`, `03/02-02`.
- `02-app-topology.md` — Deployables (`Resys.Api`, `web/storefront`, `web/admin`), networks/ports, CORS origins, store resolution by domain (or `X-Store` in dev). Link `08/01-01`.
- `03-data-flow.md` — Annotated sequence: browse → add to cart → checkout delivery/payment/confirm → fulfillment → return; where tax/promotion/inventory/outbox fire. Link `03/02-03`.

Area README becomes an index of the 4 subfolders.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/03-architecture && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/03-architecture
git commit -m "docs(architecture): add overview (C4, topology, data flow)"
```

### Task 11: Architecture — components: solution layout, module contracts, extensions


**Files:**
- Create: `docs/03-architecture/02-components/01-solution-layout.md`
- Create: `docs/03-architecture/02-components/02-module-contracts.md`
- Create: `docs/03-architecture/02-components/07-extension-architecture.md`

**Interfaces:**
- Consumes: `03/01-01`, `03/01-02`; Spec §5, §8.
- Produces: `03/02-01`, `03/02-02`, `03/02-07`, cited by `05/02-02` and `03/04-05`.

- [ ] **Step 1: Write the three component files**

- `01-solution-layout.md` — The `src/` project tree from Spec §5 (`Resys.Core`, `Resys.Modules.*`, `Resys.Infrastructure`, `Resys.Api`, `Resys.Shared`, `web/*`, `tests/*`) with one-line responsibilities and dependency direction (`Modules → Core`, `Infrastructure → Core`, `Api → Modules + Infrastructure`; `Core` depends on nothing).
- `02-module-contracts.md` — Rules for a module: owns its entities + EF configuration + services + endpoints; exposes contracts only; no module references another module's internals (communicate via `Core` contracts/events). Include the module list and each module's public contracts. Link `03/02-01`, `03/02-07`.
- `07-extension-architecture.md` — Contracts `IShippingCalculator`, `IPaymentGateway`, `IPromotionAction`, `ITaxZoneResolver`, `OrderStateHooks`; DI registration; "last-registered wins per key". Include the worked example: a new flat-rate shipping calculator and a coupon promotion type, each shown as a full C# interface implementation plus its DI registration snippet. Link `03/02-02`, `03/04-05`.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/03-architecture/02-components && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/03-architecture/02-components
git commit -m "docs(architecture): add solution layout, module contracts, extension architecture"
```

### Task 12: Architecture — components: order, promotion, tax, inventory engines


**Files:**
- Create: `docs/03-architecture/02-components/03-order-lifecycle.md`
- Create: `docs/03-architecture/02-components/04-promotion-engine.md`
- Create: `docs/03-architecture/02-components/05-tax-engine.md`
- Create: `docs/03-architecture/02-components/06-inventory-model.md`

**Interfaces:**
- Consumes: `03/02-01`, `03/02-02`; Spec §6.
- Produces: `03/02-03` … `03/02-06`, cited by functional requirements and test plans.

- [ ] **Step 1: Write the four engine files**

- `03-order-lifecycle.md` — State machine table (state → allowed transitions → trigger → emitted event); checkout steps; address handling; adjustment model; totals formula `sum(line item amounts) + adjustments + tax`; outbox write in the same transaction. Mermaid/ASCII state diagram. Link `01/02-02`, `03/01-03`.
- `04-promotion-engine.md` — `Promotion` + rules + actions; the exact application order; per-line clamping algorithm (worked numeric example: 10% off + $5 off order on a 2-line order); coupon validation; usage limits; how adjustments are attached. Link `01/02-07`.
- `05-tax-engine.md` — Jurisdiction hierarchy; `TaxCategory` resolution from taxonomy; inclusive vs exclusive computation (worked numeric example); interaction with post-promotion amounts. Link `01/02-06`.
- `06-inventory-model.md` — `StockItem` (variant × location), `available`/`on_hand`, `StockMovement` ledger, reservation/shipment/return flows, backorder rules. Link `01/02-08`.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/03-architecture/02-components && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/03-architecture/02-components
git commit -m "docs(architecture): add order, promotion, tax, inventory component designs"
```

### Task 13: Architecture — data


**Files:**
- Create: `docs/03-architecture/03-data/01-entities.md`
- Create: `docs/03-architecture/03-data/02-multi-store.md`
- Create: `docs/03-architecture/03-data/03-i18n-money.md`
- Create: `docs/03-architecture/03-data/04-migrations.md`

**Interfaces:**
- Consumes: `03/02-03` … `03/02-06`; Spec §6.
- Produces: `03/03-01` … `03/03-04`, cited by `05/01-04` and `09/03-*`.

- [ ] **Step 1: Write the four data files**

- `01-entities.md` — One table per aggregate (name, key columns, relationships, key constraints/indexes) for `Product`, `Variant`, `OptionType`/`OptionValue`, `Taxonomy`/`Taxon`, `Store`, `Order`, `LineItem`, `Adjustment`, `Address`, `Shipment`, `PaymentMethod`/`Payment`, `TaxRate`/`TaxCategory`, `Promotion`/rules/actions, `StockItem`/`StockLocation`/`StockMovement`, `ReturnItem`, `Wishlist`, `Review`, `User`. State EF Core mapping notes (owned types for `Money`/addresses, required FKs, unique indexes).
- `02-multi-store.md` — Store scoping rule, `Store` fields, domain/header resolution, per-store admin assignment, data-isolation implications and query filters.
- `03-i18n-money.md` — `Translatable` pattern tables, `Money` `(decimal, ISO-4217)` owned type, per-store price sets, `CurrencyRate`, rounding rules.
- `04-migrations.md` — EF Core migration workflow, naming, seed strategy pointer to `05/01-04`, rollback expectations.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/03-architecture/03-data && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/03-architecture/03-data
git commit -m "docs(architecture): add data model, multi-store, i18n/money, migrations"
```

### Task 14: Architecture — integration: REST, auth, OpenAPI


**Files:**
- Create: `docs/03-architecture/04-integration/01-rest-public.md`
- Create: `docs/03-architecture/04-integration/02-rest-admin.md`
- Create: `docs/03-architecture/04-integration/03-auth.md`
- Create: `docs/03-architecture/04-integration/04-openapi.md`

**Interfaces:**
- Consumes: Task 13; Spec §7; `00/01-04`.
- Produces: the contracts that `09/03-*` must match exactly.

- [ ] **Step 1: Write the four integration files**

- `01-rest-public.md` — `/api/v1` endpoint table: method, path, auth, request/response DTO shape, status codes, for catalog, cart, checkout, account, reviews. Include 2–3 full JSON request/response examples.
- `02-rest-admin.md` — `/api/admin` endpoint table covering catalog CRUD, orders, inventory, shipping/tax/promotion config, stores, users, reports; auth requirement (`admin` + store scope). Include a full order-fulfillment example.
- `03-auth.md` — Identity user model, JWT issuance/refresh, httpOnly-cookie flow for admin, roles and store-scoped authorization, CSRF/CORS/rate-limit rules, audit-log fields. Link `00/01-04`, `01/03-02`.
- `04-openapi.md` — Built-in `Microsoft.AspNetCore.OpenApi` setup, Scalar UI, how the published contract is generated, and the rule that `09/03-*` mirrors this contract. Link `09/03-01`.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/03-architecture/04-integration && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/03-architecture/04-integration
git commit -m "docs(architecture): add REST public/admin, auth, OpenAPI contracts"
```

### Task 15: Architecture — integration: gateways, email/files


**Files:**
- Create: `docs/03-architecture/04-integration/05-payment-gateways.md`
- Create: `docs/03-architecture/04-integration/06-email-files.md`

**Interfaces:**
- Consumes: `03/02-07`, `03/04-01`, `03/04-02`; Spec §8, §10.
- Produces: `03/04-05`, `03/04-06`, cited by `09/03-03` and `08/03-03`.

- [ ] **Step 1: Write the two integration files**

- `05-payment-gateways.md` — `IPaymentGateway` contract (authorize/capture/refund/void); per-gateway sections (Stripe, PayPal, bank transfer, check, store credit, cash-on-delivery); idempotency keys; webhook handling; failure/retry; **resolve Spec §13 Open Question 3** with an explicit decision (Stripe first, PayPal second) and rationale. Link `03/02-07`, `08/03-04`.
- `06-email-files.md` — `IEmailSender` (SES/SMTP) and the four templates (order confirmation, payment receipt, shipment, return status) with variable lists; `IFileStorage` (local disk + S3-compatible), product-image upload/processing flow, URL strategy.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/03-architecture/04-integration && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/03-architecture/04-integration
git commit -m "docs(architecture): add payment gateways, email/files integration"
```
