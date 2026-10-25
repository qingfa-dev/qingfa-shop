using Testing.Fixtures;

namespace Api.Tests;

/// <summary>One PostgreSQL container shared by every test that needs real SQL.</summary>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    /// <summary>The collection name tests opt into.</summary>
    public const string Name = "postgres";
}