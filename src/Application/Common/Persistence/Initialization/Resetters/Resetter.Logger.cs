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

    [LoggerMessage(
        EventId = 295,
        Level = LogLevel.Warning,
        Message = "Starting database reset with scope '{Scope}'.")]
    public static partial void StartedWithScope(
        ILogger logger, string scope);

    [LoggerMessage(
        EventId = 296,
        Level = LogLevel.Information,
        Message = "Database reset completed successfully for '{ResetterName}'.")]
    public static partial void Completed(
        ILogger logger, string resetterName);

    [LoggerMessage(
        EventId = 297,
        Level = LogLevel.Error,
        Message = "Database reset failed for '{ResetterName}'.")]
    public static partial void ResetFailed(
        ILogger logger, string resetterName, Exception exception);

    [LoggerMessage(
        EventId = 298,
        Level = LogLevel.Warning,
        Message = "Dropping and recreating database schema.")]
    public static partial void DroppingRecreatingSchema(
        ILogger logger);

    [LoggerMessage(
        EventId = 299,
        Level = LogLevel.Information,
        Message = "Database schema recreated.")]
    public static partial void SchemaRecreated(
        ILogger logger);

    [LoggerMessage(
        EventId = 310,
        Level = LogLevel.Information,
        Message = "Truncated table '{Table}'.")]
    public static partial void TruncatedTable(
        ILogger logger, string table);
}