using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.EntityFramework.Converters;

/// <summary>
/// Converts <see cref="DateTimeOffset"/> values to and from <c>string</c> for database storage.
/// </summary>
/// <remarks>
/// Uses round-trip ISO 8601 format to preserve timezone information.
/// Suitable for PostgreSQL <c>timestamptz</c> columns when string conversion is needed.
/// </remarks>
public sealed class DateTimeOffsetValueConverter : ValueConverter<DateTimeOffset, string>
{
    /// <summary>Creates a new DateTimeOffset string converter.</summary>
    public DateTimeOffsetValueConverter()
        : base(
            v => v.ToString("O"),
            v => DateTimeOffset.Parse(v, null, System.Globalization.DateTimeStyles.RoundtripKind))
    {
    }

    /// <summary>Gets a shared instance of the converter.</summary>
    public static DateTimeOffsetValueConverter Instance { get; } = new();
}
