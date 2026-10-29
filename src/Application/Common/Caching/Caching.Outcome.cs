namespace Application.Common.Caching;

/// <summary>Failures produced by the cache port's contract and provider operations.</summary>
/// <remarks>
/// The helpers are the reusable template for feature-specific outcomes: compose a stable
/// <see cref="Code"/> and choose the matching error classification. Feature outcomes such as
/// <c>PermissionCacheOutcome</c> reuse these helpers for cache plumbing and add their own
/// domain-specific failures where the surrounding aggregate needs more context.
/// </remarks>
public static class CachingOutcome
{
    /// <summary>The common prefix for every cache error code.</summary>
    public const string Prefix = "Caching";

    /// <summary>Builds a stable cache error code from an operation and a reason.</summary>
    /// <param name="operation">The operation or cache scope, for example <c>Read</c>.</param>
    /// <param name="reason">The failure reason, for example <c>ProviderUnavailable</c>.</param>
    /// <returns>The dotted error code.</returns>
    public static string Code(string operation, string reason) =>
        $"{Prefix}.{operation}.{reason}";

    /// <summary>Creates a caller-contract failure (400).</summary>
    /// <param name="operation">The operation or cache scope.</param>
    /// <param name="reason">The failure reason.</param>
    /// <param name="message">The human-readable message.</param>
    /// <returns>The validation error.</returns>
    public static Error Invalid(string operation, string reason, string message) =>
        Error.BadRequest(Code(operation, reason), message);

    /// <summary>Creates a provider-availability failure (503).</summary>
    /// <param name="operation">The operation or cache scope.</param>
    /// <param name="reason">The failure reason.</param>
    /// <param name="message">The human-readable message.</param>
    /// <returns>The availability error.</returns>
    public static Error Unavailable(string operation, string reason, string message) =>
        Error.ServiceUnavailable(Code(operation, reason), message);

    /// <summary>Creates an internal cache failure (500).</summary>
    /// <param name="operation">The operation or cache scope.</param>
    /// <param name="reason">The failure reason.</param>
    /// <param name="message">The human-readable message.</param>
    /// <returns>The internal error.</returns>
    public static Error Fault(string operation, string reason, string message) =>
        Error.InternalServerError(Code(operation, reason), message);

    /// <summary>Failure outcomes shared by every cache consumer.</summary>
    public static class Failure
    {
        /// <summary>The caller supplied no key.</summary>
        public static Error KeyRequired => Invalid(
            "Key",
            "Required",
            "A cache key is required.");

        /// <summary>The caller supplied a key beyond the configured maximum.</summary>
        /// <param name="length">The rejected key length.</param>
        /// <returns>The validation error.</returns>
        public static Error KeyTooLong(int length) => Invalid(
            "Key",
            "TooLong",
            $"A cache key cannot exceed {CachingConstant.Key.MaxLength} characters. (Provided: {length})");

        /// <summary>The caller supplied no factory.</summary>
        public static Error FactoryRequired => Invalid(
            "Factory",
            "Required",
            "A cache factory is required.");

        /// <summary>The caller supplied no value to store.</summary>
        public static Error ValueRequired => Invalid(
            "Value",
            "Required",
            "A cache value is required.");

        /// <summary>The caller supplied no tag.</summary>
        public static Error TagRequired => Invalid(
            "Tag",
            "Required",
            "A cache tag is required.");

        /// <summary>The caller supplied no tag collection.</summary>
        public static Error TagsRequired => Invalid(
            "Tags",
            "Required",
            "A cache tag collection is required.");

        /// <summary>The entry expiration is not positive.</summary>
        public static Error ExpirationInvalid => Invalid(
            "Entry",
            "ExpirationInvalid",
            "A cache entry expiration must be greater than zero.");

        /// <summary>The local expiration is not positive.</summary>
        public static Error LocalExpirationInvalid => Invalid(
            "Entry",
            "LocalExpirationInvalid",
            "A cache local expiration must be greater than zero.");

        /// <summary>The local expiration exceeds the absolute expiration.</summary>
        public static Error LocalExpirationExceedsAbsolute => Invalid(
            "Entry",
            "LocalExpirationExceedsAbsolute",
            "A cache local expiration cannot exceed the absolute expiration.");

        /// <summary>The cache provider or value factory failed during a read.</summary>
        /// <param name="key">The cache key.</param>
        /// <param name="exception">The underlying exception, when available.</param>
        /// <returns>The availability error.</returns>
        public static Error ReadFailed(string key, Exception? exception = null) => Unavailable(
            "Read",
            "ProviderUnavailable",
            $"Failed to read cache key '{key}'.{Describe(exception)}");

        /// <summary>The cache provider failed while storing a value.</summary>
        /// <param name="key">The cache key.</param>
        /// <param name="exception">The underlying exception, when available.</param>
        /// <returns>The availability error.</returns>
        public static Error WriteFailed(string key, Exception? exception = null) => Unavailable(
            "Write",
            "ProviderUnavailable",
            $"Failed to write cache key '{key}'.{Describe(exception)}");

        /// <summary>The cache provider failed while removing a value.</summary>
        /// <param name="key">The cache key.</param>
        /// <param name="exception">The underlying exception, when available.</param>
        /// <returns>The availability error.</returns>
        public static Error RemoveFailed(string key, Exception? exception = null) => Unavailable(
            "Remove",
            "ProviderUnavailable",
            $"Failed to remove cache key '{key}'.{Describe(exception)}");

        /// <summary>The cache provider failed while evicting tagged entries.</summary>
        /// <param name="tag">The tag being evicted, or <c>null</c> for a tag collection.</param>
        /// <param name="exception">The underlying exception, when available.</param>
        /// <returns>The availability error.</returns>
        public static Error EvictFailed(string? tag, Exception? exception = null) => Unavailable(
            "Evict",
            "ProviderUnavailable",
            tag is null
                ? $"Failed to evict tagged cache entries.{Describe(exception)}"
                : $"Failed to evict cache entries tagged '{tag}'.{Describe(exception)}");

        /// <summary>The cache provider could not serialize or deserialize an entry.</summary>
        /// <param name="key">The cache key.</param>
        /// <param name="valueType">The value type involved in the failure.</param>
        /// <param name="exception">The underlying exception, when available.</param>
        /// <returns>The internal error.</returns>
        public static Error SerializationFailed(
            string key,
            Type valueType,
            Exception? exception = null) => Fault(
            "Serialization",
            "Failed",
            $"Failed to serialize cache key '{key}' as '{valueType.FullName}'.{Describe(exception)}");

        private static string Describe(Exception? exception) =>
            exception is null ? string.Empty : $" ({exception.GetType().Name})";
    }
}
