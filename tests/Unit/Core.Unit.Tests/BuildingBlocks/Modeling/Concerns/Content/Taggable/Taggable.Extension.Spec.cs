using Core.BuildingBlocks.Modeling.Concerns.Content.Taggable;

namespace Core.Unit.Tests.BuildingBlocks.Modeling.Concerns.Content.Taggable;

/// <summary>
/// Specifications for tag operations, validation, normalization, and constants.
/// </summary>
public sealed class TaggableExtensionSpec
{
    private const string EntityRequired = "Taggable.Entity.Required";
    private const string TagRequired = "Taggable.Tag.Required";
    private const string TagTooLong = "Taggable.Tag.TooLong";
    private const string TagInvalid = "Taggable.Tag.Invalid";
    private const string TagDuplicate = "Taggable.Tag.Duplicate";
    private const string TagNotFound = "Taggable.Tag.NotFound";
    private const string TagsTooMany = "Taggable.Tags.TooMany";
    private const string TestFailure = "Test.Fail";

    private sealed class TestTaggable : ITaggable
    {
        public ICollection<string> Tags { get; } = new List<string>();
    }

    private static Result<TestTaggable> Success(TestTaggable entity) =>
        Result<TestTaggable>.Success(entity);

    private static TestTaggable WithTags(params string[] tags)
    {
        var entity = new TestTaggable();

        foreach (var tag in tags)
            entity.Tags.Add(tag);

        return entity;
    }

    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeNull();
        result.Errors!.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    private static Result<TestTaggable> FailedInput() =>
        Result<TestTaggable>.Fail(
            Error.UnprocessableEntity(TestFailure, "boom"));

    // AddTag

    [Fact]
    public void AddTag_NewTag_ShouldAddNormalizedTag()
    {
        var entity = new TestTaggable();

        var result = Success(entity).AddTag("sale");

        result.IsSuccess.Should().BeTrue();
        entity.Tags.Should().ContainSingle().Which.Should().Be("sale");
    }

