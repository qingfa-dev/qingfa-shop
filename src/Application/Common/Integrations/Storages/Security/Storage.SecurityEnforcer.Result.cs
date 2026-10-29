namespace Application.Common.Integrations.Storages.Security;

public static class StorageSecurityEnforcerResult
{
    public static class Failure
    {
        public static Error BlockedExtension(string extension)
            => Error.Validation("Storage.BlockedExtension", $"File extension '{extension}' is blocked.");

        public static Error MagicBytesMismatch(string extension)
            => Error.Validation("Storage.MagicBytesMismatch", $"File content does not match the expected signature for '{extension}'.");

        public static Error FileSizeUnknown()
            => Error.Validation("Storage.FileSizeUnknown", "Could not determine the file size.");
    }
}
