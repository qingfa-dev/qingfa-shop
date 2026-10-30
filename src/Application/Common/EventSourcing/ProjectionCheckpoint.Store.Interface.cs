namespace Application.Common.EventSourcing;

/// <summary>Persists each projection's last processed global position.</summary>
public interface IProjectionCheckpointStore
{
    /// <summary>Reads the checkpoint, or 0 when the projection never ran.</summary>
    Task<Result<long>> GetPositionAsync(string projection, CancellationToken cancellationToken = default);

    /// <summary>Upserts the checkpoint.</summary>
    Task<Result> SavePositionAsync(string projection, long position, CancellationToken cancellationToken = default);
}
