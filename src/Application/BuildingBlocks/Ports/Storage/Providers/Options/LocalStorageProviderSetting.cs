namespace Application.BuildingBlocks.Ports.Storage.Providers.Options;

public sealed class LocalStorageProviderSetting : BaseStorageProviderSetting
{
    public const string ProviderKey = "Local";

    public string LocalPath { get; set; } = LocalStorageProviderConstant.Defaults.LocalPath;

    public int BufferSize { get; set; } = LocalStorageProviderConstant.Defaults.BufferSize;
}
