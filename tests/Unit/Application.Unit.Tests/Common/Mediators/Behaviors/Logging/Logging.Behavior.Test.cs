using Application.Common.Mediators.Behaviors.Logging;
using Application.Unit.Tests.Common.Mediators.Fixtures;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Application.Unit.Tests.Common.Mediators.Behaviors.Logging;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Mediators")]
public class LoggingBehaviorTests
{
    #region Handle - Logs Request Start

    [Fact(DisplayName = "Should log start and success when request succeeds")]
    public async Task Handle_SuccessfulRequest_LogsStartAndSuccess()
    {
        var logger = new TestLogger<LoggingBehavior<TestRequest, Result>>();
        var behavior = new LoggingBehavior<TestRequest, Result>(logger);
        var request = new TestRequest { Data = "test-value" };

        RequestHandlerDelegate<Result> next = (_) =>
            Task.FromResult(Result.Ok());

        Result result = await behavior.Handle(request, next, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();

        logger.Entries.Should().HaveCount(2);
        logger.Entries.Should().Contain(e => e.Level == LogLevel.Debug && e.Message.Contains("Handling request TestRequest"));
        logger.Entries.Should().Contain(e => e.Level == LogLevel.Debug && e.Message.Contains("Request TestRequest succeeded"));
    }

    [Fact(DisplayName = "Should log start and success when Result<T> request succeeds")]
    public async Task Handle_SuccessfulGenericRequest_LogsStartAndSuccess()
    {
        // Arrange
        var logger = new TestLogger<LoggingBehavior<TestRequestWithGenericResult, Result<string>>>();
        var behavior = new LoggingBehavior<TestRequestWithGenericResult, Result<string>>(logger);
        var request = new TestRequestWithGenericResult();

        RequestHandlerDelegate<Result<string>> next = (_) =>
            Task.FromResult(Result<string>.Ok("success-data"));

        // Act
        Result<string> result = await behavior.Handle(request, next, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();

        logger.Entries.Should().HaveCount(2);
        logger.Entries.Should().Contain(e => e.Level == LogLevel.Debug && e.Message.Contains("Handling request TestRequestWithGenericResult"));
        logger.Entries.Should().Contain(e => e.Level == LogLevel.Debug && e.Message.Contains("Request TestRequestWithGenericResult succeeded"));
    }

    #endregion

    #region Handle - Logs Failure

    [Fact(DisplayName = "Should log start and failure when request fails")]
    public async Task Handle_FailedRequest_LogsStartAndFailure()
    {
        var logger = new TestLogger<LoggingBehavior<TestRequest, Result>>();
        var behavior = new LoggingBehavior<TestRequest, Result>(logger);
        var request = new TestRequest { Data = "test-value" };
        var failure = Error.BadRequest("Test.Code", "Test error description");

        RequestHandlerDelegate<Result> next = (_) =>
            Task.FromResult(Result.Failure([failure], 400));

        Result result = await behavior.Handle(request, next, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();

        logger.Entries.Should().HaveCount(2);
        logger.Entries.Should().Contain(e => e.Level == LogLevel.Debug && e.Message.Contains("Handling request TestRequest"));
        logger.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Message.Contains("Request TestRequest failed with Errors: Test error description"));
    }

    #endregion

    #region Handle - Request Content in Log

    [Fact(DisplayName = "Should include request type name in log messages")]
    public async Task Handle_AnyRequest_LogsRequestTypeName()
    {
        var logger = new TestLogger<LoggingBehavior<TestRequest, Result>>();
        var behavior = new LoggingBehavior<TestRequest, Result>(logger);
        var request = new TestRequest { Data = "test-value" };

        RequestHandlerDelegate<Result> next = (_) =>
            Task.FromResult(Result.Ok());

        await behavior.Handle(request, next, TestContext.Current.CancellationToken);

        logger.Entries.Any(e => e.Message.Contains("TestRequest")).Should().BeTrue();
    }

    #endregion

    public record TestRequest : IRequest<Result>
    {
        public string Data { get; init; } = default!;
    }
    public record TestRequestWithGenericResult : IRequest<Result<string>>;
}