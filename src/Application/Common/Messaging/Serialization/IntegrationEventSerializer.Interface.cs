using Application.Common.Messaging.Bus;

namespace Application.Common.Messaging.Serialization;

public interface IIntegrationEventSerializer : IMetadataSerializer
{
    string Serialize(IIntegrationEvent integrationEvent);
    IIntegrationEvent Deserialize(string eventName, int eventVersion, string payload);
}
