using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Converters;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Providers;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Conventions;

/// <summary>
/// Convention that configures all enum properties to store as their underlying integer type.
/// </summary>
/// <remarks>
/// Uses <see cref="EnumValueConverter{TEnum}"/> for compact storage and fast comparison.
/// </remarks>
public static class EnumAsIntConvention
{
    /// <summary>
    /// Applies enum-to-int conversion to all enum properties in the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="providerKind">The database provider kind (reserved for future use).</param>
    public static void Apply(ModelBuilder modelBuilder, PersistenceProviderKind providerKind)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Skip: owned value objects are configured through their owner's nested builder.
            if (entityType.IsOwned())
                continue;

            foreach (var property in entityType.GetProperties())
            {
                // Determine the underlying enum type (handle nullable enums)
                Type? enumType = property.ClrType.IsEnum ? property.ClrType : null;

                // If not a direct enum, check for Nullable<TEnum>
                if (enumType == null && Nullable.GetUnderlyingType(property.ClrType) is Type underlyingType && underlyingType.IsEnum)
                {
                    enumType = underlyingType;
                }

                if (enumType == null)
                    continue;

                // Skip if already configured with a conversion
                if (property.GetValueConverter() != null)
                    continue;

                // Skip if primary key or foreign key
                if (property.IsPrimaryKey() || property.IsForeignKey())
                    continue;

                var propertyBuilder = modelBuilder.Entity(entityType.ClrType)
                    .Property(property.Name);

                bool isNullable = property.ClrType != enumType;

                if (isNullable)
                {
                    // Create the generic NullableEnumValueConverter for this enum type
                    var converterType = typeof(NullableEnumValueConverter<>).MakeGenericType(enumType);
                    var converter = (ValueConverter)converterType.GetProperty("Instance")!.GetValue(null)!;

                    propertyBuilder.HasConversion(converter);
                }
                else
                {
                    // Create the generic EnumValueConverter for this enum type
                    var converterType = typeof(EnumValueConverter<>).MakeGenericType(enumType);
                    var converter = (ValueConverter)converterType.GetProperty("Instance")!.GetValue(null)!;

                    propertyBuilder.HasConversion(converter);
                }
            }
        }
    }

    // ─── Fluent extension methods ─────────────────────────────────────────

    /// <summary>
    /// Configures an enum property to store as its underlying integer type.
    /// </summary>
    /// <typeparam name="TEnum">The enum type being converted.</typeparam>
    /// <param name="propertyBuilder">The property builder for the enum property.</param>
    /// <returns>The property builder for further chaining.</returns>
    /// <remarks>
    /// Applies <see cref="EnumValueConverter{TEnum}"/> for compact integer storage.
    /// </remarks>
    public static PropertyBuilder<TEnum> HasEnumConversion<TEnum>(
        this PropertyBuilder<TEnum> propertyBuilder)
        where TEnum : struct, Enum
    {
        propertyBuilder.HasConversion(EnumValueConverter<TEnum>.Instance);
        return propertyBuilder;
    }

    /// <summary>
    /// Configures a nullable enum property to store as its underlying nullable integer type.
    /// </summary>
    /// <typeparam name="TEnum">The enum type being converted.</typeparam>
    /// <param name="propertyBuilder">The property builder for the nullable enum property.</param>
    /// <returns>The property builder for further chaining.</returns>
    /// <remarks>
    /// Applies <see cref="NullableEnumValueConverter{TEnum}"/> for compact nullable integer storage.
    /// </remarks>
    public static PropertyBuilder<TEnum?> HasEnumConversion<TEnum>(
        this PropertyBuilder<TEnum?> propertyBuilder)
        where TEnum : struct, Enum
    {
        propertyBuilder.HasConversion(NullableEnumValueConverter<TEnum>.Instance);
        return propertyBuilder;
    }
}
