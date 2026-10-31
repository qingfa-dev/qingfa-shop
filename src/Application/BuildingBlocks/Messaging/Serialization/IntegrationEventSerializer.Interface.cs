using Application.BuildingBlocks.Messaging.Bus;

namespace Application.BuildingBlocks.Messaging.Serialization;

public interface IIntegrationEventSerializer : IMetadataSerializer
{
    string Serialize(IIntegrationEvent integrationEvent);
    IIntegrationEvent Deserialize(string eventName, int eventVersion, string payload);
}
