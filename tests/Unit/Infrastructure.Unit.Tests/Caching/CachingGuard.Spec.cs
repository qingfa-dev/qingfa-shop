using Infrastructure.Caching.Services;

namespace Infrastructure.Unit.Tests.Caching;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Caching")]
public sealed class CachingGuardTest
{
    #region Key

    [Fact(DisplayName = "Key with valid key should not throw")]
    public void Key_WithValidKey_ShouldNotThrow()
    {
        Action act = () => CachingGuard.Key("valid-key");

        act.Should().NotThrow();
    }

    [Theory(DisplayName = "Key with blank value should throw")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Key_WithBlankValue_ShouldThrow(string? value)
    {
        Action act = () => CachingGuard.Key(value!);

        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("key")
            .WithMessage("*Caching.Key.Required*");
    }

    #endregion

    #region Factory

    [Fact(DisplayName = "Factory with valid factory should not throw")]
    public void Factory_WithValidFactory_ShouldNotThrow()
    {
        Func<CancellationToken, ValueTask<string>> factory = _ => ValueTask.FromResult("value");

        Action act = () => CachingGuard.Factory(factory);

        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Factory with null should throw")]
    public void Factory_WithNull_ShouldThrow()
    {
        Action act = () => CachingGuard.Factory(null!);

        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("factory")
            .WithMessage("*Caching.Factory.Required*");
    }

    #endregion

    #region Value

    [Fact(DisplayName = "Value with valid value should not throw")]
    public void Value_WithValidValue_ShouldNotThrow()
    {
        Action act = () => CachingGuard.Value("some-value");

        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Value with null should throw")]
    public void Value_WithNull_ShouldThrow()
    {
        Action act = () => CachingGuard.Value(null);

        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("value")
            .WithMessage("*Caching.Value.Required*");
    }

    [Fact(DisplayName = "Value with empty string should not throw")]
    public void Value_WithEmptyString_ShouldNotThrow()
    {
        Action act = () => CachingGuard.Value("");

        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Value with boxed null int should throw")]
    public void Value_WithBoxedNullInt_ShouldThrow()
    {
        int? nullableInt = null;

        Action act = () => CachingGuard.Value(nullableInt);

        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("value")
            .WithMessage("*Caching.Value.Required*");
    }

    #endregion

    #region Tag

    [Fact(DisplayName = "Tag with valid tag should not throw")]
    public void Tag_WithValidTag_ShouldNotThrow()
    {
        Action act = () => CachingGuard.Tag("valid-tag");

        act.Should().NotThrow();
    }

    [Theory(DisplayName = "Tag with blank value should throw")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t")]
    public void Tag_WithBlankValue_ShouldThrow(string? value)
    {
        Action act = () => CachingGuard.Tag(value!);

        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("tag")
            .WithMessage("*Caching.Tag.Required*");
    }

    #endregion
}
