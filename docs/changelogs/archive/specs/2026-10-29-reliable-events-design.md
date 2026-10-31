# Slice ② — Reliable Events (Outbox, Inbox, Transaction)

- **Status:** Approved
- **Date:** 2026-10-29
- **Depends on:** slice ①
- **Unblocks:** slices ③ and ④

> **Error handling:** follow `docs/specs/2026-10-30-monadic-error-handling-design.md`:
> flow-level failures return `Result`; throws are boundary-only and documented.

## 1. Goal

Make domain-event delivery durable and make command handlers atomic.

Deliverables:

1. Outbox table and store; aggregate events are persisted in the same transaction as state.
2. Decoupled outbox dispatcher with retry, lease and dead-letter.
3. Inbox table and `ExecuteOnceAsync` dedupe contract.
4. `TransactionBehavior` for commands.
5. Outbox/inbox health, retention and configuration.

## 2. Non-goals

- No broker, integration-event handler or Wolverine wiring; that is slice ③.
- No event store; that is slice ④.
- No exactly-once delivery.

## 3. Outbox model

### Location

`src/Application/Common/Messaging/Outbox`

### `OutboxMessage.Model.cs`

```csharp
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

- `OutboxMessageKind`: `Domain = 1`, `Integration = 2`.
- `OutboxMessageStatus`: `Pending = 1`, `Processed = 2`, `DeadLettered = 3`.
- `Type` is the stable event name for integration events and the assembly-qualified name
  for domain events. `Version` is the event version carried by the payload.
- `Metadata` is the serialized `DefaultMetadata` envelope.

### `Outbox.Store.Interface.cs`

```csharp
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

### `Outbox.Dispatcher.Interface.cs`

```csharp
public interface IOutboxDispatcher
{
    Task<Result<int>> DispatchBatchAsync(CancellationToken cancellationToken = default);
}
```

Returns the number of messages processed in the batch. This is the test seam; the
worker only calls it on a timer.

### `Outbox.Outcome.cs`

Stable codes:

| Code | Meaning |
|---|---|
| `Outbox.Enqueue.Failed` | Rows could not be staged |
| `Outbox.Claim.Failed` | A batch could not be claimed |
| `Outbox.Serialize.Failed` | Payload could not be serialized |
| `Outbox.Deserialize.Failed` | Payload could not be deserialized |
| `Outbox.Publish.Failed` | A handler or the bus rejected the message |
| `Outbox.Lease.Lost` | The claim lease expired before the row could be marked |
| `Outbox.DeadLetter.Failed` | Dead-letter transition failed |
| `Outbox.Batch.Empty` | No work; success, not a failure |

## 4. Serialization

### Contracts

`src/Application/Common/Messaging/Serialization/`

```csharp
public interface IDomainEventSerializer
{
    string Serialize(IDomainEvent domainEvent);
    IDomainEvent Deserialize(string eventName, int eventVersion, string payload);
}

public interface IIntegrationEventSerializer
{
    string Serialize(IIntegrationEvent integrationEvent);
    IIntegrationEvent Deserialize(string eventName, int eventVersion, string payload);
}
```

- Implementation: `JsonMessageSerializer` in `Infrastructure.Integrations.Messaging.Serialization`,
  shared `JsonSerializerOptions`:
  - web defaults (camelCase), enums as strings
  - `DefaultMetadata` written as an object map, not as a dictionary wrapper
  - no type information embedded; the `Type` column owns resolution
- Domain type resolution uses a registry built from assemblies that contain domain events.
  Unknown types fail with `Outbox.Deserialize.Failed`, never throw for a missing type at
  startup.
- Integration type resolution uses the stable `EventName` exposed by `IIntegrationEvent`.

`IIntegrationEvent` is introduced here because the outbox stores integration messages:

```csharp
public interface IIntegrationEvent
{
    Guid MessageId { get; }
    DateTimeOffset OccurredOnUtc { get; }
    string EventName { get; }
    int Version { get; }
}
```

`IIntegrationEventPublisher` stages an integration event into the outbox:

```csharp
public interface IIntegrationEventPublisher
{
    Task<Result> PublishAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default);
}
```

## 5. Outbox interceptor

### Location

`src/Infrastructure/Persistence/EntityFramework/Outbox/Outbox.Interceptor.cs`

Replaces `DomainEventInterceptor`. `OutboxInterceptor : SaveChangesInterceptor`.

### Behavior

`SavingChanges` / `SavingChangesAsync`:

1. Enumerate `context.ChangeTracker.Entries<IAggregate<IDomainEvent>>()`.
2. Collect events whose id is not already staged by this interceptor instance.
3. Serialize each event and stage one `OutboxMessage` row through
   `context.Set<OutboxMessage>().AddRange(...)`.
4. Record the staged event ids in an instance field so a second save in the same
   `DbContext` does not duplicate them.

