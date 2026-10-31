# Reliable Events Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make domain-event delivery durable with an outbox and inbox, and make command handlers atomic with a transaction pipeline behavior.

**Architecture:** Aggregate events are staged as outbox rows in the same EF Core save as the state change. A background dispatcher claims rows with `FOR UPDATE SKIP LOCKED`, publishes domain events through MediatR and integration events through a bus port, and marks/retries/dead-letters them. Consumers dedupe through an inbox. `TransactionBehavior` wraps command handlers in a unit-of-work transaction.

**Tech Stack:** .NET 10, EF Core 10 with PostgreSQL 16, `System.Text.Json`, MediatR, FluentValidation, xUnit v3, AwesomeAssertions, Moq, Testcontainers.

**Spec:** `docs/specs/2026-10-29-reliable-events-design.md`

> **Error-handling update (2026-10-30):** all new and changed code follows
> `docs/specs/2026-10-30-monadic-error-handling-design.md`. Flow failures return `Result`;
> throws are boundary-only and carry a `// Boundary:` reason.

## Global Constraints

- Target framework `net10.0`; nullable enabled; analyzers on; warnings are errors.
- `Core` has zero project references; `Application` references `Core` only; no EF/Wolverine/RabbitMQ types in either.
- `Infrastructure` owns EF stores, serializers, interceptors, workers and composition.
- New files follow `guide/dotted-file-naming-guide.md`; the final dotted segment must be a known role.
- Comments use labeled intent (`Validate:`, `Enforce:`, `Publish:`, `Retry:`), never freeform narration.
- Central Package Management: never add a `Version` attribute to a `PackageReference`.
- Outbox/inbox live in the `messaging` schema; add it to `PersistenceConstant.Schema.Names`.
- Delivery is at-least-once; handlers must be idempotent; the inbox is the consumer-side guard.
- Tests use `dotnet test --project …`; MTP exits `8` when a filter matches zero tests.
- Coverage floors: 90% line / 80% branch per gated assembly.
- Stage only files named by the task; never use `git add -A`.
- Commit after every task.

---

### Task 1: Messaging Contracts

**Files:**
- Create: `src/Application/Common/Messaging/Outbox/OutboxMessage.Model.cs`
- Create: `src/Application/Common/Messaging/Outbox/OutboxMessageKind.Enum.cs`
- Create: `src/Application/Common/Messaging/Outbox/OutboxMessageStatus.Enum.cs`
- Create: `src/Application/Common/Messaging/Outbox/Outbox.Store.Interface.cs`
- Create: `src/Application/Common/Messaging/Outbox/Outbox.Dispatcher.Interface.cs`
- Create: `src/Application/Common/Messaging/Outbox/Outbox.Outcome.cs`
- Create: `src/Application/Common/Messaging/Inbox/InboxMessage.Model.cs`
- Create: `src/Application/Common/Messaging/Inbox/Inbox.Store.Interface.cs`
- Create: `src/Application/Common/Messaging/Bus/IntegrationEvent.Interface.cs`
- Create: `src/Application/Common/Messaging/Bus/EventBus.Interface.cs`
- Create: `src/Application/Common/Messaging/Bus/IntegrationEventPublisher.Interface.cs`
- Create: `src/Application/Common/Messaging/Serialization/MetadataSerializer.Interface.cs`
- Create: `src/Application/Common/Messaging/Serialization/DomainEventSerializer.Interface.cs`
- Create: `src/Application/Common/Messaging/Serialization/IntegrationEventSerializer.Interface.cs`
- Create: `src/Application/Common/Messaging/Serialization/EventTypeRegistry.Interface.cs`
- Test: `tests/Unit/Application.Unit.Tests/Common/Messaging/MessagingContract.Test.cs`

**Interfaces:**
- Consumes: `Result`, `Error`, `IDomainEvent`, `IHasMetadata` from `Core`.
- Produces: outbox/inbox models and stores, `IIntegrationEvent`, `IEventBus`, `IIntegrationEventPublisher`, serializer contracts, `IEventTypeRegistry`.

- [ ] **Step 1: Write the failing contract test**

```csharp
using Application.Common.Messaging.Outbox;

namespace Application.Unit.Tests.Common.Messaging;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Messaging")]
public sealed class MessagingContractTest
{
    [Fact]
    public void OutboxMessage_DefaultsToPendingWithAGeneratedId()
    {
        var message = new OutboxMessage();

        message.Id.Should().NotBe(Guid.Empty);
        message.Kind.Should().Be(OutboxMessageKind.Domain);
        message.Status.Should().Be(OutboxMessageStatus.Pending);
        message.Attempts.Should().Be(0);
        message.Version.Should().Be(1);
        message.OccurredOnUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void OutboxOutcome_CodesAreStable()
    {
        OutboxOutcome.Codes.EnqueueFailed.Should().Be("Outbox.Enqueue.Failed");
        OutboxOutcome.Codes.ClaimFailed.Should().Be("Outbox.Claim.Failed");
        OutboxOutcome.Codes.SerializeFailed.Should().Be("Outbox.Serialize.Failed");
        OutboxOutcome.Codes.DeserializeFailed.Should().Be("Outbox.Deserialize.Failed");
        OutboxOutcome.Codes.PublishFailed.Should().Be("Outbox.Publish.Failed");
        OutboxOutcome.Codes.MarkFailed.Should().Be("Outbox.Mark.Failed");
        OutboxOutcome.Codes.LeaseLost.Should().Be("Outbox.Lease.Lost");
        OutboxOutcome.Codes.DeadLetterFailed.Should().Be("Outbox.DeadLetter.Failed");
        OutboxOutcome.Codes.NotFound.Should().Be("Outbox.NotFound");
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

```bash
dotnet build tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release
```

Expected: FAIL with `CS0246` for `Application.Common.Messaging.Outbox`.

- [ ] **Step 3: Write the outbox model and enums**

`OutboxMessage.Model.cs`:

```csharp
namespace Application.Common.Messaging.Outbox;

