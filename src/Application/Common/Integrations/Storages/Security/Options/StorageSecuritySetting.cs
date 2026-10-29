namespace Application.Common.Integrations.Storages.Security.Options;

public sealed class StorageSecuritySetting
{
    public const string SectionName = "Storage:Security";

    public long MaxFileSizeBytes { get; set; } = StorageSecuritySettingConstant.Defaults.MaxFileSizeBytes;

    public IReadOnlySet<string> AllowedExtensions { get; set; } = new HashSet<string>(StorageSecuritySettingConstant.Defaults.AllowedExtensions, StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<string> BlockedExtensions { get; set; } = new HashSet<string>(StorageSecuritySettingConstant.Defaults.BlockedExtensions, StringComparer.OrdinalIgnoreCase);

    public bool ValidateMagicBytes { get; set; } = StorageSecuritySettingConstant.Defaults.ValidateMagicBytes;

    public string? EncryptionKey { get; set; }
}
