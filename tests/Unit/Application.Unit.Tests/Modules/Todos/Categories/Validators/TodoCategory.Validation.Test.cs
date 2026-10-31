using Application.Modules.Todos.Categories.Models;
using Application.Modules.Todos.Categories.Validators;

using Core.Modules.Todos.Categories;

using FluentValidation;

namespace Application.Unit.Tests.Modules.Todos.Categories.Validators;

/// <summary>Unit tests for <see cref="TodoCategoryValidation"/>.</summary>
public sealed class TodoCategoryValidationTest
{
    [Fact(DisplayName = "Category rules should accept a valid request")]
    public void Validate_WithValidRequest_ShouldPass()
    {
        // Act
        var result = new TestValidator().Validate(new TestRequest
        {
            Name = "Errands",
            Description = "Weekly errands",
        });

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "Category rules should reject a blank name")]
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

    [Fact(DisplayName = "Category rules should reject an overlong description")]
    public void Validate_WithOverlongDescription_ShouldFail()
    {
        // Act
        var result = new TestValidator().Validate(new TestRequest
        {
            Name = "Errands",
            Description = new string('x', TodoCategoryConstant.Constraints.Description.MaxLength + 1),
        });

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Description");
    }

    [Fact(DisplayName = "Category patch rules should allow a missing name")]
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

    [Fact(DisplayName = "Category patch rules should reject a blank name")]
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

    [Fact(DisplayName = "Category patch rules should still reject an overlong description")]
    public void Validate_WhenPatchAndDescriptionOverlong_ShouldFail()
    {
        // Act
        var result = new TestValidator(isPatch: true).Validate(new TestRequest
        {
            Name = null!,
            Description = new string('x', TodoCategoryConstant.Constraints.Description.MaxLength + 1),
        });

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Description");
    }

    private sealed record TestRequest : TodoCategoryRequestModel;

    private sealed class TestValidator : AbstractValidator<TestRequest>
    {
        public TestValidator(bool isPatch = false)
        {
            RuleFor(request => request)
                .ApplyTodoCategoryRules(isPatch);
        }
    }
}
