# Infrastructure

External adapters — EF Core, Redis, Wolverine, Email/SMS, Storage, Security, HealthChecks. _layer · adapter_

## Summary

Infrastructure implements **all external integrations**. It provides concrete implementations for abstractions defined in Core and Application: `IRepository` (EF Core), cache ports (memory/Redis/hybrid), `IEventBus` (Wolverine/RabbitMQ), notification/storage/security adapters, and health checks. It is the only layer that references EF Core, Wolverine, and third-party SDKs.

## Contains

| Path | Role | Notes |
|---|---|---|
| `Assembly.Marker.cs` | marker | Anchors assembly for scanning |
| `BuildingBlocks/Adapters/Persistence/` | adapter | EF Core `ApplicationDbContext`, repositories, outbox, event store, initialization |
| `BuildingBlocks/Adapters/Caching/` | adapter | Memory, distributed, and hybrid cache implementations |
| `BuildingBlocks/Adapters/Messaging/` | adapter | Wolverine/RabbitMQ transport, serialization, inbox |
| `BuildingBlocks/Adapters/Notifications/` | adapter | Email (SendGrid/SMTP), SMS (Sinch), logging, and SignalR hubs |
| `BuildingBlocks/Adapters/Storage/` | adapter | Local, Azure, and S3 storage, image processing, malware scanning |
| `BuildingBlocks/Adapters/Security/` | adapter | Authentication, authorization, JWT/token stores, ASP.NET Core Identity |
| `BuildingBlocks/Adapters/Observability/` | adapter | Health checks, correlation, logging |
| `BuildingBlocks/Adapters/{BackgroundJobs,Events,Http,OpenApi,Serialization}/` | adapter | Host and cross-cutting adapters |
| `Modules/Todos/` | module | Todo messaging handlers, schemas, projections, event sourcing, and services |
| `Migrations/` | migration | EF Core migrations and model snapshot |
| `Infrastructure.Composition.cs` | composition | DI registration: `AddInfrastructure()` |

## Boundaries

In scope:
- EF Core: `ApplicationDbContext`, `IRepository<TEntity,TKey>` implementation, migrations.
- Redis: distributed and hybrid cache, SignalR backplane.
- Wolverine: message bus, RabbitMQ transport, outbox processor.
- Notifications: SendGrid/SMTP email, Sinch SMS, logging channel, SignalR hubs.
- Storage: local/Azure/S3 providers, presigned URLs, malware scanning.
- Security: JWT, refresh tokens, ASP.NET Core Identity, permission handlers.
- Observability: PostgreSQL/Redis/RabbitMQ health checks and OpenTelemetry.
- OpenAPI and JSON serialization setup.

Out of scope:
- Domain logic → [`../Core/`](../Core/).
- Use cases → [`../Application/`](../Application/).
- Aspire service config → [`../ServiceDefaults/`](../ServiceDefaults/).
- HTTP endpoints → [`../../app/Web.Api/`](../../app/Web.Api/).

## Assumptions

- EF Core uses `Npgsql` with snake_case naming (`EFCore.NamingConventions`).
- Redis is cache/collaboration only — **never source of truth for financial state**.
- Wolverine outbox ensures at-least-once delivery; handlers are idempotent.
- Email and SMS providers are selected through typed options.
- File uploads scanned via ClamAV (`nClam`) before persistence.
- Health checks tagged: `live` (liveness), `ready` (readiness), `startup`.

## Invariants

- **No Application/Core logic** in Infrastructure — only adapter implementations.
- `BuildingBlocks/Adapters` never reference `Modules`; module adapters may reference BuildingBlocks.
- `Infrastructure/Migrations` may reference both buckets; it is the exempt composition artifact.
- All external calls wrapped in resilience policies (retry, circuit breaker, timeout).
- EF Core `DbContext` scoped per request; repositories are scoped.
- Wolverine message handlers registered via module compositions.
- Connection strings from `IConfiguration` (Aspire-injected in prod).

## Contracts

- `IRepository<TEntity,TKey>` → `EfCoreRepository<TEntity,TKey>` (EF Core).
- `ICacheService` → `CacheService` / `NullCacheService` (memory, distributed, hybrid).
- `IEventBus` → `WolverineEventBus`.
- `IUnitOfWork` → `EfCoreUnitOfWork`.
- `INotificationService` → notification adapters and channel providers.
- `IStorageService` → local/Azure/S3 storage adapters.
- `IHealthCheck` → PostgreSQL, Redis, RabbitMQ, and outbox checks.

## Flows

### Order Persistence
1. `EfCoreRepository<TEntity,TKey>.Add(entity)` called from a handler.
2. `ApplicationDbContext` tracks changes.
3. `TransactionBehavior` calls `SaveChangesAsync()`.
4. The integration event is added to the outbox table (same transaction).
5. The outbox worker publishes to RabbitMQ through Wolverine.

### Cache Invalidation
1. A domain event is received.
2. `ICacheService.Remove(...)` is called.
3. L1 (in-memory) + L2 (Redis) invalidated through the hybrid cache adapter.

## Conventions

- Persistence configuration lives under `BuildingBlocks/Adapters/Persistence/EntityFramework/`.
- Module entity configurations and schemas live under `Modules/Todos/Persistence/Schemas/`.
- Repositories: `EfCoreRepository<TEntity,TKey>` (generic) plus module-specific projections.
- Wolverine handlers live under `Modules/Todos/Integrations/Messaging/Handlers/`.
- Health checks: `AddNpgSqlHealthCheck()`, `AddRedisHealthCheck()`.

## Reading Order

1. `Infrastructure.Composition.cs` — DI registration.
2. `BuildingBlocks/Adapters/Persistence/` — DbContext, repositories, and outbox.
3. `BuildingBlocks/Adapters/Caching/` — memory, distributed, and hybrid cache.
4. `BuildingBlocks/Adapters/Messaging/` — Wolverine, RabbitMQ, outbox.
5. `BuildingBlocks/Adapters/{Notifications,Storage,Security}/` — external adapters.
6. `Modules/Todos/` — module-specific handlers, schemas, and projections.
7. `Migrations/` — EF Core migrations.

## Related

- Parent: [`../README.md`](../README.md)
- Upstream: [`../Core/`](../Core/), [`../Application/`](../Application/), [`../ServiceDefaults/`](../ServiceDefaults/)
- Downstream: [`../../app/Web.Api/`](../../app/Web.Api/) (consumes DI), [`../../app/AppHost/`](../../app/AppHost/) (orchestrates)
- ADR: [`../../docs/adr/0004-infrastructure-adapters.md`](../../docs/adr/0004-infrastructure-adapters.md)

## Ownership

- Owning team: **Platform**.
- Infrastructure changes require Platform + Security review.
- Schema migrations require DBA approval.

## Notes

- `TODO-2026Q3:` Implement `IDistributedLock` using Redis for inventory reservation.
- `TEMP:` `SkiaSharp.NativeAssets.Linux` pinned for container builds; monitor for ARM64.
- `DEADLINE-2026-12-01:` Migrate from `Hangfire` to Wolverine scheduled jobs.