/// <summary>A durable message staged in the same transaction as the state change.</summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public OutboxMessageKind Kind { get; init; } = OutboxMessageKind.Domain;
    public string Type { get; init; } = string.Empty;
    public int Version { get; init; } = 1;
    public string Payload { get; init; } = string.Empty;
    public string Metadata { get; init; } = "{}";
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset AvailableOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LockedUntilUtc { get; set; }
    public Guid? LeaseId { get; set; }
    public DateTimeOffset? ProcessedOnUtc { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public Guid? CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public Guid? TenantId { get; init; }
}
```

`OutboxMessageKind.Enum.cs`: `Domain = 1`, `Integration = 2`.
`OutboxMessageStatus.Enum.cs`: `Pending = 1`, `Processed = 2`, `DeadLettered = 3`.

- [ ] **Step 4: Write the outbox contracts and outcome**

`Outbox.Store.Interface.cs`:

```csharp
namespace Application.Common.Messaging.Outbox;

/// <summary>Stages and claims durable outbox messages.</summary>
public interface IOutboxStore
{
    Task<Result> EnqueueAsync(
        IReadOnlyCollection<OutboxMessage> messages,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<OutboxMessage>>> ClaimAsync(
        int batchSize,
        DateTimeOffset nowUtc,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task<Result> MarkProcessedAsync(
        Guid id,
        Guid leaseId,
        DateTimeOffset processedOnUtc,
        CancellationToken cancellationToken = default);

    Task<Result> MarkFailedAsync(
        Guid id,
        Guid leaseId,
        string error,
        DateTimeOffset nextAttemptUtc,
        CancellationToken cancellationToken = default);

    Task<Result> DeadLetterAsync(
        Guid id,
        Guid leaseId,
        string error,
        CancellationToken cancellationToken = default);

    Task<Result<int>> CountPendingAsync(CancellationToken cancellationToken = default);

    Task<Result<DateTimeOffset?>> OldestPendingOnUtcAsync(
        CancellationToken cancellationToken = default);

    Task<Result<int>> DeleteProcessedBeforeAsync(
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken = default);
}
```

`Outbox.Dispatcher.Interface.cs`:

```csharp
namespace Application.Common.Messaging.Outbox;

/// <summary>Dispatches one batch of outbox messages.</summary>
public interface IOutboxDispatcher
{
    Task<Result<int>> DispatchBatchAsync(CancellationToken cancellationToken = default);
}
```

`Outbox.Outcome.cs`:

```csharp
namespace Application.Common.Messaging.Outbox;

/// <summary>Failures produced by the outbox pipeline.</summary>
public static class OutboxOutcome
{
    public static class Failure
    {
        public static Error EnqueueFailed(Exception exception) => Error.FromException(
            exception, Codes.EnqueueFailed);

        public static Error ClaimFailed(Exception exception) => Error.FromException(
            exception, Codes.ClaimFailed);

        public static Error SerializeFailed(Exception exception) => Error.FromException(
            exception, Codes.SerializeFailed);

        public static Error PublishFailed(OutboxMessage message, string reason) => Error.InternalServerError(
            Codes.PublishFailed, $"Outbox message '{message.Id}' could not be published: {reason}");

        public static Error DeserializeFailed(OutboxMessage message, string reason) => Error.InternalServerError(
            Codes.DeserializeFailed, $"Outbox message '{message.Id}' could not be deserialized: {reason}");

        public static Error MarkFailed(Guid id, string reason) => Error.InternalServerError(
            Codes.MarkFailed, $"Outbox message '{id}' could not be updated: {reason}");

        public static Error LeaseLost(Guid id) => Error.Conflict(
            Codes.LeaseLost, $"Outbox message '{id}' is no longer leased by this worker.");

        public static Error DeadLetterFailed(Guid id, string reason) => Error.InternalServerError(
            Codes.DeadLetterFailed, $"Outbox message '{id}' could not be dead-lettered: {reason}");

        public static Error NotFound(Guid id) => Error.NotFound(
            Codes.NotFound, $"Outbox message '{id}' was not found.");
    }

    public static class Codes
    {
        public const string EnqueueFailed = "Outbox.Enqueue.Failed";
        public const string ClaimFailed = "Outbox.Claim.Failed";
        public const string SerializeFailed = "Outbox.Serialize.Failed";
        public const string DeserializeFailed = "Outbox.Deserialize.Failed";
        public const string PublishFailed = "Outbox.Publish.Failed";
        public const string MarkFailed = "Outbox.Mark.Failed";
        public const string LeaseLost = "Outbox.Lease.Lost";
        public const string DeadLetterFailed = "Outbox.DeadLetter.Failed";
        public const string NotFound = "Outbox.NotFound";
    }
}
```

- [ ] **Step 5: Write the inbox model and store contract**

`InboxMessage.Model.cs`:

```csharp
namespace Application.Common.Messaging.Inbox;

/// <summary>A message already processed by one consumer.</summary>
public sealed class InboxMessage
{
    public Guid MessageId { get; init; }
    public string Consumer { get; init; } = string.Empty;
    public DateTimeOffset ProcessedOnUtc { get; init; }
}
```

`Inbox.Store.Interface.cs`:

```csharp
namespace Application.Common.Messaging.Inbox;

/// <summary>Runs a consumer handler at most once per message id and consumer.</summary>
public interface IInboxStore
{
    Task<Result> ExecuteOnceAsync(
        Guid messageId,
        string consumer,
        Func<CancellationToken, Task<Result>> handler,
        CancellationToken cancellationToken = default);
}
```

The XML remarks must state: the inbox row is written only after the handler returns
`Success`, in the same transaction as the handler's writes; a duplicate returns `Success`
without invoking the handler; a failure is never recorded as processed.

- [ ] **Step 6: Write the bus and serialization contracts**

`IntegrationEvent.Interface.cs`:

```csharp
namespace Application.Common.Messaging.Bus;

/// <summary>A message published to another process.</summary>
public interface IIntegrationEvent : IHasMetadata
{
    Guid MessageId { get; }
    DateTimeOffset OccurredOnUtc { get; }
    string EventName { get; }
    int Version { get; }
}
```

`EventBus.Interface.cs`:

```csharp
public interface IEventBus
{
    Task<Result> PublishAsync<T>(
        T integrationEvent,
        CancellationToken cancellationToken = default)
        where T : class, IIntegrationEvent;
}
```

`IntegrationEventPublisher.Interface.cs`:

```csharp
public interface IIntegrationEventPublisher
{
    Task<Result> PublishAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default);
}
```

`MetadataSerializer.Interface.cs`:

```csharp
public interface IMetadataSerializer
{
    string Serialize(IMetadata metadata);
}
```

`DomainEventSerializer.Interface.cs`:

```csharp
public interface IDomainEventSerializer : IMetadataSerializer
{
    string Serialize(IDomainEvent domainEvent);
    IDomainEvent Deserialize(string eventName, int eventVersion, string payload);
}
```

`IntegrationEventSerializer.Interface.cs`:

```csharp
public interface IIntegrationEventSerializer : IMetadataSerializer
{
    string Serialize(IIntegrationEvent integrationEvent);
    IIntegrationEvent Deserialize(string eventName, int eventVersion, string payload);
}
```

`EventTypeRegistry.Interface.cs`:

```csharp
public interface IEventTypeRegistry
{
    Type Resolve(string eventName, int eventVersion);
}
```

- [ ] **Step 7: Run the test and verify it passes**

```bash
dotnet build tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --no-build --filter-class "Application.Unit.Tests.Common.Messaging.MessagingContractTest"
```

Expected: PASS, 2 tests.

- [ ] **Step 8: Commit**

```bash
git add src/Application/Common/Messaging tests/Unit/Application.Unit.Tests/Common/Messaging
git commit -m "feat(app): add outbox and inbox contracts"
```

---

### Task 2: JSON Serialization and Event Type Registry

**Files:**
- Create: `src/Infrastructure/Integrations/Messaging/Serialization/JsonMessageSerializer.cs`
- Create: `src/Infrastructure/Integrations/Messaging/Serialization/MetadataJsonConverter.cs`
- Create: `src/Infrastructure/Integrations/Messaging/Serialization/EventTypeRegistry.cs`
- Test: `tests/Unit/Infrastructure.Unit.Tests/Integrations/Messaging/Serialization/JsonMessageSerializer.Test.cs`
- Test: `tests/Unit/Infrastructure.Unit.Tests/Integrations/Messaging/Serialization/EventTypeRegistry.Test.cs`

**Interfaces:**
- Consumes: Task 1 contracts; `Core.Common.Domain.Events.IDomainEvent`, `DefaultMetadata`.
- Produces: `JsonMessageSerializer` implementing both serializer contracts; `EventTypeRegistry` implementing `IEventTypeRegistry` with `Register<T>(string, int)`.

- [ ] **Step 1: Write the failing serializer test**

Test a local `TestDomainEvent` (record deriving `DomainEvent` with a `Name` property) and a local `TestIntegrationEvent` (record implementing `IIntegrationEvent` and `IHasMetadata`). Assertions:

- domain round-trip preserves type, `EventName`, `EventVersion`, and a metadata value;
- integration round-trip preserves type, `EventName`, `Version`, and metadata;
- unknown event name throws;
- metadata serializes as a JSON object, not a dictionary wrapper;
- enums serialize as strings.

Use `new EventTypeRegistry(typeof(JsonMessageSerializerTest).Assembly)` and register the integration test event as `"test.integration"` version 1.

- [ ] **Step 2: Run the test and verify it fails**

```bash
dotnet build tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release
```

Expected: FAIL with `CS0246` for `Infrastructure.Integrations.Messaging.Serialization`.

- [ ] **Step 3: Write `EventTypeRegistry`**

```csharp
using System.Reflection;

using Application.Common.Messaging.Bus;
using Application.Common.Messaging.Serialization;

using Core.Common.Domain.Events;

namespace Infrastructure.Integrations.Messaging.Serialization;

/// <summary>Resolves stable event names and versions to CLR types.</summary>
public sealed class EventTypeRegistry : IEventTypeRegistry
{
    private readonly Dictionary<string, Type> _types = new(StringComparer.Ordinal);

