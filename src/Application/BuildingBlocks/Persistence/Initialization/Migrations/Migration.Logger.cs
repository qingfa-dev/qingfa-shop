using Microsoft.Extensions.Logging;

namespace Application.BuildingBlocks.Persistence.Initialization.Migrations;

public static partial class PersistenceMigratorLoggers
{
    [LoggerMessage(
        EventId = 280,
        Level = LogLevel.Information,
        Message = "Applying migrations for {MigratorName}.")]
    public static partial void Applying(
        ILogger logger, string migratorName);

    [LoggerMessage(
        EventId = 281,
        Level = LogLevel.Information,
        Message = "Migration attempt {Attempt} of {MaxAttempts} for {MigratorName}.")]
    public static partial void Attempt(
        ILogger logger, string migratorName, int attempt, int maxAttempts);

    [LoggerMessage(
        EventId = 282,
        Level = LogLevel.Information,
        Message = "Migrations succeeded for {MigratorName} on attempt {Attempt}.")]
    public static partial void Succeeded(
        ILogger logger, string migratorName, int attempt);

    [LoggerMessage(
        EventId = 283,
        Level = LogLevel.Warning,
        Message = "Transient migration failure for {MigratorName} on attempt {Attempt}; retrying in {DelayMs}ms.")]
    public static partial void Retrying(
        ILogger logger, string migratorName, int attempt, int delayMs,
        Exception exception);

    [LoggerMessage(
        EventId = 284,
        Level = LogLevel.Error,
        Message = "Migration failed for {MigratorName} on attempt {Attempt}.")]
    public static partial void Failed(
        ILogger logger, string migratorName, int attempt,
        Exception exception);

    [LoggerMessage(
        EventId = 285,
        Level = LogLevel.Error,
        Message = "The migration adapter failed to classify an exception for {MigratorName}.")]
    public static partial void ClassificationFailed(
        ILogger logger, string migratorName, Exception exception);

    [LoggerMessage(
        EventId = 286,
        Level = LogLevel.Information,
        Message = "No pending migrations for '{MigratorName}'.")]
    public static partial void NoPendingMigrations(
        ILogger logger, string migratorName);

    [LoggerMessage(
        EventId = 287,
        Level = LogLevel.Information,
        Message = "Applying {Count} pending migration(s) for '{MigratorName}'.")]
    public static partial void ApplyingPendingMigrations(
        ILogger logger, int count, string migratorName);

    [LoggerMessage(
        EventId = 288,
        Level = LogLevel.Error,
        Message = "Migration failed for '{MigratorName}' after {Attempts} attempts.")]
    public static partial void FailedAfterAttempts(
        ILogger logger, string migratorName, int attempts, Exception? exception);
}