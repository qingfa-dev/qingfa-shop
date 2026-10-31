using Microsoft.Extensions.Logging;

namespace Application.Unit.Tests.BuildingBlocks.UseCases.Mediation.Fixtures;

/// <summary>
/// Provides extension methods for verifying log calls on mocked ILogger instances in unit tests.
/// </summary>
public static class TestLoggerExtension
{
    /// <summary>
    /// Verifies that a logger was called with the specified log level any number of times.
    /// </summary>
    /// <typeparam name="T">The logger category type.</typeparam>
    /// <param name="loggerMock">The mocked logger instance.</param>
    /// <param name="level">The expected LogLevel to verify.</param>
    /// <param name="times">The expected number of invocations.</param>
    /// <summary>
    /// Verifies that a logger was called with the specified log level any number of times.
    /// </summary>
    /// <typeparam name="T">The logger category type.</typeparam>
    /// <param name="loggerMock">The mocked logger instance.</param>
    /// <param name="level">The expected LogLevel to verify.</param>
    /// <param name="times">The expected number of invocations.</param>
    public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, LogLevel level, Times times)
    {
        EventId eventId = It.IsAny<EventId>();
        It.IsAnyType state = It.IsAny<It.IsAnyType>();
        Exception exception = It.IsAny<Exception>();
        Func<It.IsAnyType, Exception?, string> func = It.IsAny<Func<It.IsAnyType, Exception?, string>>();

        loggerMock.Verify(
            x => x.Log(
                level,
                eventId,
                state,
                exception,
                func),
            times);
    }

    public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, LogLevel level, int eventId, Times times)
    {
        EventId eventIdMatcher = It.Is<EventId>(e => e.Id == eventId);
        It.IsAnyType state = It.IsAny<It.IsAnyType>();
        Exception exception = It.IsAny<Exception>();
        Func<It.IsAnyType, Exception?, string> func = It.IsAny<Func<It.IsAnyType, Exception?, string>>();

        loggerMock.Verify(
            x => x.Log(
                level,
                eventIdMatcher,
                state,
                exception,
                func),
            times);
    }
}