`SavedChanges` / `SavedChangesAsync`:

1. Clear only the event ids recorded for this context.
2. Do not clear events that were never staged by this interceptor.

### Rules

- The interceptor is created per `DbContext`, not registered as a singleton. It is
  stateful only for the lifetime of that context.
- A failed save never reaches `SavedChanges`, so events stay on the aggregate and no
  outbox row remains (the transaction rolled back).
- The interceptor never publishes. Publishing is the dispatcher’s job.
- A save with no aggregate events is a no-op.

### Registration

`EntityFramework.Composition` registers the interceptor instead of
`DomainEventInterceptor`. `InterceptorRegistry.Apply` takes an `IOutboxStager` (a small
adapter around the serializer) and the logger.

## 6. Dispatcher

### Location

`src/Infrastructure/Persistence/EntityFramework/Outbox/Outbox.Dispatcher.cs`

### Algorithm

```text
loop over claimed batch:
  read clock once per batch
  claim rows:
    WHERE Status = Pending
      AND AvailableOnUtc <= now
      AND (LockedUntilUtc IS NULL OR LockedUntilUtc < now)
    ORDER BY OccurredOnUtc
    LIMIT @batchSize
    FOR UPDATE SKIP LOCKED
    then set LockedUntilUtc = now + lease
  for each row:
    if Kind = Domain:
      event = domainSerializer.Deserialize(Type, Payload)
      publisher.Publish((object)event)        // MediatR, runtime type
    else:
      message = integrationSerializer.Deserialize(Type, Payload)
      eventBus.PublishAsync(message)          // slice ③; a no-op adapter in slice ②
    on success: MarkProcessed(id, now)
    on failure:
      attempts += 1
      if attempts >= MaxAttempts: DeadLetter(id, error)
      else: MarkFailed(id, error, now + backoff(attempts))
```

- Claim and mark are separate transactions; publishing is outside the claim transaction.
  If the process dies after publish but before mark, the message is redelivered. That is
  the at-least-once contract.
- Every claim assigns a new `LeaseId`. A mark/dead-letter is conditional on the same
  `LeaseId` still being present and the lease still unexpired. A stale worker receives
  `Outbox.Lease.Lost` and must not mutate the row; another worker may republish it.
- Backoff: exponential from `BackoffSeconds`, capped at `MaxBackoffSeconds`.
- A batch failure never aborts the whole batch; each row is handled independently.
- Logging: one structured log per failure with message id, type, attempt and error code.

### Worker

`Outbox.Worker.cs` is a `BackgroundService`:

- `Enabled` false → exits immediately.
- Polls every `PollingIntervalSeconds`, calls `DispatchBatchAsync`.
- Catches and logs failures; never crashes the host.
- Exposes no timer dependency to tests.

### Host registration

Registered by `Infrastructure.Composition` (not by `Integration.Composition`, because the
outbox belongs to persistence).

## 7. Inbox

### Location

`src/Application/Common/Messaging/Inbox`

### `InboxMessage.Model.cs`

```csharp
public sealed class InboxMessage
{
    public Guid MessageId { get; init; }
    public string Consumer { get; init; } = string.Empty;
    public DateTimeOffset ProcessedOnUtc { get; init; }
}
```

Primary key `(MessageId, Consumer)`.

### `Inbox.Store.Interface.cs`

```csharp
public interface IInboxStore
{
    Task<Result> ExecuteOnceAsync(
        Guid messageId,
        string consumer,
        Func<CancellationToken, Task<Result>> handler,
        CancellationToken cancellationToken = default);
}
```

### Semantics

1. Open a transaction through `IUnitOfWork`.
2. If `(MessageId, Consumer)` exists → roll back/commit nothing and return success.
3. Run the handler.
4. On failure → roll back; no inbox row; the message is retried.
5. On success → insert the inbox row and commit.
6. On duplicate insertion caused by a concurrent consumer → treat as already processed
   and return success.

The contract guarantees that the inbox row is written only after the handler returns
success, in the same transaction as the handler's own writes. A duplicate call returns
success without invoking the handler. Failure results are never recorded as processed.

The handler’s own writes and the inbox row commit together. A crash after commit is safe:
redelivery is a no-op. A crash before commit is safe: the message is retried.

### Cleanup

`DeleteProcessedBeforeAsync(cutoff)` keeps the table bounded.

## 8. TransactionBehavior

### Location

`src/Application/Common/Mediators/Behaviors/Transactions/Transaction.Behavior.cs`

```csharp
public sealed class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult<Error>
```

### Behavior

- Applies only to commands (`ICommand` or `ICommand<>`); queries pass through untouched.
- Begins a transaction.
- Runs `next`.
- If the response is a failed `Result` → roll back and return the failure unchanged.
- If it is a success → commit. A commit failure returns a failed result built through
  `MediatorResult.Failure<TResponse>`.
