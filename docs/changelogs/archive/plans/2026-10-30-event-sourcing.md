# Event Sourcing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a provider-neutral event store with rehydration, snapshots, projections and upcasting; prove it on `TodoReminder`; then migrate `Todo` without changing its public API.

**Architecture:** Application owns event-store, snapshot, projection and repository ports plus outcomes. Infrastructure implements them on PostgreSQL and composes the projection worker. `TodoReminder` is the pilot; the `Todo` migration is a gated second wave after the pilot is green.

**Tech Stack:** .NET 10, EF Core 10/PostgreSQL 16, `System.Text.Json`, MediatR, xUnit v3, AwesomeAssertions, Testcontainers.PostgreSql.

**Spec:** `docs/specs/2026-10-29-event-sourcing-design.md`

> **Error handling:** follow `docs/specs/2026-10-30-monadic-error-handling-design.md`.
> All new flow failures return `Result`; throws are boundary-only and carry `// Boundary:`.
> Rehydration uses `TryLoadFromHistory`, not the throwing wrapper.

## Global Constraints

- `net10.0`; nullable; analyzers; warnings as errors.
- `Core` has zero project references; `Application` references `Core` only.
- PostgreSQL first, behind provider-neutral Application contracts.
- Stream version and row version are different concepts; read models do not implement `IVersionable`.
- Event store append, outbox staging and state changes share one transaction.
- At-least-once projections; upserts must be idempotent.
- Dotted-role filenames; labeled comments; `// Boundary:` on remaining throws.
- Coverage floors 90/80; `make ci` green after each wave.

---

### Task 1: Event Store Contracts

**Files:**
- Create: `src/Application/Common/EventSourcing/EventStore.Interface.cs`
- Create: `src/Application/Common/EventSourcing/StoredEvent.Model.cs`
- Create: `src/Application/Common/EventSourcing/Snapshot.Model.cs`
- Create: `src/Application/Common/EventSourcing/Snapshot.Store.Interface.cs`
- Create: `src/Application/Common/EventSourcing/Snapshot.Policy.cs`
- Create: `src/Application/Common/EventSourcing/EventSourcing.Outcome.cs`
- Create: `src/Application/Common/EventSourcing/EventStreamName.Factory.cs`
- Create: `src/Application/Common/EventSourcing/EventSourcedRepository.Interface.cs`
- Create: `src/Application/Common/EventSourcing/Projection.Interface.cs`
- Create: `src/Application/Common/EventSourcing/ProjectionCheckpoint.Store.Interface.cs`
- Test: `tests/Unit/Application.Unit.Tests/Common/EventSourcing/EventSourcingContract.Test.cs`

**Interfaces:**
- Consumes: `IDomainEvent`, `DefaultMetadata`, `TryLoadFromHistory`.
- Produces: `IEventStore`, `StoredEvent`, `Snapshot`, `ISnapshotStore`, `ISnapshotPolicy`, `EventSourcingOutcome`, `EventStreamName.For<T>`, `IEventSourcedRepository<TAggregate,TKey>`, `IProjection`, `IProjectionCheckpointStore`.

- [x] **Step 1: Write the failing contract tests**

Cover `EventStreamName.For<TodoReminder>(id)` (`todo-reminder:{id}`), `IntervalSnapshotPolicy`
(interval 0/negative rejected, boundary at N), and every `EventSourcingOutcome` code.

- [x] **Step 2: Run the tests and verify they fail**

```bash
dotnet build tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release
```

Expected: `CS0246` for `Application.Common.EventSourcing`.

- [x] **Step 3: Write the contracts**

`IEventStore`:

```csharp
Task<Result<long>> AppendAsync(string streamId, long expectedVersion,
    IReadOnlyCollection<IDomainEvent> events, CancellationToken ct = default);
Task<Result<IReadOnlyList<StoredEvent>>> ReadStreamAsync(string streamId, long fromVersion,
    CancellationToken ct = default);
Task<Result<IReadOnlyList<StoredEvent>>> ReadAllAsync(long fromPosition, int batchSize,
    CancellationToken ct = default);
```

`StoredEvent` carries `StreamId`, `Version`, `EventId`, `EventName`, `EventVersion`,
`Payload`, `Metadata`, `OccurredOnUtc`, `Position`.

`Snapshot` carries `StreamId`, `Version`, `State`, `TakenOnUtc`. `ISnapshotStore`
`LoadAsync`/`SaveAsync`. `ISnapshotPolicy.ShouldSnapshot(currentVersion, eventsSinceSnapshot)`.
`IntervalSnapshotPolicy` uses `EventStore:SnapshotInterval`, default 50, and rejects
intervals below 1 via `Result` or constructor boundary comment.

