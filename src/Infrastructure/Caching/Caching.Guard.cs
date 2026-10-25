using Application.Common.Caching;

namespace Infrastructure.Caching;

/// <summary>Enforces the cache port's argument contract with the shared outcome messages.</summary>
internal static class CachingGuard
{
    /// <summary>Rejects a missing or blank key.</summary>
    /// <param name="key">The key to inspect.</param>
    public static void Key(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(
                CachingOutcome.Failure.KeyRequired.ToString(),
                nameof(key));
        }
    }

    /// <summary>Rejects a missing factory.</summary>
    /// <param name="factory">The factory to inspect.</param>
    public static void Factory(Delegate factory)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(
                nameof(factory),
                CachingOutcome.Failure.FactoryRequired.ToString());
        }
    }

    /// <summary>Rejects a missing value.</summary>
    /// <param name="value">The value to inspect.</param>
    public static void Value(object? value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(
                nameof(value),
                CachingOutcome.Failure.ValueRequired.ToString());
        }
    }

    /// <summary>Rejects a missing or blank tag.</summary>
    /// <param name="tag">The tag to inspect.</param>
    public static void Tag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new ArgumentException(
                CachingOutcome.Failure.TagRequired.ToString(),
                nameof(tag));
        }
    }
}