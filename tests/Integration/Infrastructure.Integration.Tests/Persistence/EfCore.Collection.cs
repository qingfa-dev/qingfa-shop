namespace Infrastructure.Integration.Tests.Persistence;

/// <summary>
/// One EF Core fixture shared by every persistence integration test.
/// </summary>
[CollectionDefinition(Name)]
public sealed class EfCoreCollection : ICollectionFixture<EfCoreFixture>
{
    /// <summary>The collection name every EF Core integration test opts into.</summary>
    public const string Name = "efcore";
}
