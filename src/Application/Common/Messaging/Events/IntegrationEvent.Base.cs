using Application.Common.Messaging.Bus;

using Core.Common.Monad.Metadata;

namespace Application.Common.Messaging.Events;

/// <summary>Base record for integration events.</summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    #region Properties
    /// <inheritdoc />
    public Guid MessageId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public virtual string EventName => GetType().Name;

    /// <inheritdoc />
    public virtual int Version => 1;

    /// <inheritdoc />
    public DefaultMetadata Metadata { get; init; } = [];
    #endregion
}