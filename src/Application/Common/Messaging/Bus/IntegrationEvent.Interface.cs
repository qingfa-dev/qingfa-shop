using Core.Common.Monad.Metadata;

namespace Application.Common.Messaging.Bus;

/// <summary>A message published to another process.</summary>
public interface IIntegrationEvent : IHasMetadata
{
    Guid MessageId { get; }
    DateTimeOffset OccurredOnUtc { get; }
    string EventName { get; }
    int Version { get; }
}
