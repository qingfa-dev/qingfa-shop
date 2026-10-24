# Testing

Shared test plumbing — fixtures, factories, and runtime probes, not a runnable
suite. _test · shared_

## Summary

`Testing` gives every suite the same building blocks. `ShopApiFactory` boots the
real `Api` pipeline in-process with time faked; `PostgresFixture` starts one
PostgreSQL container and resets it with Respawn; `RedisFixture` starts one Redis
container; `ContainerRuntime` detects Podman or Docker so container-backed tests
can target it or skip cleanly; and `NodeToolchain` detects Node/pnpm so
Vite-backed tests can skip. It contains no `[Fact]`s, and production projects
must never reference it.

## Contains

| Path | Role | Notes |
|---|---|---|
| `Fixtures/` | fixture | `ShopApi.Factory.cs` (in-process host), `Postgres.Fixture.cs` (container + Respawn), `Redis.Fixture.cs` (container) _(no README)_ |
| `Extensions/` | util | `ContainerRuntime.Util.cs` (engine probe), `NodeToolchain.Util.cs` (toolchain probe) _(no README)_ |
| `IntegrationTest.Base.cs` | fixture | Base class; takes the factory from the collection, owns the `HttpClient` |
| `Testing.csproj` | package | References `Api` and the test packages; opts out of the test-SDK rule |

## Boundaries

In scope:
- Test host bootstrapping and DI substitution.
- Container and toolchain provisioning plus clean skipping.
- Shared fixtures consumed by more than one suite.

Out of scope:
- Assertions and scenarios for a specific feature → that feature's suite.
- Domain schema → a consumer creates its own tables.
- Production code → [`../../app/`](../../app/README.md) and [`../../src/`](../../src/README.md).

## Assumptions

- The folder name intentionally avoids a test-type suffix. It sits under
  `tests/`, so it opts out of the `RESYS010` test-SDK requirement with
  `QingFaRequireTestSdk=false`.
- Tests run against the real host pipeline with time faked, not against ASP.NET
  Core mocks.
- `PostgresFixture` snapshots the schema the first time `ResetAsync` runs; create
  tables before that call or they stay out of the reset plan.
- `RedisFixture` starts `redis:7-alpine` with no persistence; it is a cache, not a
  store, and the container is discarded when the collection ends.
- `ShopApiFactory` publishes closed-port connection strings by default, so the
  fast suites compose the real infrastructure without reaching a container.
- `ShopApiFactory.Clock` is a `FakeTimeProvider`, which only moves forward.

## Invariants

- No production (`app/**` or `src/**`) project may reference this assembly.
- No `[Fact]` lives here.
- `IntegrationTestBase` never disposes the shared factory; the collection does.

## Contracts

- `ShopApiFactory` — consumed by [`../Api.IntegrationTests/`](../Api.IntegrationTests/README.md).
- `IntegrationTestBase` — consumed by [`../Api.IntegrationTests/`](../Api.IntegrationTests/README.md).
- `ContainerRuntime` — consumed by [`../AppHost.IntegrationTests/`](../AppHost.IntegrationTests/README.md) and [`../Infrastructure.IntegrationTests/`](../Infrastructure.IntegrationTests/README.md).
- `NodeToolchain` — consumed by [`../AppHost.IntegrationTests/`](../AppHost.IntegrationTests/README.md).
- `PostgresFixture` — consumed by [`../Infrastructure.IntegrationTests/`](../Infrastructure.IntegrationTests/README.md).
- `RedisFixture` — consumed by [`../Infrastructure.IntegrationTests/`](../Infrastructure.IntegrationTests/README.md).

## Conventions

- Name files `<Concept>.<Role>.cs`: `.Fixture.cs`, `.Factory.cs`, `.Util.cs`, `.Base.cs`.
- A probe exposes `IsAvailable` plus `SkipReason` so callers can skip with a reason
  instead of failing.

## Related

- Parent: [`../README.md`](../README.md)
- Consumers: [`../Api.IntegrationTests/`](../Api.IntegrationTests/README.md) · [`../AppHost.IntegrationTests/`](../AppHost.IntegrationTests/README.md) · [`../Infrastructure.IntegrationTests/`](../Infrastructure.IntegrationTests/README.md)
- Code under test: [`../../app/services/Api/`](../../app/services/Api/)

## Notes

- `ContainerRuntime` sets `TESTCONTAINERS_RYUK_DISABLED` only for rootless
  Podman, where mounting its own socket into the Ryuk helper is not possible.
