using System.Text.Json;
using System.Text.Json.Serialization;

using Core.BuildingBlocks.Modeling.Metadata;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Converters;

/// <summary>
/// Converts <see cref="DefaultMetadata"/> properties to JSON strings for database storage.
/// </summary>
/// <remarks>
/// Serializes metadata to compact JSON with camelCase property naming.
/// Null values are ignored during serialization.
/// </remarks>
public sealed class MetadataValueConverter : ValueConverter<DefaultMetadata, string>
{
    /// <summary>JSON serializer options for metadata serialization.</summary>
    public static JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters = { JsonObjectValueConverter.Instance }
    };

    /// <summary>Creates a new metadata JSON converter.</summary>
    public MetadataValueConverter()
        : base(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions),
            v => DeserializeMetadata(v))
    {
    }

    /// <summary>Gets a shared instance of the converter.</summary>
    public static MetadataValueConverter Instance { get; } = new();

    static DefaultMetadata DeserializeMetadata(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new DefaultMetadata();

        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json, JsonSerializerOptions);
        if (dict == null)
            return new DefaultMetadata();

        return new DefaultMetadata(dict);
    }
}