    public EventTypeRegistry(params Assembly[] assemblies)
    {
        foreach (Assembly assembly in assemblies)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                if (typeof(IDomainEvent).IsAssignableFrom(type))
                    Register(type, type.Name, 1);
            }
        }
    }

    public void Register<T>(string eventName, int eventVersion) where T : class
        => Register(typeof(T), eventName, eventVersion);

    public void Register(Type type, string eventName, int eventVersion)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(eventVersion);

        _types[Key(eventName, eventVersion)] = type;
    }

    public Type Resolve(string eventName, int eventVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        return _types.TryGetValue(Key(eventName, eventVersion), out Type? type)
            ? type
            : throw new InvalidOperationException(
                $"No event type is registered for '{eventName}' version {eventVersion}.");
    }

    private static string Key(string eventName, int eventVersion)
        => $"{eventName}:{eventVersion}";
}
```

- [ ] **Step 4: Write `MetadataJsonConverter`**

Read a JSON object into `DefaultMetadata` whose values are cloned `JsonElement`s; write each key and value with the current serializer options.

- [ ] **Step 5: Write `JsonMessageSerializer`**

Use `JsonSerializerDefaults.Web`, `JsonStringEnumConverter`, and `MetadataJsonConverter`. Domain deserialization resolves `Type.GetType(typeName, throwOnError: false)` first, then the registry. Throw `InvalidOperationException` when the payload deserializes to null.

- [ ] **Step 6: Run the tests and verify they pass**

```bash
dotnet build tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release --no-build --filter-class "*JsonMessageSerializerTest*"
dotnet test --project tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release --no-build --filter-class "*EventTypeRegistryTest*"
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add src/Infrastructure/Integrations/Messaging/Serialization tests/Unit/Infrastructure.Unit.Tests/Integrations/Messaging/Serialization
git commit -m "feat(infra): serialize domain and integration events"
```

---

### Task 3: Outbox Persistence and Migration

**Files:**
- Modify: `src/Application/Common/Persistence/PersistenceConstant.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/Outbox/OutboxMessage.Configuration.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/Inbox/InboxMessage.Configuration.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/Outbox/Outbox.Store.cs`
- Test: `tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/Outbox/Outbox.Store.Test.cs`
- Test: `tests/Integration/Infrastructure.Integration.Tests/Messaging/Outbox.Store.Integration.Test.cs`
- Test fixture: `tests/Integration/Infrastructure.Integration.Tests/Messaging/Messaging.Fixture.cs`
- Test fixture: `tests/Integration/Infrastructure.Integration.Tests/Messaging/Messaging.Collection.cs`
- Migration: generated `AddOutboxInbox` under `src/Infrastructure/Persistence/Migrations/`

**Interfaces:**
- Consumes: Task 1 `OutboxMessage`, `IOutboxStore`, `OutboxOutcome`.
- Produces: `EfCoreOutboxStore`, `OutboxMessageConfiguration`, `InboxMessageConfiguration`, `messaging` schema, migration.

- [ ] **Step 1: Add the schema constant**

In `PersistenceConstant.Schema`, add:

```csharp
public const string Messaging = "messaging";
```

and include it in `Names`: `[Todo, Identity, Messaging]`.

- [ ] **Step 2: Write the failing unit test**

Cover:

- `EnqueueAsync` stages rows (no save) and returns success.
- `MarkProcessedAsync` sets `ProcessedOnUtc`, clears the lease and error.
- `MarkFailedAsync` increments attempts, sets `LastError` and `AvailableOnUtc`.
- `DeadLetterAsync` sets `DeadLettered`.
- `CountPendingAsync` counts only pending.
- `NotFound` is returned for an unknown id.

Use an InMemory `DbContext` with `OutboxMessageConfiguration` applied. Do not test `ClaimAsync` (raw SQL) or `DeleteProcessedBeforeAsync` (`ExecuteDelete`) here.

- [ ] **Step 3: Write the EF configurations**

`OutboxMessageConfiguration`:

- table `outbox_messages` in `messaging`;
- key `Id`;
- `Kind`/`Status` as integer enums;
- `Payload`/`Metadata` `jsonb`;
- indexes `(Status, AvailableOnUtc, OccurredOnUtc)` and `ProcessedOnUtc`.

`InboxMessageConfiguration`:

- table `inbox_messages` in `messaging`;
- composite key `(MessageId, Consumer)`;
- index `ProcessedOnUtc`.

- [ ] **Step 4: Write `EfCoreOutboxStore`**

- `EnqueueAsync`: guard null, add range to `Set<OutboxMessage>()`, return success. Do not save.
- `ClaimAsync`: validate batch/lease, begin a transaction, `FromSqlRaw` with the exact claim SQL from the spec (`FOR UPDATE SKIP LOCKED`), assign a new `LeaseId`, set `LockedUntilUtc`, save, commit.
- `MarkProcessedAsync` / `MarkFailedAsync` / `DeadLetterAsync`: use a single `ExecuteUpdateAsync` conditional on `Id == id && LeaseId == leaseId && LockedUntilUtc > now`; return `OutboxOutcome.Failure.LeaseLost(id)` when zero rows match. `DeadLetterAsync` must not increment `Attempts`; the dispatcher owns the attempt count.
- `CountPendingAsync` / `OldestPendingOnUtcAsync`: LINQ.
- `DeleteProcessedBeforeAsync`: `ExecuteDeleteAsync`.
- Translate exceptions through `OutboxOutcome.Failure.*`.

- [ ] **Step 5: Generate the migration**

```bash
dotnet ef migrations add AddOutboxInbox \
  -p src/Infrastructure \
  -s app/Web.Api \
  -o Persistence/Migrations
