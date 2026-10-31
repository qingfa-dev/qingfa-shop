using System.Text.Json;

using Application.BuildingBlocks.EventSourcing;
using Application.BuildingBlocks.Persistence.Initialization;
using Application.BuildingBlocks.Persistence.Initialization.Seeders;
using Application.Modules.Todos.Items.Models;

using Core.BuildingBlocks.Domain.Events;
using Core.Modules.Todos.Items;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing.Options;
using Infrastructure.Modules.Todos.Persistence.EventSourcing.Options;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Modules.Todos.Persistence.Schemas.Seeders;

/// <summary>
/// Backfills event streams for todo rows created before the event-sourced cutover.
/// </summary>
/// <remarks>
/// <para>
/// Rows are replayed into the existing read model, so the seeder resets the legacy
/// projection version before appending. The stream identifier is the idempotency key:
/// a row whose <c>todo:{id}</c> stream already exists is skipped once its marker exists.
/// </para>
/// <para>
/// Rows written to <c>projection_checkpoints</c> as <c>TodoBackfill:{id}</c> store the
/// backfilled stream version, not a global event-log position. They are one-shot markers
/// for this migration seeder, not positions consumed by a projection runner, which is why
/// reusing that table is acceptable here.
/// </para>
/// </remarks>
public sealed class TodoBackfillSeeder(
    DbContext context,
    IEventStore eventStore,
    ISnapshotStore snapshotStore,
    IProjectionCheckpointStore checkpoints,
    IOptions<EventStoreSetting> setting,
    IOptions<TodoEventStoreSetting> todoSetting,
    TimeProvider clock) : IPersistenceSeeder<TodoReadModel, Guid>
{

    /// <summary>Smallest batch that can still make progress.</summary>
    private const int MinimumBatchSize = 1;

    /// <inheritdoc />
    public string Name => TodoSeederConstant.Names.Backfill;

    /// <inheritdoc />
    public string DisplayName => "Backfill Todo event streams";

    /// <inheritdoc />
    public int Order => TodoSeederConstant.Order.Backfill;

    /// <inheritdoc />
    public string[] DependsOn => [TodoSeederConstant.Names.Todos];

    /// <inheritdoc />
    public bool IsRequired => false;

    /// <inheritdoc />
    Type? IPersistenceSeeder.EntityType => typeof(TodoReadModel);

    /// <inheritdoc />
    Type? IPersistenceSeeder.KeyType => typeof(Guid);

    /// <inheritdoc />
    public async Task<Result> ExecuteAsync(
        IInitializationContext initializationContext,
        CancellationToken cancellationToken = default)
    {
        // Skip: backfill is opt-in, so an unconfigured host keeps the legacy rows untouched.
        if (!todoSetting.Value.BackfillTodos)
            return Result.Ok();

        int batchSize = ResolveBatchSize();
        int backfilled = 0;
        int offset = 0;

        while (backfilled < batchSize)
        {
            List<TodoReadModel> page = await LoadPageAsync(offset, cancellationToken);
            if (page.Count == 0)
                break;

            offset += page.Count;

            foreach (TodoReadModel row in page)
            {
                Result<bool> result = await BackfillRowAsync(row, cancellationToken);
                if (result.IsFailure)
                    return Result.Fail(result.Errors);

                // Check: false means the row was already complete or only needed artifact recovery.
                if (!result.Value)
                    continue;

                backfilled++;
                if (backfilled >= batchSize)
                    break;
            }
        }

        PersistenceSeederLoggers.Seeded(
            initializationContext.Logger, backfilled, "todo event streams");
        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<InitializationOperation>>> PreviewAsync(
        IInitializationContext initializationContext,
        CancellationToken cancellationToken = default)
    {
        // Check: report the disabled state instead of pretending rows will be touched.
        if (!todoSetting.Value.BackfillTodos)
        {
            return Result<IReadOnlyList<InitializationOperation>>.Success(
            [
                new InitializationOperation
                {
                    StepName = Name,
                    Description = "Todo event stream backfill is disabled.",
                    Type = InitializationOperationType.DataCheck,
                    EstimatedRowsAffected = 0,
                },
            ]);
        }

        int batchSize = ResolveBatchSize();
        List<InitializationOperation> operations = [];
        int offset = 0;

        while (operations.Count < batchSize)
        {
            List<TodoReadModel> page = await LoadPageAsync(offset, cancellationToken);
            if (page.Count == 0)
                break;

            offset += page.Count;

            foreach (TodoReadModel row in page)
            {
                string streamId = EventStreamName.For<Todo>(row.Id);
                Result<IReadOnlyList<StoredEvent>> existing = await LoadStreamAsync(
                    streamId, cancellationToken);
                if (existing.IsFailure)
                    return Result<IReadOnlyList<InitializationOperation>>.Fail(existing.Errors);

                if (existing.Value.Count == 0)
                {
                    operations.Add(new InitializationOperation
                    {
                        StepName = Name,
                        Description = $"Backfill event stream '{streamId}'.",
                        Type = InitializationOperationType.DataChange,
                        EstimatedRowsAffected = 1,
                    });
                }
                else
                {
                    string marker = MarkerName(row.Id);
                    Result<long> position = await checkpoints.GetPositionAsync(
                        marker, cancellationToken);
                    if (position.IsFailure)
                        return Result<IReadOnlyList<InitializationOperation>>.Fail(position.Errors);

                    // Filter: a positive marker means the stream is already complete.
                    if (position.Value > 0)
                        continue;

                    // Audit: execute would re-save only the marker for this row, so preview
                    // must report the same recovery work.
                    operations.Add(new InitializationOperation
                    {
                        StepName = Name,
                        Description = $"Recover backfill marker for stream '{streamId}'.",
                        Type = InitializationOperationType.DataChange,
                        EstimatedRowsAffected = 1,
                    });
                }

                if (operations.Count >= batchSize)
                    break;
            }
        }

        if (operations.Count == 0)
        {
            operations.Add(new InitializationOperation
            {
                StepName = Name,
                Description = "No todo rows require an event stream backfill.",
                Type = InitializationOperationType.DataCheck,
                EstimatedRowsAffected = 0,
            });
        }

        return Result<IReadOnlyList<InitializationOperation>>.Success(operations);
    }

    /// <summary>Resolves a batch size that cannot silently report no work.</summary>
    private int ResolveBatchSize()
        => Math.Clamp(setting.Value.BatchSize, MinimumBatchSize, TodoSeederConstant.Limits.MaxBatchSize);

    /// <summary>Loads one stable page of todo rows in creation order.</summary>
    private async Task<List<TodoReadModel>> LoadPageAsync(
        int offset,
        CancellationToken cancellationToken)
        => await context.Set<TodoReadModel>()
            .AsNoTracking()
            .OrderBy(todo => todo.CreatedAtUtc)
            .ThenBy(todo => todo.Id)
            .Skip(offset)
            .Take(TodoSeederConstant.Limits.PageSize)
            .ToListAsync(cancellationToken);

    /// <summary>Backfills one row, returning false when no new events were appended.</summary>
    private async Task<Result<bool>> BackfillRowAsync(
        TodoReadModel row,
        CancellationToken cancellationToken)
    {
        string streamId = EventStreamName.For<Todo>(row.Id);
        string marker = MarkerName(row.Id);

        // Check: because the store may report a missing stream as an empty success, the
        // empty list is the authoritative "no stream" signal.
        Result<IReadOnlyList<StoredEvent>> existing = await LoadStreamAsync(
            streamId, cancellationToken);
        if (existing.IsFailure)
            return Result<bool>.Fail(existing.Errors);

        if (existing.Value.Count > 0)
        {
            Result<long> position = await checkpoints.GetPositionAsync(marker, cancellationToken);
            if (position.IsFailure)
                return Result<bool>.Fail(position.Errors);

            // Skip: a positive marker means a previous run finished this row.
            if (position.Value > 0)
                return Result<bool>.Success(false);

            // Audit: the stream exists but its marker was lost, so re-save only the marker.
            // Never rebuild the snapshot here: the projection is asynchronous, so a lagging
            // read model would be stamped at the stream's latest version and the repository
            // would then trust that stale state and skip the unreplayed tail. A missing
            // snapshot is safe because the repository falls back to a full replay.
            Result recoveredMarker = await checkpoints.SavePositionAsync(
                marker, existing.Value[^1].Version, cancellationToken);
            return recoveredMarker.IsFailure
                ? Result<bool>.Fail(recoveredMarker.Errors)
                : Result<bool>.Success(false);
        }

        IReadOnlyList<IDomainEvent> events = BuildEvents(row);

        // Update: a legacy row's Version is an aggregate version, not a stream version.
        // Zeroing it lets the projection's Version guard apply the replayed events.
        await context.Set<TodoReadModel>()
            .Where(todo => todo.Id == row.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(todo => todo.Version, 0L),
                cancellationToken);

        // Call: the stream starts empty by construction, so the expected version is 0.
        Result<long> appended = await eventStore.AppendAsync(
            streamId, 0, events, cancellationToken);
        if (appended.IsFailure)
            return Result<bool>.Fail(appended.Errors);

        Result snapshot = await SaveSnapshotAsync(row, events.Count, cancellationToken);
        if (snapshot.IsFailure)
            return Result<bool>.Fail(snapshot.Errors);

        Result savedMarker = await checkpoints.SavePositionAsync(
            marker, events.Count, cancellationToken);
        if (savedMarker.IsFailure)
            return Result<bool>.Fail(savedMarker.Errors);

        return Result<bool>.Success(true);
    }

    /// <summary>Reads an existing stream, folding the missing-stream signal into an empty list.</summary>
    private async Task<Result<IReadOnlyList<StoredEvent>>> LoadStreamAsync(
        string streamId,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<StoredEvent>> existing = await eventStore.ReadStreamAsync(
            streamId, 1, cancellationToken);

        if (existing.IsSuccess)
            return existing;

        // Fallback: some event-store implementations signal a missing stream as a failure.
        bool missing = existing.Errors.Exists(
            error => error.Code == EventSourcingOutcome.Codes.StreamNotFound);

        return missing
            ? Result<IReadOnlyList<StoredEvent>>.Success([])
            : Result<IReadOnlyList<StoredEvent>>.Fail(existing.Errors);
    }

    /// <summary>Persists the snapshot shape that the aggregate would capture.</summary>
    private async Task<Result> SaveSnapshotAsync(
        TodoReadModel row,
        long version,
        CancellationToken cancellationToken)
        => await snapshotStore.SaveAsync(
            new Snapshot(
                EventStreamName.For<Todo>(row.Id),
                version,
                JsonSerializer.Serialize(ToSnapshot(row, version)),
                clock.GetUtcNow()),
            cancellationToken);

    /// <summary>Builds the ordered event history that reproduces the current row.</summary>
    private static IReadOnlyList<IDomainEvent> BuildEvents(TodoReadModel row)
    {
        List<IDomainEvent> events =
        [
            new TodoCreated(
                row.Id, row.TenantId, row.Title, row.Description, row.Priority,
                row.DueAtUtc, row.ListId, row.CreatedAtUtc, row.CreatedBy),
        ];

        foreach (Guid categoryId in row.CategoryIds)
            events.Add(new TodoCategoryAdded(row.Id, categoryId, row.CreatedAtUtc, row.CreatedBy));

        if (row.ModifiedAtUtc is not null)
        {
            events.Add(new TodoUpdated(
                row.Id, row.Title, row.Description, row.Priority, row.DueAtUtc,
                row.ListId, row.ModifiedAtUtc.Value, row.ModifiedBy));
        }

        DateTimeOffset? statusOccurredAt = null;
        if (row.Status != TodoStatus.Pending)
        {
            statusOccurredAt = row.CompletedAtUtc ?? row.ModifiedAtUtc ?? row.CreatedAtUtc;
            events.Add(new TodoStatusChanged(
                row.Id, row.TenantId, TodoStatus.Pending, row.Status,
                statusOccurredAt.Value, row.ModifiedBy));
        }

        if (row.IsDeleted)
        {
            events.Add(new TodoDeleted(
                row.Id, false,
                row.DeletedAtUtc ?? row.ModifiedAtUtc ?? row.CreatedAtUtc,
                row.DeletedBy));
        }

        // Update: a status replay overwrites the modified stamp, so restore the legacy
        // row's real audit values with a trailing update when the timestamps differ.
        if (row.ModifiedAtUtc is not null &&
            statusOccurredAt is not null &&
            row.ModifiedAtUtc.Value != statusOccurredAt.Value)
        {
            events.Add(new TodoUpdated(
                row.Id, row.Title, row.Description, row.Priority, row.DueAtUtc,
                row.ListId, row.ModifiedAtUtc.Value, row.ModifiedBy));
        }

        return events;
    }

    /// <summary>Stable per-row marker name outside the projection checkpoint namespace.</summary>
    private static string MarkerName(Guid id) => $"TodoBackfill:{id}";

    /// <summary>Maps the row to the aggregate snapshot shape.</summary>
    private static TodoSnapshot ToSnapshot(TodoReadModel row, long version)
        => new(
            row.Id,
            row.TenantId,
            row.Title,
            row.Description,
            row.Status,
            row.Priority,
            row.DueAtUtc,
            row.CompletedAtUtc,
            row.ListId,
            [.. row.CategoryIds],
            row.CreatedAtUtc,
            row.CreatedBy,
            row.ModifiedAtUtc,
            row.ModifiedBy,
            row.IsDeleted,
            IsHardDeleted: false,
            row.DeletedAtUtc,
            row.DeletedBy,
            version);
}
