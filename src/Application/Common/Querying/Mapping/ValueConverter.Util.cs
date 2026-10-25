using System.Globalization;

namespace Application.Common.Querying.Mapping;

/// <summary>Default text-to-CLR conversion used when a map has no custom converter.</summary>
internal static class ValueConverter
{
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

        try
        {
            object? value = target switch
            {
                _ when target == typeof(string) => raw,
                _ when target == typeof(Guid) => Guid.Parse(raw),
                _ when target == typeof(DateTimeOffset) => DateTimeOffset.Parse(raw, CultureInfo.InvariantCulture),
                _ when target == typeof(DateTime) => DateTime.Parse(raw, CultureInfo.InvariantCulture),
                _ when target == typeof(DateOnly) => DateOnly.Parse(raw, CultureInfo.InvariantCulture),
                _ when target == typeof(TimeOnly) => TimeOnly.Parse(raw, CultureInfo.InvariantCulture),
                _ when target == typeof(bool) => bool.Parse(raw),
                _ when target.IsEnum => Enum.Parse(target, raw, ignoreCase: true),
                _ => System.Convert.ChangeType(raw, target, CultureInfo.InvariantCulture),
            };

            return Result<QueryValue>.Success(new QueryValue(value));
        }
        catch (Exception exception) when (
            exception is FormatException or OverflowException or ArgumentException
                or InvalidCastException or NotSupportedException)
        {
            return Result<QueryValue>.Fail(QueryOutcome.Failure.ValueInvalid(fieldName, raw));
        }
    }
}