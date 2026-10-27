using Microsoft.Extensions.Logging;

namespace Application.Common.Persistence.Initialization.Resetters;

public static partial class PersistenceSchemaResetterLoggers
{
    [LoggerMessage(
        EventId = 290,
        Level = LogLevel.Warning,
        Message = "Starting database schema reset.")]
    public static partial void Started(ILogger logger);

    [LoggerMessage(
        EventId = 291,
        Level = LogLevel.Warning,
        Message = "Dropping database schema {Schema}.")]
    public static partial void DroppingSchema(
        ILogger logger, string schema);

    [LoggerMessage(
        EventId = 292,
        Level = LogLevel.Information,
        Message = "Database schema reset completed successfully.")]
    public static partial void Succeeded(ILogger logger);

    [LoggerMessage(
        EventId = 293,
        Level = LogLevel.Error,
        Message = "Database schema reset failed for {Schema}.")]
    public static partial void Failed(
        ILogger logger, string schema, Exception exception);

    [LoggerMessage(
        EventId = 294,
        Level = LogLevel.Error,
        Message = "Database migration history table reset failed.")]
    public static partial void HistoryTableFailed(
        ILogger logger, Exception exception);
}