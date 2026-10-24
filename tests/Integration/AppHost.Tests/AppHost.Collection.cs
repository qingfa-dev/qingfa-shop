namespace AppHost.IntegrationTests;

/// <summary>Collapses every AppHost test onto one shared orchestration.</summary>
[CollectionDefinition(Name)]
public sealed class AppHostCollection : ICollectionFixture<AppHostFixture>
{
    /// <summary>The collection name every AppHost test opts into.</summary>
    public const string Name = "apphost";
}