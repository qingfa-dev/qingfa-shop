using Application.Common.Caching;

using Infrastructure.Caching;
using Infrastructure.Caching.Options;
using Infrastructure.Caching.Options.Distributed;
using Infrastructure.Caching.Options.Memory;

namespace Infrastructure.Unit.Tests.Caching;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Caching")]
public sealed class CachingOutcomeTest
{
    #region CachingOutcome.Failure

    [Fact(DisplayName = "KeyRequired should have correct code and message")]
    public void KeyRequired_ShouldHaveCorrectCodeAndMessage()
    {
        Error error = CachingOutcome.Failure.KeyRequired;

        error.Code.Should().Be("Caching.Key.Required");
        error.Message.Should().Contain("key");
    }

    [Fact(DisplayName = "FactoryRequired should have correct code and message")]
    public void FactoryRequired_ShouldHaveCorrectCodeAndMessage()
    {
        Error error = CachingOutcome.Failure.FactoryRequired;

        error.Code.Should().Be("Caching.Factory.Required");
        error.Message.Should().Contain("factory");
    }

    [Fact(DisplayName = "ValueRequired should have correct code and message")]
    public void ValueRequired_ShouldHaveCorrectCodeAndMessage()
    {
        Error error = CachingOutcome.Failure.ValueRequired;

        error.Code.Should().Be("Caching.Value.Required");
        error.Message.Should().Contain("value");
    }

    [Fact(DisplayName = "TagRequired should have correct code and message")]
    public void TagRequired_ShouldHaveCorrectCodeAndMessage()
    {
        Error error = CachingOutcome.Failure.TagRequired;

        error.Code.Should().Be("Caching.Tag.Required");
        error.Message.Should().Contain("tag");
    }

    #endregion

    #region CachingSettingOutcome.Failure

    [Fact(DisplayName = "TierRequired should have correct code and message")]
    public void TierRequired_ShouldHaveCorrectCodeAndMessage()
    {
        Error error = CachingSettingOutcome.Failure.TierRequired;

        error.Code.Should().Be("Caching.Tier.Required");
        error.Message.Should().Contain("Memory");
        error.Message.Should().Contain("Distributed");
    }

    [Fact(DisplayName = "ExpirationInverted should have correct code and message")]
    public void ExpirationInverted_ShouldHaveCorrectCodeAndMessage()
    {
        Error error = CachingSettingOutcome.Failure.ExpirationInverted;

        error.Code.Should().Be("Caching.Expiration.Inverted");
        error.Message.Should().Contain("must not exceed");
    }

    [Fact(DisplayName = "DistributedProviderMissing should have correct code and message")]
    public void DistributedProviderMissing_ShouldHaveCorrectCodeAndMessage()
    {
        Error error = CachingSettingOutcome.Failure.DistributedProviderMissing;

        error.Code.Should().Be("Caching.Distributed.Provider.Missing");
        error.Message.Should().Contain("IDistributedCache");
    }

    #endregion

    #region CachingConstant

    [Fact(DisplayName = "Key separator should be colon")]
    public void KeySeparator_ShouldBeColon()
    {
        CachingConstant.Key.Separator.Should().Be(':');
    }

    [Fact(DisplayName = "Key max length should be 1024")]
    public void KeyMaxLength_ShouldBe1024()
    {
        CachingConstant.Key.MaxLength.Should().Be(1024);
    }

    [Fact(DisplayName = "Tag separator should be colon")]
    public void TagSeparator_ShouldBeColon()
    {
        CachingConstant.Tag.Separator.Should().Be(':');
    }

    #endregion

    #region CachingSetting properties

    [Fact(DisplayName = "Memory DefaultExpiration should compute TimeSpan from minutes")]
    public void MemoryDefaultExpiration_ShouldComputeTimeSpan()
    {
        MemoryCacheSetting setting = new()
        {
            DefaultExpirationMinutes = 30
        };

        setting.DefaultExpiration.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact(DisplayName = "Distributed DefaultExpiration should compute TimeSpan from minutes")]
    public void DistributedDefaultExpiration_ShouldComputeTimeSpan()
    {
        DistributedCacheSetting setting = new()
        {
            DefaultExpirationMinutes = 60
        };

        setting.DefaultExpiration.Should().Be(TimeSpan.FromMinutes(60));
    }

    [Fact(DisplayName = "CachingSetting should have correct defaults")]
    public void CachingSetting_ShouldHaveCorrectDefaults()
    {
        CachingSetting setting = new();

        setting.Enabled.Should().BeTrue();
        setting.Memory.Should().NotBeNull();
        setting.Distributed.Should().NotBeNull();
        setting.Hybrid.Should().NotBeNull();
    }

    #endregion
}
