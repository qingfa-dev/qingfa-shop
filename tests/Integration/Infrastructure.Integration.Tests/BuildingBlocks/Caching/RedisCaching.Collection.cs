namespace Infrastructure.Integration.Tests.BuildingBlocks.Caching;

/// <summary>
/// One Redis-backed caching fixture shared by every suite that exercises the cache
/// stack with a real Redis container.
/// </summary>
[CollectionDefinition(Name)]
public sealed class RedisCachingCollection : ICollectionFixture<RedisCachingFixture>
{
    /// <summary>The collection name every Redis caching integration test opts into.</summary>
    public const string Name = "redis-caching";
}
