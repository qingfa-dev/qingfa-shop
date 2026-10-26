namespace Core.Common.Domain.Events;

/// <summary>
/// Event-scoped metadata extensions constrained to
/// <see cref="IDomainEvent"/> receivers.
/// </summary>
/// <remarks>
/// These forward to <see cref="HasMetadataExtensions"/> via static calls;
/// instance-style calls inside these bodies would rebind to themselves
/// (identity conversion to the receiver's own type beats conversion to
/// the metadata interface) and recurse forever.
/// </remarks>
public static class DomainEventExtensions
{
    #region Getters

    /// <summary>Gets the event ID from the event's metadata envelope.</summary>
    public static Guid? GetEventId<T>(this T domainEvent)
        where T : IDomainEvent
        => HasMetadataExtensions.GetEventId(domainEvent);

    /// <summary>Gets the correlation ID from the event's metadata envelope.</summary>
    public static Guid? GetCorrelationId<T>(this T domainEvent)
        where T : IDomainEvent
        => HasMetadataExtensions.GetCorrelationId(domainEvent);

    /// <summary>Gets the causation ID from the event's metadata envelope.</summary>
    public static Guid? GetCausationId<T>(this T domainEvent)
        where T : IDomainEvent
        => HasMetadataExtensions.GetCausationId(domainEvent);

    /// <summary>Gets the request ID from the event's metadata envelope.</summary>
    public static Guid? GetRequestId<T>(this T domainEvent)
        where T : IDomainEvent
        => HasMetadataExtensions.GetRequestId(domainEvent);

    /// <summary>Gets the tenant ID from the event's metadata envelope.</summary>
    public static Guid? GetTenantId<T>(this T domainEvent)
        where T : IDomainEvent
        => HasMetadataExtensions.GetTenantId(domainEvent);

    /// <summary>Gets the timestamp from the event's metadata envelope.</summary>
    public static DateTimeOffset? GetTimestamp<T>(this T domainEvent)
        where T : IDomainEvent
        => HasMetadataExtensions.GetTimestamp(domainEvent);

    #endregion

    #region Setters

    /// <summary>Sets the event ID in the event's metadata envelope.</summary>
    public static void SetEventId<T>(this T domainEvent, Guid eventId)
        where T : IDomainEvent
        => HasMetadataExtensions.SetEventId(domainEvent, eventId);

    /// <summary>Sets the correlation ID in the event's metadata envelope.</summary>
    public static void SetCorrelationId<T>(this T domainEvent, Guid correlationId)
        where T : IDomainEvent
        => HasMetadataExtensions.SetCorrelationId(domainEvent, correlationId);

    /// <summary>Sets the causation ID in the event's metadata envelope.</summary>
    public static void SetCausationId<T>(this T domainEvent, Guid causationId)
        where T : IDomainEvent
        => HasMetadataExtensions.SetCausationId(domainEvent, causationId);

    /// <summary>Sets the request ID in the event's metadata envelope.</summary>
    public static void SetRequestId<T>(this T domainEvent, Guid requestId)
        where T : IDomainEvent
        => HasMetadataExtensions.SetRequestId(domainEvent, requestId);

    /// <summary>Sets the tenant ID in the event's metadata envelope.</summary>
    public static void SetTenantId<T>(this T domainEvent, Guid tenantId)
        where T : IDomainEvent
        => HasMetadataExtensions.SetTenantId(domainEvent, tenantId);

    /// <summary>Sets the timestamp in the event's metadata envelope.</summary>
    public static void SetTimestamp<T>(this T domainEvent, DateTimeOffset timestamp)
        where T : IDomainEvent
        => HasMetadataExtensions.SetTimestamp(domainEvent, timestamp);

    #endregion

    #region Merge

    /// <summary>
    /// Merges <paramref name="metadata"/> entries into the event's metadata
    /// envelope; entries from <paramref name="metadata"/> overwrite existing
    /// entries (case-insensitive keys). Mutates in place and returns the
    /// event's metadata.
    /// </summary>
    public static DefaultMetadata MergeMetadata<T, TSource>(
        this T domainEvent,
        TSource metadata)
        where T : IDomainEvent
        where TSource : IMetadata
        => HasMetadataExtensions.MergeMetadata(domainEvent, metadata);

    #endregion
}