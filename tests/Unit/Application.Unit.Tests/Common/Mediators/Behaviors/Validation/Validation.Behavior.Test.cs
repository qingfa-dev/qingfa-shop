using Application.Common.Mediators.Behaviors.Validation;
using Application.Unit.Tests.Common.Mediators.Fixtures;

using FluentValidation;
using FluentValidation.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Application.Unit.Tests.Common.Mediators.Behaviors.Validation;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Mediators")]
public class ValidationBehaviorTests
{
    private static ValidationBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>>? logger = null)
        where TRequest : IRequest<TResponse>
        where TResponse : IResult<Error>
    {
        return new ValidationBehavior<TRequest, TResponse>(
            validators,
            logger ?? new TestLogger<ValidationBehavior<TRequest, TResponse>>());
    }

    #region Handle - No Validators

    [Fact(DisplayName = "Should log information and call next when no validators are registered")]
    public async Task Handle_NoValidators_ReturnsSuccess()
    {
        var logger = new TestLogger<ValidationBehavior<TestRequest, Result>>();
        IEnumerable<IValidator<TestRequest>> validators = Enumerable.Empty<IValidator<TestRequest>>();
        ValidationBehavior<TestRequest, Result> behavior = CreateBehavior(validators, logger);
        var request = new TestRequest { Value = "test-value" };
        var nextCalled = false;

        RequestHandlerDelegate<Result> next = (_) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Ok());
        };

        Result result = await behavior.Handle(request, next, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        nextCalled.Should().BeTrue();

        logger.Entries.Should().Contain(e => e.Level == LogLevel.Information && e.Message.Contains("No validators found for command TestRequest"));
    }

    #endregion

    #region Handle - All Validators Pass

    [Fact(DisplayName = "Should proceed to next when all validators pass")]
    public async Task Handle_AllValidatorsPass_ReturnsSuccess()
    {
        var validatorMock = new Mock<IValidator<TestRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), TestContext.Current.CancellationToken))
            .ReturnsAsync(new ValidationResult());
        IValidator<TestRequest>[] validators = [validatorMock.Object];
        ValidationBehavior<TestRequest, Result> behavior = CreateBehavior<TestRequest, Result>(validators);
        var request = new TestRequest { Value = "test-value" };
        var nextCalled = false;

        RequestHandlerDelegate<Result> next = (_) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Ok());
        };

        Result result = await behavior.Handle(request, next, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        nextCalled.Should().BeTrue();
    }

    #endregion

    #region Handle - Validators Fail

    [Fact(DisplayName = "Should return failure and log warning when validation fails")]
    public async Task Handle_ValidatorsFail_ReturnsFailure()
    {
        var logger = new TestLogger<ValidationBehavior<TestRequest, Result>>();

        var validationErrors = new List<ValidationFailure>
        {
            new("Value", "Value is required"),
            new("Value", "Value must not be empty")
        };
        var validatorMock = new Mock<IValidator<TestRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), TestContext.Current.CancellationToken))
            .ReturnsAsync(new ValidationResult(validationErrors));
        IValidator<TestRequest>[] validators = [validatorMock.Object];
        ValidationBehavior<TestRequest, Result> behavior = CreateBehavior(validators, logger);
        var request = new TestRequest { Value = "" };

        RequestHandlerDelegate<Result> next = (_) => Task.FromResult(Result.Ok());

        Result result = await behavior.Handle(request, next, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(422);
        result.Errors.Should().HaveCount(2);

        logger.Entries.Should().Contain(e => e.Level == LogLevel.Warning && e.Message.Contains("Validation failed for command TestRequest. Errors: Value: Value is required, Value: Value must not be empty"));
    }

    [Fact(DisplayName = "Should return failure with Result<T> when validation fails")]
    public async Task Handle_ValidatorsFail_WithGenericResult_ReturnsFailure()
    {
        // Arrange
        var validationErrors = new List<ValidationFailure> { new("Data", "Invalid data") };
        var validatorMock = new Mock<IValidator<TestRequestWithValue>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequestWithValue>>(), TestContext.Current.CancellationToken))
            .ReturnsAsync(new ValidationResult(validationErrors));

        ValidationBehavior<TestRequestWithValue, Result<string>> behavior = CreateBehavior<TestRequestWithValue, Result<string>>([validatorMock.Object]);
        var request = new TestRequestWithValue { Value = "data" };
        var nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = (_) => { nextCalled = true; return Task.FromResult(Result<string>.Ok("fail")); };

        // Act
        Result<string> result = await behavior.Handle(request, next, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0]!.Code.Should().Be("Data");
        nextCalled.Should().BeFalse();
    }

    #endregion

    #region Handle - Single Validation Failure

    [Fact(DisplayName = "Should use property name as error code when validation fails")]
    public async Task Handle_SingleValidationFailure_ReturnsFailureWithCorrectCode()
    {
        var validationErrors = new List<ValidationFailure>
        {
            new("Value", "Value is required")
        };
        var validatorMock = new Mock<IValidator<TestRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), TestContext.Current.CancellationToken))
            .ReturnsAsync(new ValidationResult(validationErrors));
        IValidator<TestRequest>[] validators = [validatorMock.Object];
        ValidationBehavior<TestRequest, Result> behavior = CreateBehavior<TestRequest, Result>(validators);
        var request = new TestRequest { Value = "" };

        RequestHandlerDelegate<Result> next = (_) => Task.FromResult(Result.Ok());

        Result result = await behavior.Handle(request, next, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Errors[0]!.Code.Should().Be("Value");
        result.Errors[0]!.Message.Should().Be("Value is required");
    }

    #endregion

    #region Handle - Empty Validation Result

    [Fact(DisplayName = "Should proceed to next when validation result contains no errors")]
    public async Task Handle_EmptyValidationResult_ReturnsSuccess()
    {
        var validatorMock = new Mock<IValidator<TestRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), TestContext.Current.CancellationToken))
            .ReturnsAsync(new ValidationResult(new List<ValidationFailure>()));
        IValidator<TestRequest>[] validators = [validatorMock.Object];
        ValidationBehavior<TestRequest, Result> behavior = CreateBehavior<TestRequest, Result>(validators);
        var request = new TestRequest { Value = "valid" };
        var nextCalled = false;

        RequestHandlerDelegate<Result> next = (_) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Ok());
        };

        Result result = await behavior.Handle(request, next, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        nextCalled.Should().BeTrue();
    }

    #endregion

    #region Handle - Multiple Validators

    [Fact(DisplayName = "Should combine failures from multiple validators")]
    public async Task Handle_MultipleValidatorsWithFailures_ReturnsCombinedFailures()
    {
        var validator1Mock = new Mock<IValidator<TestRequest>>();
        validator1Mock.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), TestContext.Current.CancellationToken))
            .ReturnsAsync(new ValidationResult(new List<ValidationFailure>
            {
                new("Value", "First validator error")
            }));

        var validator2Mock = new Mock<IValidator<TestRequest>>();
        validator2Mock.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), TestContext.Current.CancellationToken))
            .ReturnsAsync(new ValidationResult(new List<ValidationFailure>
            {
                new("Value", "Second validator error")
            }));

        IValidator<TestRequest>[] validators = [validator1Mock.Object, validator2Mock.Object];
        ValidationBehavior<TestRequest, Result> behavior = CreateBehavior<TestRequest, Result>(validators);
        var request = new TestRequest { Value = "" };

        RequestHandlerDelegate<Result> next = (_) => Task.FromResult(Result.Ok());

        Result result = await behavior.Handle(request, next, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
    }

    #endregion

    public record TestRequestWithValue : IRequest<Result<string>>
    {
        public string Value { get; init; } = default!;
    }
}