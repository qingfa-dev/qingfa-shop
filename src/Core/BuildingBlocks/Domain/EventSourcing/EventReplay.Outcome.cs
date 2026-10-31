namespace Core.BuildingBlocks.Domain.EventSourcing;

/// <summary>Stable failures produced while replaying an event-sourced aggregate.</summary>
public static class EventReplayOutcome
{
    /// <summary>Failure factories.</summary>
    public static class Failure
    {
        /// <summary>History was already loaded or the aggregate has uncommitted events.</summary>
        public static Error AlreadyLoaded => Error.Conflict(
            Codes.AlreadyLoaded,
            "History can only be loaded once, before the aggregate raises new events.");

        /// <summary>The supplied stream version is negative or below the supplied event count.</summary>
        /// <param name="version">The supplied stream version.</param>
        /// <param name="eventCount">The number of events supplied.</param>
        /// <returns>A validation error describing the mismatch.</returns>
        public static Error VersionInvalid(long version, int eventCount) => Error.Validation(
            Codes.VersionInvalid,
            $"Stream version {version} cannot be below the {eventCount} supplied event(s).");

        /// <summary>An event could not be applied by the aggregate.</summary>
        /// <param name="eventType">The event type that failed.</param>
        /// <param name="reason">The failure reason.</param>
        /// <returns>An internal error describing the unsupported event.</returns>
        public static Error ApplyFailed(string eventType, string reason) => Error.InternalServerError(
            Codes.ApplyFailed,
            $"Event '{eventType}' could not be applied during replay: {reason}");
    }

    /// <summary>Stable error codes.</summary>
    public static class Codes
    {
        public const string AlreadyLoaded = "EventSourcing.History.AlreadyLoaded";
        public const string VersionInvalid = "EventSourcing.History.VersionInvalid";
        public const string ApplyFailed = "EventSourcing.History.ApplyFailed";
    }
}
