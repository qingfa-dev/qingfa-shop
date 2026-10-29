# tests

Test projects organized by scope and isolation level. _layer · test_

## Summary

All .NET test projects live here, organized by **test pyramid** level:
- **Testing/** — Shared test infrastructure (fixtures, factories, containers).
- **Unit/** — Fast, isolated unit tests (no containers, no external deps).
- **Integration/** — Tests with real infrastructure (Testcontainers: Postgres, Redis).
- **Architecture.Tests/** — Reflection-based fitness checks for layers, slices, naming, and comment labels.

Web unit/integration tests live beside their apps under
`app/Web.Storefront/src/__tests__` and `app/Web.Admin/src/__tests__`.

## Contains

| Path | Role | Notes |
|---|---|---|
| `Testing/` | test · shared | `ShopApiFactory`, Postgres/Redis fixtures, Respawn, container and Node probes |
| `Unit/Core.Unit.Tests/` | test · unit | Domain logic, value objects, specifications, guards |
| `Unit/Application.Unit.Tests/` | test · unit | Handlers, mapping extensions, caching, persistence abstractions |
| `Unit/Infrastructure.Unit.Tests/` | test · unit | EF Core, interceptors, seeders, migrations (no containers) |
| `Integration/Api.Integration.Tests/` | test · integration | HTTP endpoints via `ShopApiFactory` + Postgres/Redis |
| `Integration/Infrastructure.Integration.Tests/` | test · integration | EF migrations, caching, persistence, initialization |
| `Integration/AppHost.Integration.Tests/` | test · integration | Aspire resource graph, gateway routing, Vite resources |
| `Architecture.Tests/` | test · architecture | Layer, slice, naming, and comment-label fitness checks |

## Boundaries

In scope:
- All .NET automated tests (unit, integration, architecture).
- Shared test infrastructure (`Testing/`).
- Test data builders, fixtures, containers.

Out of scope:
- Production code → [`../src/`](../src/), [`../app/`](../app/).
- Web tests → [`../app/Web.Storefront/src/__tests__/`](../app/Web.Storefront/src/__tests__/), [`../app/Web.Admin/src/__tests__/`](../app/Web.Admin/src/__tests__/).
- Performance/load tests (separate tooling).

## Assumptions

- Unit tests: **no containers**, **no external services**.
- Integration tests: **Testcontainers** for Postgres/Redis; AppHost specs also need node/pnpm.
- Tests skip with a reason when Docker/Podman or node/pnpm is unavailable.
- `Testing/` is referenced by the integration projects, not by unit projects.
- Architecture checks are reflection-based; NetArchTest rules are still being filled in.

## Invariants

- **Unit tests** have zero external dependencies (EF InMemory, mocks only).
- **Integration tests** use real infrastructure; reset DB via `Respawn` between tests.
- **Architecture tests** run as part of `make lib.test`, not during `dotnet build`.
- Test projects are **never referenced by production code**.

## Contracts

- `Testing.ShopApiFactory` — creates `WebApplicationFactory<Program>` with test config.
- `Testing.PostgresFixture` / `RedisFixture` — container lifecycle (xUnit collection fixtures).
- `Testing.Respawn` — database reset between tests.
- `Architecture.Tests.LayerArchitectureTests` — documented layer dependency rules.
- `Architecture.Tests.VerticalSliceArchitectureTests` — feature isolation.
- `Architecture.Tests.NamingConventionTests` — dotted file naming rules.
- `Architecture.Tests.CodeCommentLabelTests` — comment label vocabulary.

## Flows

### Unit Test
1. Arrange: create SUT, mock dependencies (`Moq`).
2. Act: invoke method/handler.
3. Assert: verify result, interactions.

### Integration Test (API)
1. `PostgresFixture` / `RedisFixture` start containers (collection-scoped).
2. `ShopApiFactory` boots API with test connection strings.
3. Test sends HTTP request via `HttpClient`.
4. `Respawn` resets DB after test.

### Architecture Test
1. `make lib.test` runs `Architecture.Tests` after the unit suites.
2. Reflection rules fail the run with a descriptive assertion message.

## Conventions

- Test projects end in `.Tests.csproj` (`Core.Unit.Tests`, `Application.Unit.Tests`, …).
- xUnit v3: `[Fact]`, `[Theory]`, `AwesomeAssertions` (`Should().BeTrue()`).
- Test files use a known dotted role suffix (`*.Test.cs`, `*.Spec.cs`) — `make structure.check` enforces it.
- Collections: `PostgresCollection`, `RedisCollection`, `AppHostCollection`.
- No `async void`; all tests `async Task`.
- Test data via builders/factories, not hardcoded.

## Reading Order

1. `Testing/README.md` — shared infrastructure.
2. `Unit/*.Tests/` — domain logic, validators, handlers.
3. `Integration/*.Tests/` — real infrastructure tests.
4. `Architecture.Tests/` — architectural rules.

## Ownership

- Owning team: **Platform**.
- Test changes require Platform review.
- New integration tests require container resource review.

## Notes

- `TODO-2026Q3:` Add mutation testing (Stryker.NET) to CI.
- `TEMP:` `AppHost.Integration.Tests` uses reflection to load `Projects.AppHost`; improve when Aspire testing API stabilizes.
