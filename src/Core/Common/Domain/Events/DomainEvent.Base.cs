namespace Core.Common.Domain.Events;

/// <summary>
/// Base record for domain events; populates the well-known metadata
/// envelope (event ID, timestamp, correlation, tenant) so events are
/// publishable and traceable as-is.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    #region Properties
    public DefaultMetadata Metadata { get; set; } = DefaultMetadata.Create();
    #endregion
    #region Contructors
    protected DomainEvent(Guid? eventId = null, DateTimeOffset? occurredOnUtc = null, IMetadata? metadata = null)
    {
        if (metadata is not null)
            this.MergeMetadata(metadata);

        this.SetEventId(eventId ?? Guid.NewGuid());
        this.SetTimestamp(occurredOnUtc ?? DateTimeOffset.UtcNow);
    }

    #endregion
}