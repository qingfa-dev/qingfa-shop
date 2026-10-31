using Application.Modules.Todos.Items.Models;
using Application.Modules.Todos.Items.Validators;

using FluentValidation;

namespace Application.Unit.Tests.Modules.Todos.Items.Validators;

/// <summary>Unit tests for <see cref="TodoValidation"/>.</summary>
public sealed class TodoValidationTest
{
    [Fact(DisplayName = "Todo rules should accept a valid request")]
    public void Validate_WithValidRequest_ShouldPass()
    {
        // Act
        var result = new TestValidator().Validate(new TestRequest
        {
            Title = "Ship the feature",
            Description = "With tests",
            ListId = Guid.NewGuid(),
            CategoryIds = [Guid.NewGuid()],
        });

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "Todo rules should reject a blank title")]
    public void Validate_WithBlankTitle_ShouldFail()
    {
        // Act
        var result = new TestValidator().Validate(new TestRequest
        {
            Title = " ",
        });

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Title");
    }

    [Fact(DisplayName = "Todo rules should reject an empty list id")]
    public void Validate_WithEmptyListId_ShouldFail()
    {
        // Act
        var result = new TestValidator().Validate(new TestRequest
        {
            Title = "Task",
            ListId = Guid.Empty,
        });

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "ListId");
    }

    [Fact(DisplayName = "Todo rules should reject an empty category id")]
    public void Validate_WithEmptyCategoryId_ShouldFail()
    {
        // Act
        var result = new TestValidator().Validate(new TestRequest
        {
            Title = "Task",
            CategoryIds = [Guid.Empty],
        });

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "CategoryIds");
    }

    [Fact(DisplayName = "Todo rules should reject duplicate category ids")]
    public void Validate_WithDuplicateCategoryIds_ShouldFail()
    {
        // Arrange
        Guid categoryId = Guid.NewGuid();

        // Act
        var result = new TestValidator().Validate(new TestRequest
        {
            Title = "Task",
            CategoryIds = [categoryId, categoryId],
        });

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "CategoryIds");
    }

    [Fact(DisplayName = "Todo patch rules should allow a missing title")]
    public void Validate_WhenPatchAndTitleMissing_ShouldPass()
    {
        // Act
        var result = new TestValidator(isPatch: true).Validate(new TestRequest
        {
            Title = null!,
        });

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "Todo patch rules should reject a blank title")]
    public void Validate_WhenPatchAndTitleBlank_ShouldFail()
    {
        // Act
        var result = new TestValidator(isPatch: true).Validate(new TestRequest
        {
            Title = " ",
        });

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Title");
    }

    [Fact(DisplayName = "Todo patch rules should skip category validation")]
    public void Validate_WhenPatchWithDuplicateCategoryIds_ShouldPass()
    {
        // Arrange
        Guid categoryId = Guid.NewGuid();

        // Act
        var result = new TestValidator(isPatch: true).Validate(new TestRequest
        {
            Title = null!,
            CategoryIds = [categoryId, categoryId],
        });

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "Todo patch rules should still reject an empty list id")]
    public void Validate_WhenPatchWithEmptyListId_ShouldFail()
    {
        // Act
        var result = new TestValidator(isPatch: true).Validate(new TestRequest
        {
            Title = null!,
            ListId = Guid.Empty,
        });

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "ListId");
    }

    private sealed record TestRequest : TodoRequestModel;

    private sealed class TestValidator : AbstractValidator<TestRequest>
    {
        public TestValidator(bool isPatch = false)
        {
            RuleFor(request => request)
                .ApplyTodoRules(isPatch);
        }
    }
}
