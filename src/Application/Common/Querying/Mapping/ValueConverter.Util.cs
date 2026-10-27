using System.Collections.Concurrent;
using System.Globalization;

namespace Application.Common.Querying.Mapping;

/// <summary>Default text-to-CLR conversion used when a map has no custom converter.</summary>
internal static class ValueConverter
{
    private static readonly ConcurrentDictionary<Type, Delegate> _converterCache = new();

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

        // Get or create the converter function for the target type
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

    private static Delegate CreateConverter(Type targetType)
    {
        // Create a strongly-typed converter function for the target type
        return CreateConverterInternal(targetType);
    }

    private static Delegate CreateConverterInternal(Type targetType)
    {
        // Use reflection to create the appropriate converter method
        var method = typeof(ValueConverter).GetMethod(
            nameof(ConvertCore),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(targetType);

        return (Delegate)method.Invoke(null, null)!;
    }

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

    private static T ConvertToType<T>(string raw) where T : notnull
    {
        var targetType = typeof(T);

        if (targetType == typeof(string))
            return (T)(object)raw;

        if (targetType == typeof(Guid))
            return (T)(object)Guid.Parse(raw);

        if (targetType == typeof(DateTimeOffset))
            return (T)(object)DateTimeOffset.Parse(raw, CultureInfo.InvariantCulture);

        if (targetType == typeof(DateTime))
            return (T)(object)DateTime.Parse(raw, CultureInfo.InvariantCulture);

        if (targetType == typeof(DateOnly))
            return (T)(object)DateOnly.Parse(raw, CultureInfo.InvariantCulture);

        if (targetType == typeof(TimeOnly))
            return (T)(object)TimeOnly.Parse(raw, CultureInfo.InvariantCulture);

        if (targetType == typeof(bool))
            return (T)(object)bool.Parse(raw);

        if (targetType.IsEnum)
            return (T)(object)Enum.Parse(targetType, raw, ignoreCase: true);

        // Fallback to Convert.ChangeType for other types
        return (T)System.Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture);
    }
}