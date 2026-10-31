namespace Application.BuildingBlocks.Ports.Storage.Security.Options;

public static class StorageSecuritySettingConstant
{
    public static class Defaults
    {
        public const long MaxFileSizeBytes = 10L * 1024L * 1024L;

        public const bool ValidateMagicBytes = true;

        public static readonly string[] AllowedExtensions =
        [
            ".jpg", ".jpeg", ".png", ".gif", ".webp",
            ".pdf", ".txt", ".csv", ".json", ".xml",
            ".doc", ".docx", ".xls", ".xlsx"
        ];

        public static readonly string[] BlockedExtensions =
        [
            ".exe", ".bat", ".cmd", ".sh", ".ps1",
            ".js", ".ts", ".php", ".py", ".rb",
            ".dll", ".so", ".dylib"
        ];
    }

    public static class Constraints
    {
        public const long MaxFileSizeBytesMin = 1L;

        public const long MaxFileSizeBytesMax = 100L * 1024L * 1024L;

        public static readonly int[] ValidEncryptionKeyLengths = [32, 48, 64];
    }
}
