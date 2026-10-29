using Core.Common.Monad.Metadata;

namespace Application.Common.Messaging.Serialization;

public interface IMetadataSerializer
{
    string Serialize(IMetadata metadata);
}
