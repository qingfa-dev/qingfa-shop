# Testing

Shared test infrastructure — fixtures, factories, containers, utilities. _test · shared_

## Summary

Testing provides **reusable test infrastructure** for all integration tests. It contains the `ShopApiFactory` (WebApplicationFactory), Testcontainers fixtures for Postgres/Redis/RabbitMQ, Respawn for database reset, and container runtime utilities. Unit tests do **not** reference this project.

## Contains

| Path | Role | Notes |
|---|---|---|
| `Assembly.Marker.cs` | marker | Anchors assembly for scanning |
| `Fixtures/PostgresFixture.cs` | fixture | Postgres container lifecycle (collection fixture) |
| `Fixtures/RedisFixture.cs` | fixture | Redis container lifecycle (collection fixture) |
| `Fixtures/ShopApiFactory.cs` | factory | `WebApplicationFactory<Program>` with test config |
| `Extensions/ContainerRuntimeUtil.cs` | util | Docker/container runtime detection |
| `Extensions/NodeToolchainUtil.cs` | util | Node.js/pnpm detection for Vue tests |
| `IntegrationTestBase.cs` | base | Base class for integration tests |

## Boundaries

In scope:
- `ShopApiFactory` — configures API for testing (test connection strings, fake time, auth).
- `PostgresFixture` / `RedisFixture` — container start/stop, connection strings.
- `Respawn` — fast database reset between tests (checkpoint tables).
- Container runtime utilities — detect Docker, Podman, CI environment.

Out of scope:
- Test assertions/helpers → use `AwesomeAssertions` directly.
- Test data builders → define in each test project.
- Unit test infrastructure → unit tests use mocks, not containers.
- Vitest/Vite config → frontend projects own their test setup.

## Assumptions

- Docker/Podman available for Testcontainers (CI provides).
- Postgres 16, Redis 7, RabbitMQ 3.12 images used.
- `ShopApiFactory` uses `Microsoft.AspNetCore.Mvc.Testing`.
- Database reset via `Respawn` (checkpoint table `RespawnCheckpoint`).
- xUnit collection fixtures manage container lifetime (one per test run).

## Invariants

- **Referenced only by integration tests** — never by unit tests or production code.
- Container fixtures are **collection-scoped** (start once, stop after all tests).
- `ShopApiFactory` creates **new HttpClient per test** (isolation).
- Connection strings injected via `IConfiguration` (Aspire-compatible).
- No test data seeding in fixtures; tests own their data via builders.

## Contracts

- `PostgresFixture.ConnectionString` — async property, returns PG connection string.
- `RedisFixture.ConnectionString` — async property, returns Redis connection string.
- `ShopApiFactory.CreateClient()` — returns `HttpClient` for API calls.
- `ShopApiFactory.Services` — `IServiceProvider` for DI access in tests.
- `Respawn.Checkpoint.Reset(connectionString)` — truncates all tables except `RespawnCheckpoint`.

## Flows

### Integration Test Setup
1. xUnit discovers `PostgresCollection` (uses `PostgresFixture`).
2. `PostgresFixture` starts Postgres container, exposes port.
3. Test runs, gets `ShopApiFactory` from fixture.
4. Test sends HTTP request via `HttpClient`.
5. After test, `Respawn.Reset()` truncates tables.
6. After all tests, `PostgresFixture` disposes container.

## Conventions

- Fixtures implement `IAsyncLifetime` (`InitializeAsync`/`DisposeAsync`).
- Collection fixtures: `[CollectionDefinition("PostgresCollection", DisableParallelization = true)]`.
- Test classes: `[Collection("PostgresCollection")]`.
- Use `async Task` for all test methods.
- `CancellationToken` passed through async chains.

## Reading Order

1. `Fixtures/PostgresFixture.cs` — container lifecycle.
2. `Fixtures/RedisFixture.cs` — container lifecycle.
3. `Fixtures/ShopApiFactory.cs` — API factory config.
4. `Extensions/ContainerRuntimeUtil.cs` — runtime detection.
5. `IntegrationTestBase.cs` — base class.

## Related

- Parent: [`../README.md`](../README.md)
- Consumers: [`../Integration/Api.Tests/`](../Integration/Api.Tests/), [`../Integration/Infrastructure.Integration.Tests/`](../Integration/Infrastructure.Integration.Tests/), [`../Integration/AppHost.Tests/`](../Integration/AppHost.Tests/)
- ADR: [`../../docs/adr/0007-testing-strategy.md`](../../docs/adr/0007-testing-strategy.md)

## Ownership

- Owning team: **Platform**.
- Changes require Platform review; affects all integration tests.

## Notes

- `TODO-2026Q3:` Add `RabbitMqFixture` for Wolverine integration tests.
- `TEMP:` `NodeToolchainUtil` only used for Aspire JavaScript app detection; may remove.
- `DEADLINE-2026-11-15:` Migrate to `Testcontainers.Modules` for PG/Redis when GA.