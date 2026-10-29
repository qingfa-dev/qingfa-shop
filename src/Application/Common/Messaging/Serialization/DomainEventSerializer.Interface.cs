using Core.Common.Domain.Events;

namespace Application.Common.Messaging.Serialization;

public interface IDomainEventSerializer : IMetadataSerializer
{
    string Serialize(IDomainEvent domainEvent);
    IDomainEvent Deserialize(string eventName, int eventVersion, string payload);
}
