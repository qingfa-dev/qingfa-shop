using Core.BuildingBlocks.Modeling.Concerns.Foundation.Referenceable;

namespace Core.Unit.Tests.BuildingBlocks.Modeling.Concerns.Foundation.Referenceable;

/// <summary>Specifications for <see cref="ReferenceableExtensions"/> and <see cref="ReferenceableValidator"/>.</summary>
public class ReferenceableExtensionSpec
{
    #region Helper Methods

    private sealed class TestReferenceable : IReferenceable
    {
        public string Reference { get; set; } = string.Empty;
    }

    /// <summary>Creates a successful result wrapping the given entity.</summary>
    /// <param name="entity">The entity to wrap.</param>
    /// <returns>A successful result containing the entity.</returns>
    private static Result<TestReferenceable> Success(
        TestReferenceable entity)
    {
        return Result<TestReferenceable>.Success(entity);
    }

    #endregion

    #region Test Cases

    [Fact]
    public void SetReference_ValidReference_ShouldSetReference()
    {
        // Arrange:
        var entity = new TestReferenceable();

        // Act:
        var result = Success(entity).SetReference("ORD-2024-0001");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Reference.Should().Be("ORD-2024-0001");
    }

    [Fact]
    public void SetReference_WhitespaceReference_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestReferenceable { Reference = "keep-me" };

