using Core.Common.Concerns.Lifecycle.Modifiable;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Interceptors;

/// <summary>
/// Stamps <see cref="IModifiable.ModifiedAtUtc"/> on added and modified entities.
/// </summary>
/// <remarks>
/// Only sets the UTC timestamp; actor fields (<c>ModifiedBy</c>) are left as <c>null</c>.
/// Actor injection is a separate cross-cutting concern.
/// </remarks>
public sealed class ModifiableInterceptor : ConcernInterceptor
{
    /// <inheritdoc />
    protected override void Apply(DbContext? context)
    {
        if (context == null)
            return;

        var now = DateTimeOffset.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries<IModifiable>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAtUtc = now;
            }
        }
    }
}