`EventSourcingOutcome` codes from the spec: append conflict/failed, read failed,
serialize/deserialize failed, upcast missing, snapshot read/write failed, projection
apply/checkpoint failed, stream not found.

`EventStreamName.For<TAggregate>(Guid id)` lower-cases the aggregate name and uses `:`.
`IEventSourcedRepository<TAggregate,TKey>` exposes `LoadAsync`/`SaveAsync`.
`IProjection` exposes `Name` and `ApplyAsync(StoredEvent, ct)`.
`IProjectionCheckpointStore` exposes `GetPositionAsync`/`SavePositionAsync`.

- [x] **Step 4: Run the tests**

```bash
dotnet build tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --no-build --filter-class "*EventSourcingContractTest*"
```

Expected: PASS.

- [x] **Step 5: Commit**

```bash
git add src/Application/Common/EventSourcing tests/Unit/Application.Unit.Tests/Common/EventSourcing
git commit -m "feat(app): add event store and projection contracts"
```

---

### Task 2: PostgreSQL Event Store, Snapshots and Migration

**Files:**
- Create: `src/Infrastructure/Persistence/EntityFramework/EventSourcing/EventStoreEvent.Configuration.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/EventSourcing/EventStoreSnapshot.Configuration.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/EventSourcing/ProjectionCheckpoint.Configuration.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/EventSourcing/PostgresEventStore.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/EventSourcing/EfCoreSnapshotStore.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/EventSourcing/EfCoreProjectionCheckpointStore.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/EventSourcing/EventStore.Composition.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/EventSourcing/Options/EventStoreSetting.cs` + `.Constant.cs` + `.Validator.cs`
- Modify: `src/Application/Common/Persistence/PersistenceConstant.cs` (reuse `Messaging` schema)
- Migration: generated `AddEventStore`
- Test: `tests/Integration/Infrastructure.Integration.Tests/EventSourcing/EventStore.Integration.Test.cs`
- Test fixture: `tests/Integration/Infrastructure.Integration.Tests/EventSourcing/EventSourcing.Fixture.cs` + `.Collection.cs`

**Interfaces:**
- Consumes: Task 1 contracts; `IDomainEventSerializer`; EF Core; Postgres.
- Produces: `PostgresEventStore`, snapshot/checkpoint stores, `AddEventStore`, migration.

- [x] **Step 1: Add EF configurations**

Tables in the `messaging` schema:

- `event_store_events` (`Position bigserial`, `StreamId`, `Version`, `EventId`, `EventName`,
  `EventVersion`, `Payload jsonb`, `Metadata jsonb`, `OccurredOnUtc`), PK
  `(StreamId, Version)`, unique `EventId`, index `Position`.
- `event_store_snapshots` (`StreamId` PK, `Version`, `State jsonb`, `TakenOnUtc`).
- `projection_checkpoints` (`Projection` PK, `Position`, `UpdatedOnUtc`).

- [x] **Step 2: Write the failing store integration tests**

Cover append/read order, expected-version conflict, unique event id, global `ReadAll`
position order, snapshot round-trip, and checkpoint round-trip.

- [x] **Step 3: Implement `PostgresEventStore`**

Append: open a transaction, check the current stream version, insert rows with
`Version = expectedVersion + i`, unique constraint on `EventId`, commit. Translate
`DbUpdateException`/unique violations to `EventStore.Append.Conflict` or `Append.Failed`.
Read stream: ordered by version. Read all: ordered by position.

Snapshots/checkpoints use `DbContext` upsert semantics and return stable failures.

- [x] **Step 4: Generate the migration**

```bash
dotnet ef migrations add AddEventStore -p src/Infrastructure -s app/Web.Api -o Persistence/Migrations
```

Inspect: three tables + indexes only; symmetric `Down`.

- [x] **Step 5: Compose**

`AddEventStore(builder, configuration)` binds/validates `EventStoreSetting`
(`Enabled`, `Provider=Postgres`, `BatchSize=500`, `SnapshotInterval=50`,
`PollingIntervalSeconds=2`), registers the stores and `TryAdd` implementations.
Call it from `Infrastructure.Composition.AddInfrastructure` after `AddOutbox`.

- [x] **Step 6: Run tests and commit**

```bash
dotnet build tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release
dotnet test --project tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release --no-build --filter-class "*EventStoreIntegrationTest*"
git add src/Infrastructure/Persistence/EntityFramework/EventSourcing src/Infrastructure/Persistence/Migrations tests/Integration/Infrastructure.Integration.Tests/EventSourcing
git commit -m "feat(infra): add the PostgreSQL event store"
```

