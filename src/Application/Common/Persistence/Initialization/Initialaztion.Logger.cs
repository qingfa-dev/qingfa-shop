using Microsoft.Extensions.Logging;

namespace Application.Common.Persistence.Initialization;

public static partial class PersistenceInitializerLoggers
{
    [LoggerMessage(
        EventId = 248,
        Level = LogLevel.Information,
        Message = "Starting database initialization.")]
    public static partial void Started(ILogger logger);

    [LoggerMessage(
        EventId = 249,
        Level = LogLevel.Information,
        Message = "Running {MigratorCount} database migrators.")]
    public static partial void RunningMigrators(
        ILogger logger, int migratorCount);

    [LoggerMessage(
        EventId = 250,
        Level = LogLevel.Information,
        Message = "Running {SeederCount} database seeders.")]
    public static partial void RunningSeeders(
        ILogger logger, int seederCount);

    [LoggerMessage(
        EventId = 251,
        Level = LogLevel.Information,
        Message = "Database initialization completed successfully.")]
    public static partial void Succeeded(ILogger logger);

    [LoggerMessage(
        EventId = 252,
        Level = LogLevel.Error,
        Message = "Database initialization failed with {ErrorCode}: {ErrorMessage}.")]
    public static partial void Failed(
        ILogger logger, string errorCode, string errorMessage);

    [LoggerMessage(
        EventId = 253,
        Level = LogLevel.Information,
        Message = "Database migrations skipped.")]
    public static partial void MigrationsSkipped(ILogger logger);

    [LoggerMessage(
        EventId = 254,
        Level = LogLevel.Information,
        Message = "Database seeding skipped.")]
    public static partial void SeedersSkipped(ILogger logger);

    [LoggerMessage(
        EventId = 255,
        Level = LogLevel.Critical,
        Message = "An unexpected exception escaped database initialization.")]
    public static partial void Unexpected(
        ILogger logger, Exception exception);
}