        // Act:
        var result = Success(entity).SetReference("   ");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Referenceable.Reference.Required");
        entity.Reference.Should().Be("keep-me");
    }

    [Fact]
    public void SetReference_TooLongReference_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestReferenceable { Reference = "keep-me" };
        var reference = new string(
            'r',
            ReferenceableConstant.Constraints.MaxReferenceLength + 1);

        // Act:
        var result = Success(entity).SetReference(reference);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Referenceable.Reference.TooLong");
        entity.Reference.Should().Be("keep-me");
    }

    [Fact]
    public void ValidateReference_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = ReferenceableValidator.ValidateReference(
            (TestReferenceable)null!,
            "ORD-1");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Referenceable.Entity.Required");
    }

    [Fact]
    public void SetReference_NullReference_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestReferenceable { Reference = "keep-me" };

        // Act:
        var result = Success(entity).SetReference(null);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Referenceable.Reference.Required");
        entity.Reference.Should().Be("keep-me");
    }

    [Fact]
    public void SetReference_UntrimmedReference_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestReferenceable { Reference = "keep-me" };

        // Act:
        var result = Success(entity).SetReference(" ORD-1");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Referenceable.Reference.Untrimmed");
        entity.Reference.Should().Be("keep-me");
    }

    [Fact]
    public void SetReference_InvalidCharsetReference_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestReferenceable { Reference = "keep-me" };

        // Act:
        var result = Success(entity).SetReference("ORD@001!");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Referenceable.Reference.InvalidCharset");
        entity.Reference.Should().Be("keep-me");
    }

    [Fact]
    public void SetReference_AtMaxLength_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestReferenceable();
        var reference = new string('r', ReferenceableConstant.Constraints.Reference.MaxLength);

        // Act:
        var result = Success(entity).SetReference(reference);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Reference.Should().Be(reference);
    }

    [Fact]
    public void SetReference_InputFailure_ShouldPropagateErrors()
    {
        // Arrange:
        var input = Result<TestReferenceable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        var result = input.SetReference("ORD-1");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Test.Fail");
    }

    [Fact]
    public void ClearReference_NonEmpty_ShouldClearToEmpty()
    {
        // Arrange:
        var entity = new TestReferenceable { Reference = "ORD-2024-0001" };

        // Act:
        var result = Success(entity).ClearReference();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
        entity.Reference.Should().Be(ReferenceableConstant.Defaults.Empty);
    }

    [Fact]
    public void ClearReference_AlreadyEmpty_ShouldStayEmpty()
    {
        // Arrange:
        var entity = new TestReferenceable { Reference = ReferenceableConstant.Defaults.Empty };

        // Act:
        var result = Success(entity).ClearReference();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Reference.Should().Be(string.Empty);
    }

    [Fact]
    public void ClearReference_InputFailure_ShouldPropagateErrors()
    {
        // Arrange:
        var input = Result<TestReferenceable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        var result = input.ClearReference();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Test.Fail");
    }

    [Fact]
    public void EnsureReferenced_ValidReference_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestReferenceable { Reference = "ORD-2024-0001" };

        // Act:
        var result = Success(entity).EnsureReferenced();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void EnsureReferenced_EmptyReference_ShouldFailReferenceRequired()
    {
        // Arrange:
        var entity = new TestReferenceable { Reference = string.Empty };

        // Act:
        var result = Success(entity).EnsureReferenced();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Referenceable.Reference.Required");
    }

    [Fact]
    public void EnsureReferenced_TooLongReference_ShouldFailTooLong()
    {
        // Arrange:
        var entity = new TestReferenceable { Reference = new string('r', ReferenceableConstant.Constraints.Reference.MaxLength + 1) };

        // Act:
        var result = Success(entity).EnsureReferenced();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Referenceable.Reference.TooLong");
    }

    [Fact]
    public void EnsureReferenced_UntrimmedReference_ShouldFailUntrimmed()
    {
        // Arrange:
        var entity = new TestReferenceable { Reference = "ORD-1 " };

        // Act:
        var result = Success(entity).EnsureReferenced();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Referenceable.Reference.Untrimmed");
    }

    [Fact]
    public void EnsureReferenced_InvalidCharsetReference_ShouldFailInvalidCharset()
    {
        // Arrange:
        var entity = new TestReferenceable { Reference = "ORD@001!" };

        // Act:
        var result = Success(entity).EnsureReferenced();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Referenceable.Reference.InvalidCharset");
    }

    [Fact]
    public void EnsureReferenced_InputFailure_ShouldPropagateErrors()
    {
        // Arrange:
        var input = Result<TestReferenceable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        var result = input.EnsureReferenced();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Test.Fail");
    }

    [Fact]
    public void RequireReferenced_ValidReference_ShouldReturnEntity()
    {
        // Arrange:
        var entity = new TestReferenceable { Reference = "ORD-2024-0001" };

        // Act:
        var value = Success(entity).RequireReferenced();

        // Assert:
        value.Should().BeSameAs(entity);
    }

    [Fact]
    public void RequireReferenced_EmptyReference_ShouldThrow()
    {
        // Arrange:
        var entity = new TestReferenceable { Reference = string.Empty };

        // Act:
        // Assert:
        FluentActions.Invoking(() => Success(entity).RequireReferenced()).Should().Throw<ResultException>();
    }

    [Fact]
    public void RequireReferenced_InputFailure_ShouldThrow()
    {
        // Arrange:
        var input = Result<TestReferenceable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        // Assert:
        FluentActions.Invoking(() => input.RequireReferenced()).Should().Throw<ResultException>();
    }

    #endregion

    #region Validator Specs

    [Fact]
    public void ValidateReference_NullReference_ShouldFailReferenceRequired()
    {
        // Arrange:
        var entity = new TestReferenceable();

        // Act:
        var result = ReferenceableValidator.ValidateReference(entity, null);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Referenceable.Reference.Required");
    }

    [Fact]
    public void ValidateReference_WhitespaceReference_ShouldFailReferenceRequired()
    {
        // Arrange:
        var entity = new TestReferenceable();

        // Act:
        var result = ReferenceableValidator.ValidateReference(entity, "   ");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Referenceable.Reference.Required");
    }

    [Fact]
    public void ValidateReference_TooLongReference_ShouldFailTooLong()
    {
        // Arrange:
        var entity = new TestReferenceable();
        var reference = new string('r', ReferenceableConstant.Constraints.Reference.MaxLength + 1);

        // Act:
        var result = ReferenceableValidator.ValidateReference(entity, reference);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Referenceable.Reference.TooLong");
    }

    [Fact]
    public void ValidateReference_LeadingSpaceReference_ShouldFailUntrimmed()
    {
        // Arrange:
        var entity = new TestReferenceable();

        // Act:
        var result = ReferenceableValidator.ValidateReference(entity, " ORD-1");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Referenceable.Reference.Untrimmed");
    }

    [Fact]
    public void ValidateReference_TrailingSpaceReference_ShouldFailUntrimmed()
    {
        // Arrange:
        var entity = new TestReferenceable();

        // Act:
        var result = ReferenceableValidator.ValidateReference(entity, "ORD-1 ");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Referenceable.Reference.Untrimmed");
    }

    [Fact]
    public void ValidateReference_BadCharsetReference_ShouldFailInvalidCharset()
    {
        // Arrange:
        var entity = new TestReferenceable();

        // Act:
        var result = ReferenceableValidator.ValidateReference(entity, "ORD@001!");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Referenceable.Reference.InvalidCharset");
    }

    [Fact]
    public void ValidateReference_ValidReference_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestReferenceable();

        // Act:
        var result = ReferenceableValidator.ValidateReference(entity, "ORD-2024-0001");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void Constants_ShouldMatchNestedDefinitions()
    {
        // Arrange:
        // Act:
        // Assert:
        ReferenceableConstant.Constraints.Reference.MinLength.Should().Be(1);
        ReferenceableConstant.Constraints.Reference.MaxLength.Should().Be(128);
        ReferenceableConstant.Constraints.MaxReferenceLength.Should().Be(ReferenceableConstant.Constraints.Reference.MaxLength);
        ReferenceableConstant.Defaults.Empty.Should().Be(string.Empty);
        ReferenceableConstant.Patterns.Trimmed.Should().Be(@"^\S(?:.*\S)?$");
        ReferenceableConstant.Patterns.AllowedCharset.Should().Be(@"^[A-Za-z0-9\-_/#:. ]+$");
    }

    #endregion
}