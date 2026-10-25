using Core.Common.Monad.Results;

using Infrastructure.Caching;
using Infrastructure.Caching.Options;

namespace Infrastructure.Unit.Tests.Caching;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Caching")]
public sealed class CachingSettingValidationTest
{
    [Fact(DisplayName = "Valid defaults should return a successful result")]
    public void ValidDefaults_ShouldReturnSuccess()
    {
        Result<CachingSetting> result =
            CachingSettingValidation.Validate(new CachingSetting());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact(DisplayName = "Disabled caching should skip tier validation")]
    public void DisabledCaching_ShouldSkipTierValidation()
    {
        CachingSetting setting = new()
        {
            Enabled = false,
            Memory = new MemoryCacheSetting { DefaultExpirationMinutes = 0 },
            Distributed = new DistributedCacheSetting { DefaultExpirationMinutes = 0 }
        };

        Result<CachingSetting> result = CachingSettingValidation.Validate(setting);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Both tiers disabled should return the tier error")]
    public void BothTiersDisabled_ShouldReturnTierError()
    {
        CachingSetting setting = new()
        {
            Memory = new MemoryCacheSetting { Enabled = false },
            Distributed = new DistributedCacheSetting { Enabled = false }
        };

        Result<CachingSetting> result = CachingSettingValidation.Validate(setting);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == "Caching.Tier.Required");
    }

    [Fact(DisplayName = "Every broken rule should be returned in one result")]
    public void MultipleBrokenRules_ShouldReturnEveryError()
    {
        CachingSetting setting = new()
        {
            Memory = new MemoryCacheSetting
            {
                DefaultExpirationMinutes = 0,
                CompactionPercentage = 0,
                SizeLimitBytes = 0
            },
            Hybrid = new HybridCacheSetting
            {
                MaximumPayloadBytes = 0,
                MaximumKeyLength = 0
            }
        };

        Result<CachingSetting> result = CachingSettingValidation.Validate(setting);

        result.IsFailure.Should().BeTrue();
        result.Errors.Select(error => error.Code).Should().Contain(
        [
            "Caching.Memory.Expiration.OutOfRange",
            "Caching.Memory.Compaction.OutOfRange",
            "Caching.Memory.SizeLimit.OutOfRange",
            "Caching.Hybrid.Payload.OutOfRange",
            "Caching.Hybrid.KeyLength.OutOfRange"
        ]);
    }

    [Fact(DisplayName = "Describe should format every error with its code")]
    public void Describe_ShouldFormatEveryError()
    {
        CachingSetting setting = new()
        {
            Memory = new MemoryCacheSetting { Enabled = false },
            Distributed = new DistributedCacheSetting { Enabled = false }
        };

        Result<CachingSetting> result = CachingSettingValidation.Validate(setting);

        CachingSettingValidation.Describe(result)
            .Should()
            .Contain(message => message.StartsWith("Caching.Tier.Required: "));
    }
}