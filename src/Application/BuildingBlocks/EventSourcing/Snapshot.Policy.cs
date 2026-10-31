namespace Application.BuildingBlocks.EventSourcing;

/// <summary>Decides when an aggregate should be snapshotted.</summary>
public interface ISnapshotPolicy
{
    /// <summary>Returns true when the current version should be captured.</summary>
    /// <param name="currentVersion">The stream version.</param>
    /// <param name="eventsSinceSnapshot">Events applied since the last snapshot.</param>
    bool ShouldSnapshot(long currentVersion, long eventsSinceSnapshot);
}

/// <summary>Snapshots every configured number of events.</summary>
public sealed class IntervalSnapshotPolicy : ISnapshotPolicy
{
    private readonly long _interval;

    /// <summary>Creates an interval policy.</summary>
    /// <param name="interval">Events between snapshots; must be positive.</param>
    public IntervalSnapshotPolicy(long interval)
    {
        // Boundary: composition supplies the interval; a non-positive value is a programming error.
        _interval = interval > 0
            ? interval
            : throw new ArgumentOutOfRangeException(nameof(interval), "Snapshot interval must be positive.");
    }

    /// <inheritdoc />
    public bool ShouldSnapshot(long currentVersion, long eventsSinceSnapshot)
        => eventsSinceSnapshot >= _interval;
}
