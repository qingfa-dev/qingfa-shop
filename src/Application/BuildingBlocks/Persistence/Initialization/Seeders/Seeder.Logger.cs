using Microsoft.Extensions.Logging;

namespace Application.BuildingBlocks.Persistence.Initialization.Seeders;

public static partial class PersistenceSeederLoggers
{
    [LoggerMessage(
        EventId = 300,
        Level = LogLevel.Information,
        Message = "Starting seeder {SeederName} with order {Order}.")]
    public static partial void Started(
        ILogger logger, string seederName, int order);

    [LoggerMessage(
        EventId = 301,
        Level = LogLevel.Information,
        Message = "Seeder {SeederName} completed successfully.")]
    public static partial void Succeeded(
        ILogger logger, string seederName);

    [LoggerMessage(
        EventId = 302,
        Level = LogLevel.Warning,
        Message = "Seeder {SeederName} returned failure {ErrorCode}: {ErrorMessage}.")]
    public static partial void ReturnedFailure(
        ILogger logger, string seederName,
        string errorCode, string errorMessage);

    [LoggerMessage(
        EventId = 303,
        Level = LogLevel.Error,
        Message = "Seeder {SeederName} threw an exception.")]
    public static partial void Threw(
        ILogger logger, string seederName, Exception exception);

    [LoggerMessage(
        EventId = 304,
        Level = LogLevel.Information,
        Message = "{EntityName} already seeded ({Count} found). Skipping.")]
    public static partial void AlreadySeeded(
        ILogger logger, string entityName, int count);

    [LoggerMessage(
        EventId = 305,
        Level = LogLevel.Information,
        Message = "Seeded {Count} {EntityName}.")]
    public static partial void Seeded(
        ILogger logger, int count, string entityName);
}