# Infrastructure

External adapters — EF Core, Redis, Wolverine, Stripe, Email, S3, HealthChecks. _layer · adapter_

## Summary

Infrastructure implements **all external integrations**. It provides concrete implementations for abstractions defined in Core and Application: `IRepository` (EF Core), `IDistributedCache` (Redis), `IMessageBus` (Wolverine/RabbitMQ), payment/email/storage clients, and health checks. It is the only layer that references EF Core, Wolverine, and third-party SDKs.

## Contains

| Path | Role | Notes |
|---|---|---|
| `Assembly.Marker.cs` | marker | Anchors assembly for scanning |
| `Persistence/` | repository | EF Core `DbContext`, repositories, migrations |
| `Caching/` | store | Redis cache, hybrid cache, session store |
| `Messaging/` | client | Wolverine/RabbitMQ configuration, outbox |
| `Payments/` | gateway | Stripe client, webhook handling |
| `Email/` | gateway | FluentEmail + SendGrid/SMTP |
| `Storage/` | gateway | S3 client, file upload/scan |
| `HealthChecks/` | health | PG, Redis, RabbitMQ, custom checks |
| `Telemetry/` | logger | OTel instrumentation, source-generated logging |
| `Composition.cs` | composition | DI registration: `AddInfrastructure()` |

## Boundaries

In scope:
- EF Core: `ShopDbContext`, `IRepository<T>` impl, migrations.
- Redis: `StackExchangeRedis`, hybrid cache, SignalR backplane.
- Wolverine: message bus, RabbitMQ transport, outbox processor.
- Stripe: payment intent, webhook verification, refunds.
- Email: FluentEmail abstraction, SendGrid/SMTP.
- S3: AWS SDK, presigned URLs, malware scan (nClam).
- Health checks: PG, Redis, RabbitMQ, self, dependencies.
- OpenTelemetry: EF Core, Redis, HTTP, runtime instrumentation.

Out of scope:
- Domain logic → [`../Core/`](../Core/).
- Use cases → [`../Application/`](../Application/).
- Public contracts → [`../Contract/`](../Contract/).
- Aspire service config → [`../ServiceDefaults/`](../ServiceDefaults/).
- HTTP endpoints → [`../../app/Web.Api/`](../../app/Web.Api/).

## Assumptions

- EF Core uses `Npgsql` with snake_case naming (`EFCore.NamingConventions`).
- Redis is cache/collaboration only — **never source of truth for financial state**.
- Wolverine outbox ensures at-least-once delivery; handlers are idempotent.
- Stripe webhooks verified via `Stripe.net` signing secret.
- File uploads scanned via ClamAV (`nClam`) before persistence.
- Health checks tagged: `live` (liveness), `ready` (readiness), `startup`.

## Invariants

- **No Application/Core logic** in Infrastructure — only adapter implementations.
- All external calls wrapped in resilience policies (retry, circuit breaker, timeout).
- EF Core `DbContext` scoped per request; repositories are scoped.
- Wolverine message handlers registered via assembly scanning.
- Connection strings from `IConfiguration` (Aspire-injected in prod).

## Contracts

- `IRepository<T>` → `EfRepository<T>` (EF Core).
- `IDistributedCache` → `RedisDistributedCache` + `HybridCache`.
- `IMessageBus` → `WolverineMessageBus` (Wolverine).
- `IPaymentGateway` → `StripePaymentGateway`.
- `IEmailSender` → `FluentEmailSender`.
- `IBlobStorage` → `S3BlobStorage`.
- `IHealthCheck` → PG, Redis, RabbitMQ, custom.

## Flows

### Order Persistence
1. `EfRepository<Order>.Add(order)` called from handler.
2. `ShopDbContext` tracks changes.
3. `TransactionBehavior` calls `SaveChangesAsync()`.
4. `OrderPlacedEvent` added to outbox table (same transaction).
5. Wolverine outbox processor publishes to RabbitMQ.

### Cache Invalidation
1. `OrderUpdatedEvent` received.
2. `HybridCache.Remove($"order:{id}")` called.
3. L1 (in-memory) + L2 (Redis) invalidated.

## Conventions

- EF Core entities in `Persistence/Entities/`.
- Configurations in `Persistence/Configurations/` (`IEntityTypeConfiguration<T>`).
- Repositories: `EfRepository<T>` (generic) + specialized (e.g., `OrderRepository`).
- Wolverine handlers in `Messaging/Handlers/`.
- Health checks: `AddNpgSqlHealthCheck()`, `AddRedisHealthCheck()`.

## Reading Order

1. `Composition.cs` — DI registration.
2. `Persistence/` — DbContext, entities, repositories, migrations.
3. `Caching/` — Redis, hybrid cache.
4. `Messaging/` — Wolverine, RabbitMQ, outbox.
5. `Payments/` / `Email/` / `Storage/` — external gateways.
6. `HealthChecks/` / `Telemetry/` — observability.

## Related

- Parent: [`../README.md`](../README.md)
- Upstream: [`../Core/`](../Core/), [`../Application/`](../Application/), [`../Contract/`](../Contract/), [`../ServiceDefaults/`](../ServiceDefaults/)
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