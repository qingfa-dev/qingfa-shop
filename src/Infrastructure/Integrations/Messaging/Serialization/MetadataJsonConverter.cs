using System.Text.Json;
using System.Text.Json.Serialization;

using Core.Common.Monad.Metadata;

namespace Infrastructure.Integrations.Messaging.Serialization;

/// <summary>Serializes <see cref="DefaultMetadata"/> as a flat JSON object map.</summary>
/// <remarks>
/// The dictionary base type would otherwise be treated as an arbitrary collection by some
/// serializers; writing the entries directly keeps the wire shape stable and lets readers
/// materialise values lazily as <see cref="JsonElement"/> clones.
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
            // Clone: the document is disposed before the metadata values are read.
            metadata[property.Name] = property.Value.Clone();
        }

        return metadata;
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
