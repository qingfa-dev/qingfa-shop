using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Converters;

/// <summary>
/// Converts nullable <see cref="decimal"/> values with configurable precision and scale.
/// </summary>
/// <remarks>
/// Defaults to precision 18 and scale 6. The converter ensures consistent
/// decimal handling across database providers. Null values are preserved.
/// </remarks>
public sealed class NullableDecimalValueConverter : ValueConverter<decimal?, decimal?>
{
    /// <summary>Default precision for decimal columns.</summary>
    public const int DefaultPrecision = 18;

    /// <summary>Default scale for decimal columns.</summary>
    public const int DefaultScale = 6;

    /// <summary>Creates a new nullable decimal converter with default precision and scale.</summary>
    public NullableDecimalValueConverter()
        : base(
            v => v.HasValue ? Math.Round(v.Value, DefaultScale) : null,
            v => v)
    {
    }

    /// <summary>Creates a new nullable decimal converter with custom precision and scale.</summary>
    /// <param name="precision">Total number of digits (default 18).</param>
    /// <param name="scale">Number of decimal places (default 6).</param>
    public NullableDecimalValueConverter(int precision, int scale)
        : base(
            v => v.HasValue ? Math.Round(v.Value, scale) : null,
            v => v)
    {
    }

    /// <summary>Gets a shared instance of the converter with default precision and scale.</summary>
    public static NullableDecimalValueConverter Instance { get; } = new();
}
