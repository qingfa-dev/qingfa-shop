using Core.BuildingBlocks.Modeling.Concerns.Foundation.Versionable;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Interceptors;

/// <summary>
/// Increments <see cref="IVersionable.Version"/> on modified entities for optimistic concurrency.
/// </summary>
/// <remarks>
/// Each save increments the version by 1, providing a simple optimistic concurrency token.
/// </remarks>
public sealed class VersionableInterceptor : ConcernInterceptor
{
    /// <inheritdoc />
    protected override void Apply(DbContext? context)
    {
        if (context == null)
            return;

        foreach (var entry in context.ChangeTracker.Entries<IVersionable>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.Version++;
            }
        }
    }
}
