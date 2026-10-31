using Core.BuildingBlocks.Domain.Events;

namespace Application.BuildingBlocks.Messaging.Serialization;

public interface IDomainEventSerializer : IMetadataSerializer
{
    string Serialize(IDomainEvent domainEvent);
    IDomainEvent Deserialize(string eventName, int eventVersion, string payload);
}
