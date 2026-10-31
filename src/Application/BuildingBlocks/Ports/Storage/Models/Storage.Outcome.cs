namespace Application.BuildingBlocks.Ports.Storage.Models;

/// <summary>Represents the outcome of a storage operation.</summary>
public sealed record StorageOutcome
{
    #region Failure

    /// <summary>Pre-defined storage error codes.</summary>
    public static class Failure
    {
        /// <summary>Object key was not found in the provider.</summary>
        public static Error NotFound(string key)
            => Error.NotFound(Codes.NotFound, $"Object '{key}' was not found.");

        /// <summary>Access to the object was denied by security policy.</summary>
        public static Error AccessDenied(string key)
            => Error.Forbidden(Codes.AccessDenied, $"Access to '{key}' was denied.");

        /// <summary>The requested provider is not registered.</summary>
        public static Error ProviderNotFound(string providerName)
            => Error.NotFound(Codes.ProviderNotFound, $"Provider '{providerName}' is not registered.");

        /// <summary>File size exceeds the allowed maximum.</summary>
        public static Error FileTooLarge(long maxBytes)
            => Error.Validation(Codes.FileTooLarge, $"File exceeds the maximum allowed size of {maxBytes} bytes.");

        /// <summary>File extension is not in the allowed list.</summary>
        public static Error ForbiddenExtension(string extension)
            => Error.Validation(Codes.ForbiddenExtension, $"File extension '{extension}' is not permitted.");

        /// <summary>The provider raised an unexpected exception.</summary>
        public static Error ProviderError(string detail)
            => Error.Unexpected(Codes.ProviderError, $"Provider error: {detail}");

        /// <summary>The key attempts path traversal outside the storage root.</summary>
        public static Error PathTraversalDetected(string key)
            => Error.Forbidden(Codes.PathTraversalDetected, $"Key '{key}' attempts path traversal outside the storage root.");

        /// <summary>Malware scan detected a threat and upload was rejected.</summary>
        public static Error MalwareRejected(string threatName)
            => Error.Forbidden(Codes.MalwareRejected, $"Upload rejected: malware detected — {threatName}");

        /// <summary>Encryption of the upload stream failed.</summary>
        public static Error EncryptionFailed(string detail)
            => Error.Unexpected(Codes.EncryptionFailed, $"Encryption failed: {detail}");

        /// <summary>Hash computation failed.</summary>
        public static Error HashFailed(string detail)
            => Error.Unexpected(Codes.HashFailed, $"Hash computation failed: {detail}");

        /// <summary>Image processing failed.</summary>
        public static Error ImageProcessingFailed(string detail)
            => Error.Unexpected(Codes.ImageProcessingFailed, $"Image processing failed: {detail}");

        /// <summary>The provider operation is not implemented for the requested backend.</summary>
        public static Error NotImplemented(string operation)
            => Error.Unexpected(Codes.NotImplemented, $"S3 {operation} stub — install AWSSDK.S3 for real implementation.");
    }

    #endregion Failure

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string NotFound = "Storage.NotFound";
        public const string AccessDenied = "Storage.AccessDenied";
        public const string ProviderNotFound = "Storage.ProviderNotFound";
        public const string FileTooLarge = "Storage.FileTooLarge";
        public const string ForbiddenExtension = "Storage.ForbiddenExtension";
        public const string ProviderError = "Storage.ProviderError";
        public const string PathTraversalDetected = "Storage.PathTraversal";
        public const string MalwareRejected = "Storage.MalwareRejected";
        public const string EncryptionFailed = "Storage.EncryptionFailed";
        public const string HashFailed = "Storage.HashFailed";
        public const string ImageProcessingFailed = "Storage.ImageProcessingFailed";
        public const string NotImplemented = "Storage.NotImplemented";
    }
}
