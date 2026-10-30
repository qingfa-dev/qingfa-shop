# Slice ④ — Event Sourcing (PostgreSQL, Pilot, Todo Migration)

- **Status:** Approved
- **Date:** 2026-10-29
- **Depends on:** slices ①–③
- **Final slice**

> **Error handling:** follow `docs/specs/2026-10-30-monadic-error-handling-design.md`:
> flow-level failures return `Result`; throws are boundary-only and documented.

## 1. Goal

Add a provider-neutral event store with rehydration, snapshots, projections and
upcasting, prove it on a new aggregate, then migrate `Todo` to event sourcing without
changing the public API.

Deliverables:

1. Event-store, snapshot, serializer, upcaster and projection contracts.
2. PostgreSQL implementation and schema.
3. `TodoReminder` pilot aggregate, commands, queries, endpoints and read model.
4. `Todo` migration to an event-sourced aggregate with a relational projection.
5. Projection runner, rebuild and backfill.
6. Unit and integration coverage.

## 2. Non-goals

- No Marten, EventStoreDB or Kurrent.
- No change to public Todo API contracts.
- No distributed saga.
- No multi-tenant event-store isolation beyond the metadata envelope and stream naming.

## 3. Contracts

### Location

`src/Application/Common/EventSourcing`

### `EventStore.Interface.cs`

```csharp
public interface IEventStore
{
    Task<Result<long>> AppendAsync(
        string streamId,
        long expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<StoredEvent>>> ReadStreamAsync(
        string streamId,
        long fromVersion,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<StoredEvent>>> ReadAllAsync(
        long fromPosition,
        int batchSize,
        CancellationToken cancellationToken = default);
}
```

`AppendAsync` returns the new stream version.

### `StoredEvent.Model.cs`

```csharp
public sealed record StoredEvent(
    string StreamId,
    long Version,
    Guid EventId,
    string EventName,
    int EventVersion,
    string Payload,
    DefaultMetadata Metadata,
    DateTimeOffset OccurredOnUtc,
    long Position);
```

### `Snapshot.Store.Interface.cs`

```csharp
public interface ISnapshotStore
{
    Task<Result<Snapshot?>> LoadAsync(
        string streamId,
        CancellationToken cancellationToken = default);

    Task<Result> SaveAsync(
        Snapshot snapshot,
        CancellationToken cancellationToken = default);
}
```

`Snapshot.Model.cs`: `StreamId`, `Version`, `State`, `TakenOnUtc`.

`Snapshot.Policy.cs`:

```csharp
public interface ISnapshotPolicy
{
    bool ShouldSnapshot(long currentVersion, long eventsSinceSnapshot);
}
```

Default `IntervalSnapshotPolicy` uses `EventStore:SnapshotInterval` (default 50).

### Serializer

Reuse `IDomainEventSerializer` from slice ②. Its contract is:

```csharp
string Serialize(IDomainEvent domainEvent);
IDomainEvent Deserialize(string eventName, int eventVersion, string payload);
```

- One implementation, `JsonEventStoreSerializer`, shared with the outbox serializer.
- Uses the same `JsonSerializerOptions` as slice ②.
- Resolution:
  1. Run the upcaster chain until the payload reaches the current version.
  2. Resolve `(EventName, EventVersion)` through `IEventTypeRegistry`.
  3. Deserialize into the concrete CLR type.
- Unknown `(name, version)` is a failure, never a silently skipped event.

`EventTypeRegistry.Interface.cs`:

```csharp
public interface IEventTypeRegistry
{
    Type Resolve(string eventName, int eventVersion);
}
```

The registry is populated from explicit registrations in composition, not by scanning
every type in the process.

### `EventUpcaster.Interface.cs`

```csharp
public interface IEventUpcaster
{
    bool CanUpcast(string eventName, int fromVersion);
    string Upcast(string eventName, int fromVersion, string payload);
}
```

- Upcasters are registered in order; each transforms version `N` JSON to `N + 1`.
- A gap in the chain fails deserialization with `EventSourcing.Upcast.Missing`.

### `Projection.Interface.cs`

```csharp
public interface IProjection
{
    string Name { get; }

    Task<Result> ApplyAsync(
        StoredEvent storedEvent,
        CancellationToken cancellationToken = default);
}
```

`ProjectionCheckpoint.Store.Interface.cs`:

```csharp
public interface IProjectionCheckpointStore
{
    Task<Result<long>> GetPositionAsync(
        string projection,
        CancellationToken cancellationToken = default);

    Task<Result> SavePositionAsync(
        string projection,
        long position,
        CancellationToken cancellationToken = default);
}
```

