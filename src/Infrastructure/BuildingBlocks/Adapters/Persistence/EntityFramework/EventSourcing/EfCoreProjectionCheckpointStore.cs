using Application.BuildingBlocks.EventSourcing;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing;

/// <summary>EF Core projection checkpoint store.</summary>
public sealed class EfCoreProjectionCheckpointStore(DbContext context) : IProjectionCheckpointStore
{
    public async Task<Result<long>> GetPositionAsync(
        string projection,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ProjectionCheckpoint? checkpoint = await context.Set<ProjectionCheckpoint>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Projection == projection, cancellationToken);
            return Result<long>.Success(checkpoint?.Position ?? 0);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<long>.Fail(
                EventSourcingOutcome.Failure.ProjectionCheckpointFailed(projection, exception.Message));
        }
    }

    public async Task<Result> SavePositionAsync(
        string projection,
        long position,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ProjectionCheckpoint? existing = await context.Set<ProjectionCheckpoint>()
                .FirstOrDefaultAsync(item => item.Projection == projection, cancellationToken);
            var replacement = new ProjectionCheckpoint(projection, position, DateTimeOffset.UtcNow);

            if (existing is null)
                context.Set<ProjectionCheckpoint>().Add(replacement);
            else
                context.Entry(existing).CurrentValues.SetValues(replacement);

            await context.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result.Fail(
                EventSourcingOutcome.Failure.ProjectionCheckpointFailed(projection, exception.Message));
        }
    }
}
