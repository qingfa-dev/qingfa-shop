namespace Application.BuildingBlocks.Ports.Storage.Providers.Options;

public sealed class AzureStorageProviderSetting : BaseStorageProviderSetting
{
    public const string ProviderKey = "Azure";

    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = AzureStorageProviderConstant.Defaults.ContainerName;

    public int BufferSize { get; set; } = AzureStorageProviderConstant.Defaults.BufferSize;
}
