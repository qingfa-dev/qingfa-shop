using Core.Common.Concerns.Lifecycle.Publishable;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Interceptors;

/// <summary>
/// Stamps <see cref="IPublishable.PublishedAtUtc"/> when an entity's <c>IsPublished</c> changes to <c>true</c>.
/// </summary>
/// <remarks>
/// Detects Modified entities where <c>IsPublished</c> originally was <c>false</c>
/// and is now <c>true</c>, then stamps the publication timestamp.
/// Actor fields are left as <c>null</c>; actor injection is a separate cross-cutting concern.
/// </remarks>
public sealed class PublishableInterceptor : ConcernInterceptor
{
    /// <inheritdoc />
    protected override void Apply(DbContext? context)
    {
        if (context == null)
            return;

        var now = DateTimeOffset.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries<IPublishable>())
        {
            if (entry.State != EntityState.Modified)
                continue;

            bool wasPublished = entry.Property(entity => entity.IsPublished).OriginalValue as bool? ?? false;

            // Stamp: only the transition from unpublished to published is meaningful.
            if (entry.Entity.IsPublished && !wasPublished)
            {
                entry.Entity.PublishedAtUtc = now;
            }
        }
    }
}
