using Application.BuildingBlocks.UseCases.Validation.FluentValidation;

using FluentValidation;
using FluentValidation.Results;

namespace Application.Unit.Tests.BuildingBlocks.UseCases.Validation.FluentValidation;

/// <summary>Specification for <see cref="ValidationContextExtension"/>.</summary>
[Trait("Category", "Unit")]
public sealed class ValidationContextExtensionTest
{
    #region Test Doubles

    private sealed class TestRequest
    {
        public string Value { get; set; } = string.Empty;
    }

    private static ValidationResult Validate(Result result, string? propertyName = null)
    {
        var validator = new InlineValidator<TestRequest>();
        validator
            .RuleFor(request => request.Value)
            .Custom((_, context) => context.AddResultErrors(result, propertyName));

        return validator.Validate(new TestRequest());
    }

    private static Error ErrorWithField(string code, string message, string field) =>
        new Error(code, message).WithField(field);

    #endregion

    #region Result Conversion

    [Fact]
    public void AddResultErrors_SuccessfulResult_ShouldAddNoFailures()
    {
        // Act:
        var validation = Validate(Result.Ok());

        // Assert:
        validation.Errors.Should().BeEmpty();
    }

    [Fact]
    public void AddResultErrors_FailedResult_ShouldTranslateEveryError()
    {
        // Arrange:
        var result = Result.Fail(
            ErrorWithField("first.failed", "First failed.", "First"),
            ErrorWithField("second.failed", "Second failed.", "Second"));

        // Act:
        var validation = Validate(result);

        // Assert:
        validation.Errors.Should().HaveCount(2);
        validation.Errors.Select(failure => failure.PropertyName)
            .Should().ContainInOrder("First", "Second");
        validation.Errors.Select(failure => failure.ErrorCode)
            .Should().ContainInOrder("first.failed", "second.failed");
        validation.Errors.Select(failure => failure.ErrorMessage)
            .Should().ContainInOrder("First failed.", "Second failed.");
    }

    [Fact]
    public void AddResultErrors_ExplicitPropertyName_ShouldOverrideMetadataField()
    {
        // Arrange:
        var result = Result.Fail(
            ErrorWithField("todo.name.required", "Name is required.", "MetadataField"));

        // Act:
        var validation = Validate(result, propertyName: "Request.Name");

        // Assert:
        validation.Errors.Single().PropertyName.Should().Be("Request.Name");
    }

    [Fact]
    public void AddResultErrors_TypedResult_ShouldUseTypedOverload()
    {
        // Arrange:
        var error = ErrorWithField("todo.value.invalid", "Value is invalid.", "Value");
        var validator = new InlineValidator<TestRequest>();
        validator
            .RuleFor(request => request.Value)
            .Custom((_, context) => context.AddResultErrors(Result<int>.Fail(error)));

        // Act:
        var validation = validator.Validate(new TestRequest());

        // Assert:
        validation.Errors.Single().PropertyName.Should().Be("Value");
        validation.Errors.Single().ErrorCode.Should().Be("todo.value.invalid");
    }

    [Fact]
    public void AddResultErrors_TypedSuccessfulResult_ShouldAddNoFailures()
    {
        // Arrange:
        var validator = new InlineValidator<TestRequest>();
        validator
            .RuleFor(request => request.Value)
            .Custom((_, context) => context.AddResultErrors(Result<int>.Ok(42)));

        // Act:
        var validation = validator.Validate(new TestRequest());

        // Assert:
        validation.Errors.Should().BeEmpty();
    }

    #endregion

    #region Guards

    [Fact]
    public void AddResultErrors_NullContext_ShouldThrow()
    {
        // Act:
        Action act = () => ValidationContextExtension.AddResultErrors<TestRequest>(
            null!,
            Result.Ok());

        // Assert:
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void AddResultErrors_NullResult_ShouldThrow()
    {
        // Arrange:
        var context = new ValidationContext<TestRequest>(new TestRequest());

        // Act:
        Action act = () => context.AddResultErrors(null!);

        // Assert:
        act.Should().Throw<ArgumentNullException>().WithParameterName("result");
    }

    #endregion
}