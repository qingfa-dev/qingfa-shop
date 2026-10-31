# Messaging (Wolverine + RabbitMQ) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the no-op integration bus with Wolverine, publish integration events through RabbitMQ in the host, consume them with inbox-deduped handlers, and keep local/in-memory transport for tests.

**Architecture:** Application owns integration-event contracts and the consumer base; Infrastructure owns Wolverine composition, the `IEventBus` adapter, consumers, settings and health. Domain events are bridged to integration events by a MediatR handler that stages an outbox row; the outbox dispatcher is the only `IEventBus` caller.

**Tech Stack:** .NET 10, WolverineFy 6.52.0, WolverineFx.RabbitMQ, RabbitMQ 4, `AspNetCore.HealthChecks.RabbitMQ`, xUnit v3, AwesomeAssertions, Moq, Testcontainers.RabbitMq, Aspire.Hosting.RabbitMQ.

**Spec:** `docs/specs/2026-10-29-messaging-design.md`

> **Error-handling update (2026-10-30):** all new and changed code follows
> `docs/specs/2026-10-30-monadic-error-handling-design.md`. Flow failures return `Result`;
> throws are boundary-only and carry a `// Boundary:` reason.

## Global Constraints

- Target framework `net10.0`; nullable enabled; analyzers on; warnings are errors.
- `Application` references `Core` only; no Wolverine/RabbitMQ types there.
- `Infrastructure` owns Wolverine, RabbitMQ, settings, consumers and composition.
- Do not enable Wolverine durable outbox/inbox; our outbox/inbox owns reliability.
- Delivery is at-least-once; consumer handlers are idempotent and use `IInboxStore`.
- New files follow `guide/dotted-file-naming-guide.md`; comments use labeled intent.
- Central Package Management: no `Version` on `PackageReference`.
- RabbitMQ in AppHost/Compose; InMemory/local transport in unit and API tests.
- Tests: `dotnet test --project …`; MTP exits `8` when a filter matches zero tests.
- Coverage floors: 90% line / 80% branch per gated assembly.
- Stage only task files; never `git add -A`.
- Commit after every task.

---

### Task 1: Integration Event Contracts and Domain Bridge

**Files:**
- Create: `src/Application/Common/Messaging/Events/IntegrationEvent.Base.cs`
- Create: `src/Application/Common/Messaging/Bus/EventBus.Outcome.cs`
- Create: `src/Application/Features/Todos/Events/TodoCompleted.Integration.cs`
- Create: `src/Application/Features/Todos/Events/TodoCompleted.IntegrationHandler.cs`
- Test: `tests/Unit/Application.Unit.Tests/Features/Todos/Events/TodoCompleted.IntegrationHandler.Test.cs`

**Interfaces:**
- Consumes: `IIntegrationEvent`, `IIntegrationEventPublisher`, `TodoCompleted`.
- Produces: `IntegrationEvent`, `TodoCompletedIntegration` (`EventName = "todo.completed"`, `Version = 1`), `EventBusOutcome`, the MediatR bridge.

- [ ] **Step 1: Write the failing bridge test**

Fake `IIntegrationEventPublisher` records messages and returns `Result.Ok()`.
Create a `TodoCompleted` domain event (`todoId`, `tenantId`, `completedAtUtc`), send it through
`TodoCompletedIntegrationHandler`, and assert:

- one `TodoCompletedIntegration` was published;
- its `TodoId`, `TenantId`, `CompletedAtUtc`, `EventName`, `Version` match;
- a publisher failure is propagated as a `ResultException`.

- [ ] **Step 2: Run the test and verify it fails**

```bash
dotnet build tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release
```

Expected: FAIL with `CS0246` for the event/handler types.

- [ ] **Step 3: Write the base event and outcome**

`IntegrationEvent.Base.cs`:

```csharp
namespace Application.Common.Messaging.Events;

/// <summary>Base record for integration events.</summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
    public virtual string EventName => GetType().Name;
    public virtual int Version => 1;
    public DefaultMetadata Metadata { get; init; } = [];
}
```

`EventBus.Outcome.cs` with codes `EventBus.Publish.Failed` and `EventBus.Disabled`, plus
`Failure.PublishFailed(IIntegrationEvent, Exception)` using `Error.FromException`.

- [ ] **Step 4: Write `TodoCompletedIntegration` and the bridge**

`TodoCompletedIntegration` is a sealed record with `TodoId`, `TenantId`, `CompletedAtUtc`
init properties, and `EventName => "todo.completed"`.

`TodoCompletedIntegrationHandler : INotificationHandler<TodoCompleted>` injects
`IIntegrationEventPublisher`, maps the domain event, and calls
`await publisher.PublishAsync(integration, ct)` then `ThrowIfFailure()`.

