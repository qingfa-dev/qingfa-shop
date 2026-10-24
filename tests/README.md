# tests

Test projects organized by scope and isolation level. _layer · test_

## Summary

All test projects live here, organized by **test pyramid** level:
- **Testing/** — Shared test infrastructure (fixtures, factories, containers).
- **Unit/** — Fast, isolated unit tests (no containers, no external deps).
- **Integration/** — Tests with real infrastructure (Testcontainers: Postgres, Redis, RabbitMQ).
- **Contract/** — Consumer-driven contract tests (Pact).
- **Architecture.Tests/** — NetArchTest rules enforcing layer boundaries, naming, conventions.

## Contains

| Path | Role | Notes |
|---|---|---|
| `Testing/` | test · shared | `ShopApiFactory`, Postgres/Redis fixtures, Respawn, container utils |
| `Unit/Core.Tests/` | test · unit | Domain logic, value objects, specifications, guards |
| `Unit/Contract.Unit.Tests/` | test · unit | DTO serialization, validator rules, command/query shapes |
| `Unit/Application.Tests/` | test · unit | Handlers, services, mappers, policies (mocked deps) |
| `Unit/Infrastructure.Unit.Tests/` | test · unit | EF Core InMemory, mappers, configs (no containers) |
| `Integration/Api.Tests/` | test · integration | HTTP endpoints via `ShopApiFactory` + real Postgres/Redis |
| `Integration/Infrastructure.Integration.Tests/` | test · integration | EF migrations, Wolverine outbox, email, Stripe, S3 |
| `Integration/AppHost.Tests/` | test · integration | Aspire resource graph, wiring, health checks |
| `Contract/Contract.Tests/` | test · contract | Pact consumer tests for HTTP APIs |
| `Architecture.Tests/` | test · architecture | Layer deps, vertical slices, naming, comment labels |

## Boundaries

In scope:
- All automated tests (unit, integration, contract, architecture).
- Shared test infrastructure (`Testing/`).
- Test data builders, fixtures, containers.

Out of scope:
- Production code → [`../src/`](../src/), [`../app/`](../app/).
- Manual/e2e tests (Playwright) → [`../app/Web.Storefront/`](../app/Web.Storefront/), [`../app/Web.Admin/`](../app/Web.Admin/).
- Performance/load tests (separate repo/tooling).

## Assumptions

- Unit tests: **no containers**, **no external services**, run in < 1s each.
- Integration tests: **Testcontainers** for Postgres, Redis, RabbitMQ; run in parallel via xUnit collections.
- Contract tests: **Pact** broker for consumer-driven contracts; run in CI.
- Architecture tests: **NetArchTest** runs on every build; fail fast on violations.
- `Testing/` is referenced by all integration tests; **not referenced by unit tests**.

## Invariants

- **Unit tests** have zero external dependencies (EF InMemory, mocks only).
- **Integration tests** use real infrastructure; reset DB via `Respawn` between tests.
- **Architecture tests** run on `dotnet build`; fail CI on any violation.
- **Contract tests** publish to Pact broker on merge to main.
- Test projects are **never referenced by production code** (enforced by architecture tests).

## Contracts

- `Testing.ShopApiFactory` — creates `WebApplicationFactory<Program>` with test config.
- `Testing.PostgresFixture` / `RedisFixture` — container lifecycle (xUnit collection fixtures).
- `Testing.Respawn` — database reset between tests.
- `Architecture.Tests.LayerArchitectureTests` — layer dependency rules.
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
1. `dotnet build` runs NetArchTest analyzers.
2. Violations → build failure with descriptive message.
3. No runtime execution needed.

## Conventions

- Test projects named `*.Tests.csproj` (e.g., `Core.Tests`, not `Core.Unit.Tests`).
- Assembly names: `QingFa.Shop.Unit.Core.Tests`, `QingFa.Shop.Integration.Api.Tests`.
- xUnit v3: `[Fact]`, `[Theory]`, `AwesomeAssertions` (`Should().BeTrue()`).
- Collections: `PostgresCollection`, `RedisCollection`, `AppHostCollection`.
- No `async void`; all tests `async Task`.
- Test data via builders/factories, not hardcoded.

## Reading Order

1. `Testing/README.md` — shared infrastructure.
2. `Unit/*.Tests/` — domain logic, validators, handlers.
3. `Integration/*.Tests/` — real infrastructure tests.
4. `Contract/Contract.Tests/` — Pact tests.
5. `Architecture.Tests/` — architectural rules.

## Related

- Parent: [`../README.md`](../README.md)
- Upstream: [`../src/`](../src/), [`../app/`](../app/)
- ADR: [`../docs/adr/0007-testing-strategy.md`](../docs/adr/0007-testing-strategy.md)

## Ownership

- Owning team: **Platform**.
- Test changes require Platform review.
- New integration tests require container resource review.

## Notes

- `TODO-2026Q3:` Add mutation testing (Stryker.NET) to CI.
- `TEMP:` `AppHost.Tests` uses reflection to load `Projects.AppHost`; improve when Aspire testing API stabilizes.
- `DEADLINE-2026-11-01:` Migrate contract tests to Pactflow broker.