```

Inspect it: one `EnsureSchema("messaging")`, the two tables, their indexes, and a symmetric `Down` that drops the schema. No unrelated model changes.

- [ ] **Step 6: Run the unit tests**

```bash
dotnet build tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release --no-build --filter-class "*OutboxStoreTest*"
```

Expected: PASS.

- [ ] **Step 7: Add the Postgres fixture**

`Messaging.Fixture.cs` boots a `postgres:16-alpine` container, registers `ApplicationDbContext` with `UseNpgsql` and `EnsureCreatedAsync`, `IUnitOfWork`, `EfCoreOutboxStore`, the JSON serializer, and a Respawner with `SchemasToInclude = ["messaging", "todo"]` and migration history ignored. Expose `Services`, `ConnectionString`, `CreateScope`, `CleanAsync`.

`Messaging.Collection.cs` defines `[CollectionDefinition("messaging")]`.

- [ ] **Step 8: Write the failing Postgres store integration test**

Cover:

- enqueue + claim returns the row and sets a lease;
- two stores with two contexts claim disjoint batches;
- a fresh row whose `AvailableOnUtc` is in the future is not claimed;
- mark processed/failed/dead-letter persists the status;
- retention deletes only processed rows.

- [ ] **Step 9: Run the integration tests**

```bash
dotnet build tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release
dotnet test --project tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release --no-build --filter-class "*OutboxStoreIntegrationTest*"
```

Expected: PASS, 0 skipped (Docker required).

- [ ] **Step 10: Commit**

```bash
git add \
  src/Application/Common/Persistence/PersistenceConstant.cs \
  src/Infrastructure/Persistence/EntityFramework/Outbox \
  src/Infrastructure/Persistence/EntityFramework/Inbox \
  src/Infrastructure/Persistence/Migrations \
  tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/Outbox \
  tests/Integration/Infrastructure.Integration.Tests/Messaging