### `EventSourcedRepository.Interface.cs`

`src/Application/Common/EventSourcing`

```csharp
public interface IEventSourcedRepository<TAggregate, in TKey>
    where TAggregate : class, IEventSourcedAggregate<TKey>
{
    Task<Result<TAggregate>> LoadAsync(
        TKey id,
        CancellationToken cancellationToken = default);

    Task<Result> SaveAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default);
}
```

### Stream naming

`EventStreamName.Factory.cs`:

```csharp
public static string For<TAggregate>(Guid id);
```

- `TodoReminder` → `todo-reminder:{id}`
- `Todo` → `todo:{id}`
- Names are lower-case and stable. Changing a name requires a migration, not a rename.

### Outcomes

`EventSourcing.Outcome.cs` stable codes:

| Code | Meaning |
|---|---|
| `EventStore.Append.Conflict` | Expected version does not match |
| `EventStore.Append.Failed` | Provider rejected the append |
| `EventStore.Read.Failed` | Stream or global read failed |
| `EventStore.Serialize.Failed` | Event could not be serialized |
| `EventStore.Deserialize.Failed` | Event could not be deserialized |
| `EventStore.Upcast.Missing` | No upcaster bridges the stored version |
| `Snapshot.Read.Failed` / `Snapshot.Write.Failed` | Snapshot failure |
| `Projection.Apply.Failed` | Projection rejected an event |
| `Projection.Checkpoint.Failed` | Checkpoint write failed |
| `Repository.Stream.NotFound` | Aggregate does not exist |

## 4. PostgreSQL schema

New schema `messaging` (added to `PersistenceConstant.Schema.Names`).

### `EventStoreEvents`

| Column | Type | Notes |
|---|---|---|
| `Position` | bigserial | global order, unique |
| `StreamId` | text | |
| `Version` | bigint | |
| `EventId` | uuid | unique |
| `EventName` | text | stable wire name |
| `EventVersion` | int | |
| `Payload` | jsonb | |
| `Metadata` | jsonb | |
| `OccurredOnUtc` | timestamptz | |

Constraints and indexes:

- PK `(StreamId, Version)`.
- Unique `EventId`.
- Index `(Position)` for global reads.
- Index `(EventName)` for diagnostics.

### `EventStoreSnapshots`

| Column | Type | Notes |
|---|---|---|
| `StreamId` | text | PK |
| `Version` | bigint | |
| `State` | jsonb | |
| `TakenOnUtc` | timestamptz | |

### `ProjectionCheckpoints`

| Column | Type | Notes |
|---|---|---|
| `Projection` | text | PK |
| `Position` | bigint | |
| `UpdatedOnUtc` | timestamptz | |

### Migration

`AddEventStore` under `src/Infrastructure/Persistence/Migrations`.

Provider abstraction: `EventStoreSetting.Provider` defaults to `Postgres`. The
composition switch rejects unknown providers. Contracts contain no Npgsql types.

## 5. Aggregate and repository behavior

### Rehydration

`EventSourcedRepository.LoadAsync`:

1. Load the latest snapshot.
2. If present, create the aggregate through `IAggregateFactory<TAggregate>`, restore the
   snapshot state, and read the stream from `snapshot.Version + 1`.
3. Otherwise create the aggregate and read from version 1.
4. `LoadFromHistory(lastVersion, events)`.
5. Return the aggregate; no uncommitted events.

### Save

`EventSourcedRepository.SaveAsync`:

1. Reject an aggregate with no uncommitted events (success no-op).
2. Compute `expectedVersion = aggregate.Version - uncommitted.Count`.
3. Append to the event store with that expected version.
4. Stage one outbox row per event (slice ②), so in-process and integration handlers still
   see them.
5. `IUnitOfWork.SaveChangesAsync` — event rows and outbox rows commit together.
6. Clear uncommitted events only after the commit succeeds.
7. If `ISnapshotPolicy.ShouldSnapshot(...)`, capture and save a snapshot in the same
   transaction.

### Concurrency

- Two writers at the same version: one append wins, the other receives
  `EventStore.Append.Conflict`.
- The conflict maps to `UnitOfWorkOutcome.Failure.ConcurrencyConflict` at the handler
  boundary.
- Stream version is the event count, while the row `Version` is an optimistic concurrency
  token; they are different concepts, and `TodoReadModel` must not inherit `IVersionable`
  concurrency semantics.

