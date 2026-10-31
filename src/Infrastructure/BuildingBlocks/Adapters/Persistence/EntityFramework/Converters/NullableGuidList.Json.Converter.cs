using System.Text.Json;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Converters;

/// <summary>
/// Converts a nullable list of <see cref="Guid"/> values to a JSON string for database storage.
/// </summary>
/// <remarks>
/// Stores the collection as a JSON array, suitable for PostgreSQL JSONB columns.
/// Null collections are serialized as empty arrays.
/// </remarks>
public sealed class NullableGuidListJsonValueConverter : ValueConverter<ICollection<Guid>?, string>
{
    /// <summary>Creates a new nullable Guid list JSON converter.</summary>
    public NullableGuidListJsonValueConverter()
        : base(
            v => v != null ? JsonSerializer.Serialize(v) : "[]",
            v => DeserializeGuidList(v))
    {
    }

    /// <summary>Gets a shared instance of the converter.</summary>
    public static NullableGuidListJsonValueConverter Instance { get; } = new();

    static ICollection<Guid>? DeserializeGuidList(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new List<Guid>();

        var list = JsonSerializer.Deserialize<List<Guid>>(json);
        if (list == null)
            return new List<Guid>();

        return list;
    }
}