- [ ] **Step 5: Run the test and verify it passes**

```bash
dotnet build tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --no-build --filter-class "*TodoCompletedIntegrationHandlerTest*"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Application/Common/Messaging/Events src/Application/Common/Messaging/Bus/EventBus.Outcome.cs src/Application/Features/Todos/Events tests/Unit/Application.Unit.Tests/Features/Todos/Events
git commit -m "feat(app): add integration events and the domain bridge"
```

---

### Task 2: Consumer Base and Example Handler

**Files:**
- Create: `src/Application/Common/Messaging/Handlers/IntegrationEventHandler.Base.cs`
- Create: `src/Infrastructure/Integrations/Messaging/Handlers/TodoCompletedIntegration.Handler.cs`
- Test: `tests/Unit/Application.Unit.Tests/Common/Messaging/IntegrationEventHandler.Test.cs`
- Test: `tests/Unit/Infrastructure.Unit.Tests/Integrations/Messaging/Handlers/TodoCompletedIntegration.Handler.Test.cs`

**Interfaces:**
- Consumes: `IInboxStore`, `ICacheService`, `TodoCompletedIntegration`.
- Produces: `IntegrationEventHandler<TEvent>` with public `Handle` and protected `HandleAsync`; a consumer that invalidates `todo:{TodoId}`.

- [ ] **Step 1: Write the failing base-handler test**

Use a fake `IInboxStore` that records `(messageId, consumer)` and invokes the handler.
Assert:

- `Handle` calls `ExecuteOnceAsync` once with the message id and the concrete handler type name;
- the inner `HandleAsync` runs and a success result is returned;
- a failure result throws `ResultException` after the inbox returns it (so Wolverine retries).

- [ ] **Step 2: Implement the base handler**

`Handle` calls `IInboxStore.ExecuteOnceAsync(message.MessageId, GetType().FullName!, …)`; the
inner delegate calls `HandleAsync` and `ThrowIfFailure()`. `HandleAsync` is abstract and
returns `Task<Result>`.

- [ ] **Step 3: Write the failing example-handler test**

Mock `ICacheService.RemoveAsync("todo:{id}")` to return success and a fake inbox. Assert the
handler calls `RemoveAsync` once and returns success.

- [ ] **Step 4: Implement the example consumer**

`TodoCompletedIntegrationHandler : IntegrationEventHandler<TodoCompletedIntegration>` injects
`ICacheService` and `IInboxStore`, forwards the inbox to the base, and overrides
`HandleAsync` to call `cache.RemoveAsync($"todo:{message.TodoId}", ct)`.

- [ ] **Step 5: Run tests**

```bash
dotnet build tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release
dotnet build tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --no-build --filter-class "*IntegrationEventHandlerTest*"
dotnet test --project tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release --no-build --filter-class "*TodoCompletedIntegrationHandlerTest*"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Application/Common/Messaging/Handlers src/Infrastructure/Integrations/Messaging/Handlers tests/Unit/Application.Unit.Tests/Common/Messaging tests/Unit/Infrastructure.Unit.Tests/Integrations/Messaging/Handlers
git commit -m "feat(app): add inbox-deduped integration consumers"
```

---

### Task 3: Wolverine Composition, Settings, and Event Bus

**Files:**
- Create: `src/Infrastructure/Integrations/Messaging/Options/MessagingSetting.cs`
- Create: `src/Infrastructure/Integrations/Messaging/Options/MessagingSetting.Constant.cs`
- Create: `src/Infrastructure/Integrations/Messaging/Options/MessagingSetting.Validator.cs`
- Create: `src/Infrastructure/Integrations/Messaging/Options/MessagingTransport.Enum.cs`
- Create: `src/Infrastructure/Integrations/Messaging/WolverineEventBus.cs`
- Create: `src/Infrastructure/Integrations/Messaging/Messaging.Composition.cs`
- Modify: `src/Infrastructure/Integrations/Integration.Composition.cs`
- Test: `tests/Unit/Infrastructure.Unit.Tests/Integrations/Messaging/WolverineEventBus.Test.cs`
- Test: `tests/Unit/Infrastructure.Unit.Tests/Integrations/Messaging/MessagingSetting.Validator.Test.cs`

**Interfaces:**
- Consumes: `IIntegrationEvent`, `IEventBus`, `TodoCompletedIntegration`, `EventBusOutcome`, `EventTypeRegistry`.
- Produces: `MessagingSetting`, `MessagingTransport`, `WolverineEventBus`, `AddMessaging`/`UseMessaging`.

- [ ] **Step 1: Write the failing bus and validator tests**

`WolverineEventBus` with `Mock<IMessageBus>`:

