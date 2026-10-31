using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Conventions;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Converters;

/// <summary>
/// Converts <see cref="decimal"/> values with configurable precision and scale.
/// </summary>
/// <remarks>
/// Defaults to precision 18 and scale 6. The converter ensures consistent
/// decimal handling across database providers.
/// </remarks>
public sealed class DecimalValueConverter : ValueConverter<decimal, decimal>
{
    /// <summary>Default precision for decimal columns.</summary>
    public const int DefaultPrecision = DecimalConstant.DefaultPrecision;

    /// <summary>Default scale for decimal columns.</summary>
    public const int DefaultScale = DecimalConstant.DefaultScale;

    /// <summary>Creates a new decimal converter with default precision and scale.</summary>
    public DecimalValueConverter()
        : this(DefaultPrecision, DefaultScale)
    {
    }

    /// <summary>Creates a new decimal converter with custom precision and scale.</summary>
    /// <param name="precision">Total number of digits (default 18).</param>
    /// <param name="scale">Number of decimal places (default 6).</param>
    public DecimalValueConverter(int precision, int scale)
        : base(
            v => Math.Round(v, scale),
            v => v)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(precision);
        ArgumentOutOfRangeException.ThrowIfNegative(scale);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(scale, precision);

        Precision = precision;
        Scale = scale;
    }

    /// <summary>Gets the total number of digits supported by the converter.</summary>
    public int Precision { get; }

    /// <summary>Gets the number of decimal places the converter rounds to.</summary>
    public int Scale { get; }

    /// <summary>Gets a shared instance of the converter with default precision and scale.</summary>
    public static DecimalValueConverter Instance { get; } = new();
}
