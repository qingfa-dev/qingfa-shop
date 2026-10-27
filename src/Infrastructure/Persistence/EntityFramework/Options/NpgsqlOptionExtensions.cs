using Application.Common.Persistence;

using Microsoft.EntityFrameworkCore;

using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Infrastructure.Persistence.EntityFramework.Options;

/// <summary>PostgreSQL option helpers shared by runtime and design-time configuration.</summary>
public static class NpgsqlOptionExtensions
{
    /// <summary>The EF Core migration-history table name.</summary>
    public const string MigrationHistoryTable = "__EFMigrationsHistory";

    /// <summary>
    /// Moves the EF Core migration history into the persistence default schema.
    /// </summary>
    /// <remarks>
    /// EF Core keeps an unconfigured history table in <c>public</c>, outside the
    /// schema the migrations create. Naming it explicitly keeps history and module
    /// tables in the same schema, so a schema reset removes both.
    /// </remarks>
    /// <param name="options">The PostgreSQL options builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static NpgsqlDbContextOptionsBuilder UsePersistenceMigrationHistory(
        this NpgsqlDbContextOptionsBuilder options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.MigrationsHistoryTable(
            MigrationHistoryTable,
            PersistenceConstant.Schema.Default);
    }
}
