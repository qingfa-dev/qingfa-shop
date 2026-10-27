using Application.Common.Caching;

using Infrastructure.Caching.Services;

using Microsoft.Extensions.Caching.Hybrid;

namespace Infrastructure.Unit.Tests.Caching.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Caching")]
public sealed class CachingEntryOptionMapperTest
{
    [Fact(DisplayName = "ToHybridCacheEntryOptions with null should return null")]
    public void ToHybridCacheEntryOptions_WithNull_ShouldReturnNull()
    {
        HybridCacheEntryOptions? result = CachingEntryOptionMapper.ToHybridCacheEntryOptions(null);

        result.Should().BeNull();
    }

    [Fact(DisplayName = "ToHybridCacheEntryOptions with default options should map expiration only")]
    public void ToHybridCacheEntryOptions_WithDefaultOptions_ShouldMapExpiration()
    {
        CachingEntryOption option = new()
        {
            Expiration = TimeSpan.FromMinutes(30)
        };

        HybridCacheEntryOptions? result = CachingEntryOptionMapper.ToHybridCacheEntryOptions(option);

        result.Should().NotBeNull();
        result!.Expiration.Should().Be(TimeSpan.FromMinutes(30));
        result.LocalCacheExpiration.Should().BeNull();
        result.Flags.Should().BeNull();
    }

    [Fact(DisplayName = "ToHybridCacheEntryOptions should map local cache expiration")]
    public void ToHybridCacheEntryOptions_ShouldMapLocalCacheExpiration()
    {
        CachingEntryOption option = new()
        {
            Expiration = TimeSpan.FromMinutes(30),
            LocalCacheExpiration = TimeSpan.FromMinutes(5)
        };

        HybridCacheEntryOptions? result = CachingEntryOptionMapper.ToHybridCacheEntryOptions(option);

        result.Should().NotBeNull();
        result!.LocalCacheExpiration.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact(DisplayName = "ToHybridCacheEntryOptions should map DisableLocalCache flag")]
    public void ToHybridCacheEntryOptions_ShouldMapDisableLocalCacheFlag()
    {
        CachingEntryOption option = new()
        {
            DisableLocalCache = true
        };

        HybridCacheEntryOptions? result = CachingEntryOptionMapper.ToHybridCacheEntryOptions(option);

        result.Should().NotBeNull();
        result!.Flags.Should().Be(HybridCacheEntryFlags.DisableLocalCache);
    }

    [Fact(DisplayName = "ToHybridCacheEntryOptions should map DisableDistributedCache flag")]
    public void ToHybridCacheEntryOptions_ShouldMapDisableDistributedCacheFlag()
    {
        CachingEntryOption option = new()
        {
            DisableDistributedCache = true
        };

        HybridCacheEntryOptions? result = CachingEntryOptionMapper.ToHybridCacheEntryOptions(option);

        result.Should().NotBeNull();
        result!.Flags.Should().Be(HybridCacheEntryFlags.DisableDistributedCache);
    }

    [Fact(DisplayName = "ToHybridCacheEntryOptions should combine both disable flags")]
    public void ToHybridCacheEntryOptions_ShouldCombineBothDisableFlags()
    {
        CachingEntryOption option = new()
        {
            DisableLocalCache = true,
            DisableDistributedCache = true
        };

        HybridCacheEntryOptions? result = CachingEntryOptionMapper.ToHybridCacheEntryOptions(option);

        result.Should().NotBeNull();
        result!.Flags.Should().Be(
            HybridCacheEntryFlags.DisableLocalCache | HybridCacheEntryFlags.DisableDistributedCache);
    }

    [Fact(DisplayName = "ToHybridCacheEntryOptions with all options set should map everything")]
    public void ToHybridCacheEntryOptions_WithAllOptions_ShouldMapEverything()
    {
        CachingEntryOption option = new()
        {
            Expiration = TimeSpan.FromMinutes(60),
            LocalCacheExpiration = TimeSpan.FromMinutes(10),
            DisableLocalCache = true,
            DisableDistributedCache = false
        };

        HybridCacheEntryOptions? result = CachingEntryOptionMapper.ToHybridCacheEntryOptions(option);

        result.Should().NotBeNull();
        result!.Expiration.Should().Be(TimeSpan.FromMinutes(60));
        result.LocalCacheExpiration.Should().Be(TimeSpan.FromMinutes(10));
        result.Flags.Should().Be(HybridCacheEntryFlags.DisableLocalCache);
    }

    [Fact(DisplayName = "ToHybridCacheEntryOptions with no flags should have null Flags")]
    public void ToHybridCacheEntryOptions_WithNoFlags_ShouldHaveNullFlags()
    {
        CachingEntryOption option = new()
        {
            Expiration = TimeSpan.FromMinutes(15),
            DisableLocalCache = false,
            DisableDistributedCache = false
        };

        HybridCacheEntryOptions? result = CachingEntryOptionMapper.ToHybridCacheEntryOptions(option);

        result.Should().NotBeNull();
        result!.Flags.Should().BeNull();
    }
}
