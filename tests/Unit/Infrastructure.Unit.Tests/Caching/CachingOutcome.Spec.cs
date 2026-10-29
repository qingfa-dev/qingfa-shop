using Application.Common.Caching;

using Infrastructure.Caching.Options;
using Infrastructure.Caching.Options.Distributed;
using Infrastructure.Caching.Options.Memory;

namespace Infrastructure.Unit.Tests.Caching;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Caching")]
public sealed class CachingOutcomeTest
{
    #region CachingOutcome template

    [Fact(DisplayName = "Code should compose the stable dotted code")]
    public void Code_ShouldComposeStableCode()
    {
        CachingOutcome.Code("Read", "ProviderUnavailable")
            .Should().Be("Caching.Read.ProviderUnavailable");
    }

    [Fact(DisplayName = "Invalid should create a caller-contract error")]
    public void Invalid_ShouldCreateBadRequest()
    {
        Error error = CachingOutcome.Invalid("Key", "Required", "A key is required.");

        error.Code.Should().Be("Caching.Key.Required");
        error.Status.Should().Be(400);
    }

    [Fact(DisplayName = "Unavailable should create a dependency error")]
    public void Unavailable_ShouldCreateServiceUnavailable()
    {
        Error error = CachingOutcome.Unavailable("Read", "ProviderUnavailable", "Unavailable.");

        error.Code.Should().Be("Caching.Read.ProviderUnavailable");
        error.Status.Should().Be(503);
    }

    [Fact(DisplayName = "Fault should create an internal error")]
    public void Fault_ShouldCreateInternalServerError()
    {
        Error error = CachingOutcome.Fault("Serialization", "Failed", "Fault.");

        error.Code.Should().Be("Caching.Serialization.Failed");
        error.Status.Should().Be(500);
    }

    #endregion

    #region CachingOutcome.Failure

    [Fact(DisplayName = "KeyRequired should have correct code and message")]
    public void KeyRequired_ShouldHaveCorrectCodeAndMessage()
    {
        Error error = CachingOutcome.Failure.KeyRequired;

        error.Code.Should().Be("Caching.Key.Required");
        error.Message.Should().Contain("key");
        error.Status.Should().Be(400);
    }

    [Fact(DisplayName = "KeyTooLong should have correct code and message")]
    public void KeyTooLong_ShouldHaveCorrectCodeAndMessage()
    {
        Error error = CachingOutcome.Failure.KeyTooLong(2048);

        error.Code.Should().Be("Caching.Key.TooLong");
        error.Message.Should().Contain("2048");
        error.Status.Should().Be(400);
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

    [Fact(DisplayName = "TagsRequired should have correct code and message")]
    public void TagsRequired_ShouldHaveCorrectCodeAndMessage()
    {
        Error error = CachingOutcome.Failure.TagsRequired;

        error.Code.Should().Be("Caching.Tags.Required");
        error.Message.Should().Contain("tag");
    }

    [Fact(DisplayName = "Entry lifetime failures should have specific codes")]
    public void EntryLifetimeFailures_ShouldHaveSpecificCodes()
    {
        CachingOutcome.Failure.ExpirationInvalid.Code
            .Should().Be("Caching.Entry.ExpirationInvalid");
        CachingOutcome.Failure.LocalExpirationInvalid.Code
            .Should().Be("Caching.Entry.LocalExpirationInvalid");
        CachingOutcome.Failure.LocalExpirationExceedsAbsolute.Code
            .Should().Be("Caching.Entry.LocalExpirationExceedsAbsolute");
    }

    [Fact(DisplayName = "Operation failures should be availability errors with the key")]
    public void OperationFailures_ShouldBeAvailabilityErrors()
    {
        Error read = CachingOutcome.Failure.ReadFailed("user:1");
        Error write = CachingOutcome.Failure.WriteFailed("user:1");
        Error remove = CachingOutcome.Failure.RemoveFailed("user:1");
        Error evict = CachingOutcome.Failure.EvictFailed("group");
        Error evictAll = CachingOutcome.Failure.EvictFailed(tag: null);

        read.Code.Should().Be("Caching.Read.ProviderUnavailable");
        read.Message.Should().Contain("user:1");
        write.Code.Should().Be("Caching.Write.ProviderUnavailable");
        remove.Code.Should().Be("Caching.Remove.ProviderUnavailable");
        evict.Code.Should().Be("Caching.Evict.ProviderUnavailable");
        evict.Message.Should().Contain("group");
        evictAll.Message.Should().NotContain("group");
        read.Status.Should().Be(503);
    }

    [Fact(DisplayName = "Operation failures should include the exception type")]
    public void OperationFailures_ShouldIncludeExceptionType()
    {
        Error error = CachingOutcome.Failure.ReadFailed(
            "key",
            new TimeoutException("timed out"));

        error.Message.Should().Contain("TimeoutException");
    }

    [Fact(DisplayName = "SerializationFailed should be an internal error with key and type")]
    public void SerializationFailed_ShouldBeInternalError()
    {
        Error error = CachingOutcome.Failure.SerializationFailed(
            "key",
            typeof(string),
            new InvalidOperationException());

        error.Code.Should().Be("Caching.Serialization.Failed");
        error.Message.Should().Contain("key");
        error.Message.Should().Contain("System.String");
        error.Status.Should().Be(500);
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
