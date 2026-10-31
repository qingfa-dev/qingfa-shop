using Core.BuildingBlocks.Modeling.Metadata;

namespace Application.BuildingBlocks.Messaging.Bus;

/// <summary>A message published to another process.</summary>
public interface IIntegrationEvent : IHasMetadata
{
    Guid MessageId { get; }
    DateTimeOffset OccurredOnUtc { get; }
    string EventName { get; }
    int Version { get; }
}
