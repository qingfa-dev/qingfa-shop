namespace Application.Common.Integrations.Storages.Providers.Options;

public sealed class S3StorageProviderSetting : BaseStorageProviderSetting
{
    public const string ProviderKey = "S3";

    public string? ServiceUrl { get; set; }

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string BucketName { get; set; } = S3StorageProviderConstant.Defaults.BucketName;

    public string Region { get; set; } = S3StorageProviderConstant.Defaults.Region;

    public bool ForcePathStyle { get; set; }

    public int BufferSize { get; set; } = S3StorageProviderConstant.Defaults.BufferSize;
}
