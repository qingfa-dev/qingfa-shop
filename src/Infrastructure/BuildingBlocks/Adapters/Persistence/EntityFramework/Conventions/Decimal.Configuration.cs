using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Providers;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Conventions;

/// <summary>
/// Convention that configures <see cref="decimal"/> properties with provider-appropriate precision.
/// </summary>
/// <remarks>
/// Defaults to precision 18 and scale 6 for all providers.
/// PostgreSQL uses <c>numeric(18,6)</c>, SQL Server uses <c>decimal(18,6)</c>.
/// </remarks>
public static class DecimalConvention
{
    /// <summary>Default precision for decimal columns.</summary>
    public const int DefaultPrecision = DecimalConstant.DefaultPrecision;

    /// <summary>Default scale for decimal columns.</summary>
    public const int DefaultScale = DecimalConstant.DefaultScale;

    /// <summary>
    /// Applies decimal precision configuration to all decimal properties in the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="providerKind">The database provider kind.</param>
    public static void Apply(ModelBuilder modelBuilder, PersistenceProviderKind providerKind)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Skip: owned value objects are configured through their owner's nested builder.
            if (entityType.IsOwned())
                continue;

            foreach (var property in entityType.GetProperties())
            {
                // Handle both decimal and decimal?
                Type? clrType = property.ClrType;
                bool isDecimal = clrType == typeof(decimal) || clrType == typeof(decimal?);

                if (!isDecimal)
                    continue;

                // Skip if already configured with precision/scale
                if (property.GetPrecision() != null)
                    continue;

                // Skip if primary key or foreign key
                if (property.IsPrimaryKey() || property.IsForeignKey())
                    continue;

                var propertyBuilder = modelBuilder.Entity(entityType.ClrType)
                    .Property(property.Name);

                switch (providerKind)
                {
                    case PersistenceProviderKind.PostgreSql:
                        propertyBuilder.HasColumnType("numeric");
                        propertyBuilder.HasPrecision(DefaultPrecision, DefaultScale);
                        break;

                    case PersistenceProviderKind.SqlServer:
                        propertyBuilder.HasColumnType("decimal");
                        propertyBuilder.HasPrecision(DefaultPrecision, DefaultScale);
                        break;

                    case PersistenceProviderKind.Sqlite:
                        // SQLite stores decimals as REAL by default
                        propertyBuilder.HasColumnType("REAL");
                        break;

                    case PersistenceProviderKind.InMemory:
                        // InMemory provider ignores column types
                        break;
                }
            }
        }
    }

    // ─── Fluent extension methods ─────────────────────────────────────────

    /// <summary>
    /// Configures a <see cref="decimal"/> property with the specified precision and scale.
    /// </summary>
    /// <param name="propertyBuilder">The property builder for the decimal property.</param>
    /// <param name="precision">Total number of digits (default 18).</param>
    /// <param name="scale">Number of decimal places (default 6).</param>
    /// <returns>The property builder for further chaining.</returns>
    /// <remarks>
    /// Sets <c>numeric(precision, scale)</c> column type for PostgreSQL and SQL Server.
    /// </remarks>
    public static PropertyBuilder<decimal> HasDecimalPrecision(
        this PropertyBuilder<decimal> propertyBuilder,
        int precision = DefaultPrecision,
        int scale = DefaultScale)
    {
        propertyBuilder.HasPrecision(precision, scale);
        propertyBuilder.HasColumnType($"numeric({precision},{scale})");
        return propertyBuilder;
    }

    /// <summary>
    /// Configures a nullable <see cref="decimal"/> property with the specified precision and scale.
    /// </summary>
    /// <param name="propertyBuilder">The property builder for the nullable decimal property.</param>
    /// <param name="precision">Total number of digits (default 18).</param>
    /// <param name="scale">Number of decimal places (default 6).</param>
    /// <returns>The property builder for further chaining.</returns>
    /// <remarks>
    /// Sets <c>numeric(precision, scale)</c> column type for PostgreSQL and SQL Server.
    /// </remarks>
    public static PropertyBuilder<decimal?> HasDecimalPrecision(
        this PropertyBuilder<decimal?> propertyBuilder,
        int precision = DefaultPrecision,
        int scale = DefaultScale)
    {
        propertyBuilder.HasPrecision(precision, scale);
        propertyBuilder.HasColumnType($"numeric({precision},{scale})");
        return propertyBuilder;
    }
}
