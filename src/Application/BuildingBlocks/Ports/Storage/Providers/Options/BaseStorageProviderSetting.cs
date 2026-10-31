namespace Application.BuildingBlocks.Ports.Storage.Providers.Options;

public abstract class BaseStorageProviderSetting
{
    public const string BaseSection = "Storage:Providers";

    public bool IsEnabled { get; set; } = BaseStorageProviderConstant.Defaults.IsEnabled;
}
