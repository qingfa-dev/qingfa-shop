using Application.Common.Caching;

using Infrastructure.Caching.Services;

namespace Infrastructure.Unit.Tests.Caching.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Caching")]
public sealed class CachingGuardTest
{
    [Fact(DisplayName = "Key with valid key should return null")]
    public void Key_WithValidKey_ShouldReturnNull()
    {
        CachingGuard.Key("valid-key").Should().BeNull();
    }

    [Theory(DisplayName = "Key with blank value should return KeyRequired")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Key_WithBlankValue_ShouldReturnKeyRequired(string? value)
    {
        Error? error = CachingGuard.Key(value!);

        error.Should().NotBeNull();
        error!.Code.Should().Be("Caching.Key.Required");
    }

    [Fact(DisplayName = "Key beyond the maximum length should return KeyTooLong")]
    public void Key_TooLong_ShouldReturnKeyTooLong()
    {
        string key = new('k', CachingConstant.Key.MaxLength + 1);

        Error? error = CachingGuard.Key(key);

        error.Should().NotBeNull();
        error!.Code.Should().Be("Caching.Key.TooLong");
        error.Message.Should().Contain((CachingConstant.Key.MaxLength + 1).ToString());
    }

    [Fact(DisplayName = "Factory with valid factory should return null")]
    public void Factory_WithValidFactory_ShouldReturnNull()
    {
        Func<CancellationToken, ValueTask<string>> factory = _ => ValueTask.FromResult("value");

        CachingGuard.Factory(factory).Should().BeNull();
    }

    [Fact(DisplayName = "Factory with null should return FactoryRequired")]
    public void Factory_WithNull_ShouldReturnFactoryRequired()
    {
        Error? error = CachingGuard.Factory(null);

        error.Should().NotBeNull();
        error!.Code.Should().Be("Caching.Factory.Required");
    }

    [Fact(DisplayName = "Value with valid value should return null")]
    public void Value_WithValidValue_ShouldReturnNull()
    {
        CachingGuard.Value("some-value").Should().BeNull();
    }

    [Fact(DisplayName = "Value with null should return ValueRequired")]
    public void Value_WithNull_ShouldReturnValueRequired()
    {
        Error? error = CachingGuard.Value(null);

        error.Should().NotBeNull();
        error!.Code.Should().Be("Caching.Value.Required");
    }

    [Fact(DisplayName = "Value with empty string should return null")]
    public void Value_WithEmptyString_ShouldReturnNull()
    {
        CachingGuard.Value("").Should().BeNull();
    }

    [Fact(DisplayName = "Value with boxed null int should return ValueRequired")]
    public void Value_WithBoxedNullInt_ShouldReturnValueRequired()
    {
        int? nullableInt = null;

        Error? error = CachingGuard.Value(nullableInt);

        error.Should().NotBeNull();
        error!.Code.Should().Be("Caching.Value.Required");
    }

    [Fact(DisplayName = "Tag with valid tag should return null")]
    public void Tag_WithValidTag_ShouldReturnNull()
    {
        CachingGuard.Tag("valid-tag").Should().BeNull();
    }

    [Theory(DisplayName = "Tag with blank value should return TagRequired")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t")]
    public void Tag_WithBlankValue_ShouldReturnTagRequired(string? value)
    {
        Error? error = CachingGuard.Tag(value!);

        error.Should().NotBeNull();
        error!.Code.Should().Be("Caching.Tag.Required");
    }

    [Fact(DisplayName = "Tags with null collection should be treated as no optional tags")]
    public void Tags_WithNullCollection_ShouldReturnNull()
    {
        CachingGuard.Tags(null).Should().BeNull();
    }

    [Fact(DisplayName = "TagsRequired with null collection should return TagsRequired")]
    public void TagsRequired_WithNullCollection_ShouldReturnTagsRequired()
    {
        Error? error = CachingGuard.TagsRequired(null);

        error.Should().NotBeNull();
        error!.Code.Should().Be("Caching.Tags.Required");
    }

    [Fact(DisplayName = "Tags with a blank member should return TagRequired")]
    public void Tags_WithBlankMember_ShouldReturnTagRequired()
    {
        Error? error = CachingGuard.Tags(["valid", " "]);

        error.Should().NotBeNull();
        error!.Code.Should().Be("Caching.Tag.Required");
    }

    [Fact(DisplayName = "Tags with valid members should return null")]
    public void Tags_WithValidMembers_ShouldReturnNull()
    {
        CachingGuard.Tags(["tag1", "tag2"]).Should().BeNull();
    }

    [Fact(DisplayName = "Options with null should return null")]
    public void Options_WithNull_ShouldReturnNull()
    {
        CachingGuard.Options(null).Should().BeNull();
    }

    [Fact(DisplayName = "Options with a zero expiration should return ExpirationInvalid")]
    public void Options_WithZeroExpiration_ShouldReturnExpirationInvalid()
    {
        CachingEntryOption options = new() { Expiration = TimeSpan.Zero };

        Error? error = CachingGuard.Options(options);

        error.Should().NotBeNull();
        error!.Code.Should().Be("Caching.Entry.ExpirationInvalid");
    }

    [Fact(DisplayName = "Options with a zero local expiration should return LocalExpirationInvalid")]
    public void Options_WithZeroLocalExpiration_ShouldReturnLocalExpirationInvalid()
    {
        CachingEntryOption options = new()
        {
            Expiration = TimeSpan.FromMinutes(10),
            LocalCacheExpiration = TimeSpan.Zero,
        };

        Error? error = CachingGuard.Options(options);

        error.Should().NotBeNull();
        error!.Code.Should().Be("Caching.Entry.LocalExpirationInvalid");
    }

    [Fact(DisplayName = "Options where local exceeds absolute should return LocalExpirationExceedsAbsolute")]
    public void Options_WithLocalExceedingAbsolute_ShouldReturnContradiction()
    {
        CachingEntryOption options = new()
        {
            Expiration = TimeSpan.FromMinutes(5),
            LocalCacheExpiration = TimeSpan.FromMinutes(10),
        };

        Error? error = CachingGuard.Options(options);

        error.Should().NotBeNull();
        error!.Code.Should().Be("Caching.Entry.LocalExpirationExceedsAbsolute");
    }

    [Fact(DisplayName = "Options with a valid lifetime should return null")]
    public void Options_WithValidLifetime_ShouldReturnNull()
    {
        CachingEntryOption options = new()
        {
            Expiration = TimeSpan.FromMinutes(30),
            LocalCacheExpiration = TimeSpan.FromMinutes(5),
        };

        CachingGuard.Options(options).Should().BeNull();
    }
}
