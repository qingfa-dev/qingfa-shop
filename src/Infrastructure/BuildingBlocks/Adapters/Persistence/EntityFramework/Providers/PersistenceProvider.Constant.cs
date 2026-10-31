namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Providers;

/// <summary>EF Core provider assembly names used for detection.</summary>
public static class PersistenceProviderConstant
{
    /// <summary>Provider name strings reported by <c>Database.ProviderName</c>.</summary>
    public static class Names
    {
        /// <summary>In-memory provider.</summary>
        public const string InMemory = "Microsoft.EntityFrameworkCore.InMemory";

        /// <summary>PostgreSQL provider.</summary>
        public const string PostgreSql = "Npgsql.EntityFrameworkCore.PostgreSQL";

        /// <summary>SQLite provider.</summary>
        public const string Sqlite = "Microsoft.EntityFrameworkCore.Sqlite";

        /// <summary>SQL Server provider.</summary>
        public const string SqlServer = "Microsoft.EntityFrameworkCore.SqlServer";
    }
}
