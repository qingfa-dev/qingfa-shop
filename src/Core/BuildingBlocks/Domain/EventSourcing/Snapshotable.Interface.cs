namespace Core.BuildingBlocks.Domain.EventSourcing;

/// <summary>Captures and restores an aggregate's state for snapshots.</summary>
public interface ISnapshotable
{
    /// <summary>Captures the current state as a serialized snapshot.</summary>
    string CaptureSnapshot();

    /// <summary>Restores state from a snapshot.</summary>
    /// <param name="state">The serialized state.</param>
    void RestoreSnapshot(string state);
}
