using Core.BuildingBlocks.Modeling.Metadata;

namespace Application.BuildingBlocks.Messaging.Serialization;

public interface IMetadataSerializer
{
    string Serialize(IMetadata metadata);
}
