using Infrastructure.BuildingBlocks.Adapters.Security.Authentication.Tokens.Stores;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Authentication.Tokens.Stores;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Blacklist")]
public sealed class DistributedTokenBlacklistStoreTest
{
    [Fact(DisplayName = "Distributed blacklist store should set and read entries")]
    public async Task Store_ShouldSetAndRead()
    {
        var cache = new MemoryDistributedCache(
            Options.Create(new MemoryDistributedCacheOptions()));
        var store = new DistributedTokenBlacklistStore(cache);

        (await store.SetAsync("blacklist:jti", TimeSpan.FromMinutes(5))).IsSuccess
            .Should().BeTrue();

        Result<bool> exists = await store.ExistsAsync("blacklist:jti");
        exists.IsSuccess.Should().BeTrue();
        exists.Value.Should().BeTrue();

        Result<bool> missing = await store.ExistsAsync("blacklist:other");
        missing.Value.Should().BeFalse();
    }
}
