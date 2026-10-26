using Microsoft.Extensions.Logging;

namespace Application.Common.Mediators.Behaviors.Exceptions;

public sealed partial class ExceptionMappingBehavior<TRequest, TResponse>
{
    internal static partial class Logger
    {
        [LoggerMessage(
            EventId = 119,
            Level = LogLevel.Error,
            Message = "Unhandled exception while handling {RequestType}")]
        public static partial void UnhandledException(ILogger logger, string requestType, Exception exception);
    }
}