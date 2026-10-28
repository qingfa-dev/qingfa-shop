using Core.Common.Concerns.Lifecycle.Activatable;
using Core.Common.Concerns.Lifecycle.Creatable;
using Core.Common.Concerns.Lifecycle.Modifiable;
using Core.Common.Concerns.Lifecycle.Publishable;
using Core.Common.Concerns.Lifecycle.SoftDeletable;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Persistence.EntityFramework.Interceptors;

/// <summary>
/// Applies every persistence-time Lifecycle concern in one pass over the change tracker.
/// </summary>
/// <remarks>
/// <para>
/// The Lifecycle concerns share one save boundary, so they are applied together:
/// soft deletion converts a <c>Deleted</c> entry into an update before the creation and
/// modification stamps run, which guarantees the row persists its flags and audit fields.
/// </para>
/// <para>
/// Only <see cref="IActivatable"/> and <see cref="IPublishable"/> transitions from
/// <c>false</c> to <c>true</c> are stamped; repeated saves of an already-active or
/// already-published entity keep the original timestamp.
/// </para>
/// <para>
/// Actor fields remain untouched. Actor injection is a separate cross-cutting concern.
/// </para>
/// </remarks>
public sealed class LifecycleInterceptor : ConcernInterceptor
{
    /// <inheritdoc />
    protected override void Apply(DbContext? context)
    {
        if (context is null)
            return;

        var now = DateTimeOffset.UtcNow;

        foreach (EntityEntry entry in context.ChangeTracker.Entries())
        {
            object entity = entry.Entity;

            // Handle: soft delete first so the entry becomes an update and receives the stamps below.
            if (entity is ISoftDeletable softDeletable && entry.State == EntityState.Deleted)
            {
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAtUtc = now;
                entry.State = EntityState.Modified;
            }

            // Stamp: creation and modification audit timestamps.
            if (entry.State == EntityState.Added)
            {
                if (entity is ICreatable creatable)
                    creatable.CreatedAtUtc = now;

                if (entity is IModifiable modifiable)
                    modifiable.ModifiedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entity is IModifiable modifiable)
                    modifiable.ModifiedAtUtc = now;

                if (entity is IActivatable activatable &&
                    IsTransition(entry, nameof(IActivatable.IsActive), activatable.IsActive))
                {
                    activatable.ActivatedAtUtc = now;
                }

                if (entity is IPublishable publishable &&
                    IsTransition(entry, nameof(IPublishable.IsPublished), publishable.IsPublished))
                {
                    publishable.PublishedAtUtc = now;
                }
            }
        }
    }

    /// <summary>
    /// Determines whether a boolean concern flag moved from <c>false</c> to <c>true</c>.
    /// </summary>
    /// <param name="entry">The tracked entry that owns the flag.</param>
    /// <param name="propertyName">The mapped property name of the flag.</param>
    /// <param name="currentValue">The flag's current value.</param>
    /// <returns><c>true</c> only for an inactive-to-active or unpublished-to-published transition.</returns>
    private static bool IsTransition(
        EntityEntry entry,
        string propertyName,
        bool currentValue)
        => currentValue && entry.Property(propertyName).OriginalValue is not true;
}
