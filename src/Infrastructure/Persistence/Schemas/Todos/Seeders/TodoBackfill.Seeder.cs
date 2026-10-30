using System.Text.Json;

using Application.Common.EventSourcing;
using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Seeders;
using Application.Features.Todos.Items.Models;

using Core.Common.Domain.Events;
using Core.Todos.Items;

using Infrastructure.Persistence.EntityFramework.EventSourcing.Options;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Persistence.Schemas.Todos.Seeders;

/// <summary>
/// Backfills event streams for todo rows created before the event-sourced cutover.
/// </summary>
/// <remarks>
/// Rows are replayed into the existing read model, so the seeder resets the legacy
/// projection version before appending. The stream identifier is the idempotency key:
/// a row whose <c>todo:{id}</c> stream already exists is skipped.
/// </remarks>
public sealed class TodoBackfillSeeder(
    DbContext context,
    IEventStore eventStore,
    ISnapshotStore snapshotStore,
    IProjectionCheckpointStore checkpoints,
    IOptions<EventStoreSetting> setting,
    TimeProvider clock) : IPersistenceSeeder<TodoReadModel, Guid>
{
    /// <summary>Rows fetched per page; the batch setting caps how many rows are backfilled.</summary>
    private const int PageSize = 100;

    /// <inheritdoc />
    public string Name => "seeder:todo-backfill";

    /// <inheritdoc />
    public string DisplayName => "Backfill Todo event streams";

    /// <inheritdoc />
    public int Order => 35;

    /// <inheritdoc />
    public string[] DependsOn => ["seeder:todos"];

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
        if (!setting.Value.BackfillTodos)
            return Result.Ok();

        int batchSize = setting.Value.BatchSize;
        int backfilled = 0;
        int offset = 0;

        while (backfilled < batchSize)
        {
            List<TodoReadModel> page = await LoadPageAsync(
                offset,
                Math.Min(PageSize, batchSize - backfilled),
                cancellationToken);
            if (page.Count == 0)
                break;

            offset += page.Count;

            foreach (TodoReadModel row in page)
            {
                Result<bool> result = await BackfillRowAsync(row, cancellationToken);
                if (result.IsFailure)
                    return Result.Fail(result.Errors);

                // Check: false means the row already had a stream and was left untouched.
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
        if (!setting.Value.BackfillTodos)
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

        int batchSize = setting.Value.BatchSize;
        List<InitializationOperation> operations = [];
        int offset = 0;

        while (operations.Count < batchSize)
        {
            List<TodoReadModel> page = await LoadPageAsync(
                offset,
                Math.Min(PageSize, batchSize - operations.Count),
                cancellationToken);
            if (page.Count == 0)
                break;

            offset += page.Count;

            foreach (TodoReadModel row in page)
            {
                string streamId = EventStreamName.For<Todo>(row.Id);
                Result<bool> hasStream = await HasStreamAsync(streamId, cancellationToken);
                if (hasStream.IsFailure)
                    return Result<IReadOnlyList<InitializationOperation>>.Fail(hasStream.Errors);

                // Filter: rows with a stream are already backfilled.
                if (hasStream.Value)
                    continue;

                operations.Add(new InitializationOperation
                {
                    StepName = Name,
                    Description = $"Backfill event stream '{streamId}'.",
                    Type = InitializationOperationType.DataChange,
                    EstimatedRowsAffected = 1,
                });

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

    /// <summary>Loads one stable page of todo rows in creation order.</summary>
    private async Task<List<TodoReadModel>> LoadPageAsync(
        int offset,
        int take,
        CancellationToken cancellationToken)
        => await context.Set<TodoReadModel>()
            .AsNoTracking()
            .OrderBy(todo => todo.CreatedAtUtc)
            .ThenBy(todo => todo.Id)
            .Skip(offset)
            .Take(take)
            .ToListAsync(cancellationToken);

    /// <summary>Backfills one row, returning false when its stream already exists.</summary>
    private async Task<Result<bool>> BackfillRowAsync(
        TodoReadModel row,
        CancellationToken cancellationToken)
    {
        string streamId = EventStreamName.For<Todo>(row.Id);

        // Check: an existing stream means this row was already migrated or replayed.
        Result<bool> hasStream = await HasStreamAsync(streamId, cancellationToken);
        if (hasStream.IsFailure)
            return Result<bool>.Fail(hasStream.Errors);
        if (hasStream.Value)
            return Result<bool>.Success(false);

        IReadOnlyList<IDomainEvent> events = BuildEvents(row);

        // Reset: a legacy row's Version is an aggregate version, not a stream version.
        // Zeroing it lets the projection's Version guard apply the replayed events.
        await context.Set<TodoReadModel>()
            .Where(todo => todo.Id == row.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(todo => todo.Version, 0L),
                cancellationToken);

        // Append: the stream starts empty by construction, so the expected version is 0.
        Result<long> appended = await eventStore.AppendAsync(
            streamId, 0, events, cancellationToken);
        if (appended.IsFailure)
            return Result<bool>.Fail(appended.Errors);

        // Persist: the snapshot mirrors the same state the projection will replay to.
        Result snapshot = await snapshotStore.SaveAsync(
            new Snapshot(
                streamId,
                events.Count,
                JsonSerializer.Serialize(ToSnapshot(row, events.Count)),
                clock.GetUtcNow()),
            cancellationToken);
        if (snapshot.IsFailure)
            return Result<bool>.Fail(snapshot.Errors);

        // Mark: the per-row checkpoint records that this stream was backfilled.
        Result marker = await checkpoints.SavePositionAsync(
            $"TodoBackfill:{row.Id}", events.Count, cancellationToken);
        if (marker.IsFailure)
            return Result<bool>.Fail(marker.Errors);

        return Result<bool>.Success(true);
    }

    /// <summary>Reports whether a stream has at least one stored event.</summary>
    private async Task<Result<bool>> HasStreamAsync(
        string streamId,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<StoredEvent>> existing = await eventStore.ReadStreamAsync(
            streamId, 1, cancellationToken);

        if (existing.IsSuccess)
            return Result<bool>.Success(existing.Value.Count > 0);

        // Fallback: some event-store implementations signal a missing stream as a failure.
        bool missing = existing.Errors.Exists(
            error => error.Code == EventSourcingOutcome.Codes.StreamNotFound);

        return missing
            ? Result<bool>.Success(false)
            : Result<bool>.Fail(existing.Errors);
    }

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

        if (row.Status != TodoStatus.Pending)
        {
            events.Add(new TodoStatusChanged(
                row.Id, row.TenantId, TodoStatus.Pending, row.Status,
                row.CompletedAtUtc ?? row.ModifiedAtUtc ?? row.CreatedAtUtc,
                row.ModifiedBy));
        }

        if (row.IsDeleted)
        {
            events.Add(new TodoDeleted(
                row.Id, false,
                row.DeletedAtUtc ?? row.ModifiedAtUtc ?? row.CreatedAtUtc,
                row.DeletedBy));
        }

        return events;
    }

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
