using Microsoft.Extensions.Logging;

namespace Application.Common.Mediators.Behaviors.Logging;

public sealed partial class LoggingBehavior<TRequest, TResponse>
{
    internal static partial class Logger
    {
        [LoggerMessage(
            EventId = 116,
            Level = LogLevel.Debug,
            Message = "Handling request {RequestName}")]
        public static partial void HandlingRequest(ILogger logger, string requestName);

        [LoggerMessage(
            EventId = 117,
            Level = LogLevel.Error,
            Message = "Request {RequestName} failed with Errors: {Errors}")]
        public static partial void RequestFailed(ILogger logger, string requestName, string errors);

        [LoggerMessage(
            EventId = 118,
            Level = LogLevel.Debug,
            Message = "Request {RequestName} succeeded")]
        public static partial void RequestSucceeded(ILogger logger, string requestName);
    }
}