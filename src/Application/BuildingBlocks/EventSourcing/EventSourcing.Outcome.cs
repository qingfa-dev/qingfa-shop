namespace Application.BuildingBlocks.EventSourcing;

/// <summary>Stable failures for event-store, snapshot and projection operations.</summary>
public static class EventSourcingOutcome
{
    /// <summary>Failure factories.</summary>
    public static class Failure
    {
        public static Error AppendConflict(string streamId, long expectedVersion) => Error.Conflict(
            Codes.AppendConflict,
            $"Stream '{streamId}' is not at expected version {expectedVersion}.");

        public static Error AppendFailed(string streamId, string reason) => Error.InternalServerError(
            Codes.AppendFailed, $"Stream '{streamId}' could not be appended: {reason}");

        public static Error ReadFailed(string streamId, string reason) => Error.InternalServerError(
            Codes.ReadFailed, $"Stream '{streamId}' could not be read: {reason}");

        public static Error SerializeFailed(string reason) => Error.InternalServerError(
            Codes.SerializeFailed, $"The event could not be serialized: {reason}");

        public static Error DeserializeFailed(string reason) => Error.InternalServerError(
            Codes.DeserializeFailed, $"The event could not be deserialized: {reason}");

        public static Error UpcastMissing(string eventName, int version) => Error.InternalServerError(
            Codes.UpcastMissing, $"No upcaster bridges '{eventName}' version {version}.");

        public static Error SnapshotReadFailed(string streamId, string reason) => Error.InternalServerError(
            Codes.SnapshotReadFailed, $"Snapshot for '{streamId}' could not be read: {reason}");

        public static Error SnapshotWriteFailed(string streamId, string reason) => Error.InternalServerError(
            Codes.SnapshotWriteFailed, $"Snapshot for '{streamId}' could not be written: {reason}");

        public static Error ProjectionApplyFailed(string projection, string reason) => Error.InternalServerError(
            Codes.ProjectionApplyFailed, $"Projection '{projection}' could not apply an event: {reason}");

        public static Error ProjectionCheckpointFailed(string projection, string reason) => Error.InternalServerError(
            Codes.ProjectionCheckpointFailed, $"Projection '{projection}' could not save its checkpoint: {reason}");

        public static Error StreamNotFound(string streamId) => Error.NotFound(
            Codes.StreamNotFound, $"Stream '{streamId}' was not found.");
    }

    /// <summary>Stable error codes.</summary>
    public static class Codes
    {
        public const string AppendConflict = "EventStore.Append.Conflict";
        public const string AppendFailed = "EventStore.Append.Failed";
        public const string ReadFailed = "EventStore.Read.Failed";
        public const string SerializeFailed = "EventStore.Serialize.Failed";
        public const string DeserializeFailed = "EventStore.Deserialize.Failed";
        public const string UpcastMissing = "EventStore.Upcast.Missing";
        public const string SnapshotReadFailed = "Snapshot.Read.Failed";
        public const string SnapshotWriteFailed = "Snapshot.Write.Failed";
        public const string ProjectionApplyFailed = "Projection.Apply.Failed";
        public const string ProjectionCheckpointFailed = "Projection.Checkpoint.Failed";
        public const string StreamNotFound = "Repository.Stream.NotFound";
    }
}