### Snapshot state

- Aggregates implement `ISnapshotable`:

  ```csharp
  string CaptureSnapshot();
  void RestoreSnapshot(string state);
  ```

- Snapshot DTOs are versioned with the same `EventVersion` discipline.
- A snapshot restore failure falls back to a full replay and logs the fallback; it never
  corrupts state silently.

## 6. Projection runner

### Location

`src/Infrastructure/Persistence/EntityFramework/Projections/Projection.Worker.cs`

### Behavior

1. For each registered `IProjection`, read its checkpoint.
2. `ReadAllAsync(checkpoint + 1, batchSize)`.
3. Apply events in global position order.
4. Save the checkpoint after the batch commits.
5. On failure: stop the batch, keep the checkpoint, log, and retry next interval.
6. Projections are idempotent: an upsert keyed by stream id with a version guard
   (`incoming version >= stored version`).

### Rebuild

`IRebuildProjectionService`:

- deletes the projection’s read model rows and its checkpoint,
- resets the position to 0,
- lets the worker replay the full log.

Exposed through:
- `EventStore:RebuildProjectionsOnStartup` (`string[]` of projection names), and
- an internal admin-only endpoint `POST /api/event-store/projections/{name}/rebuild`
  guarded by `PermissionContext.Identity.Permissions.Manage`.

## 7. Pilot — `TodoReminder`

### Domain

`src/Core/Todos/Reminders`

```text
TodoReminder : EventSourcedAggregate<Guid>, ISnapshotable
  Id, TodoId, TenantId, RemindAtUtc, Status, SnoozeCount, Version
```

Events:

| Event | Payload |
|---|---|
| `ReminderScheduled` | ReminderId, TodoId, TenantId, RemindAtUtc |
| `ReminderSnoozed` | ReminderId, RemindAtUtc, SnoozeCount |
| `ReminderDismissed` | ReminderId, DismissedAtUtc |

Rules:

- `RemindAtUtc` must be in the future relative to the supplied `nowUtc`.
- Snooze increments `SnoozeCount` and must move the reminder forward.
- Dismiss is terminal; snoozing or dismissing twice is a failure.
- A reminder cannot be scheduled for a completed todo — enforced by
  `ReminderSchedulingPolicy` (slice ①).

### Application

`src/Application/Features/Todos/Reminders`

- `Commands/ScheduleTodoReminder.cs`
- `Commands/SnoozeTodoReminder.cs`
- `Commands/DismissTodoReminder.cs`
- `Queries/GetTodoReminder.cs`
- `Queries/SearchTodoReminders.cs`
- `Models/TodoReminder.Model.cs`
- `Mappers/TodoReminder.Mapper.cs`

Commands use `IEventSourcedRepository<TodoReminder, Guid>`, `ITenantContext`,
`IUserContext` and `TimeProvider`.

### Read model and projection

- `TodoReminderReadModel` in the `todo` schema:
  `Id`, `TodoId`, `TenantId`, `RemindAtUtc`, `Status`, `SnoozeCount`, `Version`.
- `TodoReminder.Projection.cs` writes the read model from the three events.
- Queries read only the read model.

### API

New Carter module `app/Web.Api/Endpoints/Todos/TodoReminder.Endpoint.cs`, registered in
`Program.cs`:

| Method | Route |
|---|---|
| `POST` | `/api/todos/{todoId:guid}/reminders` |
| `PATCH` | `/api/todos/{todoId:guid}/reminders/{reminderId:guid}/snooze` |
| `DELETE` | `/api/todos/{todoId:guid}/reminders/{reminderId:guid}` |
| `GET` | `/api/todos/{todoId:guid}/reminders` |

All routes require an authenticated user and a tenant.

## 8. `Todo` migration (wave 2)

Wave 2 starts only after wave 1 is green.

### Target

`Todo` becomes:

```csharp
public sealed class Todo : EventSourcedAggregate<Guid>, ITodo, ISnapshotable
```

### Events

Replace the current thin events with self-contained events:

| Event | Carries |
|---|---|
| `TodoCreated` | Id, TenantId, Title, Description, Priority, DueAtUtc, ListId, CreatedAtUtc, CreatedBy |
| `TodoUpdated` | Title, Description, Priority, DueAtUtc, ListId, ModifiedAtUtc, ModifiedBy |
| `TodoStatusChanged` | From, To, OccurredAtUtc (start, complete, cancel, reopen) |
| `TodoCategoryAdded` / `TodoCategoryRemoved` | CategoryId |
| `TodoListChanged` | ListId |
| `TodoDeleted` / `TodoRestored` | OccurredAtUtc, Actor |

