using Core.Common.Concerns.Content.MediaAttachable;

namespace Core.Tests.Common.Concerns.Content.MediaAttachable;

/// <summary>Spec for <see cref="MediaAttachableExtensions"/> covering extension behavior,
/// <see cref="MediaAttachableValidator"/> branches and <see cref="MediaAttachableConstant"/> values.</summary>
public class MediaAttachableExtensionSpec
{
    #region Helper Methods

    private sealed class TestMediaAttachable : IMediaAttachable<string>
    {
        public ICollection<string> Media { get; } = new List<string>();
    }

    /// <summary>Creates a successful result wrapping the test entity.</summary>
    /// <param name="entity">The test entity.</param>
    /// <returns>A successful result.</returns>
    private static Result<TestMediaAttachable> Success(
        TestMediaAttachable entity)
    {
        return Result<TestMediaAttachable>.Success(entity);
    }

    #endregion

    #region Test Cases

    [Fact]
    public void AddMedia_NewMedia_ShouldAttachMedia()
    {
        // Arrange:
        var entity = new TestMediaAttachable();

        // Act:
        var result = Success(entity).AddMedia("photo.jpg");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Media.Should().ContainSingle().Which.Should().Be("photo.jpg");
    }

    [Fact]
    public void AddMedia_DuplicateMedia_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestMediaAttachable();
        Success(entity).AddMedia("photo.jpg");

