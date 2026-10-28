using Core.Common.Concerns.Lifecycle.SoftDeletable;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Interceptors;

/// <summary>
/// Handles soft deletion for entities implementing <see cref="ISoftDeletable"/>.
/// </summary>
/// <remarks>
/// Instead of deleting rows, sets <c>IsDeleted</c> to <c>true</c>, stamps <c>DeletedAtUtc</c>,
/// and changes the entry to <c>Modified</c> so EF Core updates the row instead of deleting it.
/// Actor fields are left as <c>null</c>; actor injection is a separate cross-cutting concern.
/// </remarks>
public sealed class SoftDeletableInterceptor : ConcernInterceptor
{
    /// <inheritdoc />
    protected override void Apply(DbContext? context)
    {
        if (context == null)
            return;

        var now = DateTimeOffset.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted)
                continue;

            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAtUtc = now;
            entry.State = EntityState.Modified;
        }
    }
}
