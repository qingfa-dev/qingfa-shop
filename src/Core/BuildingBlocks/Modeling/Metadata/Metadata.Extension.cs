using System.ComponentModel;
using System.Globalization;

namespace Core.BuildingBlocks.Modeling.Metadata;

/// <summary>
/// Extension methods for reading, converting, and mutating <see cref="IMetadata"/>.
/// </summary>
/// <remarks>
/// Provides typed accessors, conversion logic, and mutation helpers
/// for metadata dictionaries. All methods handle null inputs gracefully
/// and throw appropriate exceptions for invalid operations.
/// </remarks>
public static class MetadataExtensions
{
    #region Raw Access

    /// <summary>
    /// Returns the raw value associated with <paramref name="metadataKey"/>,
    /// or <c>null</c> when the key does not exist.
    /// </summary>
    /// <param name="metadata">The metadata dictionary.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <returns>The raw value, or <c>null</c> if not found.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="metadata"/> or <paramref name="metadataKey"/> is <c>null</c>.
    /// </exception>
    public static object? GetValueOrDefault(
        this IMetadata metadata,
        string metadataKey)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        if (metadataKey is null)
            throw new ArgumentNullException(
                "metadataKey",
                MetadataOutcome.Failure.Key.Argument.Null.Representation);

        return metadata.TryGetValue(metadataKey, out var value)
            ? value
            : null;
    }

    #endregion

    #region Typed Access

    /// <summary>
    /// Returns the converted value associated with <paramref name="metadataKey"/>,
    /// or <c>default</c> when the key is missing or conversion fails.
    /// </summary>
    /// <typeparam name="TValue">The target type.</typeparam>
    /// <param name="metadata">The metadata dictionary.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <returns>The converted value, or <c>default</c> if not found or conversion fails.</returns>
    public static TValue? GetValueOrDefault<TValue>(
        this IMetadata metadata,
        string metadataKey)
    {
        var rawValue = metadata.GetValueOrDefault(metadataKey);

        return TryConvertValue(
            rawValue,
            out TValue? convertedValue)
            ? convertedValue
            : default;
    }

    /// <summary>
    /// Returns the converted value associated with <paramref name="metadataKey"/>,
    /// or <paramref name="fallback"/> when the key is missing or conversion fails.
    /// </summary>
    /// <typeparam name="TValue">The target type.</typeparam>
    /// <param name="metadata">The metadata dictionary.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <param name="fallback">The fallback value.</param>
    /// <returns>The converted value, or <paramref name="fallback"/> if not found or conversion fails.</returns>
    public static TValue? GetValueOrDefault<TValue>(
        this IMetadata metadata,
        string metadataKey,
        TValue? fallback)
    {
        var rawValue = metadata.GetValueOrDefault(metadataKey);

        return TryConvertValue(
            rawValue,
            out TValue? convertedValue)
            ? convertedValue
            : fallback;
    }

    #endregion

    #region Required Access

    /// <summary>
    /// Returns the raw value associated with <paramref name="metadataKey"/>.
    /// </summary>
    /// <param name="metadata">The metadata dictionary.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <returns>The raw value.</returns>
    /// <exception cref="KeyNotFoundException">
    /// The metadata key does not exist.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="metadata"/> or <paramref name="metadataKey"/> is <c>null</c>.
    /// </exception>
    public static object GetRequiredValue(
        this IMetadata metadata,
        string metadataKey)
    {
        var value = metadata.GetValueOrDefault(metadataKey);

        if (value is null)
        {
            throw new KeyNotFoundException(
                MetadataOutcome.Failure.Key.Metadata.NotFound.Representation);
        }

        return value;
    }

    /// <summary>
    /// Returns the converted value associated with <paramref name="metadataKey"/>.
    /// </summary>
    /// <typeparam name="TValue">The target type.</typeparam>
    /// <param name="metadata">The metadata dictionary.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="KeyNotFoundException">
    /// The metadata key does not exist.
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// The metadata value cannot be converted to <typeparamref name="TValue"/>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="metadata"/> or <paramref name="metadataKey"/> is <c>null</c>.
    /// </exception>
    public static TValue GetRequiredValue<TValue>(
        this IMetadata metadata,
        string metadataKey)
    {
        var rawValue = metadata.GetValueOrDefault(metadataKey);

        if (rawValue is null)
        {
            throw new KeyNotFoundException(
                MetadataOutcome.Failure.Key.Metadata.NotFound.Representation);
        }

        if (!TryConvertValue(
                rawValue,
                out TValue? convertedValue) ||
            convertedValue is null)
        {
            throw new InvalidCastException(
                MetadataOutcome.Failure.Value.Metadata.InvalidType.Representation);
        }

        return convertedValue;
    }

    #endregion

    #region Try Get

    /// <summary>
    /// Attempts to retrieve and convert the value associated with
    /// <paramref name="metadataKey"/>.
    /// </summary>
    /// <typeparam name="TValue">The target type.</typeparam>
    /// <param name="metadata">The metadata dictionary.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <param name="value">The converted value if successful; otherwise <c>default</c>.</param>
    /// <returns><c>true</c> if the key exists and conversion succeeds; otherwise <c>false</c>.</returns>
    public static bool TryGetValue<TValue>(
        this IMetadata metadata,
        string metadataKey,
        out TValue? value)
    {
        var rawValue = metadata.GetValueOrDefault(metadataKey);

        if (rawValue is null)
        {
            value = default;
            return false;
        }

        return TryConvertValue(rawValue, out value);
    }

    /// <summary>
    /// Attempts to read a required metadata value without throwing.
    /// </summary>
    /// <typeparam name="TValue">The target type.</typeparam>
    /// <param name="metadata">The metadata dictionary.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <returns>The converted value, or a stable missing/invalid-type failure.</returns>
    public static Result<TValue> TryGetRequiredValue<TValue>(
        this IMetadata metadata,
        string metadataKey)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentException.ThrowIfNullOrWhiteSpace(metadataKey);

        if (!metadata.TryGetValue(metadataKey, out object? rawValue) || rawValue is null)
        {
            return Result<TValue>.Fail(
                MetadataOutcome.Failure.Key.Metadata.NotFound.ToError());
        }

        if (!TryConvertValue(rawValue, out TValue? convertedValue) || convertedValue is null)
        {
            return Result<TValue>.Fail(
                MetadataOutcome.Failure.Value.Metadata.InvalidType.ToError());
        }

        return Result<TValue>.Success(convertedValue);
    }

    #endregion

    #region Presence

    /// <summary>
    /// Determines whether <paramref name="metadataKey"/> exists.
    /// </summary>
    /// <param name="metadata">The metadata dictionary.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <returns><c>true</c> if the key exists; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="metadata"/> or <paramref name="metadataKey"/> is <c>null</c>.
    /// </exception>
    public static bool Contains(
        this IMetadata metadata,
        string metadataKey)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(metadataKey);

        return metadata.ContainsKey(metadataKey);
    }

    #endregion

    #region Mutation

    /// <summary>
    /// Adds or replaces a metadata value.
    /// </summary>
    /// <param name="metadata">The metadata dictionary.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <exception cref="NotSupportedException">
    /// The underlying metadata implementation does not support mutation.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="metadata"/>, <paramref name="metadataKey"/>, or <paramref name="value"/> is <c>null</c>.
    /// </exception>
    public static void SetValue(
        this IMetadata metadata,
        string metadataKey,
        object value)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(metadataKey);
        ArgumentNullException.ThrowIfNull(value);

        if (metadata is IDictionary<string, object> mutable)
        {
            mutable[metadataKey] = value;
            return;
        }

        throw new NotSupportedException(
            MetadataOutcome.Failure.Dictionary.Mutation.NotSupported.Representation);
    }

    /// <summary>
    /// Removes a metadata value.
    /// </summary>
    /// <param name="metadata">The metadata dictionary.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <returns>
    /// <c>true</c> when the value was found and removed; otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="metadata"/> or <paramref name="metadataKey"/> is <c>null</c>.
    /// </exception>
    public static bool Remove(
        this IMetadata metadata,
        string metadataKey)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(metadataKey);

        return metadata is IDictionary<string, object> mutable &&
               mutable.Remove(metadataKey);
    }

    /// <summary>
    /// Merges <paramref name="source"/> entries into
    /// <paramref name="destination"/>.
    /// </summary>
    /// <remarks>
    /// Entries from <paramref name="source"/> overwrite entries already
    /// present in <paramref name="destination"/> (case-insensitive keys).
    /// The merge mutates <paramref name="destination"/> in place and
    /// returns it.
    /// </remarks>
    /// <typeparam name="TSource">The source metadata type.</typeparam>
    /// <typeparam name="TDestination">The destination metadata type.</typeparam>
    /// <param name="destination">The metadata dictionary receiving the entries.</param>
    /// <param name="source">The metadata dictionary supplying the entries.</param>
    /// <returns>The <paramref name="destination"/> instance, for chaining.</returns>
    /// <exception cref="NotSupportedException">
    /// The underlying metadata implementation does not support mutation.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="destination"/> or <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static TDestination MergeMetadata<TSource, TDestination>(
        this TDestination destination,
        TSource source)
        where TSource : IMetadata
        where TDestination : IMetadata
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(source);

        if (destination is not IDictionary<string, object> mutable)
            throw new NotSupportedException(
                MetadataOutcome.Failure.Dictionary.Mutation.NotSupported.Representation);

        foreach (var (key, value) in source)
            mutable[key] = value;

        return destination;
    }

    #endregion

    #region Conversion

    /// <summary>
    /// Attempts to convert a raw metadata value to the target type.
    /// </summary>
    /// <typeparam name="TValue">The target type.</typeparam>
    /// <param name="value">The raw value.</param>
    /// <param name="result">The converted value if successful.</param>
    /// <returns><c>true</c> if conversion succeeds; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// Supports direct assignment, string conversion, Guid, DateTime/DateTimeOffset,
    /// enums, IConvertible, and TypeConverter-based conversions.
    /// </remarks>
    internal static bool TryConvertValue<TValue>(
        object? value,
        out TValue? result)
    {
        result = default;

        if (value is null)
        {
            return default(TValue) is null;
        }

        if (value is TValue direct)
        {
            result = direct;
            return true;
        }

        var targetType =
            Nullable.GetUnderlyingType(typeof(TValue))
            ?? typeof(TValue);

        var sourceType = value.GetType();

        if (targetType == typeof(string))
        {
            result = (TValue)(object)
                Convert.ToString(
                    value,
                    CultureInfo.InvariantCulture)!;

            return true;
        }

        if (targetType == typeof(Guid))
        {
            if (value is string guidText &&
                Guid.TryParse(guidText, out var guid))
            {
                result = (TValue)(object)guid;
                return true;
            }

            return false;
        }

        if (targetType == typeof(DateTimeOffset))
        {
            if (value is string dateText &&
                DateTimeOffset.TryParse(
                    dateText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var dateTimeOffset))
            {
                result = (TValue)(object)dateTimeOffset;
                return true;
            }

            if (value is DateTime dateTime)
            {
                result = (TValue)(object)new DateTimeOffset(
                    dateTime,
                    TimeSpan.Zero);

                return true;
            }

            return false;
        }

        if (targetType == typeof(DateTime))
        {
            if (value is string dateText &&
                DateTime.TryParse(
                    dateText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var dateTime))
            {
                result = (TValue)(object)dateTime;
                return true;
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                result = (TValue)(object)dateTimeOffset.UtcDateTime;
                return true;
            }

            return false;
        }

        if (targetType.IsEnum)
        {
            try
            {
                if (value is string enumName)
                {
                    result = (TValue)Enum.Parse(
                        targetType,
                        enumName,
                        ignoreCase: true);

                    return true;
                }

                result = (TValue)Enum.ToObject(
                    targetType,
                    value);

                return true;
            }
            catch
            {
                return false;
            }
        }

        if (value is IConvertible &&
            typeof(IConvertible).IsAssignableFrom(targetType))
        {
            try
            {
                var converted = Convert.ChangeType(
                    value,
                    targetType,
                    CultureInfo.InvariantCulture);

                result = (TValue)converted!;
                return true;
            }
            catch
            {
                // Fall through to TypeConverter.
            }
        }

        try
        {
            var converter = TypeDescriptor.GetConverter(targetType);

            if (!converter.CanConvertFrom(sourceType))
            {
                return false;
            }

            var converted = converter.ConvertFrom(
                context: null,
                culture: CultureInfo.InvariantCulture,
                value: value);

            if (converted is null)
            {
                return false;
            }

            result = (TValue)converted;
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}