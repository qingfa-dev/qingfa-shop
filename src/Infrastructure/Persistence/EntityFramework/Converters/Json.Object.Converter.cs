using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Persistence.EntityFramework.Converters;

/// <summary>
/// Converts JSON object values into CLR primitives, lists, and dictionaries.
/// </summary>
/// <remarks>
/// Without it, <see cref="Dictionary{TKey, TValue}"/> values typed as <see cref="object"/>
/// materialize as <see cref="JsonElement"/> instances, which break structural equality and
/// value comparers. Numbers are read as <see cref="long"/> when integral and
/// <see cref="decimal"/> otherwise; nested objects use case-insensitive keys.
/// </remarks>
internal sealed class JsonObjectValueConverter : JsonConverter<object>
{
    /// <summary>Gets the shared converter instance.</summary>
    public static JsonObjectValueConverter Instance { get; } = new();

    /// <inheritdoc />
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => ReadValue(ref reader, options);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        Type type = value.GetType();

        // Guard: a plain object carries no shape, so write an empty JSON object.
        if (type == typeof(object))
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
            return;
        }

        JsonSerializer.Serialize(writer, value, type, options);
    }

    private static object? ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.True:
                return true;

            case JsonTokenType.False:
                return false;

            case JsonTokenType.String:
                return reader.GetString();

            case JsonTokenType.Number:
                return ReadNumber(ref reader);

            case JsonTokenType.StartArray:
                return ReadArray(ref reader, options);

            case JsonTokenType.StartObject:
                return ReadObject(ref reader, options);

            default:
                throw new JsonException($"Unsupported JSON token '{reader.TokenType}'.");
        }
    }

    private static object ReadNumber(ref Utf8JsonReader reader)
    {
        if (reader.TryGetInt64(out long integral))
            return integral;

        if (reader.TryGetDecimal(out decimal exact))
            return exact;

        return reader.GetDouble();
    }

    private static List<object?> ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var items = new List<object?>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            items.Add(ReadValue(ref reader, options));
        }

        return items;
    }

    private static Dictionary<string, object?> ReadObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string key = reader.GetString()!;
            reader.Read();
            values[key] = ReadValue(ref reader, options);
        }

        return values;
    }
}
