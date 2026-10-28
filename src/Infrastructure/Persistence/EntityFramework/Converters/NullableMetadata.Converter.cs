using System.Text.Json;
using System.Text.Json.Serialization;

using Core.Common.Monad.Metadata;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.EntityFramework.Converters;

/// <summary>
/// Converts nullable <see cref="DefaultMetadata"/> properties to JSON strings for database storage.
/// </summary>
/// <remarks>
/// Serializes metadata to compact JSON with camelCase property naming.
/// Null values are serialized as empty JSON objects.
/// </remarks>
public sealed class NullableMetadataValueConverter : ValueConverter<DefaultMetadata?, string>
{
    /// <summary>JSON serializer options for metadata serialization.</summary>
    public static JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters = { JsonObjectValueConverter.Instance }
    };

    /// <summary>Creates a new nullable metadata JSON converter.</summary>
    public NullableMetadataValueConverter()
        : base(
            v => v != null ? JsonSerializer.Serialize(v, JsonSerializerOptions) : "{}",
            v => DeserializeMetadata(v))
    {
    }

    /// <summary>Gets a shared instance of the converter.</summary>
    public static NullableMetadataValueConverter Instance { get; } = new();

    static DefaultMetadata? DeserializeMetadata(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new DefaultMetadata();

        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json, JsonSerializerOptions);
        if (dict == null)
            return new DefaultMetadata();

        return new DefaultMetadata(dict);
    }
}
