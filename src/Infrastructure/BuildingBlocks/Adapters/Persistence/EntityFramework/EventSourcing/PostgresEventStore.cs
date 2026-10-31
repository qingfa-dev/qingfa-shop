using System.Diagnostics.CodeAnalysis;

using Application.BuildingBlocks.EventSourcing;
using Application.BuildingBlocks.Messaging.Serialization;

using Core.BuildingBlocks.Domain.Events;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing;

/// <summary>PostgreSQL event store with optimistic stream concurrency.</summary>
/// <remarks>Provider-specific SQL/identity behavior is covered by the PostgreSQL integration suite.</remarks>
[ExcludeFromCodeCoverage]
public sealed class PostgresEventStore(
    DbContext context,
    IDomainEventSerializer serializer,
    TimeProvider timeProvider) : IEventStore
{
    private DbSet<StoredEvent> Events => context.Set<StoredEvent>();

    public async Task<Result<long>> AppendAsync(
        string streamId,
        long expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            return Result<long>.Fail(EventSourcingOutcome.Failure.AppendFailed(streamId, "Stream id is required."));
        if (events is null)
            return Result<long>.Fail(EventSourcingOutcome.Failure.AppendFailed(streamId, "Events are required."));
        if (events.Count == 0)
            return Result<long>.Success(expectedVersion);

        IDbContextTransaction? transaction = null;

        try
        {
            // Handle: enrol in the command's ambient transaction instead of opening a nested one.
            bool ownsTransaction = context.Database.CurrentTransaction is null;
            transaction = ownsTransaction
                ? await context.Database.BeginTransactionAsync(cancellationToken)
                : null;

            long current = await Events
                .Where(stored => stored.StreamId == streamId)
                .MaxAsync(stored => (long?)stored.Version, cancellationToken) ?? 0;

            if (current != expectedVersion)
            {
                if (transaction is not null)
                    await transaction.RollbackAsync(cancellationToken);

                return Result<long>.Fail(EventSourcingOutcome.Failure.AppendConflict(streamId, expectedVersion));
            }

            long version = expectedVersion;
            List<StoredEvent> rows = [];
            foreach (IDomainEvent domainEvent in events)
            {
                version++;
                Guid eventId = domainEvent.GetEventId() ?? Guid.NewGuid();
                if (domainEvent.GetEventId() is null)
                    domainEvent.SetEventId(eventId);

                rows.Add(new StoredEvent(
                    streamId,
                    version,
                    eventId,
                    domainEvent.EventName,
                    domainEvent.EventVersion,
                    serializer.Serialize(domainEvent),
                    domainEvent.Metadata,
                    domainEvent.GetTimestamp() ?? timeProvider.GetUtcNow(),
                    0));
            }

            Events.AddRange(rows);
            await context.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return Result<long>.Success(version);
        }
        catch (OperationCanceledException)
        {
            // Boundary: preserve cancellation for the caller.
            throw;
        }
        catch (DbUpdateException exception)
        {
            string reason = exception.InnerException?.Message ?? exception.Message;
            return reason.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                   reason.Contains("unique", StringComparison.OrdinalIgnoreCase)
                ? Result<long>.Fail(EventSourcingOutcome.Failure.AppendConflict(streamId, expectedVersion))
                : Result<long>.Fail(EventSourcingOutcome.Failure.AppendFailed(streamId, reason));
        }
        catch (Exception exception)
        {
            return Result<long>.Fail(
                EventSourcingOutcome.Failure.AppendFailed(streamId, exception.Message));
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    public async Task<Result<IReadOnlyList<StoredEvent>>> ReadStreamAsync(
        string streamId,
        long fromVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            List<StoredEvent> events = await Events
                .Where(stored => stored.StreamId == streamId && stored.Version >= fromVersion)
                .OrderBy(stored => stored.Version)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            return Result<IReadOnlyList<StoredEvent>>.Success(events);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Result<IReadOnlyList<StoredEvent>>.Fail(
                EventSourcingOutcome.Failure.ReadFailed(streamId, exception.Message));
        }
    }

    public async Task<Result<IReadOnlyList<StoredEvent>>> ReadAllAsync(
        long fromPosition,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            List<StoredEvent> events = await Events
                .Where(stored => stored.Position >= fromPosition)
                .OrderBy(stored => stored.Position)
                .Take(batchSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            return Result<IReadOnlyList<StoredEvent>>.Success(events);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Result<IReadOnlyList<StoredEvent>>.Fail(
                EventSourcingOutcome.Failure.ReadFailed("all", exception.Message));
        }
    }
}
