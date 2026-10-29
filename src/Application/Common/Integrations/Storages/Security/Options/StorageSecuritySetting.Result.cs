namespace Application.Common.Integrations.Storages.Security.Options;

public static class StorageSecuritySettingResult
{
    public static class Failure
    {
        public static Error MaxFileSizeBytesInvalid => Error.Validation(
            code: "Storage.Security.MaxFileSizeBytes.Invalid",
            message: "Storage.Security.MaxFileSizeBytes must be greater than 0");

        public static Error AllowedExtensionsRequired => Error.Validation(
            code: "Storage.Security.AllowedExtensions.Required",
            message: "Storage.Security.AllowedExtensions cannot be null");

        public static Error BlockedExtensionsRequired => Error.Validation(
            code: "Storage.Security.BlockedExtensions.Required",
            message: "Storage.Security.BlockedExtensions cannot be null");

        public static Error EncryptionKeyInvalid => Error.Validation(
            code: "Storage.Security.EncryptionKey.Invalid",
            message: $"Storage.Security.EncryptionKey must be {string.Join(", ", StorageSecuritySettingConstant.Constraints.ValidEncryptionKeyLengths)} chars for AES, or empty");

        public static Error ValidateMagicBytesRequired => Error.Validation(
            code: "Storage.Security.ValidateMagicBytes.Required",
            message: "Storage.Security.ValidateMagicBytes must be set");
    }
}
