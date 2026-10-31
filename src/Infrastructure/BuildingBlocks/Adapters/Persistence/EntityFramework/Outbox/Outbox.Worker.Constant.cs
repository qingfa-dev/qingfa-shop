namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;

/// <summary>Stable log event identifiers for the outbox worker.</summary>
public static class OutboxWorkerConstant
{
    /// <summary>Event IDs used by <c>OutboxWorkerLoggers</c>.</summary>
    public static class LogEventIds
    {
        /// <summary>A dispatch attempt failed.</summary>
        public const int DispatchFailed = 234;

        /// <summary>An iteration failed unexpectedly.</summary>
        public const int Unhandled = 235;

        /// <summary>A retention sweep failed.</summary>
        public const int RetentionFailed = 236;
    }
}
