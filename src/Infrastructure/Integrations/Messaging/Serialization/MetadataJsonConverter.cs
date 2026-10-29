using System.Text.Json;
using System.Text.Json.Serialization;

using Core.Common.Monad.Metadata;

namespace Infrastructure.Integrations.Messaging.Serialization;

/// <summary>Serializes <see cref="DefaultMetadata"/> as a flat JSON object map.</summary>
/// <remarks>
/// The dictionary base type would otherwise be treated as an arbitrary collection by some
/// serializers; writing the entries directly keeps the wire shape stable. Readers materialise
/// scalars as CLR values and keep structured values as cloned <see cref="JsonElement"/>s.
/// </remarks>
internal sealed class MetadataJsonConverter : JsonConverter<DefaultMetadata>
{
    /// <inheritdoc />
    public override DefaultMetadata? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected a JSON object for metadata but found '{reader.TokenType}'.");

        using JsonDocument document = JsonDocument.ParseValue(ref reader);

        var metadata = DefaultMetadata.Create();
        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            // Preserve: a JSON null keeps its key so presence is not silently dropped.
            metadata[property.Name] = Normalize(property.Value)!;
        }

        return metadata;
    }

    /// <summary>Materialises a JSON value as the CLR scalar the metadata accessors expect.</summary>
    /// <remarks>
    /// Typed accessors convert from strings, numbers and booleans, so scalars are unwrapped here;
    /// structured values stay as <see cref="JsonElement"/> clones because they have no wire type.
    /// </remarks>
    private static object? Normalize(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                // Split: a ternary would unify long and double to double.
                if (element.TryGetInt64(out long integer))
                    return integer;

                return element.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            default:
                // Clone: the owning document is disposed before the value is read.
                return element.Clone();
        }
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        DefaultMetadata value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach ((string key, object entry) in value)
        {
            if (entry is null)
            {
                writer.WriteNull(key);
                continue;
            }

            // Serialize by runtime type: values are boxed as object and would otherwise lose shape.
            writer.WritePropertyName(key);
            JsonSerializer.Serialize(writer, entry, entry.GetType(), options);
        }

        writer.WriteEndObject();
    }
}
