using Application.Common.EventSourcing;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.EventSourcing;

/// <summary>EF Core snapshot store.</summary>
public sealed class EfCoreSnapshotStore(DbContext context) : ISnapshotStore
{
    public async Task<Result<Snapshot?>> LoadAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Snapshot? snapshot = await context.Set<Snapshot>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.StreamId == streamId, cancellationToken);
            return Result<Snapshot?>.Success(snapshot);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<Snapshot?>.Fail(
                EventSourcingOutcome.Failure.SnapshotReadFailed(streamId, exception.Message));
        }
    }

    public async Task<Result> SaveAsync(
        Snapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Snapshot? existing = await context.Set<Snapshot>()
                .FirstOrDefaultAsync(item => item.StreamId == snapshot.StreamId, cancellationToken);

            if (existing is null)
                context.Set<Snapshot>().Add(snapshot);
            else
                context.Entry(existing).CurrentValues.SetValues(snapshot);

            await context.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result.Fail(
                EventSourcingOutcome.Failure.SnapshotWriteFailed(snapshot.StreamId, exception.Message));
        }
    }
}
