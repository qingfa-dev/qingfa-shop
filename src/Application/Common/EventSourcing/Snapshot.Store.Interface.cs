namespace Application.Common.EventSourcing;

/// <summary>Persists and loads aggregate snapshots.</summary>
public interface ISnapshotStore
{
    /// <summary>Loads the latest snapshot for a stream.</summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The snapshot, or null when none exists.</returns>
    Task<Result<Maybe<Snapshot>>> LoadAsync(
        string streamId,
        CancellationToken cancellationToken = default);

    /// <summary>Upserts a snapshot.</summary>
    /// <param name="snapshot">The snapshot to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or a stable snapshot failure.</returns>
    Task<Result> SaveAsync(
        Snapshot snapshot,
        CancellationToken cancellationToken = default);
}
