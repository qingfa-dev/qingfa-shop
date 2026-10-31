# Integration Tests

The Testcontainers PostgreSQL 18 fixture, the per-module integration suites, the transaction and reset strategy, and the outbox assertions that prove durable event delivery.

**Links:** [Test matrix](./01-matrix.md) · [Unit tests](./02-unit.md) · [End-to-end tests](./04-e2e.md) · [Coverage](./05-coverage.md) · [Migrations](../../03-architecture/03-data/04-migrations.md) · [Order lifecycle](../../03-architecture/02-components/03-order-lifecycle.md) · [Extension architecture](../../03-architecture/02-components/07-extension-architecture.md) · [CI pipeline](../../05-development/03-workflow/03-ci.md) · [Verification index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Integration tests are the layer that proves the platform against a real PostgreSQL 18 and a real HTTP
host. The per-module suites live in `tests/Resys.IntegrationTests` on xUnit and Testcontainers; the
BuildingBlocks cross-cutting suites described below already run in `tests/BuildingBlocks/` on the
same container strategy. This is the only layer that exercises EF Core mappings, module migrations,
the confirm transaction, the outbox, and store-scoped endpoints end to end. They are CI stage 3 in
`05/03-03` and require a working Docker engine. The migrations they apply are owned by `03/03-04`;
the contract surface they assert is `03/04-01` and `03/04-02`.

The guiding rule: **integration tests use the real database and the real host, and replace only the
external providers at the `Resys.Core` seam.**

---

## Test Project and Host

```
tests/Resys.IntegrationTests/
├─ Resys.IntegrationTests.csproj
├─ Infrastructure/
│  ├─ PostgresFixture.cs        # container + template database
│  ├─ IsolatedDatabase.cs       # per-test clone and drop
│  ├─ ResysApiFactory.cs        # WebApplicationFactory<Program>
│  └─ TestGateway.cs            # scriptable IPaymentGateway
├─ Catalog/
├─ Orders/
├─ Inventory/
├─ Shipping/
├─ Tax/
├─ Promotions/
├─ Payments/
├─ Returns/
├─ Wishlists/
├─ Reviews/
├─ Accounts/
├─ Stores/
└─ CrossCutting/                # outbox, OpenAPI parity, auth scoping
```

`ResysApiFactory : WebApplicationFactory<Program>` starts the API host in-process, overrides
`ConnectionStrings:ResysDb` with the per-test database, and registers the test doubles below through
DI. The same factory serves endpoint tests and migration tests.

---

## Cross-Cutting BuildingBlocks Suites

Two kit suites run today, ahead of the per-module suites above:

| Project | Asserts | Container |
|---|---|---|
| `tests/BuildingBlocks/BuildingBlocks.ContractTests` | Shared in-memory contract suites run against every implementation: outbox claim/lease/settle plus the `Outbox.DuplicateEventId` guard; inbox `TryBegin` dedupe per `(EventId, HandlerType)`; event store expected-version conflict (`EventStore.StreamVersionConflict`) and duplicate event id; aggregate store round-trip; specification executor/repository behavior; unit-of-work commit choreography (enqueue failure aborts, dispatch failure never fails a commit). | none — pure in-memory |
| `tests/BuildingBlocks/BuildingBlocks.IntegrationTests` | The same contracts against PostgreSQL: EF outbox claim/lease/backoff, relay batches (`OutboxRelayService.RunBatchAsync`, including a throwing dispatcher and retry backoff), inbox and event store, specification executor/repository SQL translation, unit-of-work choreography, and migration sanity (the kit migration applies twice without error). | Testcontainers `postgres:18`, one container per `postgres` collection |

Isolation matches the module strategy below in shape: `PostgresFixture` starts one `postgres:18`
container for the `postgres` collection, `TestDatabase.CreateAsync` creates a unique `bb_<guid>`
database on it, `Migrate()` applies the kit migration (schema `buildingblocks`: `outbox_messages`,
`inbox_messages`, `event_store`), and disposal drops the database `WITH (FORCE)`. Tests that add
test-only entities extend the model and create those tables with `EnsureCreated`; the shipped kit
tables always come from the migration.

---

## Testcontainers Setup for PostgreSQL

One container per test assembly, started once by an xUnit assembly fixture and reused by every test.

```csharp
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:18")
        .WithDatabase("resys_template")
        .WithUsername("resys")
        .WithPassword("resys_test")
        .Build();

    public string TemplateConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();          // waits for the readiness log/health check
        await ApplyMigrationsAsync(TemplateConnectionString);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
```

- **Image is pinned to `postgres:18`**, the same major version as local and production (`05/01-01`,
  `05/01-03`). A floating tag would hide a version-specific mapping defect.
- **Migrations, not `EnsureCreated`.** `ApplyMigrationsAsync` invokes each module's migration in turn,
  so the schema is exactly what a deploy produces and `HasData` reference rows are present
  (`03/03-04`).
- **One container for the assembly.** Starting a container per test class would dominate the run time;
  isolation comes from the database clone below, not from a new server.
- **Readiness is awaited, never slept on.** The builder's wait strategy replaces a fixed delay.
- **The CI runner exposes the Docker socket** to the job (`05/03-03` stage 3); a developer runs Docker
  locally (`05/01-03`).

---

## Transaction and Reset Strategy

Isolation uses a **template database cloned per test**, not `TRUNCATE` and not a shared schema.

1. `PostgresFixture` migrates the `resys_template` database once, including reference data.
2. For each test, `IsolatedDatabase.CreateAsync` issues
   `CREATE DATABASE resys_test_<n> TEMPLATE resys_template`, yielding a fresh connection string.
3. The test runs against that database; `ResysApiFactory` is constructed with it.
4. On teardown the database is dropped with `DROP DATABASE ... WITH (FORCE)`.

Why clone-and-drop rather than rollback:

- **Transaction rollback does not cross the HTTP boundary.** An endpoint test drives the host, which
  opens its own connection and commits; a test-owned transaction cannot roll those writes back.
- **Truncation must reason about reference rows.** A clone preserves migration-carried roles,
  currencies, the default store, and the default tax category without a reset script that could drift
  from the migrations.
- **A clone is fast.** PostgreSQL copies the template at the file level, so the cost is a directory
  copy, not a re-run of migrations.

Within a test, arrange data through domain operations or through the module service, never through
raw SQL, so invariants and events hold. A test that needs a specific state uses a builder from the
unit project or a small integration-specific helper; it does not import the development seeder.

**Concurrency tests** (two confirms for the last unit, two coupon redemptions at the limit) run on a
dedicated database and drive the two operations with real, overlapping tasks; they assert the
database constraint or the conditional update, not a mock.

---

## External Providers Replaced at the Seam

Integration tests never call a real payment gateway, mail server, or object store. Each is replaced by
a DI registration that implements the `Resys.Core` contract, exactly as an extension would
(`03/02-07`):

| Contract | Test double | Purpose |
|---|---|---|
| `IPaymentGateway` | `TestGateway` | Scriptable authorize/capture/refund outcomes, including decline. |
| `IEmailSender` | `RecordingEmailSender` | Records queued messages for assertion. |
| `IFileStorage` | `TemporaryFileStorage` | Writes to a per-test temp directory. |
| `IClock` | `FixedClock` | Pins `OccurredAtUtc` so outbox assertions are exact. |

The double is registered in `ResysApiFactory`, not injected into a module directly, so the module's
DI and endpoint path are exercised unchanged. A test asserts that a payment attempt was persisted and
that the gateway was called with the server-computed total; it does not assert the provider's own
behavior.

---

## Per-Module Integration Suites

| Module | Integration suite |
|---|---|
| Catalog | EF mappings and unique indexes (`StoreId`+`Slug`, variant SKU); store-scoped product and taxonomy queries; translation rows per locale; image metadata; a query-count assertion that a listing does not issue per-row queries (`01/03-01`). |
| Orders | Confirm commits state, stock reservation, adjustments, tax, and the outbox row in one transaction; an unreservable line rolls the whole confirm back and leaves the order in `cart`; replaying an `Idempotency-Key` returns the original order; order history is caller- and store-scoped. |
| Inventory | `StockMovement` ledger is append-only and sums to `on_hand`; two concurrent reservations of the last unit yield one success and one conflict; a movement that drives a non-backorder location negative is rejected; return reception restocks idempotently. |
| Shipping | Zone specificity resolves against stored zones; `calculator_key` resolves through the registered `IShippingCalculator`; methods are store-scoped. |
| Tax | Effective-dated rates apply by date; inclusive and exclusive store flags compute the documented amounts; a destination with no jurisdiction yields no tax. |
| Promotions | Coupon codes are unique per store; the usage-limit increment is atomic under two concurrent confirms; persisted adjustments carry their application order and source reference. |
| Payments | Payment attempts persist with the gateway reference and outcome; a refund never exceeds captured minus prior refunds; no PAN or CVV column exists. |
| Returns | Request → approve → receive persists each transition and its event; restock links to the inventory ledger; settlement respects the refund ceiling. |
| Wishlists | One wishlist per (customer, store, name); item add is idempotent; a non-owner is rejected. |
| Reviews | One active review per product per store; moderation persists; the public aggregate counts only `published` reviews. |
| Accounts | Identity stores are wired; duplicate email is rejected; store assignments are enforced by `IAdminAccessPolicy`; a mutating admin call writes an `AuditLog` row in the same transaction (`03/04-03`). |
| Stores | Resolution by request domain and by the dev `X-Store` header; store settings read; settings changes affect new orders only. |
| Cross-cutting | Migrations apply cleanly to an empty database; the generated OpenAPI document matches the endpoint response shapes (`09/03-03`); CSRF and rate-limit behavior; a store-A admin receives `403` for store-B resources. |

---

## Outbox Assertions

The outbox is the durability contract of the order lifecycle (`03/02-03`), so it is asserted at the
transaction level rather than inferred from an HTTP response.

- **Same-transaction write.** After `confirm`, query the outbox table in the same database and assert
  the expected `OrderPlaced` row exists with its `StoreId`, `OccurredAtUtc`, and payload identifiers.
- **Rollback.** Force a failure after the state change but before commit (an unreservable line) and
  assert that neither the order state nor the outbox row was persisted.
- **Relay after commit.** Run the relay and assert the row is marked delivered and the registered
  consumer received the event once.
- **At-least-once.** Stop the relay before it marks delivery, restart it, and assert the row is
  claimed and dispatched again; the outbox guarantees redelivery of the row, not of the side
  effect (`03/02-03`).
- **Inbox dedupe.** The handler-side half of that contract: a handler runs at most once per
  `(EventId, HandlerType)` because `InboxAwareNotificationPublisher` writes the inbox row before
  it invokes — asserted by the inbox contract suite in both the in-memory and the PostgreSQL
  suites, so a direct dispatch followed by a relay replay never double-handles.
- **No event on a non-transition.** A failed payment attempt advances no state and writes no
  transition event.

`FixedClock` pins `OccurredAtUtc` so the assertion compares exact values rather than a range.

---

## What Integration Tests Do Not Do

- **No mocked database.** `InMemory` and a stubbed `DbContext` are forbidden; the point of the layer is
  real SQL and real constraints.
- **No `EnsureCreated` for shipped schemas.** Migrations are the schema source of truth
  (`03/03-04`); the only `EnsureCreated` use is a test-only model extension in
  `BuildingBlocks.IntegrationTests`, never the kit or module tables.
- **No developer's local database.** The connection string always points at the per-test container.
- **No real external provider.** A gateway, mail, or storage call over the network is a defect.
- **No browser.** A journey belongs to `06/01-04`.
- **No shared mutable state across tests.** Each test owns its cloned database; tests are
  parallel-safe by construction.

---

## Open Questions

1. Whether the assembly uses one container shared by all collections or one per collection is a
   runtime-tuning decision; the clone-per-test isolation is fixed.
2. The exact helper that creates module fixtures inside a test is settled when the test project is
   scaffolded; the rule that fixtures are built through domain operations is fixed.
3. Whether the query-count assertion uses a logging interceptor or a profiling hook is an
   implementation choice; the no-N+1 guarantee is the contract.