The old `TodoStarted` / `TodoCompleted` / `TodoCancelled` / `TodoReopened` payloads are
replaced by `TodoStatusChanged`; any external consumer must be migrated in the same
change.

### Read model

- `Todo` stops being an EF entity.
- New `TodoReadModel` maps to the existing `Todos` table and columns.
- Query handlers and projections use `TodoReadModel`.
- `TodoResponseModel` and all API contracts stay unchanged.

### Write path

- Commands load through `IEventSourcedRepository<Todo, Guid>`.
- Aggregate methods validate, then `Raise` one event; `When` mutates state.
- `SaveAsync` appends and stages outbox rows.

### Backfill

`TodoBackfill.Seeder.cs`, enabled by `EventStore:BackfillTodos`:

1. For every `Todos` row without a `todo:{id}` stream, append a `TodoCreated` event that
   captures the current state, then a synthetic `TodoUpdated`/`TodoStatusChanged` only
   when needed to reach the current status.
2. Save a snapshot at the resulting version.
3. Record the stream id in a `ProjectionCheckpoints` style marker (`TodoBackfill`) so a
   restart does not duplicate streams.
4. Run in a transaction per batch; batch size configurable.

The backfill is idempotent and can be re-run.

### Migration order

1. Apply `AddEventStore` and read-model migrations.
2. Run backfill with the app offline.
3. Deploy the event-sourced write path.
4. Rebuild the `Todos` projection from the event log and compare row counts/checksums.
5. Remove the old state-based write code and its tests.

### Rollback

- Keep a database backup before the backfill.
- The `Todos` table is untouched by the event store; rolling back the app restores the
  state-based path.
- Event streams remain; a later retry reuses them.

### Release note

- Deploy all instances together when the concurrency token is introduced, because old
  instances do not bump it.

## 9. Configuration

```json
{
  "EventStore": {
    "Enabled": true,
    "Provider": "Postgres",
    "BatchSize": 500,
    "SnapshotInterval": 50,
    "PollingIntervalSeconds": 2,
    "BackfillTodos": false,
    "RebuildProjectionsOnStartup": []
  }
}
```

- `EventStoreSetting` in `Infrastructure.Persistence.EntityFramework.EventSourcing.Options`.
- Validator rejects unknown providers, non-positive intervals and snapshot counts below 1.
- `Enabled=false` keeps the pilot endpoints unavailable and the Todo path state-based
  (only valid before wave 2 ships).

## 10. Tests

### Unit

`tests/Unit/Core.Unit.Tests/Todos/Reminders/`

- schedule/snooze/dismiss transition rules and terminal-state guards.
- snapshot capture/restore round-trip.
- rehydration produces the same state as direct construction.

`tests/Unit/Application.Unit.Tests/Common/EventSourcing/`

- stream name factory.
- repository save rejects an empty uncommitted list.
- expected version is computed from aggregate version minus uncommitted count.
- projection checkpoint advances only after success.

`tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/EventSourcing/`

- serializer round-trip for every event.
- upcaster chain applies in order and fails on a missing bridge.
- unknown event name fails with the right code.
- snapshot policy triggers at the configured interval.

### Integration (PostgreSQL)

`tests/Integration/Infrastructure.Integration.Tests/EventSourcing/`

- append then read a stream in order.
- expected-version conflict on concurrent append.
- global `ReadAll` returns stable position order.
- snapshot + tail rehydration equals full replay.
- projection worker builds the reminder read model.
- projection rebuild produces identical rows.
- upcaster reads a v1 payload through the v2 CLR type.
- transaction rollback leaves neither events nor outbox rows.
- Todo backfill is idempotent and produces a stream per existing row.

### API

`tests/Integration/Api.Integration.Tests/`

- schedule → snooze → dismiss a reminder, then query the read model.
- scheduling a reminder for a completed todo fails with the policy code.
- after the Todo migration, every existing Todo API test still passes and a freshly
  replayed projection returns the same data.

## 11. Acceptance criteria

- `make ci.full` passes.
- Event store append is atomic with outbox staging.
- A stale aggregate append returns a concurrency conflict, never a duplicate version.
- Snapshot rehydration reads only the tail.
- A projection can be deleted and rebuilt from the log with identical results.
- `Todo` is fully event-sourced; its table is a projection.
- No public Todo API contract changed.
- No Npgsql type appears in `Core` or `Application`.
