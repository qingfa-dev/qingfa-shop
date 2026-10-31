namespace Application.BuildingBlocks.Ports.Storage.Security;

public static class StorageSecurityEnforcerOutcome
{
    public static class Failure
    {
        public static Error BlockedExtension(string extension)
            => Error.Validation(Codes.BlockedExtension, $"File extension '{extension}' is blocked.");

        public static Error MagicBytesMismatch(string extension)
            => Error.Validation(Codes.MagicBytesMismatch, $"File content does not match the expected signature for '{extension}'.");

        public static Error FileSizeUnknown()
            => Error.Validation(Codes.FileSizeUnknown, "Could not determine the file size.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string BlockedExtension = "Storage.BlockedExtension";
        public const string MagicBytesMismatch = "Storage.MagicBytesMismatch";
        public const string FileSizeUnknown = "Storage.FileSizeUnknown";
    }
}
