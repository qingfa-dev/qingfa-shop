# Slice ③ — Messaging (Wolverine + RabbitMQ)

- **Status:** Approved
- **Date:** 2026-10-29
- **Depends on:** slice ②
- **Unblocks:** distributed consumers and slice ④ notifications

## 1. Goal

Turn the existing outbox into a real distributed message path using the Wolverine and
RabbitMQ packages already declared in the repository.

Deliverables:

1. Integration-event contracts and a transport-neutral bus port.
2. Wolverine composition with RabbitMQ and local (in-memory) transports.
3. Consumer base with inbox dedupe.
4. Retry, dead-letter and health checks.
5. AppHost and Compose RabbitMQ wiring.
6. Testcontainers RabbitMQ integration suite.

## 2. Non-goals

- No Wolverine durable outbox or inbox. Our outbox/inbox owns reliability.
- No sagas or process managers.
- No event store.
- No cross-datacenter topology.

## 3. Integration events

### Location

`src/Application/Common/Messaging/Events`

```csharp
public abstract record IntegrationEvent : IIntegrationEvent, IHasMetadata
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
    public virtual string EventName => GetType().Name;
    public virtual int Version => 1;
    public DefaultMetadata Metadata { get; init; } = [];
}
```

Feature events live with their feature, for example:

- `src/Application/Features/Todos/Events/TodoCompleted.Integration.cs`
- `src/Application/Features/Todos/Events/TodoReopened.Integration.cs`

Rules:

- `EventName` is the wire contract. Renaming the CLR type without overriding it is a
  breaking change.
- A breaking payload change increments `Version`; consumers must tolerate unknown
  versions by dead-lettering, never by guessing.
- Events carry no EF entities and no infrastructure types.
- `EventName` and `Version` are envelope metadata for our serializer, not a broker wire
  header yet. Wolverine routes by CLR message type, so renaming a type remains a breaking
  change until a wire-type mapper is added. Cross-process payloads must carry tenant and
  correlation as explicit event fields; the metadata dictionary is not a guaranteed
  cross-process contract under Wolverine's serializer.

## 4. Bus port

### Location

`src/Application/Common/Messaging/Bus`

```csharp
public interface IEventBus
{
    Task<Result> PublishAsync<T>(
        T integrationEvent,
        CancellationToken cancellationToken = default)
        where T : class, IIntegrationEvent;
}
```

- Application handlers never call `IEventBus` directly. They call
  `IIntegrationEventPublisher` (slice ②), which stages an outbox row.
- The outbox dispatcher is the only caller of `IEventBus`.
- In slice ② the port is bound to a no-op adapter; slice ③ replaces it with the Wolverine
  adapter.

`EventBus.Outcome.cs` codes:

| Code | Meaning |
|---|---|
| `EventBus.Publish.Failed` | Wolverine rejected the message |
| `EventBus.Disabled` | No transport is configured |

## 5. Wolverine composition

### Location

`src/Infrastructure/Integrations/Messaging`

`Messaging.Composition.cs`:

```csharp
public static WebApplicationBuilder AddMessaging(
    this WebApplicationBuilder builder);

public static WebApplication UseMessaging(this WebApplication app);
```

`AddMessaging`:

1. Bind and validate `MessagingSetting`.
2. `builder.Host.UseWolverine(opts => …)`.
3. Register `IEventBus` → `WolverineEventBus`.
4. Register the health check when RabbitMQ is active.
5. Register consumer assemblies.

`UseMessaging`:

1. Resolve `IMessageBus` inside a scope to fail fast on topology errors.
2. No timer or listener is started for the local transport.

### Transport selection

| Condition | Transport |
|---|---|
| `Messaging:Enabled = false` | Wolverine local transport; no broker connection |
| `Transport = InMemory` | Wolverine local transport |
| `Transport = RabbitMq` and connection string present | RabbitMQ |
| `Transport = RabbitMq` and connection string missing | startup failure |

The connection string is read in this order:

1. `ConnectionStrings:messaging`
2. `ConnectionStrings:rabbitmq` (Aspire resource name)
3. `Messaging:ConnectionString`

### Wolverine options

- `opts.Discovery.IncludeAssembly(Application assembly)` and the Infrastructure assembly.
- Do **not** call `UseDurableOutboxOnAllSendingEndpoints` or `AutoApplyTransactions`.
  Reliability is ours; Wolverine must not create a second outbox.
- Publishing:
  - Exchange `Messaging:ExchangeName` (default `qingfa.events`), topic exchange.
  - Routing key = `IIntegrationEvent.EventName`.
  - `opts.PublishMessage<TEvent>().ToRabbitExchange(exchange, routingKey)` for every
    registered integration event.
- Listening:
  - One queue per consumer endpoint: `{QueuePrefix}.{EventName}.{ConsumerName}`.
  - `opts.ListenToRabbitQueue(queueName)`.
  - Prefetch = `Messaging:PrefetchCount`.
- Retry:
  - `MaxAttempts` and exponential delay from settings.
  - Exhausted messages go to `{queue}.dlq`.
- Concurrency:
  - `Messaging:MaxConcurrency` per listener.
- No global ordering guarantee. Consumers must not assume order across streams.

### `WolverineEventBus`

```csharp
public sealed class WolverineEventBus(IMessageBus bus) : IEventBus
{
    public async Task<Result> PublishAsync<T>(T integrationEvent, CancellationToken ct)
        where T : class, IIntegrationEvent
    {
        try
        {
            await bus.PublishAsync(integrationEvent);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(EventBusOutcome.Failure.PublishFailed(integrationEvent, ex));
        }
    }
}
```

