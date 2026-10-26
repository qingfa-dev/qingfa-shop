namespace Infrastructure.Integration.Tests.Caching;

/// <summary>
/// One caching fixture shared by every suite that exercises the cache stack.
/// </summary>
[CollectionDefinition(Name)]
public sealed class CachingCollection : ICollectionFixture<CachingFixture>
{
    /// <summary>The collection name every caching integration test opts into.</summary>
    public const string Name = "caching";
}
