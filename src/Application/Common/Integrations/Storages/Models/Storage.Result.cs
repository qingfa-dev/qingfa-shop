namespace Application.Common.Integrations.Storages.Models;

/// <summary>Represents the outcome of a storage operation.</summary>
public sealed record StorageResult
{
    #region Failure

    /// <summary>Pre-defined storage error codes.</summary>
    public static class Failure
    {
        /// <summary>Object key was not found in the provider.</summary>
        public static Error NotFound(string key)
            => Error.NotFound("Storage.NotFound", $"Object '{key}' was not found.");

        /// <summary>Access to the object was denied by security policy.</summary>
        public static Error AccessDenied(string key)
            => Error.Forbidden("Storage.AccessDenied", $"Access to '{key}' was denied.");

        /// <summary>The requested provider is not registered.</summary>
        public static Error ProviderNotFound(string providerName)
            => Error.NotFound("Storage.ProviderNotFound", $"Provider '{providerName}' is not registered.");

        /// <summary>File size exceeds the allowed maximum.</summary>
        public static Error FileTooLarge(long maxBytes)
            => Error.Validation("Storage.FileTooLarge", $"File exceeds the maximum allowed size of {maxBytes} bytes.");

        /// <summary>File extension is not in the allowed list.</summary>
        public static Error ForbiddenExtension(string extension)
            => Error.Validation("Storage.ForbiddenExtension", $"File extension '{extension}' is not permitted.");

        /// <summary>The provider raised an unexpected exception.</summary>
        public static Error ProviderError(string detail)
            => Error.Unexpected("Storage.ProviderError", $"Provider error: {detail}");

        /// <summary>The key attempts path traversal outside the storage root.</summary>
        public static Error PathTraversalDetected(string key)
            => Error.Forbidden("Storage.PathTraversal", $"Key '{key}' attempts path traversal outside the storage root.");

        /// <summary>Malware scan detected a threat and upload was rejected.</summary>
        public static Error MalwareRejected(string threatName)
            => Error.Forbidden("Storage.MalwareRejected", $"Upload rejected: malware detected — {threatName}");

        /// <summary>Encryption of the upload stream failed.</summary>
        public static Error EncryptionFailed(string detail)
            => Error.Unexpected("Storage.EncryptionFailed", $"Encryption failed: {detail}");

        /// <summary>Hash computation failed.</summary>
        public static Error HashFailed(string detail)
            => Error.Unexpected("Storage.HashFailed", $"Hash computation failed: {detail}");

        /// <summary>Image processing failed.</summary>
        public static Error ImageProcessingFailed(string detail)
            => Error.Unexpected("Storage.ImageProcessingFailed", $"Image processing failed: {detail}");
    }

    #endregion Failure
}

