using Application.BuildingBlocks.EventSourcing;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing.Options;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Projections;

/// <summary>Processes the global event log into registered projections.</summary>
public sealed class ProjectionRunner(
    IEventStore eventStore,
    IEnumerable<IProjection> projections,
    IProjectionCheckpointStore checkpoints,
    EventStoreSetting setting,
    ILogger<ProjectionRunner> logger)
{
    /// <summary>Runs one bounded pass over every projection.</summary>
    public async Task<Result> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        foreach (IProjection projection in projections)
        {
            Result<long> position = await checkpoints.GetPositionAsync(projection.Name, cancellationToken);
            if (position.IsFailure)
                return Result.Fail(position.Errors);

            Result<IReadOnlyList<StoredEvent>> batch = await eventStore.ReadAllAsync(
                position.Value + 1,
                setting.BatchSize,
                cancellationToken);

            if (batch.IsFailure)
                return Result.Fail(batch.Errors);

            if (batch.Value.Count == 0)
                continue;

            long lastPosition = position.Value;
            foreach (StoredEvent stored in batch.Value)
            {
                Result applied = await projection.ApplyAsync(stored, cancellationToken);
                if (applied.IsFailure)
                {
                    // Retry: keep the checkpoint at the last successful event.
                    return Result.Fail(EventSourcingOutcome.Failure.ProjectionApplyFailed(
                        projection.Name,
                        applied.Errors[0].Message));
                }

                lastPosition = stored.Position;
            }

            Result saved = await checkpoints.SavePositionAsync(
                projection.Name,
                lastPosition,
                cancellationToken);

            if (saved.IsFailure)
                return Result.Fail(saved.Errors);

            logger.LogDebug(
                "Projection {Projection} advanced to position {Position}.",
                projection.Name,
                lastPosition);
        }

        return Result.Ok();
    }
}
