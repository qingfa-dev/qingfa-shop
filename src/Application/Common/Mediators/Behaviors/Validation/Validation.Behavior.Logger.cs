using Microsoft.Extensions.Logging;

namespace Application.Common.Mediators.Behaviors.Validation;

public sealed partial class ValidationBehavior<TRequest, TResponse>
{
    internal static partial class Logger
    {
        [LoggerMessage(
            EventId = 101,
            Level = LogLevel.Information,
            Message = "No validators found for command {CommandType}")]
        public static partial void NoValidators(ILogger logger, string commandType);

        [LoggerMessage(
            EventId = 102,
            Level = LogLevel.Warning,
            Message = "Validation failed for command {CommandType}. Errors: {Errors}")]
        public static partial void ValidationFailed(ILogger logger, string commandType, string errors);
    }
}