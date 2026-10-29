# ServiceDefaults

Shared Aspire service configuration — OTel, resilience, service discovery, health checks. _layer · shared_

## Summary

ServiceDefaults encapsulates **common ASP.NET Core service configuration** used by all backend services (API, workers). It provides `AddServiceDefaults()` extension that configures OpenTelemetry, resilience pipelines (retry, circuit breaker, timeout), service discovery (YARP), health checks, and problem details. It has no project references — only NuGet packages.

## Contains

| Path | Role | Notes |
|---|---|---|
| `Assembly.Marker.cs` | marker | Anchors assembly for scanning |
| `ServiceDefaults.Extension.cs` | extension | `AddServiceDefaults()` entry point |
| `OpenTelemetry/` | config | OTel metrics, traces, logs, exporters |
| `Resilience/` | policy | Retry, circuit breaker, timeout, hedging |
| `ServiceDiscovery/` | config | YARP + Aspire service discovery |
| `HealthChecks/` | health | Standard health checks (self, deps, UI) |
| `ProblemDetails/` | config | RFC 9457 problem details |

## Boundaries

In scope:
- `AddServiceDefaults(WebApplicationBuilder)` — one-call setup.
- OpenTelemetry: metrics (Prometheus), traces (OTLP), logs (structured).
- Resilience: `StandardResiliencePipeline` (retry, CB, timeout).
- Service Discovery: `AddServiceDiscovery()` + YARP integration.
- Health Checks: self, DB, Redis, RabbitMQ, downstream services.
- Problem Details: RFC 9457 for all unhandled exceptions.

Out of scope:
- Business logic → [`../Application/`](../Application/).
- Infrastructure adapters → [`../Infrastructure/`](../Infrastructure/).
- Domain/application contracts → [`../Core/`](../Core/), [`../Application/`](../Application/).
- Application-specific config (connection strings, API keys).

## Assumptions

- All services call `builder.AddServiceDefaults()` early in `Program.cs`.
- Aspire injects connection strings via `ConnectionStrings__<name>`.
- OTLP endpoint defaults to Aspire Dashboard (`http://localhost:4317`).
- Resilience pipelines use `Polly` via `Microsoft.Extensions.Resilience`.
- Health check UI available at `/health` (liveness) and `/health/ready` (readiness).

## Invariants

- **Zero project references** — only NuGet packages.
- Configuration is **additive** — services can override via `builder.Services.Configure<...>()`.
- No opinionated business logic — only cross-cutting concerns.
- Compatible with both Aspire orchestration and standalone container deployment.

## Contracts

- `AddServiceDefaults(IHostApplicationBuilder)` — main entry point.
- `AddOpenTelemetry()` — metrics + traces + logs.
- `AddResilience()` — standard pipeline + named pipelines.
- `AddServiceDiscovery()` + `AddYarp()` — client-side load balancing.
- `AddHealthChecks()` — standard + custom checks.
- `AddProblemDetails()` — RFC 9457 mapping.

## Conventions

- Extension methods in `ServiceDefaults.Extension.cs`.
- Named resilience pipelines: `standard`, `critical`, `background`.
- Health check tags: `live`, `ready`, `startup`.
- OTel resource attributes: `service.name`, `service.version`, `deployment.environment`.

## Reading Order

1. `ServiceDefaults.Extension.cs` — public API.
2. `OpenTelemetry/` — OTel setup.
3. `Resilience/` — Polly pipelines.
4. `ServiceDiscovery/` — YARP integration.
5. `HealthChecks/` — standard checks.
6. `ProblemDetails/` — error mapping.

## Related

- Parent: [`../README.md`](../README.md)
- Consumers: [`../../app/Web.Api/`](../../app/Web.Api/), [`../../app/AppHost/`](../../app/AppHost/) (via project reference)
- ADR: [`../../docs/adr/0005-service-defaults.md`](../../docs/adr/0005-service-defaults.md)

## Ownership

- Owning team: **Platform**.
- Changes require Platform review; affects all services.

## Notes

- `TODO-2026Q3:` Add `AddRateLimiting()` for API gateway scenarios.
- `TEMP:` `Microsoft.Extensions.ServiceDiscovery.Yarp` still preview; monitor for GA.
- `DEADLINE-2026-11-15:` Migrate custom health checks from Infrastructure to ServiceDefaults.