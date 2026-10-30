using System.Text.Json;
using System.Text.Json.Serialization;

namespace Web.Api.Endpoints.Results;

/// <summary>The single JSON configuration for API requests and responses.</summary>
public static class ApiJson
{
    /// <summary>Gets the shared serializer options.</summary>
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = false,
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