`PublishAsync` only hands the message to the transport. The outbox dispatcher marks the
row processed after the call returns. Delivery is still at-least-once.

## 6. Consumers

### Base handler

`src/Application/Common/Messaging/Handlers/IntegrationEventHandler.Base.cs`

```csharp
public abstract class IntegrationEventHandler<TEvent>
    where TEvent : class, IIntegrationEvent
{
    protected IntegrationEventHandler(IInboxStore inbox);

    public Task Handle(TEvent message, CancellationToken cancellationToken);

    protected abstract Task<Result> HandleAsync(
        TEvent message,
        CancellationToken cancellationToken);
}
```

`Handle` calls `IInboxStore.ExecuteOnceAsync(message.MessageId, GetType().FullName, …)`.
A failure is thrown after the inbox transaction rolls back, which tells Wolverine to
retry. A duplicate message returns success without invoking `HandleAsync`.

### Example consumer

`src/Infrastructure/Integrations/Messaging/Handlers/TodoCompleted.Handler.cs`

- invalidates the Todo cache entry.
- writes an audit row.
- does not publish another event unless it uses `IIntegrationEventPublisher`, which
  stages a new outbox row.

### Consumer rules

- Handlers are idempotent even without the inbox (defense in depth).
- No handler opens a transaction manually; `IInboxStore` owns it.
- No handler blocks on `Task.Result` or sleeps.
- Handler names end in `.Handler.cs` and live under `Handlers`.

## 7. Failure semantics

| Failure | Result |
|---|---|
| Transport unavailable during dispatch | Outbox row marked failed, retried with backoff |
| Consumer throws | Wolverine retries up to `MaxAttempts` |
| Consumer exhausts retries | Message moved to `{queue}.dlq`, logged with envelope id |
| Duplicate delivery | Inbox returns success, handler skipped |
| Unknown event version | Dead-letter immediately; no blind deserialization |
| Poison payload | Dead-letter after deserialization failure; outbox row not retried forever |

## 8. Configuration

```json
{
  "Messaging": {
    "Enabled": true,
    "Transport": "RabbitMq",
    "ExchangeName": "qingfa.events",
    "QueuePrefix": "qingfa",
    "PrefetchCount": 16,
    "MaxConcurrency": 8,
    "MaxAttempts": 5,
    "RetryDelaySeconds": 2
  }
}
```

- `MessagingSetting` in `Infrastructure.Integrations.Messaging.Options`.
- Validator rejects unknown transports, non-positive counts and missing connection strings
  when RabbitMQ is selected.
- AppHost uses RabbitMQ. Unit and API tests use `InMemory`.

## 9. Infrastructure wiring

### AppHost

```csharp
var rabbitmq = builder.AddRabbitMQ("rabbitmq");

api.WithReference(rabbitmq)
   .WaitFor(rabbitmq);
```

The reference injects `ConnectionStrings__rabbitmq`.

### Compose

Add a `rabbitmq` service:

```yaml
rabbitmq:
  image: rabbitmq:4-management
  environment:
    RABBITMQ_DEFAULT_USER: qingfa
    RABBITMQ_DEFAULT_PASS: qingfa
  ports:
    - "5672:5672"
    - "15672:15672"
  healthcheck:
    test: ["CMD", "rabbitmq-diagnostics", "-q", "ping"]
    interval: 10s
    timeout: 5s
    retries: 10
```

Api gains:

- `ConnectionStrings__rabbitmq: amqp://qingfa:qingfa@rabbitmq:5672`
- `depends_on: rabbitmq: condition: service_healthy`

### Health

- Add `AspNetCore.HealthChecks.RabbitMQ` to `Directory.Packages.props` and
  `Infrastructure.csproj`.
- `AddRabbitMQ` health check tagged `ready`, enabled only for the RabbitMQ transport.

## 10. Tests

### Unit

`tests/Unit/Infrastructure.Unit.Tests/Integrations/Messaging/`

- settings validator accepts local and RabbitMQ configurations, rejects invalid ones.
- `WolverineEventBus` returns success and maps transport exceptions to
  `EventBus.Publish.Failed`.
- routing key equals `EventName`.
- consumer base calls the inbox once and skips duplicates.
- consumer base throws when `HandleAsync` fails, so Wolverine retries.

### Integration (Testcontainers RabbitMQ)

`tests/Integration/Infrastructure.Integration.Tests/Messaging/`

- publish → consume round-trip through RabbitMQ.
- transient consumer failure → retry succeeds.
- exhausted consumer failure → message lands in the dead-letter queue.
- duplicate delivery executes the handler once (inbox).
- RabbitMQ health check reports healthy.

The fixture follows the existing Testcontainers pattern and is skipped when Docker is
unavailable, matching the rest of the integration suite.

### API

- API tests run with `Messaging:Transport=InMemory`.
- `CompleteTodo` → outbox dispatch → integration event → consumer marker, without a
  broker.

## 11. Acceptance criteria

- `make ci` passes.
- The host starts with RabbitMQ and with the local transport.
- No Wolverine outbox/inbox tables are created; only our tables exist.
- A duplicate delivery is a no-op.
- Poisons reach the DLQ and are logged.
- AppHost and Compose expose RabbitMQ and the Api waits for it.