---

### Task 3: Event-Sourced Repository and Snapshots

**Files:**
- Create: `src/Infrastructure/Persistence/EntityFramework/EventSourcing/EventSourcedRepository.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/EventSourcing/Snapshot.Policy.cs`
- Modify: `src/Infrastructure/Persistence/EntityFramework/EntityFramework.Composition.cs` (serializer registration)
- Test: `tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/EventSourcing/EventSourcedRepository.Test.cs`

**Interfaces:**
- Consumes: `IEventStore`, `ISnapshotStore`, `ISnapshotPolicy`, `IDomainEventSerializer`, `IOutboxStore`, `IUnitOfWork`, `IAggregateFactory<TAggregate>`, `TryLoadFromHistory`.
- Produces: `EventSourcedRepository<TAggregate,TKey>`.

- [x] **Step 1: Write the failing tests**

Use a test aggregate + factory. Cover: load from snapshot + tail; full replay when no
snapshot; save computes `expectedVersion = aggregate.Version - uncommitted.Count`; append
stages outbox rows; clears uncommitted only after commit; snapshot policy triggers at the
interval; append conflict surfaces a stable failure.

- [x] **Step 2: Implement the repository**

Load: snapshot → `TryLoadFromHistory(snapshot.Version, tail)` or full stream from 1.
Save: reject no uncommitted events; append; stage one outbox row per event; `SaveChanges`;
clear; optionally snapshot. Use `// Boundary:` only for truly corrupt stream states; return
`Result` for conflicts/not-found.

- [x] **Step 3: Run tests and commit**

```bash
dotnet build tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release --no-build --filter-class "*EventSourcedRepositoryTest*"
git add src/Infrastructure/Persistence/EntityFramework/EventSourcing tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/EventSourcing
git commit -m "feat(infra): add the event-sourced repository"
```

---

### Task 4: Projection Runner

**Files:**
- Create: `src/Infrastructure/Persistence/EntityFramework/Projections/Projection.Worker.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/Projections/Projection.Runner.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/Projections/RebuildProjection.Service.cs`
- Test: `tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/Projections/Projection.Runner.Test.cs`

**Interfaces:**
- Consumes: `IEventStore`, `IProjection`, `IProjectionCheckpointStore`.
- Produces: `ProjectionRunner` with a callable `RunOnceAsync`; hosted worker; rebuild service.

- [x] **Step 1: Write the failing runner tests**

Cover: checkpoint advances only after apply success; failure keeps the checkpoint; batch
stops at the failing event; empty batch is a no-op; rebuild deletes/rewinds.

- [x] **Step 2: Implement runner + worker**

Read `ReadAllAsync(checkpoint + 1, batchSize)`, apply in order, save checkpoint after the
batch. Worker creates a scope per tick and catches/logs without crashing. Rebuild resets the
checkpoint and lets the worker replay; no `throw` in the flow.

- [x] **Step 3: Run tests and commit**

```bash
dotnet build tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release --no-build --filter-class "*ProjectionRunnerTest*"
git add src/Infrastructure/Persistence/EntityFramework/Projections tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/Projections
git commit -m "feat(infra): run and rebuild projections"
```

---

### Task 5: `TodoReminder` Pilot Domain and Use Cases

**Files:**
- Create: `src/Core/Todos/Reminders/TodoReminder.cs`
- Create: `src/Core/Todos/Reminders/TodoReminder.Event.cs`
- Create: `src/Core/Todos/Reminders/TodoReminder.Constant.cs`
- Create: `src/Core/Todos/Reminders/TodoReminder.Outcome.cs`
- Create: `src/Core/Todos/Reminders/TodoReminder.Factory.cs`
- Create: `src/Core/Todos/Reminders/TodoReminder.Snapshot.cs`
- Create: `src/Core/Todos/Reminders/TodoReminder.Validator.cs`
- Create: `src/Core/Todos/Reminders/ReminderStatus.cs`
- Create: `src/Application/Features/Todos/Reminders/Commands/ScheduleTodoReminder.cs`
- Create: `src/Application/Features/Todos/Reminders/Commands/SnoozeTodoReminder.cs`
- Create: `src/Application/Features/Todos/Reminders/Commands/DismissTodoReminder.cs`
- Create: `src/Application/Features/Todos/Reminders/Queries/GetTodoReminder.cs`
- Create: `src/Application/Features/Todos/Reminders/Queries/SearchTodoReminders.cs`
- Create: `src/Application/Features/Todos/Reminders/Models/TodoReminder.Model.cs`
- Test: `tests/Unit/Core.Unit.Tests/Todos/Reminders/TodoReminder.Test.cs`
- Test: `tests/Unit/Application.Unit.Tests/Features/Todos/Reminders/Reminder.Commands.Test.cs`