- success returns `Result.Ok()` and calls `PublishAsync` once;
- a transport exception returns `EventBus.Publish.Failed`.

Validator:

- defaults and `InMemory` are valid;
- `RabbitMq` with no connection is rejected by the composition, not the validator;
- unknown transport, zero/negative counts are invalid.

- [ ] **Step 2: Implement settings and validator**

`MessagingSetting` fields: `Enabled`, `Transport` (`MessagingTransport`), `ExchangeName`,
`QueuePrefix`, `PrefetchCount`, `MaxConcurrency`, `MaxAttempts`, `RetryDelaySeconds`.
Defaults: enabled `true`, `InMemory`, exchange `qingfa.events`, prefix `qingfa`, prefetch 16,
concurrency 8, attempts 5, delay 2. Validator checks transport and positive bounds.

- [ ] **Step 3: Implement `WolverineEventBus`**

Wrap `IMessageBus.PublishAsync`; catch non-cancellation exceptions and return
`EventBusOutcome.Failure.PublishFailed(integrationEvent, exception)`.

- [ ] **Step 4: Implement `AddMessaging`**

- Bind/validate `MessagingSetting`.
- Resolve the connection string in this order: `ConnectionStrings:messaging`,
  `ConnectionStrings:rabbitmq`, `Messaging:ConnectionString`.
- If `Enabled=false`, keep the no-op bus and return.
- `builder.UseWolverine(opts => …)`:
  - include the Application event assembly and Infrastructure consumer assembly;
  - `opts.OnException<Exception>().RetryWithCooldown(delays)` using exponential delays;
  - for RabbitMQ: `opts.UseRabbitMq(new Uri(connection))`,
    `opts.PublishMessage<TodoCompletedIntegration>().ToRabbitExchange(exchange, "todo.completed")`,
    `opts.ListenToRabbitQueue($"{prefix}.todo.completed")`;
  - do not call `UseDurableOutboxOnAllSendingEndpoints` or `AutoApplyTransactions`.
- Register `IEventBus` → `WolverineEventBus` (last registration wins over the no-op).
- Register the RabbitMQ health check when active.
- Replace `EventTypeRegistry` with one that scans Core + Infrastructure and registers
  `TodoCompletedIntegration` as `"todo.completed"` version 1; keep the `IEventTypeRegistry`
  alias pointing at it.
- `UseMessaging` resolves `IMessageBus` in a scope to fail fast; disabled is a no-op.

- [ ] **Step 5: Call messaging from `Integration.Composition`**

Add `builder.AddMessaging(builder.Configuration);` in `AddIntegration` and
`app.UseMessaging();` in `UseIntegration`.

- [ ] **Step 6: Run tests**

```bash
dotnet build tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release --no-build --filter-class "*WolverineEventBusTest*"
dotnet test --project tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release --no-build --filter-class "*MessagingSettingValidatorTest*"
dotnet build QingFa.Shop.slnx -c Release
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add src/Infrastructure/Integrations/Messaging src/Infrastructure/Integrations/Integration.Composition.cs tests/Unit/Infrastructure.Unit.Tests/Integrations/Messaging
git commit -m "feat(infra): compose Wolverine and the integration bus"
```

---

### Task 4: RabbitMQ Host, Compose, Health, and API Test Defaults

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `src/Infrastructure/Infrastructure.csproj`
- Modify: `app/AppHost/AppHost.cs`
- Modify: `compose.yaml`
- Modify: `app/Web.Api/appsettings.json`
- Modify: `tests/Testing/Fixtures/ShopApi.Factory.cs`

**Interfaces:**
- Consumes: `MessagingSetting`, `AddMessaging`.
- Produces: RabbitMQ resource/connection wiring; config defaults; API tests with messaging disabled.

- [ ] **Step 1: Add the RabbitMQ health-check package**

Add `AspNetCore.HealthChecks.RabbitMQ` version `9.0.0` to `Directory.Packages.props` and the
matching `PackageReference` to `Infrastructure.csproj`. Commit `packages.lock.json` changes
from restore.

- [ ] **Step 2: Register the health check**

In `AddMessaging`, when RabbitMQ is active:
`builder.Services.AddHealthChecks().AddRabbitMQ(connection, tags: ["ready", "messaging"]);`.

- [ ] **Step 3: AppHost**

Add:

```csharp
var rabbitmq = builder.AddRabbitMQ("rabbitmq");
```

and on `api`:

```csharp
.WithReference(rabbitmq)
.WaitFor(rabbitmq)
```

- [ ] **Step 4: Compose**

Add a `rabbitmq` service using `rabbitmq:4-management`, user/password `qingfa`, ports
`5672`/`15672`, and `rabbitmq-diagnostics -q ping` health check. Add to `api`:

```yaml
ConnectionStrings__rabbitmq: amqp://qingfa:qingfa@rabbitmq:5672
depends_on:
  rabbitmq:
    condition: service_healthy
```

- [ ] **Step 5: Configuration and API tests**

Add the `Messaging` section from spec §8 to `app/Web.Api/appsettings.json` with
`Transport: "RabbitMq"`.

In `ShopApiFactory.CreateHost`, set `Messaging__Enabled=false` (alongside
`Outbox__Enabled=false`) so the fast API host does not open a broker connection.

- [ ] **Step 6: Build and run the API suite**

```bash
dotnet build QingFa.Shop.slnx -c Release
dotnet test --project tests/Integration/Api.Integration.Tests/Api.Integration.Tests.csproj -c Release
```

Expected: PASS, 25 tests.

- [ ] **Step 7: Commit**

```bash
git add Directory.Packages.props src/Infrastructure/Infrastructure.csproj app/AppHost/AppHost.cs compose.yaml app/Web.Api/appsettings.json tests/Testing/Fixtures/ShopApi.Factory.cs src/Infrastructure/Integrations/Messaging
git commit -m "feat(infra): wire RabbitMQ into the host and compose"
```

---

### Task 5: RabbitMQ Integration Tests and Full Gates

**Files:**
- Create: `tests/Integration/Infrastructure.Integration.Tests/Messaging/RabbitMq.Fixture.cs`
- Create: `tests/Integration/Infrastructure.Integration.Tests/Messaging/RabbitMq.Collection.cs`
- Create: `tests/Integration/Infrastructure.Integration.Tests/Messaging/RabbitMq.Messaging.Test.cs`

**Interfaces:**
- Consumes: Wolverine, `WolverineEventBus`, `MessagingSetting`, `TodoCompletedIntegration`.
- Produces: a real-broker publish/consume/retry suite; final gate evidence.

- [ ] **Step 1: Add the RabbitMQ fixture**

Use `Testcontainers.RabbitMq` (`rabbitmq:4-management`) and expose the AMQP connection string.
Skip cleanly when Docker is unavailable, matching the existing integration suites.

- [ ] **Step 2: Write the publish/consume test**

Build a `Host.CreateApplicationBuilder`, call `UseWolverine` with:

- `UseRabbitMq(new Uri(connection))`;
- `Discovery.IncludeAssembly` for the test consumer assembly;
- `PublishMessage<TestIntegrationEvent>().ToRabbitExchange("test.exchange", "test.event")`;
- `ListenToRabbitQueue("test.queue")`.

Publish through `WolverineEventBus`, wait for a `TaskCompletionSource` in the consumer, and
assert the payload and `MessageId` arrived. Use a timeout (`WaitAsync(15s)`), never an
unbounded wait.

- [ ] **Step 3: Write the retry test**

A consumer throws on the first two deliveries and succeeds on the third. Configure
`RetryWithCooldown` with short delays; assert three attempts and success. Keep the test
deterministic and independent from the transport's queue names.

- [ ] **Step 4: Run the RabbitMQ suite**

```bash
dotnet build tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release
dotnet test --project tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release --no-build --filter-class "*RabbitMqMessagingTest*"
```

Expected: PASS, 0 skipped (Docker required).

- [ ] **Step 5: Run full gates**

```bash
make format
make lint
make build
make test
make coverage.unit
make api.test
make infra.test
```

Expected: all green; no skipped integration tests; coverage floors unchanged.

- [ ] **Step 6: Commit**

```bash
git add tests/Integration/Infrastructure.Integration.Tests/Messaging
git commit -m "test(infra): cover the RabbitMQ messaging path"
```

---

## Self-Review

**Spec coverage**

| Spec section | Task |
|---|---|
| Integration-event contracts + domain bridge | Task 1 |
| Consumer base + inbox dedupe + example handler | Task 2 |
| Bus port, Wolverine composition, settings, transport selection | Task 3 |
| AppHost, Compose, health check, config, API defaults | Task 4 |
| RabbitMQ publish/consume/retry + full gates | Task 5 |

**Placeholder scan:** no `TBD`, `TODO`, or “similar to Task N”.

**Type consistency:** `IntegrationEvent`, `TodoCompletedIntegration`,
`IIntegrationEventPublisher`, `IntegrationEventHandler<TEvent>`, `WolverineEventBus`,
`MessagingSetting`, `MessagingTransport`, `AddMessaging`/`UseMessaging` are defined once and
used with the same names in later tasks.

## Execution Handoff

Plan complete and saved to `docs/plans/2026-10-30-messaging.md`. Execute with
subagent-driven development: fresh implementer per task, task review after each, final
whole-branch review.
