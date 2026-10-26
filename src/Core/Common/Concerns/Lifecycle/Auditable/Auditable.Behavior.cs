namespace Core.Common.Concerns.Lifecycle.Auditable;

/// <summary>Applies audit timestamps and actors without going through a result pipeline.</summary>
public static class AuditableBehavior
{
    /// <summary>Marks a new entity as created, and therefore also modified, by the given actor.</summary>
    /// <param name="entity">The entity to stamp.</param>
    /// <param name="by">The actor identifier.</param>
    /// <param name="atUtc">The timestamp to apply; defaults to the current UTC time.</param>
    public static void CreateBy(IAuditable entity, string by, DateTimeOffset? atUtc = null)
    {
        DateTimeOffset now = atUtc ?? DateTimeOffset.UtcNow;
        entity.CreatedAtUtc = now;
        entity.CreatedBy = by;
        entity.ModifiedAtUtc = now;
        entity.ModifiedBy = by;
    }

    /// <summary>Marks an existing entity as modified by the given actor.</summary>
    /// <param name="entity">The entity to stamp.</param>
    /// <param name="by">The actor identifier.</param>
    /// <param name="atUtc">The timestamp to apply; defaults to the current UTC time.</param>
    public static void TouchBy(IAuditable entity, string by, DateTimeOffset? atUtc = null)
    {
        entity.ModifiedAtUtc = atUtc ?? DateTimeOffset.UtcNow;
        entity.ModifiedBy = by;
    }
}
