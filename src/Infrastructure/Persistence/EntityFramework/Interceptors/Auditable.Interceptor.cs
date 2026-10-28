using Core.Common.Concerns.Lifecycle.Auditable;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Interceptors;

/// <summary>
/// Stamps creation and modification timestamps on entities implementing <see cref="IAuditable"/>.
/// </summary>
/// <remarks>
/// On <c>Added</c> entities: sets <c>CreatedAtUtc</c> and <c>ModifiedAtUtc</c>.
/// On <c>Modified</c> entities: sets <c>ModifiedAtUtc</c> only.
/// Actor fields are left as <c>null</c>; actor injection is a separate cross-cutting concern.
/// </remarks>
public sealed class AuditableInterceptor : ConcernInterceptor
{
    /// <inheritdoc />
    protected override void Apply(DbContext? context)
    {
        if (context == null)
            return;

        var now = DateTimeOffset.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.ModifiedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAtUtc = now;
            }
        }
    }
}
