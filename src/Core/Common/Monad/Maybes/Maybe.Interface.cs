namespace Shared.Foundations.Maybes;

/// <summary>
/// Represents an optional value that may or may not be present.
/// </summary>
/// <typeparam name="T">The type of the contained value.</typeparam>
public interface IMaybe<out T>
{
    /// <summary>
    /// Gets a value indicating whether this instance has a value.
    /// </summary>
    bool HasValue { get; }

    /// <summary>
    /// Gets a value indicating whether this instance has no value.
    /// </summary>
    bool HasNoValue { get; }

    /// <summary>
    /// Gets the contained value.
    /// </summary>
    T? Value { get; }
}