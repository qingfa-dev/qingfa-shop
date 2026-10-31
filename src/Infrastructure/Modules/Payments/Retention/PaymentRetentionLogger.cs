namespace Infrastructure.Modules.Payments.Retention;

/// <summary>Source-generated log messages and identifiers for the retention worker.</summary>
internal static partial class PaymentRetentionLoggers
{
    /// <summary>Logs a completed sweep that removed rows.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">Rows removed.</param>
    /// <param name="cutoff">The cutoff used.</param>
    [LoggerMessage(
        EventId = PaymentRetentionConstant.EventId.Pruned,
        Level = LogLevel.Information,
        Message = "Pruned {Count} processed webhook deliveries received before {Cutoff}.")]
    public static partial void Pruned(ILogger logger, int count, DateTimeOffset cutoff);

    /// <summary>Logs a failed sweep.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reason">Why the sweep failed.</param>
    [LoggerMessage(
        EventId = PaymentRetentionConstant.EventId.SweepFailed,
        Level = LogLevel.Warning,
        Message = "Webhook retention sweep failed: {Reason}")]
    public static partial void SweepFailed(ILogger logger, string reason);
}