- Any exception → roll back with `CancellationToken.None` and rethrow so the exception
  behavior maps it.
- Always disposes the transaction.

### Provider constraint

Do not enable EF Core's retrying execution strategy for the request context. Command and
inbox transactions are user-initiated, and EF rejects `SaveChanges` inside a user-initiated
transaction when `EnableRetryOnFailure` is active. Retries belong at the command or HTTP
boundary.

### Pipeline order

`Logging → Validation → Transaction → ExceptionMapping`

`ExceptionMapping` is innermost so the transaction sees a failed `Result` rather than an
exception for mapped failures; the transaction still rolls back for unmapped exceptions.

## 9. Configuration

New section `Outbox`:

```json
{
  "Outbox": {
    "Enabled": true,
    "BatchSize": 100,
    "PollingIntervalSeconds": 2,
    "MaxAttempts": 10,
    "BackoffSeconds": 5,
    "MaxBackoffSeconds": 300,
    "LeaseSeconds": 60,
    "RetentionDays": 7
  }
}
```

- `OutboxSetting` lives in `Infrastructure.Persistence.EntityFramework.Outbox.Options`.
- Validator enforces positive bounds; `ValidateOnStart`.
- AppHost keeps `Enabled=true`; migration-only hosts set `false`.

## 10. Persistence

Outbox and inbox tables live in a new `messaging` schema. Add `Messaging = "messaging"`
to `PersistenceConstant.Schema` and to `PersistenceConstant.Schema.Names` so reset and
initialization cover it.

### Table `OutboxMessages`

| Column | Type | Notes |
|---|---|---|
| `Id` | uuid | PK |
| `Kind` | int | enum |
| `Type` | text | event name / AQN |
| `Version` | int | event version |
| `Payload` | jsonb | serialized event |
| `Metadata` | jsonb | envelope |
| `Status` | int | pending/processed/dead |
| `OccurredOnUtc` | timestamptz | |
| `AvailableOnUtc` | timestamptz | |
| `LockedUntilUtc` | timestamptz | null |
| `LeaseId` | uuid | null |
| `LeaseId` | uuid | null |
| `ProcessedOnUtc` | timestamptz | null |
| `Attempts` | int | |
| `LastError` | text | null |
| `CorrelationId` | uuid | null |
| `CausationId` | uuid | null |
| `TenantId` | uuid | null |

Indexes:

- `(Status, AvailableOnUtc, OccurredOnUtc)` for claim.
- `(ProcessedOnUtc)` for retention.

### Table `InboxMessages`

| Column | Type | Notes |
|---|---|---|
| `MessageId` | uuid | PK part |
| `Consumer` | text | PK part |
| `ProcessedOnUtc` | timestamptz | |

Index `(ProcessedOnUtc)` for retention.

### Migration

`AddOutboxInbox` under `src/Infrastructure/Persistence/Migrations`.

## 11. Health and operations

- `OutboxHealthCheck`:
  - healthy when pending count is 0 or oldest pending is younger than `MaxAgeSeconds`.
  - degraded with count/age otherwise.
- Health check tags: `ready`.
- Logs include `OutboxId`, `OutboxKind`, `OutboxType`, `Attempts`.

## 12. Tests

### Unit

`tests/Unit/Application.Unit.Tests/Common/Mediators/Behaviors/Transactions/Transaction.Behavior.Test.cs`

- command success commits.
- command failure rolls back and preserves errors.
- query bypasses transactions.
- exception rolls back and rethrows.
- begin/commit failure returns a failed result.

`tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/Outbox/`

- interceptor stages one row per aggregate event and does not duplicate on a second save.
- failed save leaves events on the aggregate.
- dispatcher marks processed, retries with backoff, dead-letters at max attempts.
- domain rows go to MediatR; integration rows go to the bus port.
- deserialization failure marks the row failed instead of crashing the batch.

### Integration (PostgreSQL)

`tests/Integration/Infrastructure.Integration.Tests/Messaging/`

- a command that mutates state and raises an event writes state + outbox in one save.
- a forced save failure leaves neither state nor outbox rows.
- concurrent claim with two contexts returns disjoint batches (`SKIP LOCKED`).
- inbox duplicate delivery executes the handler exactly once.
- handler failure rolls back the inbox row and allows retry.
- retention deletes only processed rows.

### API

`tests/Integration/Api.Integration.Tests/`

- `CompleteTodo` writes an outbox row and, after a dispatcher pass, the in-process handler
  has run. The handler writes a marker row or log that the test can assert.

## 13. Acceptance criteria

- `make ci` passes.
- No domain event is dispatched directly from the interceptor.
- A failed command leaves no outbox row.
- A duplicate consumer message executes once.
- PostgreSQL concurrency/claim tests pass.
- Outbox lag is visible through health checks.
