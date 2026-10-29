using Infrastructure.Integrations.OpenApi.Options;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Scalar.AspNetCore;

namespace Infrastructure.Integrations.OpenApi;

/// <summary>
/// Provides extension methods for configuring and enabling OpenAPI documentation with Scalar integration.
/// </summary>
public static class OpenApiComposition
{
    #region Service Registration

    /// <summary>
    /// Registers and configures the OpenAPI documentation services, including custom schemas and endpoint profiles.
    /// </summary>
    /// <param name="builder">The web application builder to add services to.</param>
    /// <returns>The web application builder for method chaining.</returns>
    public static WebApplicationBuilder AddOpenApiDocumentation(
        this WebApplicationBuilder builder)
    {
        // Contract: pre=builder!=null, post=builder.Services.Contains(OpenApiOptions)
        #region OpenAPI Configuration

        // Add: OpenAPI infrastructure with custom options and transformers
        builder.Services.AddOpenApi(options =>
            // Call: Apply custom formatting    and documentation rules
            options.ConfigureCustomOptions());

        #endregion

        return builder;
    }

    #endregion

    #region Pipeline Configuration

    /// <summary>
    /// Enables OpenAPI endpoints and the interactive Scalar API documentation UI in the application pipeline.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for method chaining.</returns>
    public static WebApplication UseOpenApiDocumentation(this WebApplication app)
    {
        // Contract: pre=app!=null

        #region Endpoint Mapping

        // Map: Expose the generated OpenAPI JSON specification
        app.MapOpenApi();

        // Map: Enable the Scalar interactive API reference UI
        app.MapScalarApiReference(OpenApiOptionsConstant.Info.Endpoint, options =>
            // Update: Configure UI visual appearance and metadata
            options
                .WithTitle(OpenApiOptionsConstant.Info.Title)
                .WithTheme(ScalarTheme.DeepSpace));

        #endregion

        return app;
    }

    #endregion
}