**Interfaces:**
- Consumes: `EventSourcedAggregate`, `ISnapshotable`, `IEventSourcedRepository`, `TimeProvider`, `ITenantContext`.
- Produces: `TodoReminder`, events, commands/queries, validators.

- [x] **Step 1: Write the failing domain tests**

Cover schedule/snooze/dismiss transitions, terminal-state guards, future-time validation,
snooze count, snapshot capture/restore, and rehydration equivalence.

- [x] **Step 2: Implement the aggregate**

`TodoReminder : EventSourcedAggregate<Guid>, ISnapshotable` with `When` switch for
`ReminderScheduled`, `ReminderSnoozed`, `ReminderDismissed`; `Create`/`Snooze`/`Dismiss`
return `Result<TodoReminder>` and use outcome factories. No throwing for expected rules.

- [x] **Step 3: Implement commands/queries/validators**

Commands use `IEventSourcedRepository<TodoReminder, Guid>`, `ITenantContext`,
`IUserContext`, `TimeProvider`; FluentValidation validates request shape; domain rules return
`Result`. Queries read the projection/read model, not the event stream.

- [x] **Step 4: Run tests and commit**

```bash
dotnet build tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release
dotnet build tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release --no-build --filter-class "*TodoReminderTest*"
dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --no-build --filter-class "*ReminderCommandsTest*"
git add src/Core/Todos/Reminders src/Application/Features/Todos/Reminders tests/Unit/Core.Unit.Tests/Todos/Reminders tests/Unit/Application.Unit.Tests/Features/Todos/Reminders
git commit -m "feat(core): add the event-sourced reminder aggregate"
```

---

### Task 6: Reminder Projection, Endpoints and End-to-End Gates

**Files:**
- Create: `src/Infrastructure/Persistence/EntityFramework/Projections/TodoReminder.ReadModel.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/Projections/TodoReminder.Projection.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/Projections/TodoReminderReadModel.Configuration.cs`
- Create: `app/Web.Api/Endpoints/Todos/TodoReminder.Endpoint.cs`
- Modify: `app/Web.Api/Program.cs` (register the Carter module)
- Test: `tests/Integration/Infrastructure.Integration.Tests/EventSourcing/TodoReminder.Flow.Integration.Test.cs`
- Test: `tests/Integration/Api.Integration.Tests/TodoReminder.Api.Test.cs`

**Interfaces:**
- Consumes: Tasks 1–5.
- Produces: read model, projection, endpoints, end-to-end tests.

- [x] **Step 1: Implement the projection/read model**

Upsert keyed by reminder id with a version guard. Map the three events to `Status`,
`RemindAtUtc`, `SnoozeCount`, `Version`.

- [x] **Step 2: Implement endpoints**

`POST /api/todos/{todoId}/reminders`, `PATCH .../{reminderId}/snooze`,
`DELETE .../{reminderId}`, `GET .../reminders`. Require an authenticated user/tenant.

- [x] **Step 3: Run the projection/API tests**

```bash
dotnet build tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release
dotnet test --project tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release --no-build --filter-class "*TodoReminderFlowIntegrationTest*"
dotnet test --project tests/Integration/Api.Integration.Tests/Api.Integration.Tests.csproj -c Release --no-build --filter-class "*TodoReminderApiTest*"
```

- [x] **Step 4: Rebuild check**

Delete the reminder read model, reset the checkpoint, run the projection runner, and assert
the rows are identical.

- [x] **Step 5: Commit**

```bash
git add src/Infrastructure/Persistence/EntityFramework/Projections app/Web.Api tests
git commit -m "feat(infra): project and expose todo reminders"
```

---

### Wave 2: `Todo` Migration

Completed in `docs/plans/2026-10-30-todo-event-sourcing-migration.md`. `Todo` is event-sourced,
`TodoReadModel` owns the `todos` projection, and the backfill is idempotent.

---

## Self-Review

| Spec section | Task |
|---|---|
| Event store/snapshot/projection contracts | Task 1 |
| PostgreSQL event store + migration | Task 2 |
| Repository + snapshots | Task 3 |
| Projection runner/rebuild | Task 4 |
| `TodoReminder` domain/use cases | Task 5 |
| Projection/endpoints/tests | Task 6 |
| `Todo` migration | gated wave 2 |

No placeholders; types are defined once and reused. Monadic policy applies to every task.

## Execution Handoff

Plan saved to `docs/plans/2026-10-30-event-sourcing.md`. Execute Tasks 1–6, then request
explicit approval for wave 2.
