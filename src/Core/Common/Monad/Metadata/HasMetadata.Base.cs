namespace Core.Common.Monad.Metadata;

public abstract record HasMetadataBase : IHasMetadata
{
    public DefaultMetadata Metadata { get; protected set; } = DefaultMetadata.Create();
}