        // Act:
        var result = Success(entity).AddMedia("photo.jpg");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(
            "MediaAttachable.Media.Duplicate");
        entity.Media.Should().ContainSingle();
    }

    [Fact]
    public void RemoveMedia_AttachedMedia_ShouldDetachMedia()
    {
        // Arrange:
        var entity = new TestMediaAttachable();
        Success(entity).AddMedia("photo.jpg");

        // Act:
        var result = Success(entity).RemoveMedia("photo.jpg");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Media.Should().BeEmpty();
    }

    [Fact]
    public void RemoveMedia_MissingMedia_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestMediaAttachable();
        Success(entity).AddMedia("photo.jpg");

        // Act:
        var result = Success(entity).RemoveMedia("video.mp4");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("MediaAttachable.Media.NotFound");
        entity.Media.Should().ContainSingle();
    }

    [Fact]
    public void ValidateAddMedia_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = MediaAttachableValidator.ValidateAddMedia(
            (TestMediaAttachable)null!,
            "photo.jpg");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("MediaAttachable.Entity.Required");
    }

    [Fact]
    public void AddMedia_NullMedia_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestMediaAttachable();

        // Act:
        var result = Success(entity).AddMedia((string)null!);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("MediaAttachable.Media.Required");
        entity.Media.Should().BeEmpty();
    }

    [Fact]
    public void ClearMedia_WithEntries_ShouldClearMedia()
    {
        // Arrange:
        var entity = new TestMediaAttachable();
        Success(entity).AddMedia("photo.jpg");
        Success(entity).AddMedia("video.mp4");

        // Act:
        var result = Success(entity).ClearMedia<TestMediaAttachable, string>();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Media.Should().BeEmpty();
    }

    [Fact]
    public void ClearMedia_EmptyCollection_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestMediaAttachable();

        // Act:
        var result = Success(entity).ClearMedia<TestMediaAttachable, string>();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Media.Should().BeEmpty();
    }

    [Fact]
    public void AddMedia_TooManyViaExtension_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestMediaAttachable();
        for (var i = 0; i < MediaAttachableConstant.Constraints.Collection.MaxAttachments; i++)
        {
            entity.Media.Add($"media{i}.jpg");
        }

        // Act:
        var result = Success(entity).AddMedia("overflow.jpg");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("MediaAttachable.Media.TooMany");
        entity.Media.Count.Should().Be(
            MediaAttachableConstant.Constraints.Collection.MaxAttachments);
    }

    [Fact]
    public void AddMedia_InputFailure_ShouldPropagateWithoutMutation()
    {
        // Arrange:
        var entity = new TestMediaAttachable();
        var input = Result<TestMediaAttachable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        var result = input.AddMedia("photo.jpg");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Test.Fail");
        entity.Media.Should().BeEmpty();
    }

    [Fact]
    public void RemoveMedia_InputFailure_ShouldPropagateWithoutMutation()
    {
        // Arrange:
        var entity = new TestMediaAttachable();
        entity.Media.Add("photo.jpg");
        var input = Result<TestMediaAttachable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        var result = input.RemoveMedia("photo.jpg");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Test.Fail");
        entity.Media.Should().ContainSingle();
    }

    [Fact]
    public void ClearMedia_InputFailure_ShouldPropagateWithoutMutation()
    {
        // Arrange:
        var entity = new TestMediaAttachable();
        entity.Media.Add("photo.jpg");
        var input = Result<TestMediaAttachable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        var result = input.ClearMedia<TestMediaAttachable, string>();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Test.Fail");
        entity.Media.Should().ContainSingle();
    }

    [Fact]
    public void NormalizeUrl_Null_ShouldReturnNull()
    {
        // Arrange:
        // Act:
        var normalized = MediaAttachableExtensions.NormalizeUrl(null);

        // Assert:
        normalized.Should().BeNull();
    }

    [Fact]
    public void NormalizeUrl_PaddedUrl_ShouldTrim()
    {
        // Arrange:
        // Act:
        var normalized = MediaAttachableExtensions.NormalizeUrl("  https://example.com/a.jpg  ");

        // Assert:
        normalized.Should().Be("https://example.com/a.jpg");
    }

    [Fact]
    public void NormalizeUrl_PlainUrl_ShouldReturnSame()
    {
        // Arrange:
        // Act:
        var normalized = MediaAttachableExtensions.NormalizeUrl("https://example.com/a.jpg");

        // Assert:
        normalized.Should().Be("https://example.com/a.jpg");
    }

    [Fact]
    public void NormalizeAltText_Null_ShouldReturnNull()
    {
        // Arrange:
        // Act:
        var normalized = MediaAttachableExtensions.NormalizeAltText(null);

        // Assert:
        normalized.Should().BeNull();
    }

    [Fact]
    public void NormalizeAltText_PaddedText_ShouldTrim()
    {
        // Arrange:
        // Act:
        var normalized = MediaAttachableExtensions.NormalizeAltText("  A photo  ");

        // Assert:
        normalized.Should().Be("A photo");
    }

    [Fact]
    public void NormalizeMediaType_Null_ShouldReturnNull()
    {
        // Arrange:
        // Act:
        var normalized = MediaAttachableExtensions.NormalizeMediaType(null);

        // Assert:
        normalized.Should().BeNull();
    }

    [Fact]
    public void NormalizeMediaType_MixedCasePadded_ShouldTrimAndLowercase()
    {
        // Arrange:
        // Act:
        var normalized = MediaAttachableExtensions.NormalizeMediaType(" Video ");

        // Assert:
        normalized.Should().Be("video");
    }

    [Fact]
    public void NormalizeMedia_AllNull_ShouldReturnAllNull()
    {
        // Arrange:
        // Act:
        var normalized = MediaAttachableExtensions.NormalizeMedia(null, null, null);

        // Assert:
        normalized.Url.Should().BeNull();
        normalized.AltText.Should().BeNull();
        normalized.MediaType.Should().BeNull();
    }

    [Fact]
    public void NormalizeMedia_MixedValues_ShouldNormalizeEach()
    {
        // Arrange:
        // Act:
        var normalized = MediaAttachableExtensions.NormalizeMedia(
            "  https://example.com/a.jpg  ",
            "  A photo  ",
            " Video ");

        // Assert:
        normalized.Url.Should().Be("https://example.com/a.jpg");
        normalized.AltText.Should().Be("A photo");
        normalized.MediaType.Should().Be("video");
    }

    #endregion

    #region Validator Specs

    [Fact]
    public void ValidateAddMedia_NullMedia_ShouldFailMediaRequired()
    {
        // Arrange:
        var entity = new TestMediaAttachable();

        // Act:
        var result = MediaAttachableValidator
            .ValidateAddMedia<TestMediaAttachable, string>(entity, null!);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("MediaAttachable.Media.Required");
        entity.Media.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAddMedia_TooManyAttachments_ShouldFailMediaTooMany()
    {
        // Arrange:
        var entity = new TestMediaAttachable();
        for (var i = 0; i < MediaAttachableConstant.Constraints.Collection.MaxAttachments; i++)
        {
            entity.Media.Add($"media{i}.jpg");
        }

        // Act:
        var result = MediaAttachableValidator.ValidateAddMedia(
            entity,
            "overflow.jpg");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("MediaAttachable.Media.TooMany");
        entity.Media.Count.Should().Be(
            MediaAttachableConstant.Constraints.Collection.MaxAttachments);
    }

    [Fact]
    public void ValidateRemoveMedia_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = MediaAttachableValidator.ValidateRemoveMedia(
            (TestMediaAttachable)null!,
            "photo.jpg");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("MediaAttachable.Entity.Required");
    }

    [Fact]
    public void ValidateRemoveMedia_NullMedia_ShouldFailMediaRequired()
    {
        // Arrange:
        var entity = new TestMediaAttachable();
        entity.Media.Add("photo.jpg");

        // Act:
        var result = MediaAttachableValidator
            .ValidateRemoveMedia<TestMediaAttachable, string>(entity, null!);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("MediaAttachable.Media.Required");
        entity.Media.Should().ContainSingle();
    }

    [Fact]
    public void ValidateRemoveMedia_MissingMedia_ShouldFailMediaNotFound()
    {
        // Arrange:
        var entity = new TestMediaAttachable();
        entity.Media.Add("photo.jpg");

        // Act:
        var result = MediaAttachableValidator.ValidateRemoveMedia(
            entity,
            "video.mp4");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("MediaAttachable.Media.NotFound");
        entity.Media.Should().ContainSingle();
    }

    [Fact]
    public void ValidateClearMedia_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = MediaAttachableValidator
            .ValidateClearMedia<TestMediaAttachable, string>(
                (TestMediaAttachable)null!);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("MediaAttachable.Entity.Required");
    }

    [Fact]
    public void ValidateClearMedia_ExistingEntity_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestMediaAttachable();
        entity.Media.Add("photo.jpg");

        // Act:
        var result = MediaAttachableValidator
            .ValidateClearMedia<TestMediaAttachable, string>(entity);

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateMediaUrl_BlankUrl_ShouldFailUrlRequired(string? url)
    {
        // Arrange:
        // Act:
        var result = MediaAttachableValidator.ValidateMediaUrl(url);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("MediaAttachable.Url.Required");
    }

    [Fact]
    public void ValidateMediaUrl_TooLongUrl_ShouldFailUrlTooLong()
    {
        // Arrange:
        var url = new string(
            'a',
            MediaAttachableConstant.Constraints.Url.MaxLength + 1);

        // Act:
        var result = MediaAttachableValidator.ValidateMediaUrl(url);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("MediaAttachable.Url.TooLong");
    }

    [Theory]
    [InlineData("ftp://example.com/photo.jpg")]
    [InlineData("not a url")]
    [InlineData("https://exa mple.com/photo.jpg")]
    public void ValidateMediaUrl_InvalidUrl_ShouldFailUrlInvalid(string url)
    {
        // Arrange:
        // Act:
        var result = MediaAttachableValidator.ValidateMediaUrl(url);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("MediaAttachable.Url.Invalid");
    }

    [Fact]
    public void ValidateMediaUrl_ValidUrl_ShouldSucceed()
    {
        // Arrange:
        // Act:
        var result = MediaAttachableValidator.ValidateMediaUrl(
            "https://example.com/media/photo.jpg");

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateMediaAltText_TooLongAltText_ShouldFailAltTextTooLong()
    {
        // Arrange:
        var altText = new string(
            'a',
            MediaAttachableConstant.Constraints.AltText.MaxLength + 1);

        // Act:
        var result = MediaAttachableValidator.ValidateMediaAltText(altText);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("MediaAttachable.AltText.TooLong");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A photo")]
    public void ValidateMediaAltText_NullOrWithinLimit_ShouldSucceed(string? altText)
    {
        // Arrange:
        // Act:
        var result = MediaAttachableValidator.ValidateMediaAltText(altText);

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateMediaType_BlankType_ShouldFailMediaTypeRequired(string? mediaType)
    {
        // Arrange:
        // Act:
        var result = MediaAttachableValidator.ValidateMediaType(mediaType);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("MediaAttachable.MediaType.Required");
    }

    [Fact]
    public void ValidateMediaType_UnknownType_ShouldFailMediaTypeInvalid()
    {
        // Arrange:
        // Act:
        var result = MediaAttachableValidator.ValidateMediaType("exe");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("MediaAttachable.MediaType.Invalid");
    }

    [Theory]
    [InlineData("image")]
    [InlineData("video")]
    [InlineData("audio")]
    [InlineData("document")]
    [InlineData(" Video ")]
    public void ValidateMediaType_AllowedType_ShouldSucceed(string mediaType)
    {
        // Arrange:
        // Act:
        var result = MediaAttachableValidator.ValidateMediaType(mediaType);

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void FailureCodes_ShouldMatchDocumentedCodes()
    {
        // Arrange:
        // Act:
        // Assert:
        MediaAttachableOutcome.Failure.EntityRequired.Code.Should().Be("MediaAttachable.Entity.Required");
        MediaAttachableOutcome.Failure.MediaRequired.Code.Should().Be("MediaAttachable.Media.Required");
        MediaAttachableOutcome.Failure.MediaDuplicate.Code.Should().Be("MediaAttachable.Media.Duplicate");
        MediaAttachableOutcome.Failure.MediaNotFound.Code.Should().Be("MediaAttachable.Media.NotFound");
        MediaAttachableOutcome.Failure.MediaTooMany.Code.Should().Be("MediaAttachable.Media.TooMany");
        MediaAttachableOutcome.Failure.UrlRequired.Code.Should().Be("MediaAttachable.Url.Required");
        MediaAttachableOutcome.Failure.UrlTooLong.Code.Should().Be("MediaAttachable.Url.TooLong");
        MediaAttachableOutcome.Failure.UrlInvalid.Code.Should().Be("MediaAttachable.Url.Invalid");
        MediaAttachableOutcome.Failure.AltTextTooLong.Code.Should().Be("MediaAttachable.AltText.TooLong");
        MediaAttachableOutcome.Failure.MediaTypeRequired.Code.Should().Be("MediaAttachable.MediaType.Required");
        MediaAttachableOutcome.Failure.MediaTypeInvalid.Code.Should().Be("MediaAttachable.MediaType.Invalid");
    }

    #endregion

    #region Constant Specs

    [Fact]
    public void Constants_ShouldMatchDocumentedValues()
    {
        // Arrange:
        // Act:
        // Assert:
        MediaAttachableConstant.Constraints.Url.MaxLength.Should().Be(2048);
        MediaAttachableConstant.Constraints.AltText.MaxLength.Should().Be(500);
        MediaAttachableConstant.Constraints.Collection.MinAttachments.Should().Be(0);
        MediaAttachableConstant.Constraints.Collection.MaxAttachments.Should().Be(20);
        MediaAttachableConstant.Defaults.MediaType.Default.Should().Be("image");
        MediaAttachableConstant.Defaults.MediaType.Allowed.Should().Equal(
            ["image", "video", "audio", "document"]);
        MediaAttachableConstant.Patterns.Url.Should().Be(@"^https?://[^\s/$.?#].[^\s]*$");
    }

    #endregion
}