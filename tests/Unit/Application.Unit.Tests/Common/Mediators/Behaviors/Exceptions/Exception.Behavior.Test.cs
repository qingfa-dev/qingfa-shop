using Application.Common.Mediators.Behaviors.Exceptions;
using Application.Unit.Tests.Common.Mediators.Fixtures;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Application.Unit.Tests.Common.Mediators.Behaviors.Exceptions;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Mediators")]
public class ExceptionMappingBehaviorTests
{
    private readonly TestLogger<ExceptionMappingBehavior<TestRequest, Result>> _logger = new();

    private static ExceptionMappingBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        ILogger<ExceptionMappingBehavior<TRequest, TResponse>> logger)
        where TRequest : IRequest<TResponse>
        where TResponse : IResult<Error>
    {
        return new ExceptionMappingBehavior<TRequest, TResponse>(logger);
    }

    #region Handle - No Exception

    [Fact(DisplayName = "Should return success when no exception is thrown")]
    public async Task Handle_NoException_ReturnsSuccess()
    {
        var behavior = CreateBehavior(_logger);
        var request = new TestRequest { Value = "test-value" };

        RequestHandlerDelegate<Result> next = (_) => Task.FromResult(Result.Ok());

        Result result = await behavior.Handle(request, next, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _logger.Entries.Should().BeEmpty();
    }

    #endregion

    #region Handle - Exception Caught

    [Fact(DisplayName = "Should propagate cancellation instead of returning a failure")]
    public async Task Handle_Cancellation_Propagates()
    {
        var behavior = CreateBehavior(_logger);
        var request = new TestRequest { Value = "test-value" };

        RequestHandlerDelegate<Result> next = _ =>
            throw new OperationCanceledException();

        Func<Task> handle = () =>
            behavior.Handle(request, next, TestContext.Current.CancellationToken);

        await handle.Should().ThrowAsync<OperationCanceledException>();
        _logger.Entries.Should().BeEmpty();
    }

    [Fact(DisplayName = "Should return failure when an exception is thrown")]
    public async Task Handle_ExceptionThrown_ReturnsFailure()
    {
        var behavior = CreateBehavior(_logger);
        var request = new TestRequest { Value = "test-value" };
        var exception = new InvalidOperationException("Test exception");

        RequestHandlerDelegate<Result> next = (_) =>
            throw exception;

        Result result = await behavior.Handle(request, next, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Errors[0]!.Code.Should().Be("TestRequest.Unexpected");
        result.Errors[0].Message.Should().Contain("An unhandled exception occurred while processing TestRequest.");
    }

    [Fact(DisplayName = "Should return failure with Result<T> when an exception is thrown")]
    public async Task Handle_ExceptionThrown_WithGenericResult_ReturnsFailure()
    {
        // Arrange
        var logger = new TestLogger<ExceptionMappingBehavior<TestRequestWithGenericResult, Result<string>>>();
        var behavior = CreateBehavior(logger);
        var request = new TestRequestWithGenericResult();
        var exception = new InvalidOperationException("Critical failure");

        RequestHandlerDelegate<Result<string>> next = (_) => throw exception;

        // Act
        Result<string> result = await behavior.Handle(request, next, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("TestRequestWithGenericResult.Unexpected");

        logger.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Message.Contains("Unhandled exception while handling TestRequestWithGenericResult") && e.Exception == exception);
    }

    #endregion

    #region Handle - Exception Logged

    [Fact(DisplayName = "Should log error with correct event ID and message when exception is thrown")]
    public async Task Handle_ExceptionThrown_LogsError()
    {
        var behavior = CreateBehavior(_logger);
        var request = new TestRequest { Value = "test-value" };
        var exception = new InvalidOperationException("Test exception");

        RequestHandlerDelegate<Result> next = (_) =>
            throw exception;

        await behavior.Handle(request, next, TestContext.Current.CancellationToken);

        var log = _logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error).Subject;
        log.Message.Should().Contain("Unhandled exception while handling TestRequest");
        log.EventId.Id.Should().Be(119);
        log.Exception.Should().Be(exception);
    }

    [Fact(DisplayName = "Should NOT log error when LogLevel.Error is disabled")]
    public async Task Handle_ExceptionThrown_LoggerDisabled_DoesNotLog()
    {
        // Arrange
        _logger.SetEnabled(LogLevel.Error, false);
        var behavior = CreateBehavior(_logger);
        var request = new TestRequest { Value = "test-value" };
        var exception = new InvalidOperationException("Test exception");

        RequestHandlerDelegate<Result> next = (_) => throw exception;

        // Act
        await behavior.Handle(request, next, TestContext.Current.CancellationToken);

        // Assert
        _logger.Entries.Should().BeEmpty();
    }

    #endregion

    #region Handle - Different Exception Types

    [Theory(DisplayName = "Should return failure for various exception types")]
    [InlineData(typeof(ArgumentNullException))]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(NotImplementedException))]
    public async Task Handle_VariousExceptionTypes_ReturnsFailure(Type exceptionType)
    {
        var behavior = CreateBehavior(_logger);
        var request = new TestRequest { Value = "test-value" };
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        RequestHandlerDelegate<Result> next = (_) =>
            throw exception;

        Result result = await behavior.Handle(request, next, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Errors[0]!.Code.Should().Be("TestRequest.Unexpected");
    }

    #endregion

    public record TestRequestWithGenericResult : IRequest<Result<string>>;
}