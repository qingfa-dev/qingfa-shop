using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Converters;

/// <summary>
/// Converts nullable <see cref="DateTimeOffset"/> values to and from <c>string</c> for database storage.
/// </summary>
/// <remarks>
/// Uses round-trip ISO 8601 format to preserve timezone information.
/// Null values are preserved as null during conversion.
/// </remarks>
public sealed class NullableDateTimeOffsetValueConverter : ValueConverter<DateTimeOffset?, string?>
{
    /// <summary>Creates a new nullable DateTimeOffset string converter.</summary>
    public NullableDateTimeOffsetValueConverter()
        : base(
            v => v.HasValue ? v.Value.ToString("O") : null,
            v => string.IsNullOrEmpty(v) ? (DateTimeOffset?)null : DateTimeOffset.Parse(v, null, System.Globalization.DateTimeStyles.RoundtripKind))
    {
    }

    /// <summary>Gets a shared instance of the converter.</summary>
    public static NullableDateTimeOffsetValueConverter Instance { get; } = new();
}
