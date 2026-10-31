using System.Text.Json.Serialization;

using Application.BuildingBlocks.Messaging.Bus;

using Core.BuildingBlocks.Modeling.Metadata;

namespace Application.BuildingBlocks.Messaging.Events;

/// <summary>Base record for integration events.</summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    #region Properties
    /// <inheritdoc />
    public Guid MessageId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Gets the stable wire name of the event; defaults to the CLR type name.</summary>
    /// <remarks>The serialized envelope carries the name separately, so the payload omits it.</remarks>
    [JsonIgnore]
    public virtual string EventName => GetType().Name;

    /// <summary>Gets the payload version of the event; starts at <c>1</c>.</summary>
    /// <remarks>The serialized envelope carries the version separately, so the payload omits it.</remarks>
    [JsonIgnore]
    public virtual int Version => 1;

    /// <inheritdoc />
    public DefaultMetadata Metadata { get; init; } = [];
    #endregion
}