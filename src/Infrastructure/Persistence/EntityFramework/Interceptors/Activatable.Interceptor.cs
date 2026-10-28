using Core.Common.Concerns.Lifecycle.Activatable;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Interceptors;

/// <summary>
/// Stamps <see cref="IActivatable.ActivatedAtUtc"/> when an entity's <c>IsActive</c> changes to <c>true</c>.
/// </summary>
/// <remarks>
/// Detects Modified entities where <c>IsActive</c> originally was <c>false</c>
/// and is now <c>true</c>, then stamps the activation timestamp.
/// Actor fields are left as <c>null</c>; actor injection is a separate cross-cutting concern.
/// </remarks>
public sealed class ActivatableInterceptor : ConcernInterceptor
{
    /// <inheritdoc />
    protected override void Apply(DbContext? context)
    {
        if (context == null)
            return;

        var now = DateTimeOffset.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries<IActivatable>())
        {
            if (entry.State != EntityState.Modified)
                continue;

            bool wasActive = entry.Property(entity => entity.IsActive).OriginalValue as bool? ?? false;

            // Stamp: only the transition from inactive to active is meaningful.
            if (entry.Entity.IsActive && !wasActive)
            {
                entry.Entity.ActivatedAtUtc = now;
            }
        }
    }
}
