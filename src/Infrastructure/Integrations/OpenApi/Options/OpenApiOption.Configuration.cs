using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Infrastructure.Integrations.OpenApi.Options;

/// <summary>
/// Provides extension methods for configuring OpenAPI options using a structured set of transformers.
/// </summary>
public static class OpenApiOptionsSetup
{
    /// <summary>
    /// Configures the <see cref="OpenApiOptions"/> with standalone schema identifiers, 
    /// specialized documentation transformers, and security requirements.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions"/> to configure.</param>
    /// <returns>The configured <see cref="OpenApiOptions"/>.</returns>
    public static OpenApiOptions ConfigureCustomOptions(this OpenApiOptions options)
    {
        // Contract: pre=options!=null
        ArgumentNullException.ThrowIfNull(options);

        // Update: Generate unique schema reference IDs for types that need
        // disambiguation (nested types like PasswordLogin.Request, generic
        // envelopes like Result<T>, arrays, etc.). Relies on the built-in
        // default to decide which types should be inlined (primitives,
        // collections, dictionaries — these return null). For everything
        // else, delegates to the custom naming which adds nested-type
        // prefixing, "And" separator for generic args, and PascalCase names.
        options.CreateSchemaReferenceId = jsonTypeInfo =>
        {
            string? defaultId = OpenApiOptions.CreateDefaultSchemaReferenceId(jsonTypeInfo);
            if (defaultId is null)
            {
                return null;
            }

            return OpenApiSchemaNaming.GetSchemaReferenceId(jsonTypeInfo.Type);
        };

        options.AddDocumentTransformer((document, _, _) =>
        {
            // Update: Set global document metadata (Title, Version, Description)
            document.Info.Title = OpenApiOptionsConstant.Info.Title;
            document.Info.Version = OpenApiOptionsConstant.Info.Version;
            document.Info.Description = OpenApiOptionsConstant.Info.Description;

            // Assume: OpenAPI generator detects host automatically; avoiding hardcoding document.Servers 
            // ensuring "Try it out" feature points to the correct endpoint regardless of port or proxy.

            // Add: Bearer JWT security scheme so Scalar shows the "Authorize" button
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter your JWT Bearer token"
            };

            document.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", null, null)] = []
                }
            ];

            return Task.CompletedTask;
        });

        return options;
    }
}
