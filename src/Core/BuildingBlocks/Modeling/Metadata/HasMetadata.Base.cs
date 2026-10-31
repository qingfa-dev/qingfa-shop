namespace Core.BuildingBlocks.Modeling.Metadata;

public abstract record HasMetadataBase : IHasMetadata
{
    public DefaultMetadata Metadata { get; protected set; } = DefaultMetadata.Create();
}