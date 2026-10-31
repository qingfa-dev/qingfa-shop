namespace Application.BuildingBlocks.Ports.Storage.Security.Options;

public static class StorageSecuritySettingOutcome
{
    public static class Failure
    {
        public static Error MaxFileSizeBytesInvalid => Error.Validation(
            code: Codes.MaxFileSizeBytesInvalid,
            message: "Storage.Security.MaxFileSizeBytes must be greater than 0");

        public static Error AllowedExtensionsRequired => Error.Validation(
            code: Codes.AllowedExtensionsRequired,
            message: "Storage.Security.AllowedExtensions cannot be null");

        public static Error BlockedExtensionsRequired => Error.Validation(
            code: Codes.BlockedExtensionsRequired,
            message: "Storage.Security.BlockedExtensions cannot be null");

        public static Error EncryptionKeyInvalid => Error.Validation(
            code: Codes.EncryptionKeyInvalid,
            message: $"Storage.Security.EncryptionKey must be {string.Join(", ", StorageSecuritySettingConstant.Constraints.ValidEncryptionKeyLengths)} chars for AES, or empty");

        public static Error ValidateMagicBytesRequired => Error.Validation(
            code: Codes.ValidateMagicBytesRequired,
            message: "Storage.Security.ValidateMagicBytes must be set");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string MaxFileSizeBytesInvalid = "Storage.Security.MaxFileSizeBytes.Invalid";
        public const string AllowedExtensionsRequired = "Storage.Security.AllowedExtensions.Required";
        public const string BlockedExtensionsRequired = "Storage.Security.BlockedExtensions.Required";
        public const string EncryptionKeyInvalid = "Storage.Security.EncryptionKey.Invalid";
        public const string ValidateMagicBytesRequired = "Storage.Security.ValidateMagicBytes.Required";
    }
}