git commit -m "feat(infra): add outbox persistence"
```

---

### Task 4: Inbox Persistence

**Files:**
- Create: `src/Infrastructure/Persistence/EntityFramework/Inbox/Inbox.Store.cs`
- Modify: `tests/Integration/Infrastructure.Integration.Tests/Messaging/Messaging.Fixture.cs` (register the inbox store)
- Test: `tests/Integration/Infrastructure.Integration.Tests/Messaging/Inbox.Store.Integration.Test.cs`

**Interfaces:**
- Consumes: Task 1 `InboxMessage`, `IInboxStore`; `IUnitOfWork`; `UnitOfWorkOutcome.Codes.DuplicateKey`.
- Produces: `EfCoreInboxStore.ExecuteOnceAsync`.

- [ ] **Step 1: Write the failing integration test**

Cover:

- first delivery executes the handler and inserts one inbox row;
- a duplicate delivery returns success and does not execute the handler again;
- a handler failure rolls back, leaves no inbox row, and a retry executes it again;
- a concurrent duplicate insert is treated as already processed.

- [ ] **Step 2: Implement `EfCoreInboxStore`**

Follow the spec §7 semantics:

- begin a transaction through `IUnitOfWork`;
- return success immediately when `(MessageId, Consumer)` exists (rollback/commit nothing);
- run the handler;
- on failure roll back and return the failure;
- insert the inbox row, save, commit;
- on a duplicate-key save failure roll back and return success;
- on any exception roll back with `CancellationToken.None` and return a stable failure `Inbox.Execution.Failed`.

Inject `DbContext`, `IUnitOfWork`, `TimeProvider`.

- [ ] **Step 3: Run the integration tests**

```bash
dotnet build tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release
dotnet test --project tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release --no-build --filter-class "*InboxStoreIntegrationTest*"
```

Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add src/Infrastructure/Persistence/EntityFramework/Inbox/Inbox.Store.cs tests/Integration/Infrastructure.Integration.Tests/Messaging
git commit -m "feat(infra): add inbox dedupe store"
```

