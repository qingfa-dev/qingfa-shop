namespace Infrastructure.BuildingBlocks.Adapters.Events;

/// <summary>Stable log event identifiers for the no-op event bus.</summary>
public static class NoOpEventBusConstant
{
    /// <summary>Event IDs used by <c>NoOpEventBusLoggers</c>.</summary>
    public static class LogEventIds
    {
        /// <summary>An integration event was accepted without a transport.</summary>
        public const int Accepted = 222;
    }
}
