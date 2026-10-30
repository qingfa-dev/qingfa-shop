using System.Diagnostics.CodeAnalysis;

using Application.Common.EventSourcing;

namespace Infrastructure.Persistence.EntityFramework.Projections;

/// <summary>Resets a projection so the worker replays the log from the beginning.</summary>
[ExcludeFromCodeCoverage]
public sealed class RebuildProjectionService(
    IProjectionCheckpointStore checkpoints,
    IProjection projection)
{
    /// <summary>Rewinds the projection checkpoint to 0.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or a stable checkpoint failure.</returns>
    public Task<Result> RebuildAsync(CancellationToken cancellationToken = default)
        => checkpoints.SavePositionAsync(projection.Name, 0, cancellationToken);
}
