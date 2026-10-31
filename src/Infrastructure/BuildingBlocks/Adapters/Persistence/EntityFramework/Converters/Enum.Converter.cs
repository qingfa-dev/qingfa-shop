using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Converters;

/// <summary>
/// Converts an enum property to its underlying integer type for database storage.
/// </summary>
/// <typeparam name="TEnum">The enum type being converted.</typeparam>
/// <remarks>
/// Stores enums as their underlying integer (typically <c>int</c>), providing
/// compact storage and fast comparison across all database providers.
/// </remarks>
public sealed class EnumValueConverter<TEnum> : ValueConverter<TEnum, int>
    where TEnum : struct, Enum
{
    /// <summary>Creates a new converter for the specified enum type.</summary>
    public EnumValueConverter()
        : base(
            v => Convert.ToInt32(v),
            v => (TEnum)Enum.ToObject(typeof(TEnum), v))
    {
    }

    /// <summary>Gets a shared instance of the converter.</summary>
    public static EnumValueConverter<TEnum> Instance { get; } = new();
}
