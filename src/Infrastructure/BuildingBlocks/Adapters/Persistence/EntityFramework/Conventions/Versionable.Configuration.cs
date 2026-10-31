using Core.BuildingBlocks.Modeling.Concerns.Foundation.Versionable;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Conventions;

/// <summary>
/// Marks the <see cref="IVersionable.Version"/> property as an EF Core concurrency token.
/// </summary>
public static class VersionableConfiguration
{
    /// <summary>Applies the convention to every versionable entity type.</summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    public static void Apply(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Skip: entities without the version contract have no token to guard.
            if (!typeof(IVersionable).IsAssignableFrom(entityType.ClrType))
                continue;

            var property = entityType.FindProperty(nameof(IVersionable.Version));

            // Skip: a versionable CLR type that does not map Version as a property.
            if (property is null)
                continue;

            modelBuilder
                .Entity(entityType.ClrType)
                .Property(nameof(IVersionable.Version))
                .IsConcurrencyToken();
        }
    }
}