    [Fact]
    public void AddTag_PaddedMixedCase_ShouldTrimAndLowercase()
    {
        var entity = new TestTaggable();

        var result = Success(entity).AddTag(" SALE ");

        result.IsSuccess.Should().BeTrue();
        entity.Tags.Should().ContainSingle().Which.Should().Be("sale");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddTag_MissingTag_ShouldFailWithoutMutation(string? tag)
    {
        var entity = new TestTaggable();

        var result = Success(entity).AddTag(tag!);

        ShouldFailWith(result, TagRequired);
        entity.Tags.Should().BeEmpty();
    }

    [Fact]
    public void AddTag_TooLongTag_ShouldFailWithoutMutation()
    {
        var entity = new TestTaggable();
        var tag = new string(
            't',
            TaggableConstant.Constraints.Tag.MaxLength + 1);

        var result = Success(entity).AddTag(tag);

        ShouldFailWith(result, TagTooLong);
        entity.Tags.Should().BeEmpty();
    }

    [Fact]
    public void AddTag_DuplicateTag_ShouldFailWithoutMutation()
    {
        var entity = WithTags("sale");

        var result = Success(entity).AddTag("sale");

        ShouldFailWith(result, TagDuplicate);
        entity.Tags.Should().ContainSingle().Which.Should().Be("sale");
    }

    [Fact]
    public void AddTag_DuplicateAfterNormalization_ShouldFailWithoutMutation()
    {
        var entity = WithTags("sale");

        var result = Success(entity).AddTag(" SALE ");

        ShouldFailWith(result, TagDuplicate);
        entity.Tags.Should().ContainSingle().Which.Should().Be("sale");
    }

    [Fact]
    public void AddTag_InputFailure_ShouldPropagateWithoutMutation()
    {
        var input = FailedInput();

        var result = input.AddTag("sale");

        ShouldFailWith(result, TestFailure);
    }

    // RemoveTag

    [Fact]
    public void RemoveTag_ExistingTag_ShouldRemoveTag()
    {
        var entity = WithTags("sale");

        var result = Success(entity).RemoveTag("sale");

        result.IsSuccess.Should().BeTrue();
        entity.Tags.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTag_PaddedMixedCase_ShouldNormalizeAndRemove()
    {
        var entity = WithTags("sale");

        var result = Success(entity).RemoveTag(" SALE ");

        result.IsSuccess.Should().BeTrue();
        entity.Tags.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTag_MissingTag_ShouldFailWithoutMutation()
    {
        var entity = WithTags("sale");

        var result = Success(entity).RemoveTag("clearance");

        ShouldFailWith(result, TagNotFound);
        entity.Tags.Should().ContainSingle().Which.Should().Be("sale");
    }

    [Fact]
    public void RemoveTag_NullTag_ShouldFailWithoutMutation()
    {
        var entity = WithTags("sale");

        var result = Success(entity).RemoveTag(null);

        ShouldFailWith(result, TagNotFound);
        entity.Tags.Should().ContainSingle().Which.Should().Be("sale");
    }

    [Fact]
    public void RemoveTag_InputFailure_ShouldPropagateWithoutMutation()
    {
        var entity = WithTags("sale");
        var input = FailedInput();

        var result = input.RemoveTag("sale");

        ShouldFailWith(result, TestFailure);
        entity.Tags.Should().ContainSingle().Which.Should().Be("sale");
    }

    // ClearTags

    [Fact]
    public void ClearTags_WithEntries_ShouldClearTags()
    {
        var entity = WithTags("sale", "new");

        var result = Success(entity).ClearTags();

        result.IsSuccess.Should().BeTrue();
        entity.Tags.Should().BeEmpty();
    }

    [Fact]
    public void ClearTags_EmptyCollection_ShouldSucceed()
    {
        var entity = new TestTaggable();

        var result = Success(entity).ClearTags();

        result.IsSuccess.Should().BeTrue();
        entity.Tags.Should().BeEmpty();
    }

    [Fact]
    public void ClearTags_InputFailure_ShouldPropagateWithoutMutation()
    {
        var entity = WithTags("sale");
        var input = FailedInput();

        var result = input.ClearTags();

        ShouldFailWith(result, TestFailure);
        entity.Tags.Should().ContainSingle().Which.Should().Be("sale");
    }

    // NormalizeTag

    [Fact]
    public void NormalizeTag_Null_ShouldReturnNull()
    {
        TaggableExtensions.NormalizeTag(null).Should().BeNull();
    }

    [Theory]
    [InlineData(" SALE ", "sale")]
    [InlineData("Sale", "sale")]
    [InlineData("SALE", "sale")]
    [InlineData("clearance", "clearance")]
    public void NormalizeTag_ShouldTrimAndLowercase(
        string input,
        string expected)
    {
        TaggableExtensions.NormalizeTag(input).Should().Be(expected);
    }

    // ValidateAddTag

    [Fact]
    public void ValidateAddTag_NullEntity_ShouldFail()
    {
        var result = TaggableValidator.ValidateAddTag(
            (TestTaggable)null!,
            "sale");

        ShouldFailWith(result, EntityRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAddTag_MissingTag_ShouldFail(string? tag)
    {
        var entity = new TestTaggable();

        var result = TaggableValidator.ValidateAddTag(entity, tag!);

        ShouldFailWith(result, TagRequired);
        entity.Tags.Should().BeEmpty();
    }

    [Theory]
    [InlineData(" leading")]
    [InlineData("trailing ")]
    public void ValidateAddTag_UntrimmedTag_ShouldFail(string tag)
    {
        var entity = new TestTaggable();

        // Direct validation rejects input that extensions normalize first.
        var result = TaggableValidator.ValidateAddTag(entity, tag);

        ShouldFailWith(result, TagInvalid);
        entity.Tags.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAddTag_TooManyTags_ShouldFailWithoutMutation()
    {
        var maxTags = TaggableConstant.Constraints.Collection.MaxTags;
        var entity = new TestTaggable();

        for (var i = 0; i < maxTags; i++)
            entity.Tags.Add($"tag{i}");

        var result = TaggableValidator.ValidateAddTag(entity, "overflow");

        ShouldFailWith(result, TagsTooMany);
        entity.Tags.Count.Should().Be(maxTags);
    }

    // ValidateRemoveTag

    [Fact]
    public void ValidateRemoveTag_NullEntity_ShouldFail()
    {
        var result = TaggableValidator.ValidateRemoveTag(
            (TestTaggable)null!,
            "sale");

        ShouldFailWith(result, EntityRequired);
    }

    [Fact]
    public void ValidateRemoveTag_MissingTag_ShouldFail()
    {
        var entity = WithTags("sale");

        var result = TaggableValidator.ValidateRemoveTag(
            entity,
            "clearance");

        ShouldFailWith(result, TagNotFound);
        entity.Tags.Should().ContainSingle().Which.Should().Be("sale");
    }

    [Fact]
    public void ValidateRemoveTag_ExistingTag_ShouldSucceed()
    {
        var entity = WithTags("sale");

        var result = TaggableValidator.ValidateRemoveTag(entity, "sale");

        result.IsSuccess.Should().BeTrue();
        entity.Tags.Should().ContainSingle();
    }

    // ValidateClearTags

    [Fact]
    public void ValidateClearTags_NullEntity_ShouldFail()
    {
        var result = TaggableValidator.ValidateClearTags(
            (TestTaggable)null!);

        ShouldFailWith(result, EntityRequired);
    }

    [Fact]
    public void ValidateClearTags_ExistingEntity_ShouldSucceed()
    {
        var entity = WithTags("sale");

        var result = TaggableValidator.ValidateClearTags(entity);

        result.IsSuccess.Should().BeTrue();
        entity.Tags.Should().ContainSingle();
    }

    // Failure definitions

    [Fact]
    public void FailureCodes_ShouldMatchDocumentedCodes()
    {
        TaggableOutcome.Failure.EntityRequired.Code.Should()
            .Be(EntityRequired);

        TaggableOutcome.Failure.TagRequired.Code.Should()
            .Be(TagRequired);

        TaggableOutcome.Failure.TagTooShort.Code.Should()
            .Be("Taggable.Tag.TooShort");

        TaggableOutcome.Failure.TagTooLong.Code.Should()
            .Be(TagTooLong);

        TaggableOutcome.Failure.TagInvalid.Code.Should()
            .Be(TagInvalid);

        TaggableOutcome.Failure.TagDuplicate.Code.Should()
            .Be(TagDuplicate);

        TaggableOutcome.Failure.TagNotFound.Code.Should()
            .Be(TagNotFound);

        TaggableOutcome.Failure.TagsTooMany.Code.Should()
            .Be(TagsTooMany);
    }

    // Constants

    [Fact]
    public void Constants_ShouldMatchDocumentedValues()
    {
        TaggableConstant.Constraints.Tag.MinLength.Should().Be(1);
        TaggableConstant.Constraints.Tag.MaxLength.Should().Be(128);

        TaggableConstant.Constraints.Collection.MinTags.Should().Be(0);
        TaggableConstant.Constraints.Collection.MaxTags.Should().Be(50);

        TaggableConstant.Defaults.Normalization.TrimWhitespace.Should()
            .BeTrue();

        TaggableConstant.Defaults.Normalization.ToLowercase.Should()
            .BeTrue();

        TaggableConstant.Patterns.Tag.Should()
            .Be(@"^\S(?:.*\S)?$");
    }
}