using Core.Common.Concerns.Lifecycle.Creatable;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Interceptors;

/// <summary>
/// Stamps <see cref="ICreatable.CreatedAtUtc"/> on newly added entities.
/// </summary>
/// <remarks>
/// Only sets the UTC timestamp; actor fields (<c>CreatedBy</c>) are left as <c>null</c>.
/// Actor injection is a separate cross-cutting concern.
/// </remarks>
public sealed class CreatableInterceptor : ConcernInterceptor
{
    /// <inheritdoc />
    protected override void Apply(DbContext? context)
    {
        if (context == null)
            return;

        var now = DateTimeOffset.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries<ICreatable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
            }
        }
    }
}
