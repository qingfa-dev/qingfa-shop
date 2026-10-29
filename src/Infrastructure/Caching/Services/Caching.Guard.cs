using Application.Common.Caching;

namespace Infrastructure.Caching.Services;

/// <summary>Enforces the cache port's argument contract and returns reusable cache failures.</summary>
internal static class CachingGuard
{
    /// <summary>Rejects a missing, blank, or overlong key.</summary>
    /// <param name="key">The key to inspect.</param>
    /// <returns>The first failure, or <c>null</c> when the key is valid.</returns>
    public static Error? Key(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return CachingOutcome.Failure.KeyRequired;

        return key.Length > CachingConstant.Key.MaxLength
            ? CachingOutcome.Failure.KeyTooLong(key.Length)
            : null;
    }

    /// <summary>Rejects a missing factory.</summary>
    /// <param name="factory">The factory to inspect.</param>
    /// <returns>The first failure, or <c>null</c> when the factory is valid.</returns>
    public static Error? Factory(Delegate? factory) =>
        factory is null ? CachingOutcome.Failure.FactoryRequired : null;

    /// <summary>Rejects a missing value.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns>The first failure, or <c>null</c> when the value is valid.</returns>
    public static Error? Value(object? value) =>
        value is null ? CachingOutcome.Failure.ValueRequired : null;

    /// <summary>Rejects a missing or blank tag.</summary>
    /// <param name="tag">The tag to inspect.</param>
    /// <returns>The first failure, or <c>null</c> when the tag is valid.</returns>
    public static Error? Tag(string tag) =>
        string.IsNullOrWhiteSpace(tag) ? CachingOutcome.Failure.TagRequired : null;

    /// <summary>Rejects a blank member in an optional tag collection.</summary>
    /// <param name="tags">The tags to inspect; <c>null</c> means no tags were supplied.</param>
    /// <returns>The first failure, or <c>null</c> when every supplied tag is valid.</returns>
    public static Error? Tags(IEnumerable<string>? tags)
    {
        if (tags is null)
            return null;

        foreach (string tag in tags)
        {
            Error? error = Tag(tag);
            if (error is not null)
                return error;
        }

        return null;
    }

    /// <summary>Rejects a missing tag collection or a blank member.</summary>
    /// <param name="tags">The tags to inspect.</param>
    /// <returns>The first failure, or <c>null</c> when every tag is valid.</returns>
    public static Error? TagsRequired(IEnumerable<string>? tags) =>
        tags is null ? CachingOutcome.Failure.TagsRequired : Tags(tags);

    /// <summary>Rejects contradictory or non-positive entry lifetimes.</summary>
    /// <param name="options">The entry options to inspect.</param>
    /// <returns>The first failure, or <c>null</c> when the options are valid.</returns>
    public static Error? Options(CachingEntryOption? options)
    {
        if (options is null)
            return null;

        if (options.Expiration is { } expiration && expiration <= TimeSpan.Zero)
            return CachingOutcome.Failure.ExpirationInvalid;

        if (options.LocalCacheExpiration is { } local && local <= TimeSpan.Zero)
            return CachingOutcome.Failure.LocalExpirationInvalid;

        if (options.Expiration is { } absolute &&
            options.LocalCacheExpiration is { } localExpiration &&
            localExpiration > absolute)
        {
            return CachingOutcome.Failure.LocalExpirationExceedsAbsolute;
        }

        return null;
    }
}
