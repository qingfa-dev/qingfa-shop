using System.Collections.Concurrent;
using System.Globalization;

namespace Application.Common.Querying.Mapping;

/// <summary>Default text-to-CLR conversion used when a map has no custom converter.</summary>
internal static class ValueConverter
{
    private static readonly ConcurrentDictionary<Type, Delegate> _converterCache = new();

    /// <summary>
    /// Converts a raw string value to the target .NET type, using a custom converter if provided.
    /// </summary>
    /// <param name="raw">The raw string value from the query.</param>
    /// <param name="type">The target .NET type.</param>
    /// <param name="converter">Optional custom converter; takes precedence over built-in conversion.</param>
    /// <param name="fieldName">The field name for error messages.</param>
    /// <returns>The converted value, or a failure if conversion is not possible.</returns>
    public static Result<QueryValue> Convert(
        string raw,
        Type type,
        QueryValueConverter? converter,
        string fieldName)
    {
        if (converter is not null)
            return converter(raw);

        Type target = Nullable.GetUnderlyingType(type) ?? type;

        // Normalize: Empty and explicit null both mean "no value"; value types use default.
        if (raw.Length == 0)
        {
            object? empty = type.IsValueType && Nullable.GetUnderlyingType(type) is null
                ? Activator.CreateInstance(type)
                : null;
            return Result<QueryValue>.Success(new QueryValue(empty));
        }

        if (string.Equals(raw, QueryConstant.Tokens.Keyword.Null, StringComparison.OrdinalIgnoreCase))
        {
            if (type.IsValueType && Nullable.GetUnderlyingType(type) is null)
                return Result<QueryValue>.Fail(QueryOutcome.Failure.ValueInvalid(fieldName, raw));

            return Result<QueryValue>.Success(new QueryValue(null));
        }

        var converterFunc = (Func<string, Result<object>>)_converterCache.GetOrAdd(target, CreateConverter);

        try
        {
            var valueResult = converterFunc(raw);
            if (valueResult.IsFailure)
                return Result<QueryValue>.Fail(valueResult.Errors);

            return Result<QueryValue>.Success(new QueryValue(valueResult.Value));
        }
        catch (Exception exception) when (
            exception is FormatException or OverflowException or ArgumentException
                or InvalidCastException or NotSupportedException)
        {
            return Result<QueryValue>.Fail(QueryOutcome.Failure.ValueInvalid(fieldName, raw));
        }
    }

    /// <summary>Creates a cached converter delegate for the given target type.</summary>
    /// <param name="targetType">The .NET type to convert to.</param>
    /// <returns>A delegate that converts a string to the target type.</returns>
    private static Delegate CreateConverter(Type targetType)
    {
        return CreateConverterInternal(targetType);
    }

    /// <summary>
    /// Uses reflection to create a strongly-typed converter by invoking <see cref="ConvertCore{T}"/>
    /// with the target type as the generic argument.
    /// </summary>
    /// <param name="targetType">The .NET type to convert to.</param>
    /// <returns>A delegate that wraps the generic converter method.</returns>
    private static Delegate CreateConverterInternal(Type targetType)
    {
        var method = typeof(ValueConverter).GetMethod(
            nameof(ConvertCore),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(targetType);

        return (Delegate)method.Invoke(null, null)!;
    }

    /// <summary>
    /// Creates a converter function for type T that wraps <see cref="ConvertToType{T}"/> in a try-catch.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>A function that converts a string to T, returning a Result.</returns>
    private static Func<string, Result<object>> ConvertCore<T>() where T : notnull
    {
        return (string raw) =>
        {
            try
            {
                T value = ConvertToType<T>(raw);
                return Result<object>.Success(value);
            }
            catch (Exception exception) when (
                exception is FormatException or OverflowException or ArgumentException
                    or InvalidCastException or NotSupportedException)
            {
                return Result<object>.Fail(QueryOutcome.Failure.ValueInvalid(typeof(T).Name, raw));
            }
        };
    }

    /// <summary>
    /// Converts a raw string to type T using type-specific parsing logic.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="raw">The raw string value.</param>
    /// <returns>The converted value.</returns>
    private static T ConvertToType<T>(string raw) where T : notnull
    {
        var targetType = typeof(T);

        // Normalize: strings pass through without parsing.
        if (targetType == typeof(string))
            return (T)(object)raw;

        // Parse: GUID uses the standard parser which accepts multiple formats.
        if (targetType == typeof(Guid))
            return (T)(object)Guid.Parse(raw);

        // Parse: date/time types use InvariantCulture for consistent parsing across locales.
        if (targetType == typeof(DateTimeOffset))
            return (T)(object)DateTimeOffset.Parse(raw, CultureInfo.InvariantCulture);

        if (targetType == typeof(DateTime))
            return (T)(object)DateTime.Parse(raw, CultureInfo.InvariantCulture);

        if (targetType == typeof(DateOnly))
            return (T)(object)DateOnly.Parse(raw, CultureInfo.InvariantCulture);

        if (targetType == typeof(TimeOnly))
            return (T)(object)TimeOnly.Parse(raw, CultureInfo.InvariantCulture);

        // Parse: booleans accept "true"/"false" (case-insensitive).
        if (targetType == typeof(bool))
            return (T)(object)bool.Parse(raw);

        // Parse: enums are matched case-insensitively.
        if (targetType.IsEnum)
            return (T)(object)Enum.Parse(targetType, raw, ignoreCase: true);

        // Fallback: numeric and other primitive types use Convert.ChangeType.
        return (T)System.Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture);
    }
}