---

### Task 5: Outbox Interceptor and Integration Publisher

**Files:**
- Create: `src/Infrastructure/Persistence/EntityFramework/Outbox/Outbox.Interceptor.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/Outbox/Outbox.Interceptor.Logger.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/Outbox/OutboxIntegrationEventPublisher.cs`
- Create: `src/Infrastructure/Events/NoOpEventBus.cs`
- Modify: `src/Infrastructure/Persistence/EntityFramework/Interceptors/Interceptor.Registry.cs`
- Modify: `src/Infrastructure/Persistence/EntityFramework/EntityFramework.Composition.cs`
- Delete: `src/Infrastructure/Persistence/EntityFramework/Interceptors/DomainEvent.Interceptor.cs`
- Delete: `src/Infrastructure/Persistence/EntityFramework/Interceptors/DomainEvent.Interceptor.Logger.cs`
- Modify: `tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/Interceptors/Interceptor.Test.cs`
- Test: `tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/Outbox/Outbox.Interceptor.Test.cs`
- Test: `tests/Integration/Infrastructure.Integration.Tests/Messaging/Outbox.Atomicity.Integration.Test.cs`

**Interfaces:**
- Consumes: Task 1 contracts; Task 2 serializer; Task 3 store.
- Produces: `OutboxInterceptor` (stages rows in `SavingChanges`, clears in `SavedChanges`, resets in `SaveChangesFailed`), `OutboxIntegrationEventPublisher`, `NoOpEventBus`, new `InterceptorRegistry.Apply(IDomainEventSerializer, …)`.

- [ ] **Step 1: Write the failing interceptor test**

Use a small `OutboxTestContext` with `DbSet<EventAggregate>` and `DbSet<OutboxMessage>`, `OutboxMessageConfiguration`, and the real `JsonMessageSerializer`.

Cover:

- one aggregate event stages exactly one domain outbox row with the right type/version/payload/metadata;
- a second save in the same context does not duplicate the row;
- a successful save clears the aggregate's events;
- a failed save leaves the events on the aggregate and clears the interceptor's staged state;
- the interceptor never calls a publisher.

- [ ] **Step 2: Implement `OutboxInterceptor`**

Follow spec §5:

- `SavingChanges`/`SavingChangesAsync`: collect `IAggregate<IDomainEvent>` entries, skip ids already staged, serialize to `OutboxMessage` rows, add them to `Set<OutboxMessage>()`, record aggregate/event ids.
- `SavedChanges`/`SavedChangesAsync`: for each staged aggregate whose events are all staged, call `RemoveAllEvents()`, then reset staging state.
- `SaveChangesFailed`/`SaveChangesFailedAsync`: reset staging state only.
- Log staging/failure through a source-generated logger.

- [ ] **Step 3: Register the serializer and replace the interceptor**

- `EntityFramework.Composition` registers `EventTypeRegistry` (Core assembly), `JsonMessageSerializer`, and both serializer interfaces with `TryAddSingleton`.
- `InterceptorRegistry.Apply` takes `IDomainEventSerializer` and registers `OutboxInterceptor` instead of `DomainEventInterceptor`.
- Both `AddDbContext` call sites pass `provider.GetRequiredService<IDomainEventSerializer>()` and `provider.GetService<ILogger<OutboxInterceptor>>()`.
- Delete `DomainEventInterceptor` and its logger.
- Rewrite the old domain-event interceptor tests in `Interceptor.Test.cs` as outbox assertions; update the `Apply` registration tests to pass a serializer and assert `OutboxInterceptor` is registered.

- [ ] **Step 4: Write `OutboxIntegrationEventPublisher` and `NoOpEventBus`**

`OutboxIntegrationEventPublisher` serializes the event, creates an `OutboxMessage` with `Kind = Integration`, stages it through `IOutboxStore.EnqueueAsync`, then calls `IUnitOfWork.SaveChangesAsync`. `NoOpEventBus` returns success and logs at debug.

- [ ] **Step 5: Write the failing atomicity integration test**

Cover:

- a repository save of a `Todo` with a raised event writes the Todo row and one outbox row in one `SaveChanges`;
- a forced save failure leaves neither the row nor an outbox row;
- the aggregate still exposes its event after the failed save.

- [ ] **Step 6: Run the tests**

```bash
dotnet build tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release --no-build --filter-class "*OutboxInterceptorTest*"
dotnet build tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release
dotnet test --project tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release --no-build --filter-class "*OutboxAtomicityIntegrationTest*"
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add \
  src/Infrastructure/Persistence/EntityFramework/Outbox \
  src/Infrastructure/Persistence/EntityFramework/Interceptors \
  src/Infrastructure/Events/NoOpEventBus.cs \
  src/Infrastructure/Persistence/EntityFramework/EntityFramework.Composition.cs \
  tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework \
  tests/Integration/Infrastructure.Integration.Tests/Messaging
git commit -m "feat(infra): stage aggregate events in the outbox"
```

