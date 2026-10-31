# Api.Integration.Tests

In-process HTTP tests against the real `Api` pipeline. _test · api_

## Summary

These tests boot the actual `Program.cs` through `ShopApiFactory` and assert on
status codes and payloads without opening a socket. They cover the API contract,
the Identity slices, and the Todo routes, catching a broken composition or a
missing endpoint before the heavier AppHost suite runs. The host is shared per
collection, so the pipeline boots once per suite.

## Contains

| Path | Role | Notes |
|---|---|---|
| `BuildingBlocks/Api/` | test | `/time`, OpenAPI, and the shared `Result` wire contract |
| `BuildingBlocks/Security/Identity/` | test | Auth, external login, identity users/roles/permissions |
| `Modules/Todos/` | test | Todo item/list/category route coverage |
| `ApiHost.Collection.cs` | collection | `ICollectionFixture<ShopApiFactory>`; one host for the suite |
| `Postgres.Collection.cs` | collection | `ICollectionFixture<PostgresFixture>` for database-backed suites |
| `Api.Integration.Tests.csproj` | package | xunit v3; references `Testing` and `Api` |

## Boundaries

In scope:
- HTTP surface: routing, status codes, serialization, exception handling.
- The shared `Result` wire contract and OpenAPI document.
- Identity auth/identity endpoints and Todo route handlers.
- Clock substitution through DI.

Out of scope:
- Domain and application logic → [`../../Unit/Core.Unit.Tests/`](../../Unit/Core.Unit.Tests/) and the other unit suites.
- Full orchestration → [`../AppHost.Integration.Tests/`](../AppHost.Integration.Tests/).

## Assumptions

- `IntegrationTestBase` receives the shared `ShopApiFactory` from
  `ApiHostCollection`; it owns only the `HttpClient`.
- Identity and Todo suites use their own container-backed collections and skip
  with a reason when Docker/Podman is unavailable.
- `FakeTimeProvider` only moves forward, so clock tests advance from
  `Clock.GetUtcNow()` instead of setting absolute dates. Absolute dates would make
  the result depend on execution order.

## Invariants

- Exactly one Api host is created per collection run.
- `/time` is asserted exactly, never with a tolerance window; a tolerance would let
  the real clock pass.
- Tests never mutate the clock backwards.

## Contracts

- `GET /time` → `{ "utcNow": "<ISO-8601>" }`.
- `ShopApiFactory.Clock` → the `FakeTimeProvider` the host resolves.
- Every `/api/*` success/failure follows the shared `Result` envelope; `/time`
  stays a raw payload.

## Conventions

- Derive from `IntegrationTestBase` (or the identity stack base); do not construct a factory in a test body.
- Assert status codes with AwesomeAssertions; assert payloads with `Assert`.

## Related

- Parent: [`../../README.md`](../../README.md)
- Fixtures: [`../../Testing/`](../../Testing/README.md)
- Code under test: [`../../../app/Web.Api/`](../../../app/Web.Api/)
- Unit suites: [`../../Unit/Application.Unit.Tests/`](../../Unit/Application.Unit.Tests/), [`../../Unit/Core.Unit.Tests/`](../../Unit/Core.Unit.Tests/)

## Notes

- Container-backed Identity/Todo suites skip when Docker or Podman is missing.
- `Postgres.Collection.cs` is retained deliberately; it is the wiring point for
  database-backed suites.
