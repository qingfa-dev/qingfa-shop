namespace Infrastructure.Persistence.EntityFramework.Providers;

/// <summary>
/// Identifies the database provider used by the EF Core persistence layer.
/// </summary>
public enum PersistenceProviderKind
{
    /// <summary>Provider could not be identified.</summary>
    Unknown,

    /// <summary>In-memory provider for testing.</summary>
    InMemory,

    /// <summary>PostgreSQL via Npgsql.</summary>
    PostgreSql,

    /// <summary>SQLite for lightweight scenarios.</summary>
    Sqlite,

    /// <summary>Microsoft SQL Server.</summary>
    SqlServer
}