---

### Task 6: Dispatcher, Worker, Settings and Health

**Files:**
- Create: `src/Infrastructure/Persistence/EntityFramework/Outbox/Options/OutboxSetting.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/Outbox/Options/OutboxSetting.Constant.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/Outbox/Options/OutboxSetting.Validator.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/Outbox/Outbox.Dispatcher.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/Outbox/Outbox.Dispatcher.Logger.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/Outbox/Outbox.Worker.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/Outbox/Outbox.HealthCheck.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/Outbox/Outbox.Composition.cs`
- Modify: `src/Infrastructure/Infrastructure.Composition.cs`
- Modify: `app/Web.Api/appsettings.json`
- Modify: `tests/Testing/Fixtures/ShopApi.Factory.cs`
- Test: `tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/Outbox/Outbox.Dispatcher.Test.cs`

**Interfaces:**
- Consumes: Task 1 `IOutboxDispatcher`; Task 2 serializers; Task 3 store; `IDomainEventPublisher`; `IEventBus`.
- Produces: `OutboxDispatcher`, `OutboxWorker`, `OutboxSetting`, `OutboxHealthCheck`, `AddOutbox()`.

- [ ] **Step 1: Write the failing dispatcher test**

Use a fake `IOutboxStore`, fake serializers, a fake `IDomainEventPublisher`, a fake `IEventBus`, a fake `TimeProvider`, and the real `OutboxSetting`.

Cover:

- domain rows are deserialized and published through `IDomainEventPublisher`, then marked processed;
- integration rows go to `IEventBus`, then marked processed;
- a publish failure increments attempts and sets the next `AvailableOnUtc` from the backoff;
- a failure at `MaxAttempts` dead-letters;
- a deserialization failure marks failed without crashing the batch;
- an empty batch returns success with count 0.

- [ ] **Step 2: Implement settings**

`OutboxSetting` fields and defaults exactly as spec §9, plus `MaxAgeSeconds = 300`. Validator rejects non-positive batch/interval/lease/attempts and negative backoff. `OutboxSettingConstant` holds the default values and `SectionName = "Outbox"`.

- [ ] **Step 3: Implement `OutboxDispatcher`**

Follow spec §6 exactly:

- claim once per batch with `now`, `lease`;
- publish domain via `IDomainEventPublisher.PublishAsync([evt])`;
- publish integration via `IEventBus.PublishAsync(msg)`;
- on success `MarkProcessedAsync(row.Id, row.LeaseId!.Value, now)`;
- on failure `attempts = row.Attempts + 1`; dead-letter at `>= MaxAttempts` with `row.LeaseId!.Value`, otherwise `MarkFailedAsync(row.Id, row.LeaseId!.Value, error, now + min(MaxBackoff, Backoff * 2^(attempts-1)))`;
- a `Outbox.Lease.Lost` result means another worker owns the row; stop handling that row and count it as handled;
- return the number of rows handled.

- [ ] **Step 4: Implement `OutboxWorker`, health check and composition**

`OutboxWorker` is a `BackgroundService` that exits when disabled and otherwise creates a scope per tick and calls `DispatchBatchAsync`. `OutboxHealthCheck` reports `Degraded` when the oldest pending row is older than `MaxAgeSeconds`, otherwise `Healthy`; disabled is `Healthy`.

`Outbox.Composition.AddOutbox(builder, configuration)`:

- bind/validate `OutboxSetting` with `ValidateFluentValidation().ValidateOnStart()`;
- register the setting singleton, `IOutboxStore`, `IInboxStore`, `IOutboxDispatcher`, `IIntegrationEventPublisher`, `IEventBus` (no-op), `OutboxWorker`, and the `outbox` health check tagged `ready`.

Call `builder.AddOutbox(builder.Configuration)` in `Infrastructure.Composition.AddInfrastructure` after `AddServices()` and before `AddIntegration()`.

- [ ] **Step 5: Add configuration and test-host defaults**

Add the `Outbox` section from spec §9 to `app/Web.Api/appsettings.json`.

In `ShopApiFactory.CreateHost`, set `Outbox__Enabled=false` alongside the connection strings so the fast API host does not run a poller against a closed database. Production/AppHost keeps `true`.

- [ ] **Step 6: Run the tests**

```bash
dotnet build tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release --no-build --filter-class "*OutboxDispatcherTest*"
dotnet build QingFa.Shop.slnx -c Release
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add \
  src/Infrastructure/Persistence/EntityFramework/Outbox \
  src/Infrastructure/Infrastructure.Composition.cs \
  app/Web.Api/appsettings.json \
  tests/Testing/Fixtures/ShopApi.Factory.cs \
  tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/Outbox
git commit -m "feat(infra): dispatch outbox messages with retry and health"
```

---

### Task 7: Transaction Behavior

**Files:**
- Create: `src/Application/Common/Mediators/Behaviors/Transactions/Transaction.Behavior.cs`
- Modify: `src/Application/Common/Mediators/Mediator.Composition.cs`
- Test: `tests/Unit/Application.Unit.Tests/Common/Mediators/Behaviors/Transactions/Transaction.Behavior.Test.cs`

