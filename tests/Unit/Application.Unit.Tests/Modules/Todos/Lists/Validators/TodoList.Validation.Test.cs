using Application.Modules.Todos.Lists.Models;
using Application.Modules.Todos.Lists.Validators;

using Core.Modules.Todos.Lists;

using FluentValidation;

namespace Application.Unit.Tests.Modules.Todos.Lists.Validators;

/// <summary>Unit tests for <see cref="TodoListValidation"/>.</summary>
public sealed class TodoListValidationTest
{
    [Fact(DisplayName = "List rules should accept a valid request")]
    public void Validate_WithValidRequest_ShouldPass()
    {
        // Act
        var result = new TestValidator().Validate(new TestRequest
        {
            Name = "Groceries",
            Description = "Weekly groceries",
        });

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "List rules should reject a blank name")]
    public void Validate_WithBlankName_ShouldFail()
    {
        // Act
        var result = new TestValidator().Validate(new TestRequest
        {
            Name = " ",
        });

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Name");
    }

    [Fact(DisplayName = "List rules should reject an overlong description")]
    public void Validate_WithOverlongDescription_ShouldFail()
    {
        // Act
        var result = new TestValidator().Validate(new TestRequest
        {
            Name = "Groceries",
            Description = new string('x', TodoListConstant.Constraints.Description.MaxLength + 1),
        });

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Description");
    }

    [Fact(DisplayName = "List patch rules should allow a missing name")]
    public void Validate_WhenPatchAndNameMissing_ShouldPass()
    {
        // Act
        var result = new TestValidator(isPatch: true).Validate(new TestRequest
        {
            Name = null!,
        });

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "List patch rules should reject a blank name")]
    public void Validate_WhenPatchAndNameBlank_ShouldFail()
    {
        // Act
        var result = new TestValidator(isPatch: true).Validate(new TestRequest
        {
            Name = " ",
        });

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Name");
    }

    [Fact(DisplayName = "List patch rules should still reject an overlong description")]
    public void Validate_WhenPatchAndDescriptionOverlong_ShouldFail()
    {
        // Act
        var result = new TestValidator(isPatch: true).Validate(new TestRequest
        {
            Name = null!,
            Description = new string('x', TodoListConstant.Constraints.Description.MaxLength + 1),
        });

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Description");
    }

    private sealed record TestRequest : TodoListRequestModel;

    private sealed class TestValidator : AbstractValidator<TestRequest>
    {
        public TestValidator(bool isPatch = false)
        {
            RuleFor(request => request)
                .ApplyTodoListRules(isPatch);
        }
    }
}
