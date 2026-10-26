# AppHost.Tests

End-to-end tests over a real Aspire orchestration. _test · orchestrator_

## Summary

This suite starts the whole system — DCP, the API, the gateway, and both Vite
dev servers — and asserts what a browser would see through the gateway. It is the
most expensive suite, so every test shares one orchestration through
`AppHostFixture` and runs sequentially in a single collection.

## Contains

| Path | Role | Notes |
|---|---|---|
| `AppHost.Fixture.cs` | fixture | Lazily starts one `DistributedApplication` on first use |
| `AppHost.Collection.cs` | collection | `ICollectionFixture<AppHostFixture>`; name `apphost` |
| `GatewayRouting.Spec.cs` | spec | `/api/time`, storefront root, `/admin/`, and `/alive` |
| `ApiResource.Spec.cs` | spec | `api` health endpoint responds 200 |
| `WebResource.Spec.cs` | spec | Every declared resource reaches a runnable state |
| `JavaScriptResources.Util.cs` | util | `SkipUnlessToolchainAvailable()` guard for Node/pnpm |
| `AppHost.Tests.csproj` | package | Aspire.Hosting.Testing; references `AppHost` and `Testing` |

## Boundaries

In scope:
- Resource startup and health.
- Gateway routing, prefix handling, and liveness.
- What each frontend serves behind the gateway.

Out of scope:
- Edge policy internals (JWT, rate limiting) beyond reachability.
- Business behaviour of the API → [`../Api.Tests/`](../Api.Tests/README.md).

## Assumptions

- Every test calls `JavaScriptResources.SkipUnlessToolchainAvailable()` **before**
  touching the fixture. The fixture starts nothing until `GetAppAsync()` is
  awaited, so a machine without Node skips instead of paying a startup attempt.
- The shared app also starts the Vite resources, so even the gateway-only test
  needs the Node toolchain.
- Resource names are contracts: `api`, `gateway`, `shopfront`, `admin`.

## Invariants

- One orchestration per run; no test builds its own `DistributedApplication`.
- Tests never dispose the shared app; the collection fixture owns its lifetime.
- A half-started orchestration is disposed before its failure is surfaced, so
  containers and child processes do not leak.

## Contracts

- `AppHostFixture.GetAppAsync()` → the shared `DistributedApplication`.

## Flows

### Shared startup
1. The first test calls `GetAppAsync()`.
2. The fixture configures the container environment and builds the AppHost.
3. `StartAsync()` is bounded by a 180 s timeout.
4. Every later test receives the same running app.

## Conventions

- Name behavioural tests `.Spec.cs`; name shared plumbing `.Fixture.cs`,
  `.Collection.cs`, or `.Util.cs`.
- Wait for `Healthy` on project resources and `Running` on Vite resources; Vite
  publishes no readiness check.

## Related

- Parent: [`../README.md`](../README.md)
- Fixtures: [`../../Testing/`](../../Testing/README.md)
- Topology: [`../../../app/AppHost/`](../../../app/AppHost/)

## Notes

- Six tests; a full run takes roughly 20 s once the shared app is up.
- `TODO-2026Q4:` add an auth spec once token issuance exists.
