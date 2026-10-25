# Api.IntegrationTests

In-process HTTP tests against the real `Api` pipeline. _test · api_

## Summary

These tests boot the actual `Program.cs` through `ShopApiFactory` and assert on
status codes and payloads without opening a socket. They catch a broken
composition, a missing endpoint, or a clock-wiring regression before the heavier
AppHost suite runs. The host is shared per collection, so the pipeline boots once.

## Contains

| Path | Role | Notes |
|---|---|---|
| `ApiSmoke.Test.cs` | test | `GET /time` returns 200 with a JSON body |
| `FakeClock.Test.cs` | test | `/time` reports the injected clock, not the system clock |
| `ApiHost.Collection.cs` | collection | `ICollectionFixture<ShopApiFactory>`; one host for the suite |
| `Postgres.Collection.cs` | collection | `ICollectionFixture<PostgresFixture>` for future database suites; currently unused |
| `Api.IntegrationTests.csproj` | package | xunit v3; references `Testing` and `Api` |

## Boundaries

In scope:
- HTTP surface: routing, status codes, serialization, exception handling.
- Clock substitution through DI.

Out of scope:
- Domain and application logic → [`../Core.UnitTests/`](../Core.UnitTests/README.md) and the other unit suites.
- Database-backed scenarios; the collection exists but has no consumer yet.
- Full orchestration → [`../AppHost.IntegrationTests/`](../AppHost.IntegrationTests/README.md).

## Assumptions

- `IntegrationTestBase` receives the shared `ShopApiFactory` from
  `ApiHostCollection`; it owns only the `HttpClient`.
- `FakeTimeProvider` only moves forward, so both clock tests advance from
  `Clock.GetUtcNow()` instead of setting absolute dates. Absolute dates would make
  the result depend on execution order.
- No container engine is required; the Postgres collection is dormant.

## Invariants

- Exactly one Api host is created per collection run.
- `/time` is asserted exactly, never with a tolerance window; a tolerance would let
  the real clock pass.
- Tests never mutate the clock backwards.

## Contracts

- `GET /time` → `{ "utcNow": "<ISO-8601>" }`.
- `ShopApiFactory.Clock` → the `FakeTimeProvider` the host resolves.

## Conventions

- Derive from `IntegrationTestBase`; do not construct a factory in a test body.
- Assert status codes with AwesomeAssertions; assert payloads with `Assert`.

## Related

- Parent: [`../README.md`](../README.md)
- Fixtures: [`../Testing/`](../Testing/README.md)
- Code under test: [`../../../app/Web.Api/`](../../../app/Web.Api/)
- Layer suites: [`../Application.UnitTests/`](../Application.UnitTests/README.md)

## Notes

- 3 tests, all passing.
- `Postgres.Collection.cs` is retained deliberately; it is the wiring point for the
  first database-backed suite.
