using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Converters;

/// <summary>
/// Converts a nullable enum property to its underlying nullable integer type for database storage.
/// </summary>
/// <typeparam name="TEnum">The enum type being converted.</typeparam>
/// <remarks>
/// Stores nullable enums as their underlying nullable integer (typically <c>int?</c>), providing
/// compact storage and fast comparison across all database providers.
/// </remarks>
public sealed class NullableEnumValueConverter<TEnum> : ValueConverter<TEnum?, int?>
    where TEnum : struct, Enum
{
    /// <summary>Creates a new converter for the specified nullable enum type.</summary>
    public NullableEnumValueConverter()
        : base(
            v => v.HasValue ? Convert.ToInt32(v.Value) : null,
            v => v.HasValue ? (TEnum?)Enum.ToObject(typeof(TEnum), v.Value) : null)
    {
    }

    /// <summary>Gets a shared instance of the converter.</summary>
    public static NullableEnumValueConverter<TEnum> Instance { get; } = new();
}
