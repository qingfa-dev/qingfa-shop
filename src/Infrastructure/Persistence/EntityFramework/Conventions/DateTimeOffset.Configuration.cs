using Infrastructure.Persistence.EntityFramework.Providers;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Conventions;

/// <summary>
/// Convention that configures <see cref="DateTimeOffset"/> properties with provider-appropriate types.
/// </summary>
/// <remarks>
/// PostgreSQL uses <c>timestamptz</c>, SQL Server uses <c>datetime2</c>, SQLite uses <c>TEXT</c>.
/// </remarks>
public static class DateTimeOffsetConvention
{
    /// <summary>Default precision for datetime columns.</summary>
    public const int DefaultPrecision = 6;

    /// <summary>
    /// Applies DateTimeOffset column type configuration to all entities in the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="providerKind">The database provider kind.</param>
    public static void Apply(ModelBuilder modelBuilder, PersistenceProviderKind providerKind)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                // Handle both DateTimeOffset and DateTimeOffset?
                Type? clrType = property.ClrType;
                bool isDateTimeOffset = clrType == typeof(DateTimeOffset) || clrType == typeof(DateTimeOffset?);

                if (!isDateTimeOffset)
                    continue;

                // Skip if already configured explicitly
                if (property.IsPrimaryKey() || property.IsForeignKey())
                    continue;

                // Skip if column type is already set
                if (property.GetColumnType() != null)
                    continue;

                var propertyBuilder = modelBuilder.Entity(entityType.ClrType)
                    .Property(property.Name);

                switch (providerKind)
                {
                    case PersistenceProviderKind.PostgreSql:
                        propertyBuilder.HasColumnType("timestamptz");
                        break;

                    case PersistenceProviderKind.SqlServer:
                        propertyBuilder.HasColumnType("datetime2");
                        propertyBuilder.HasPrecision(DefaultPrecision);
                        break;

                    case PersistenceProviderKind.Sqlite:
                        // SQLite stores timestamps as TEXT by default
                        propertyBuilder.HasColumnType("TEXT");
                        break;

                    case PersistenceProviderKind.InMemory:
                        // InMemory provider ignores column types
                        break;
                }
            }
        }
    }

    // ─── Fluent extension methods ─────────────────────────────────────────
}
