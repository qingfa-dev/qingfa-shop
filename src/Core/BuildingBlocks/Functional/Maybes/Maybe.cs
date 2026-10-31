using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Core.BuildingBlocks.Functional.Maybes;

/// <summary>
/// Represents an optional value that may or may not be present.
/// Provides a monadic interface for handling nullable domain values.
/// </summary>
/// <typeparam name="T">The type of the contained value.</typeparam>
public partial record Maybe<T> : IMaybe<T>
{
    private readonly T? _value;

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="Maybe{T}"/> record.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="hasValue">Whether the instance has a value.</param>
    [JsonConstructor]
    internal Maybe(T? value, bool hasValue)
    {
        _value = value;
        HasValue = hasValue;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets a value indicating whether this instance has a value.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool HasValue { get; }

    /// <summary>
    /// Gets a value indicating whether this instance has no value.
    /// </summary>
    [JsonIgnore]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool HasNoValue => !HasValue;

    /// <summary>
    /// Gets the contained value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when there is no value.</exception>
    public T Value => HasValue
        ? _value!
        : throw new InvalidOperationException("Maybe has no value.");

    #endregion

    #region Overrides

    /// <inheritdoc />
    public override string ToString()
    {
        return HasValue
            ? $"Maybe({Value})"
            : "Maybe(None)";
    }

    #endregion
}