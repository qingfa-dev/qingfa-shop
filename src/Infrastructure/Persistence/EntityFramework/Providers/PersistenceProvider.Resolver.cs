namespace Infrastructure.Persistence.EntityFramework.Providers;

/// <summary>
/// Detects the database provider from the EF Core provider name string.
/// </summary>
public static class PersistenceProviderDetector
{
    /// <summary>Provider name for InMemory.</summary>
    public const string InMemoryProviderName =
        "Microsoft.EntityFrameworkCore.InMemory";

    /// <summary>Provider name for PostgreSQL via Npgsql.</summary>
    public const string PostgreSqlProviderName =
        "Npgsql.EntityFrameworkCore.PostgreSQL";

    /// <summary>Provider name for SQLite.</summary>
    public const string SqliteProviderName =
        "Microsoft.EntityFrameworkCore.Sqlite";

    /// <summary>Provider name for SQL Server.</summary>
    public const string SqlServerProviderName =
        "Microsoft.EntityFrameworkCore.SqlServer";

    /// <summary>
    /// Detects the provider kind from the given provider name.
    /// </summary>
    /// <param name="providerName">The provider name string from EF Core options.</param>
    /// <returns>The corresponding <see cref="PersistenceProviderKind"/>.</returns>
    public static PersistenceProviderKind Detect(string? providerName)
    {
        return providerName switch
        {
            InMemoryProviderName => PersistenceProviderKind.InMemory,
            PostgreSqlProviderName => PersistenceProviderKind.PostgreSql,
            SqliteProviderName => PersistenceProviderKind.Sqlite,
            SqlServerProviderName => PersistenceProviderKind.SqlServer,
            _ => PersistenceProviderKind.Unknown
        };
    }

}
