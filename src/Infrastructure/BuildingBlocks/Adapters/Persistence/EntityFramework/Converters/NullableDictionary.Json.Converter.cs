using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Converters;

/// <summary>
/// Converts nullable <see cref="Dictionary{TKey, TValue}"/> properties to JSON strings for database storage.
/// </summary>
/// <remarks>
/// Serializes dictionaries to compact JSON with camelCase property naming.
/// Null values are serialized as empty JSON objects.
/// </remarks>
public sealed class NullableDictionaryJsonValueConverter : ValueConverter<Dictionary<string, object>?, string>
{
    /// <summary>JSON serializer options for dictionary serialization.</summary>
    public static JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters = { JsonObjectValueConverter.Instance }
    };

    /// <summary>Creates a new nullable dictionary JSON converter.</summary>
    public NullableDictionaryJsonValueConverter()
        : base(
            v => v != null ? JsonSerializer.Serialize(v, JsonSerializerOptions) : "{}",
            v => DeserializeDictionary(v))
    {
    }

    /// <summary>Gets a shared instance of the converter.</summary>
    public static NullableDictionaryJsonValueConverter Instance { get; } = new();

    static Dictionary<string, object>? DeserializeDictionary(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new Dictionary<string, object>();

        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json, JsonSerializerOptions);
        if (dict == null)
            return new Dictionary<string, object>();

        return dict;
    }
}
