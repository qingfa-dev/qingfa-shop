using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.EntityFramework.Interceptors;

/// <summary>Structured log events for domain-event publication.</summary>
internal static partial class DomainEventInterceptorLoggers
{
    [LoggerMessage(
        EventId = 210,
        Level = LogLevel.Debug,
        Message = "Publishing {Count} domain event(s) after a successful save.")]
    public static partial void Publishing(ILogger logger, int count);

    [LoggerMessage(
        EventId = 211,
        Level = LogLevel.Error,
        Message = "Failed to publish {Count} domain event(s): {ErrorCode} - {ErrorMessage}.")]
    public static partial void PublishFailed(
        ILogger logger,
        int count,
        string errorCode,
        string errorMessage);
}
