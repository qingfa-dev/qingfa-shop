using System.Text.Json.Serialization;

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

    /// <summary>Gets the stable wire name of the event; defaults to the CLR type name.</summary>
    /// <remarks>The serialized envelope carries the name separately, so the payload omits it.</remarks>
    [JsonIgnore]
    public virtual string EventName => GetType().Name;

    /// <summary>Gets the payload version of the event; starts at <c>1</c>.</summary>
    /// <remarks>The serialized envelope carries the version separately, so the payload omits it.</remarks>
    [JsonIgnore]
    public virtual int EventVersion => 1;
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