using Infrastructure.BuildingBlocks.Adapters.Caching.Options;
using Infrastructure.BuildingBlocks.Adapters.Caching.Options.Distributed;
using Infrastructure.BuildingBlocks.Adapters.Caching.Options.Hybrid;
using Infrastructure.BuildingBlocks.Adapters.Caching.Options.Memory;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Caching.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Caching")]
public sealed class CachingSettingValidationTest
{
    [Fact(DisplayName = "Valid defaults should pass validation")]
    public void ValidDefaults_ShouldPassValidation()
    {
        Result<CachingSetting> result = CachingSettingValidation.Validate(new CachingSetting());
        result.IsSuccess.Should().BeTrue();
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

    [Theory(DisplayName = "Memory expiration out of range should fail")]
    [InlineData(0)]
    [InlineData(1441)]
    public void MemoryExpiration_OutOfRange_ShouldFail(int value)
    {
        CachingSetting setting = new()
        {
            Distributed = new DistributedCacheSetting { Enabled = false }
        };
        setting.Memory.DefaultExpirationMinutes = value;

        Result<CachingSetting> result = CachingSettingValidation.Validate(setting);
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code.Contains("Memory.Expiration"));
    }

    [Theory(DisplayName = "Compaction percentage out of range should fail")]
    [InlineData(0)]
    [InlineData(101)]
    public void CompactionPercentage_OutOfRange_ShouldFail(int value)
    {
        CachingSetting setting = new()
        {
            Distributed = new DistributedCacheSetting { Enabled = false }
        };
        setting.Memory.CompactionPercentage = value;

        Result<CachingSetting> result = CachingSettingValidation.Validate(setting);
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code.Contains("Memory.Compaction"));
    }

    [Fact(DisplayName = "Size limit below one byte should fail")]
    public void SizeLimit_BelowOneByte_ShouldFail()
    {
        CachingSetting setting = new()
        {
            Distributed = new DistributedCacheSetting { Enabled = false }
        };
        setting.Memory.SizeLimitBytes = 0;

        Result<CachingSetting> result = CachingSettingValidation.Validate(setting);
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code.Contains("Memory.SizeLimit"));
    }

    [Fact(DisplayName = "Null size limit should pass")]
    public void SizeLimit_Null_ShouldPass()
    {
        CachingSetting setting = new()
        {
            Distributed = new DistributedCacheSetting { Enabled = false },
            Memory = new MemoryCacheSetting { SizeLimitBytes = null }
        };

        Result<CachingSetting> result = CachingSettingValidation.Validate(setting);
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "Distributed expiration out of range should fail")]
    [InlineData(0)]
    [InlineData(1441)]
    public void DistributedExpiration_OutOfRange_ShouldFail(int value)
    {
        CachingSetting setting = new()
        {
            Distributed = new DistributedCacheSetting
            {
                Enabled = true,
                Provider = "redis",
                ConnectionStringName = "Cache",
                DefaultExpirationMinutes = value
            }
        };

        Result<CachingSetting> result = CachingSettingValidation.Validate(setting);
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code.Contains("Distributed.Expiration"));
    }

    [Theory(DisplayName = "Maximum payload out of range should fail")]
    [InlineData(0)]
    [InlineData(10 * 1024 * 1024 + 1)]
    public void MaximumPayload_OutOfRange_ShouldFail(long value)
    {
        CachingSetting setting = new()
        {
            Distributed = new DistributedCacheSetting { Enabled = false },
            Hybrid = new HybridCacheSetting { MaximumPayloadBytes = value }
        };

        Result<CachingSetting> result = CachingSettingValidation.Validate(setting);
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code.Contains("Hybrid.Payload"));
    }

    [Theory(DisplayName = "Maximum key length out of range should fail")]
    [InlineData(0)]
    [InlineData(2049)]
    public void MaximumKeyLength_OutOfRange_ShouldFail(int value)
    {
        CachingSetting setting = new()
        {
            Distributed = new DistributedCacheSetting { Enabled = false },
            Hybrid = new HybridCacheSetting { MaximumKeyLength = value }
        };

        Result<CachingSetting> result = CachingSettingValidation.Validate(setting);
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code.Contains("Hybrid.KeyLength"));
    }

    [Fact(DisplayName = "Both tiers disabled should fail")]
    public void BothTiersDisabled_ShouldFail()
    {
        CachingSetting setting = new()
        {
            Memory = new MemoryCacheSetting { Enabled = false },
            Distributed = new DistributedCacheSetting { Enabled = false }
        };

        Result<CachingSetting> result = CachingSettingValidation.Validate(setting);
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code.Contains("Caching.Tier.Required"));
    }

    [Fact(DisplayName = "Memory expiration longer than distributed should fail")]
    public void MemoryExpiration_LongerThanDistributed_ShouldFail()
    {
        CachingSetting setting = new()
        {
            Memory = new MemoryCacheSetting { DefaultExpirationMinutes = 120 },
            Distributed = new DistributedCacheSetting
            {
                Enabled = true,
                Provider = "redis",
                ConnectionStringName = "Cache",
                DefaultExpirationMinutes = 60
            }
        };

        Result<CachingSetting> result = CachingSettingValidation.Validate(setting);
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("must not exceed"));
    }
}