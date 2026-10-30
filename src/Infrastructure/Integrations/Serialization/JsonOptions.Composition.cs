using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Integrations.Serialization;

/// <summary>Registers the shared API JSON contract with the host.</summary>
public static class JsonOptionsComposition
{
    /// <summary>Applies <see cref="ApiJson.Options"/> to minimal-API request binding and responses.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddApiJsonOptions(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            // Enforce: JsonOptions.SerializerOptions is read-only, so mirror every setting
            // that changes the wire format; a partial copy lets escaping or numbers drift.
            JsonSerializerOptions target = options.SerializerOptions;
            JsonSerializerOptions source = ApiJson.Options;

            target.AllowTrailingCommas = source.AllowTrailingCommas;
            target.DefaultIgnoreCondition = source.DefaultIgnoreCondition;
            target.DictionaryKeyPolicy = source.DictionaryKeyPolicy;
            target.Encoder = source.Encoder;
            target.IgnoreReadOnlyFields = source.IgnoreReadOnlyFields;
            target.IgnoreReadOnlyProperties = source.IgnoreReadOnlyProperties;
            target.IncludeFields = source.IncludeFields;
            target.MaxDepth = source.MaxDepth;
            target.NumberHandling = source.NumberHandling;
            target.PropertyNameCaseInsensitive = source.PropertyNameCaseInsensitive;
            target.PropertyNamingPolicy = source.PropertyNamingPolicy;
            target.ReadCommentHandling = source.ReadCommentHandling;
            target.ReferenceHandler = source.ReferenceHandler;
            target.UnmappedMemberHandling = source.UnmappedMemberHandling;
            target.WriteIndented = source.WriteIndented;

            target.Converters.Clear();
            foreach (var converter in source.Converters)
                target.Converters.Add(converter);
        });

        return builder;
    }
}