**Interfaces:**
- Consumes: `IUnitOfWork`, `ITransaction`, `ICommand`, `ICommandBase<T>`, `IQuery<T>`, `MediatorResult.Failure<T>`.
- Produces: `TransactionBehavior<TRequest,TResponse>` registered between validation and exception mapping.

- [ ] **Step 1: Write the failing behavior test**

Use a `FakeTransaction` implementing `ITransaction` (records commit/rollback/dispose) and a `Mock<IUnitOfWork>`.

Cover:

- a command success commits and returns the response;
- a failed `Result` rolls back and returns the same errors;
- a query never begins a transaction;
- an exception rolls back and rethrows;
- a begin failure returns a failed result without calling `next`;
- a commit failure returns a failed result.

Define `TestCommand : ICommand<Result<string>>` and `TestQuery : IQuery<string>` in the test file.

- [ ] **Step 2: Run the test and verify it fails**

```bash
dotnet build tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release
```

Expected: FAIL with `CS0246` for `TransactionBehavior`.

- [ ] **Step 3: Implement `TransactionBehavior`**

Follow spec §8 exactly. Command detection: `request is ICommand` or any interface is `ICommandBase<>`. Use `MediatorResult.Failure<TResponse>` for begin/commit failures. Always dispose the transaction.

- [ ] **Step 4: Register the behavior**

In `Mediator.Composition`, add after `ValidationBehavior` and before `ExceptionMappingBehavior`:

```csharp
cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
```

- [ ] **Step 5: Run the tests**

```bash
dotnet build tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --no-build --filter-class "*TransactionBehaviorTest*"
dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --no-build --filter-class "*Mediator*"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add \
  src/Application/Common/Mediators/Behaviors/Transactions \
  src/Application/Common/Mediators/Mediator.Composition.cs \
  tests/Unit/Application.Unit.Tests/Common/Mediators/Behaviors/Transactions
git commit -m "feat(app): wrap commands in a transaction behavior"
```

---

### Task 8: End-to-End Flow and Full Gates

**Files:**
- Create: `tests/Integration/Infrastructure.Integration.Tests/Messaging/Outbox.Flow.Integration.Test.cs`
- Modify: `docs/specs/2026-10-29-reliable-events-design.md` only if implementation diverged.
- No production files unless a gate fails.

**Interfaces:**
- Consumes: all prior tasks.
- Produces: a green slice baseline for slice ③.

- [ ] **Step 1: Write the end-to-end integration test**

Build a `ServiceCollection` with:

- the real `ApplicationDbContext` against the messaging Postgres fixture;
- `IUnitOfWork`, `EfCoreOutboxStore`, `EfCoreInboxStore`;
- the real serializers and `OutboxInterceptor`;
- `IDomainEventPublisher` backed by a recording publisher;
- `IEventBus` backed by a recording bus;
- `OutboxSetting` with a small batch.

Test:

1. create a `Todo` through `SaveNewAsync` with a raised `TodoCreated`;
2. assert the Todo row and exactly one outbox row exist;
3. run `OutboxDispatcher.DispatchBatchAsync`;
4. assert the recording publisher saw `TodoCreated` and the row is processed;
5. enqueue an integration event through `OutboxIntegrationEventPublisher`;
6. dispatch and assert the recording bus saw it;
7. deliver the same integration event through `EfCoreInboxStore` twice and assert the consumer ran once.

- [ ] **Step 2: Run the flow test**

```bash
dotnet build tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release
dotnet test --project tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release --no-build --filter-class "*OutboxFlowIntegrationTest*"
```

Expected: PASS.

- [ ] **Step 3: Run the full gates**

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

- [ ] **Step 4: Commit gate fixes only if needed**

```bash
git add <only the files changed by the fix>
git commit -m "fix: close reliable-events gate findings"
```

If no files changed, do not create an empty commit.

---

## Self-Review

**Spec coverage**

| Spec section | Task |
|---|---|
| Outbox model, store, dispatcher contracts | Task 1 |
| Serialization and type resolution | Task 2 |
| Outbox table, store, schema, migration | Task 3 |
| Inbox table and store | Task 4 |
| Interceptor + integration publisher + atomicity | Task 5 |
| Dispatcher, worker, settings, health | Task 6 |
| TransactionBehavior | Task 7 |
| End-to-end flow + gates | Task 8 |

**Placeholder scan:** no `TBD`, `TODO`, or “similar to Task N”.

**Type consistency:** `OutboxMessage`, `OutboxMessageKind`, `OutboxMessageStatus`,
`IOutboxStore`, `IOutboxDispatcher`, `InboxMessage`, `IInboxStore`, `IIntegrationEvent`,
`IEventBus`, `IIntegrationEventPublisher`, `IDomainEventSerializer`,
`IIntegrationEventSerializer`, `IEventTypeRegistry` are defined once in Task 1 and used
with the same names in every later task.

## Execution Handoff

Plan complete and saved to `docs/plans/2026-10-30-reliable-events.md`. Execute with
subagent-driven development: fresh implementer per task, task review after each, final
whole-branch review.
