namespace Infrastructure.Integration.Tests.Persistence.Initialization;

/// <summary>
/// One PostgreSQL fixture shared by migration, seeder, resetter, and initializer tests.
/// Tests in this collection run serially because each one owns the database state.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PersistenceInitializationCollection : ICollectionFixture<PersistenceInitializationFixture>
{
    /// <summary>The collection name every persistence initialization test opts into.</summary>
    public const string Name = "persistence-initialization";
}
