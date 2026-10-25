using Core.Common.Concerns.Content.Sluggable;

namespace Core.UnitTests.Common.Concerns.Content.Sluggable;

/// <summary>Spec for <see cref="SluggableExtensions"/> covering extension behavior,
/// <see cref="SluggableValidator"/> branches and <see cref="SluggableConstant"/> values.</summary>
public class SluggableExtensionSpec
{
    #region Helper Methods

    private sealed class TestSluggable : ISluggable
    {
        public string Slug { get; set; } = string.Empty;
    }

    /// <summary>Creates a successful result wrapping the test entity.</summary>
    /// <param name="entity">The test entity.</param>
    /// <returns>A successful result.</returns>
    private static Result<TestSluggable> Success(
        TestSluggable entity)
    {
        return Result<TestSluggable>.Success(entity);
    }

    #endregion

    #region Test Cases

    [Fact]
    public void SetSlug_ValidSlug_ShouldSetSlug()
    {
        // Arrange:
        var entity = new TestSluggable();

        // Act:
        var result = Success(entity).SetSlug("hello-world");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Slug.Should().Be("hello-world");
    }

    [Fact]
    public void SetSlug_NullSlug_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestSluggable { Slug = "keep-me" };

        // Act:
        var result = Success(entity).SetSlug(null);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Sluggable.Slug.Required");
        entity.Slug.Should().Be("keep-me");
    }

    [Fact]
    public void SetSlug_UppercaseSlug_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestSluggable { Slug = "keep-me" };

        // Act:
        var result = Success(entity).SetSlug("Hello-World");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Sluggable.Slug.Invalid");
        entity.Slug.Should().Be("keep-me");
    }

    [Fact]
    public void SetSlug_TooLongSlug_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestSluggable { Slug = "keep-me" };
        var slug = new string(
            's',
            SluggableConstant.Constraints.Slug.MaxLength + 1);

        // Act:
        var result = Success(entity).SetSlug(slug);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Sluggable.Slug.TooLong");
        entity.Slug.Should().Be("keep-me");
    }

    [Fact]
    public void SetSlugFromText_PlainText_ShouldSlugify()
    {
        // Arrange:
        var entity = new TestSluggable();

        // Act:
        var result = Success(entity)
            .SetSlugFromText("Hello, World!");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Slug.Should().Be("hello-world");
    }

    [Fact]
    public void SetSlugFromText_Diacritics_ShouldFoldToAscii()
    {
        // Arrange:
        var entity = new TestSluggable();

        // Act:
        var result = Success(entity)
            .SetSlugFromText("Crème Brûlée");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Slug.Should().Be("creme-brulee");
    }

    [Fact]
    public void SetSlugFromText_SymbolsOnly_ShouldFailSlugRequired()
    {
        // Arrange:
        var entity = new TestSluggable { Slug = "keep-me" };

        // Act:
        var result = Success(entity).SetSlugFromText("!!!");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Sluggable.Slug.Required");
        entity.Slug.Should().Be("keep-me");
    }

    [Fact]
    public void ValidateSlug_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = SluggableValidator.ValidateSlug(
            (TestSluggable)null!,
            "hello-world");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Sluggable.Entity.Required");
    }

    [Fact]
    public void SetSlug_WhitespaceSlug_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestSluggable { Slug = "keep-me" };

        // Act:
        var result = Success(entity).SetSlug("   ");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Sluggable.Slug.Required");
        entity.Slug.Should().Be("keep-me");
    }

    [Fact]
    public void SetSlugFromText_NullText_ShouldFailSlugRequired()
    {
        // Arrange:
        var entity = new TestSluggable { Slug = "keep-me" };

        // Act:
        var result = Success(entity).SetSlugFromText(null);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Sluggable.Slug.Required");
        entity.Slug.Should().Be("keep-me");
    }

    [Fact]
    public void SetSlugFromText_WhitespaceText_ShouldFailSlugRequired()
    {
        // Arrange:
        var entity = new TestSluggable { Slug = "keep-me" };

        // Act:
        var result = Success(entity).SetSlugFromText("   ");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Sluggable.Slug.Required");
        entity.Slug.Should().Be("keep-me");
    }

    [Fact]
    public void SetSlugFromText_TrailingSeparator_ShouldTrimHyphen()
    {
        // Arrange:
        var entity = new TestSluggable();

        // Act:
        var result = Success(entity).SetSlugFromText("Hello World! ");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Slug.Should().Be("hello-world");
    }

    #endregion

    #region Validator Specs

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateSlug_BlankSlug_ShouldFailSlugRequired(string? slug)
    {
        // Arrange:
        var entity = new TestSluggable { Slug = "keep-me" };

        // Act:
        var result = SluggableValidator.ValidateSlug(entity, slug);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Sluggable.Slug.Required");
        entity.Slug.Should().Be("keep-me");
    }

    [Fact]
    public void ValidateSlug_TooLongSlug_ShouldFailSlugTooLong()
    {
        // Arrange:
        var entity = new TestSluggable { Slug = "keep-me" };
        var slug = new string(
            's',
            SluggableConstant.Constraints.Slug.MaxLength + 1);

        // Act:
        var result = SluggableValidator.ValidateSlug(entity, slug);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Sluggable.Slug.TooLong");
        entity.Slug.Should().Be("keep-me");
    }

    [Theory]
    [InlineData("Hello-World")]
    [InlineData("hello--world")]
    [InlineData("hello_world")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    public void ValidateSlug_InvalidSlug_ShouldFailSlugInvalid(string slug)
    {
        // Arrange:
        var entity = new TestSluggable { Slug = "keep-me" };

        // Act:
        var result = SluggableValidator.ValidateSlug(entity, slug);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Sluggable.Slug.Invalid");
        entity.Slug.Should().Be("keep-me");
    }

    [Fact]
    public void ValidateSlug_ValidSlug_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestSluggable();

        // Act:
        var result = SluggableValidator.ValidateSlug(entity, "hello-world-123");

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Slugify_BlankText_ShouldReturnEmpty(string? text, string expected)
    {
        // Arrange:
        // Act:
        var slug = SluggableExtensions.Slugify(text);

        // Assert:
        slug.Should().Be(expected);
    }

    [Fact]
    public void Slugify_PaddedText_ShouldTrimHyphens()
    {
        // Arrange:
        // Act:
        var slug = SluggableExtensions.Slugify("--Hello--");

        // Assert:
        slug.Should().Be("hello");
    }

    [Fact]
    public void Slugify_SymbolsOnly_ShouldReturnEmpty()
    {
        // Arrange:
        // Act:
        var slug = SluggableExtensions.Slugify("!!!");

        // Assert:
        slug.Should().Be(string.Empty);
    }

    [Fact]
    public void Slugify_ConsecutiveSeparators_ShouldCollapseToSingleHyphen()
    {
        // Arrange:
        // Act:
        var slug = SluggableExtensions.Slugify("hello   world");

        // Assert:
        slug.Should().Be("hello-world");
    }

    [Fact]
    public void Slugify_TrailingSeparator_ShouldTrimHyphen()
    {
        // Arrange:
        // Act:
        var slug = SluggableExtensions.Slugify("hello-");

        // Assert:
        slug.Should().Be("hello");
    }

    [Fact]
    public void Slugify_LeadingSeparator_ShouldTrimHyphen()
    {
        // Arrange:
        // Act:
        var slug = SluggableExtensions.Slugify("   hello");

        // Assert:
        slug.Should().Be("hello");
    }

    [Fact]
    public void Slugify_Digits_ShouldPreserveDigits()
    {
        // Arrange:
        // Act:
        var slug = SluggableExtensions.Slugify("product 123");

        // Assert:
        slug.Should().Be("product-123");
    }

    [Fact]
    public void Slugify_MixedSeparators_ShouldCollapseToSingleHyphens()
    {
        // Arrange:
        // Act:
        var slug = SluggableExtensions.Slugify("a--b__c");

        // Assert:
        slug.Should().Be("a-b-c");
    }

    [Fact]
    public void FailureCodes_ShouldMatchDocumentedCodes()
    {
        // Arrange:
        // Act:
        // Assert:
        // Note: SlugTooShort is shadowed by SlugRequired for empty input
        // (MinLength 1), so its code is asserted here rather than via the validator.
        SluggableOutcome.Failure.EntityRequired.Code.Should().Be("Sluggable.Entity.Required");
        SluggableOutcome.Failure.SlugRequired.Code.Should().Be("Sluggable.Slug.Required");
        SluggableOutcome.Failure.SlugTooShort.Code.Should().Be("Sluggable.Slug.TooShort");
        SluggableOutcome.Failure.SlugTooLong.Code.Should().Be("Sluggable.Slug.TooLong");
        SluggableOutcome.Failure.SlugInvalid.Code.Should().Be("Sluggable.Slug.Invalid");
    }

    #endregion

    #region Constant Specs

    [Fact]
    public void Constants_ShouldMatchDocumentedValues()
    {
        // Arrange:
        // Act:
        // Assert:
        SluggableConstant.Constraints.Slug.MinLength.Should().Be(1);
        SluggableConstant.Constraints.Slug.MaxLength.Should().Be(200);
        SluggableConstant.Defaults.FallbackSlug.Should().Be("n-a");
        SluggableConstant.Patterns.Slug.Should().Be(@"^[a-z0-9]+(?:-[a-z0-9]+)*$");
    }

    #endregion
}