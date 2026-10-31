namespace Core.BuildingBlocks.Modeling.Metadata;

/// <summary>
/// Default mutable implementation of <see cref="IMetadata"/>.
/// </summary>
/// <remarks>
/// Uses case-insensitive key comparison via <see cref="StringComparer.OrdinalIgnoreCase"/>.
/// Provides factory methods and a fluent <see cref="With(string, object)"/> method
/// for building metadata dictionaries.
/// </remarks>
public sealed class DefaultMetadata : Dictionary<string, object>, IMetadata
{
    #region Constructors

    /// <summary>
    /// Initializes an empty metadata dictionary.
    /// </summary>
    public DefaultMetadata()
        : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a metadata dictionary from an existing dictionary.
    /// </summary>
    /// <param name="dictionary">The source dictionary.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="dictionary"/> is <c>null</c>.
    /// </exception>
    public DefaultMetadata(IDictionary<string, object> dictionary)
        : base(
            EnsureNotNull(dictionary),
            StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a metadata dictionary from a collection of key-value pairs.
    /// </summary>
    /// <param name="pairs">The key-value pairs.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="pairs"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// A key in <paramref name="pairs"/> is <c>null</c>.
    /// </exception>
    public DefaultMetadata(
        IEnumerable<KeyValuePair<string, object>> pairs)
        : base(StringComparer.OrdinalIgnoreCase)
    {
        if (pairs is null)
            throw new ArgumentNullException(
                nameof(pairs),
                MetadataOutcome.Failure.Dictionary.Argument.Null.Representation);

        foreach (var pair in pairs)
        {
            if (pair.Key is null)
            {
                throw new ArgumentException(
                    MetadataOutcome.Failure.Key.Argument.Null.Representation,
                    nameof(pairs));
            }

            this[pair.Key] = pair.Value;
        }
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates an empty metadata dictionary.
    /// </summary>
    /// <returns>A new <see cref="DefaultMetadata"/>.</returns>
    public static DefaultMetadata Create()
        => new();

    /// <summary>
    /// Creates a metadata dictionary from an existing dictionary.
    /// </summary>
    /// <param name="dictionary">The source dictionary.</param>
    /// <returns>A new <see cref="DefaultMetadata"/>.</returns>
    public static DefaultMetadata Create(
        IDictionary<string, object> dictionary)
        => new(dictionary);

    /// <summary>
    /// Creates a metadata dictionary from a collection of key-value pairs.
    /// </summary>
    /// <param name="pairs">The key-value pairs.</param>
    /// <returns>A new <see cref="DefaultMetadata"/>.</returns>
    public static DefaultMetadata Create(
        IEnumerable<KeyValuePair<string, object>> pairs)
        => new(pairs);

    /// <summary>
    /// Gets a shared empty metadata dictionary instance.
    /// </summary>
    public static DefaultMetadata Empty { get; } = new();

    /// <summary>
    /// Adds or replaces a metadata entry and returns this dictionary (fluent).
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>This dictionary.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> or <paramref name="value"/> is <c>null</c>.
    /// </exception>
    public DefaultMetadata With(string key, object value)
    {
        if (key is null)
            throw new ArgumentNullException(
                nameof(key),
                MetadataOutcome.Failure.Key.Argument.Null.Representation);

        ArgumentNullException.ThrowIfNull(value);

        this[key] = value;

        return this;
    }

    #endregion

    #region Helpers

    private static IDictionary<string, object> EnsureNotNull(
        IDictionary<string, object> dictionary)
    {
        if (dictionary is null)
            throw new ArgumentNullException(
                nameof(dictionary),
                MetadataOutcome.Failure.Dictionary.Argument.Null.Representation);

        return dictionary;
    }

    #endregion
}