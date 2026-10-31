# Test Matrix

Which test layer covers which component, the tool each layer runs on, and how the layers map to the CI pipeline.

**Links:** [Unit tests](./02-unit.md) · [Integration tests](./03-integration.md) · [End-to-end tests](./04-e2e.md) · [Coverage](./05-coverage.md) · [Solution layout](../../03-architecture/02-components/01-solution-layout.md) · [Module contracts](../../03-architecture/02-components/02-module-contracts.md) · [CI pipeline](../../05-development/03-workflow/03-ci.md) · [Verification index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the entry point to the test strategy. It names the four test layers, the tool each
layer runs on, the boundary between them, and the module-by-module assignment of coverage. The
per-layer rules live in the companion files: unit (`06/01-02`), integration (`06/01-03`), E2E
(`06/01-04`), and coverage (`06/01-05`). The behavior under test is defined by the functional
requirements in `01/02-*`, the engine designs in `03/02-*`, and the journeys in `04/01-01` and
`04/02-01`.

The guiding rule: **test the smallest component that can prove the behavior, and use the layer whose
cost matches the risk.** Domain arithmetic and engine algorithms are proven with fast unit tests;
persistence, transactions, and HTTP contracts are proven against a real PostgreSQL; user-visible
journeys are proven in a browser.

---

## Test Layers and Tools

| Layer | Project / location | Tooling | Runs against | Owned by |
|---|---|---|---|---|
| Unit | `tests/Resys.UnitTests` | xUnit | In-process, no I/O | `06/01-02` |
| Integration | `tests/Resys.IntegrationTests` | xUnit + Testcontainers (PostgreSQL 18) | Disposable PostgreSQL 18 + the API host | `06/01-03` |
| Front-end component | `web/storefront`, `web/admin` | Nuxt test utils (Vitest) | Nuxt/Vue test environment, mocked HTTP | `06/01-02` |
| End-to-end | `tests/Resys.E2E` | Playwright | Full stack (API + both front-ends) + seeded dataset | `06/01-04` |

The three back-end layers map to the required CI stages in `05/03-03`: unit to stage 2, integration
to stage 3, and E2E to stage 5. The front-end component suite is the front-end half of the unit
layer; it runs with each app's `test` script in the front-end stage, alongside the lint and typecheck
commands. Stage numbering and required-check semantics are the contract of `05/03-03`; the layer
scope and the tool per layer are the contract of this file.

---

## Layer Boundaries

- **Unit** proves one behavior of one type with every collaborator replaced by a hand-written test
  double. It never opens a database connection, touches the network or file system, reads the wall
  clock, or starts a host. A unit test that needs any of those belongs in a higher layer.
- **Integration** proves that the module's EF Core mapping, its migrations, its services, and its
  endpoints behave correctly against a real PostgreSQL 18 and a real HTTP host. It uses the real
  `DbContext` and the real database; it never mocks persistence. External providers (payment
  gateway, email, file storage) are replaced at the `Resys.Core` seam through DI.
- **Front-end component** proves that a Pinia store, a composable, a formatter, or a component with
  behavior produces the right state or output for a given input. HTTP is stubbed at the `web/shared`
  API client boundary; the test never starts the API.
- **End-to-end** proves a complete user journey through the built storefront or admin and the real
  API. It never mocks the platform; it is the only layer allowed to depend on the seeded demo
  dataset.

A defect that a lower layer can prove must not be pushed up: an illegal order transition is a unit
test, not an E2E test. A defect that only appears across a transaction boundary is an integration
test, not a unit test.

---

## Module × Test-Type Matrix

Each row names the primary concern at each layer. "E2E" means a user-visible journey that exercises
the module; a module with no E2E cell is exercised transitively by the checkout or admin journey and
does not need its own browser spec.

| Module | Unit (xUnit) | Integration (Testcontainers) | E2E (Playwright) |
|---|---|---|---|
| Catalog | Option/variant resolution, slug and SKU uniqueness, per-locale translation fallback, price-set invariants. | Store-scoped EF mappings, translation rows, image metadata, taxonomy tree, query-count guard. | Storefront listing, search facets, product detail; admin product create/edit. |
| Orders | State machine (legal and illegal transitions), line-item merge, address snapshot, totals formula, idempotency key. | Confirm transaction atomicity, rollback on an unreservable line, idempotent replay, order-history scoping. | Storefront 3-step checkout and confirmation; admin order list → fulfill/ship. |
| Inventory | `on_hand`/`available`/`reserved` arithmetic, backorder policy, movement deltas, reserve/release/commit semantics. | Append-only ledger, concurrent last-unit reservation, no-negative-stock constraint, return restock. | Out-of-stock storefront path; admin stock adjustment and transfer. |
| Shipping | Zone specificity (city > region > country), calculator key resolution, free-shipping threshold. | Zone and method store scoping, calculator registration, rate quoting against DB. | Checkout delivery step and shipping-method selection. |
| Tax | Jurisdiction hierarchy, per-line half-up rounding, inclusive vs exclusive, post-promotion base. | Effective-dated rates, store tax-inclusive flag, jurisdiction and category queries. | Checkout totals reflect tax; admin tax settings. |
| Promotions | Rule AND semantics, application-order sort, per-line clamping, coupon reasons, usage limits. | Per-store coupon uniqueness, atomic usage increment, adjustment persistence in application order. | Cart coupon apply/remove; admin promotion configuration. |
| Payments | Attempt state, gateway registry (last-registered-wins), refund ceiling, no PAN retention. | Attempts persisted, refund ceiling enforced, gateway wiring through a test double. | Payment step success; failed-payment recovery. |
| Returns | Eligibility window, approve/receive transitions, restock idempotency, refund vs store credit. | Return → restock movement linkage, idempotent restock, settlement ceiling. | Admin authorize → approve → receive → restock → refund. |
| Wishlists | Get-or-create, idempotent item add, ownership enforcement. | Unique wishlist per (customer, store, name), item uniqueness. | Account wishlist add and move-to-cart. |
| Reviews | One active review per product/store, moderation transitions, verified-purchase computation. | Uniqueness, moderation persistence, aggregate read. | Submit review; admin publish; storefront visibility. |
| Accounts | Address defaults, email uniqueness, `IAdminAccessPolicy.CanManageStoreAsync`. | Identity wiring, store assignment, cross-store `403`, audit-log write. | Register/login, profile/addresses/orders; admin user management. |
| Stores | Domain/code resolution, locale and currency validation, settings immutability for placed orders. | Resolution by domain and `X-Store` header, store settings read, per-store scoping. | Store switcher scope; per-store branding and locale. |
| Cross-cutting | Dependency direction, module entry-point surface, `KeyedRegistry` last-registered-wins, `Money` currency mismatch. | Outbox relay, migrations on a fresh database, OpenAPI parity, CSRF and rate limiting, store-scope `403`. | Auth cookies, store scope, seeded-data smoke. |

The architecture-boundary tests (dependency direction, no module-to-module reference) live with the
unit tests and enforce `03/02-01` and `03/02-02`. The outbox relay and the API contracts are proven
at the integration layer because they need a real transaction and a real host.

---

## Front-End Coverage Matrix

| Surface | Component / unit (Nuxt test utils) | End-to-end (Playwright) |
|---|---|---|
| Storefront | `useCart`, `useMoney`, option resolver, checkout-step gating, problem-detail mapping. | Home, listing, search, product detail, cart, delivery → payment → confirm, account, review. |
| Admin | `useAdminAuth`, `useStoreContext`, `AdminDataTable` paging, money/date formatting. | Session boot, store switch, catalog editor, order fulfillment, inventory, settings, users. |
| `web/shared` | API client wrappers, money and currency formatting, DTO mapping helpers. | Exercised through both apps; no standalone browser spec. |

The front-end component layer covers logic that is cheap to prove in isolation; the E2E layer covers
the journeys in `04/01-01` and `04/02-01` and does not re-test pure formatting.

---

## Cross-Cutting Verification

Spec §12 defines the verification obligations that are not a single module's test:

- **Internal consistency** — no contradiction between areas. Architecture tests and the documentation
  checks in `05/03-03` enforce the structural half; review (`00/02-03`) enforces the semantic half.
- **Link and token checks** — the documentation checks in `05/03-03` (stage 6) prove every relative
  link resolves and no forbidden token remains.
- **API parity** — `09/03-03` must match the contracts in `03/04-01` and `03/04-02`. The integration
  layer asserts the generated OpenAPI document and the endpoint response shapes; the parity check is
  part of the integration suite.
- **Store scoping** — a store-A admin receives `403` for store-B resources; the integration suite
  asserts this for every admin route group.
- **Outbox durability** — the integration suite asserts the event row and the state change commit
  together, and that the relay delivers at-least-once.

---

## Decisions Recorded Here

1. **Integration tests apply migrations; they do not use `EnsureCreated` for shipped schemas.**
   Migrations are the source of truth (`03/03-04`); the integration fixture applies each module's
   migrations to the Testcontainers database so mapping drift is caught before it reaches a
   deploy. The only `EnsureCreated` use is a test-only model extension in
   `BuildingBlocks.IntegrationTests`; kit and module tables always come from migrations
   (`06/01-03`).
2. **Integration tests build their own fixtures; E2E consumes the demo seed dataset.** The integration
   layer creates the minimum rows it needs through domain operations, so a test never depends on the
   shape of the development dataset. The E2E layer runs against `seed --profile demo` (`05/01-04`)
   because a journey needs a realistic catalog, and keys off stable natural identifiers.
3. **E2E browser matrix.** Chromium is the required, blocking project on every pull request. Firefox
   and WebKit run on the scheduled nightly lane and on `main`; a failure there blocks the release, not
   the pull request.
4. **No third-party mocking framework.** Unit and integration test doubles are hand-written, narrow
   fakes and stubs; the details are in `06/01-02` and `06/01-03`.

---

## Open Questions

1. The exact CI job and workflow file that runs the front-end component suite is a pipeline-scaffold
   decision; the layer and its command are fixed here, and the stage mapping is owned by `05/03-03`.
2. Whether the integration suite runs as one job or splits per module collection is a pipeline
   scaling decision; the isolation strategy in `06/01-03` holds either way.
3. The reference dataset size that the performance budgets in `01/03-01` assume is owned by `06/03-02`;
   the functional matrix above is dataset-independent.
