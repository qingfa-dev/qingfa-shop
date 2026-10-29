using Application.Common.Validation.FluentValidation;

using Core.Common.Monad.Metadata;

using FluentValidation;
using FluentValidation.Results;

namespace Application.Unit.Tests.Common.Validation.FluentValidation;

/// <summary>Specification for <see cref="RuleBuilderExtension"/>.</summary>
[Trait("Category", "Unit")]
public sealed class RuleBuilderExtensionTest
{
    #region Test Doubles

    private sealed class TestRequest
    {
        public string? Name { get; set; }
    }

    private sealed class TestValidator : AbstractValidator<TestRequest>
    {
        public TestValidator(Func<string?, Result> validate)
        {
            RuleFor(request => request.Name).ValidateWith(validate);
        }
    }

    private sealed class ChainedValidator : AbstractValidator<TestRequest>
    {
        public ChainedValidator()
        {
            RuleFor(request => request.Name)
                .NotEmpty()
                .ValidateWith(_ => Result.Success())
                .MaximumLength(10);
        }
    }

    private static ValidationResult Validate(
        Func<string?, Result> validate,
        string? value = null)
        => new TestValidator(validate).Validate(new TestRequest { Name = value });

    #endregion

    #region ValidateWith

    [Fact]
    public void ValidateWith_SuccessfulRule_ShouldAddNoFailures()
    {
        // Act:
        var validation = Validate(_ => Result.Success(), value: "valid");

        // Assert:
        validation.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateWith_FailedRule_ShouldPreserveErrorsAndPropertyPath()
    {
        // Arrange:
        var errors = new[]
        {
            new Error("test.required", "The value is required."),
            new Error("test.invalid", "The value is invalid.")
        };

        // Act:
        var validation = Validate(_ => Result.Fail(errors), value: "invalid");

        // Assert:
        validation.Errors.Should().HaveCount(2);
        validation.Errors.Select(failure => failure.PropertyName)
            .Should().OnlyContain(propertyName => propertyName == "Name");
        validation.Errors.Select(failure => failure.ErrorCode)
            .Should().ContainInOrder("test.required", "test.invalid");

        var state = validation.Errors[0].CustomState
            .Should().BeOfType<ValidationErrorState>().Subject;
        state.Metadata[ErrorConstant.Metadata.Field].Should().Be("Name");
        state.Error.Code.Should().Be("test.required");
    }

    [Fact]
    public void ValidateWith_ShouldRemainChainable()
    {
        // Act:
        var valid = new ChainedValidator().Validate(new TestRequest { Name = "valid" });
        var tooLong = new ChainedValidator().Validate(
            new TestRequest { Name = new string('a', 11) });

        // Assert:
        valid.Errors.Should().BeEmpty();
        tooLong.Errors.Should().ContainSingle()
            .Which.ErrorCode.Should().Be("MaximumLengthValidator");
    }

    #endregion

    #region Guards

    [Fact]
    public void ValidateWith_NullRuleBuilder_ShouldThrow()
    {
        // Act:
        Action act = () => RuleBuilderExtension.ValidateWith<TestRequest, string?>(
            null!,
            _ => Result.Success());

        // Assert:
        act.Should().Throw<ArgumentNullException>().WithParameterName("ruleBuilder");
    }

    [Fact]
    public void ValidateWith_NullRule_ShouldThrow()
    {
        // Act:
        Action act = () => new InlineValidator<TestRequest>()
            .RuleFor(request => request.Name)
            .ValidateWith(null!);

        // Assert:
        act.Should().Throw<ArgumentNullException>().WithParameterName("validate");
    }

    #endregion
}