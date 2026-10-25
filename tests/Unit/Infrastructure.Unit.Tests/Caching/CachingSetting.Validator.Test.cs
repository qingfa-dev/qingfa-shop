using Infrastructure.Caching;
using Infrastructure.Caching.Options;

using Microsoft.Extensions.Options;

namespace Infrastructure.Unit.Tests.Caching;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Caching")]
public sealed class CachingSettingValidatorTest
{
    private readonly CachingSettingValidator _validator = new();

    [Fact(DisplayName = "Valid defaults should pass validation")]
    public void ValidDefaults_ShouldPassValidation()
    {
        ValidateOptionsResult result = _validator.Validate(null, new CachingSetting());

        result.Succeeded.Should().BeTrue();
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

        ValidateOptionsResult result = _validator.Validate(null, setting);

        result.Succeeded.Should().BeTrue();
    }

    [Theory(DisplayName = "Memory expiration out of range should fail")]
    [InlineData(0)]
    [InlineData(1441)]
    public void MemoryExpiration_OutOfRange_ShouldFail(int value)
    {
        CachingSetting setting = new()
        {
            Memory = new MemoryCacheSetting { DefaultExpirationMinutes = value }
        };

        ValidateOptionsResult result = _validator.Validate(null, setting);

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("Caching:Memory:DefaultExpirationMinutes");
    }

    [Theory(DisplayName = "Compaction percentage out of range should fail")]
    [InlineData(0)]
    [InlineData(101)]
    public void CompactionPercentage_OutOfRange_ShouldFail(int value)
    {
        CachingSetting setting = new()
        {
            Memory = new MemoryCacheSetting { CompactionPercentage = value }
        };

        ValidateOptionsResult result = _validator.Validate(null, setting);

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("Caching:Memory:CompactionPercentage");
    }

    [Fact(DisplayName = "Size limit below one byte should fail")]
    public void SizeLimit_BelowOneByte_ShouldFail()
    {
        CachingSetting setting = new()
        {
            Memory = new MemoryCacheSetting { SizeLimitBytes = 0 }
        };

        ValidateOptionsResult result = _validator.Validate(null, setting);

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("Caching:Memory:SizeLimitBytes");
    }

    [Fact(DisplayName = "Null size limit should pass")]
    public void SizeLimit_Null_ShouldPass()
    {
        CachingSetting setting = new()
        {
            Memory = new MemoryCacheSetting { SizeLimitBytes = null }
        };

        ValidateOptionsResult result = _validator.Validate(null, setting);

        result.Succeeded.Should().BeTrue();
    }

    [Theory(DisplayName = "Distributed expiration out of range should fail")]
    [InlineData(0)]
    [InlineData(1441)]
    public void DistributedExpiration_OutOfRange_ShouldFail(int value)
    {
        CachingSetting setting = new()
        {
            Distributed = new DistributedCacheSetting { DefaultExpirationMinutes = value }
        };

        ValidateOptionsResult result = _validator.Validate(null, setting);

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("Caching:Distributed:DefaultExpirationMinutes");
    }

    [Theory(DisplayName = "Maximum payload out of range should fail")]
    [InlineData(0)]
    [InlineData(10 * 1024 * 1024 + 1)]
    public void MaximumPayload_OutOfRange_ShouldFail(long value)
    {
        CachingSetting setting = new()
        {
            Hybrid = new HybridCacheSetting { MaximumPayloadBytes = value }
        };

        ValidateOptionsResult result = _validator.Validate(null, setting);

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("Caching:Hybrid:MaximumPayloadBytes");
    }

    [Theory(DisplayName = "Maximum key length out of range should fail")]
    [InlineData(0)]
    [InlineData(2049)]
    public void MaximumKeyLength_OutOfRange_ShouldFail(int value)
    {
        CachingSetting setting = new()
        {
            Hybrid = new HybridCacheSetting { MaximumKeyLength = value }
        };

        ValidateOptionsResult result = _validator.Validate(null, setting);

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("Caching:Hybrid:MaximumKeyLength");
    }

    [Fact(DisplayName = "Both tiers disabled should fail")]
    public void BothTiersDisabled_ShouldFail()
    {
        CachingSetting setting = new()
        {
            Memory = new MemoryCacheSetting { Enabled = false },
            Distributed = new DistributedCacheSetting { Enabled = false }
        };

        ValidateOptionsResult result = _validator.Validate(null, setting);

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("Caching.Tier.Required");
    }

    [Fact(DisplayName = "Memory expiration longer than distributed should fail")]
    public void MemoryExpiration_LongerThanDistributed_ShouldFail()
    {
        CachingSetting setting = new()
        {
            Memory = new MemoryCacheSetting { DefaultExpirationMinutes = 120 },
            Distributed = new DistributedCacheSetting { DefaultExpirationMinutes = 60 }
        };

        ValidateOptionsResult result = _validator.Validate(null, setting);

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("must not exceed");
    }
}