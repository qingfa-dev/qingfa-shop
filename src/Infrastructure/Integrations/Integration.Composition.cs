using Infrastructure.Integrations.Backgrounds;
using Infrastructure.Integrations.Http;
using Infrastructure.Integrations.Notifications;
using Infrastructure.Integrations.Observability;
using Infrastructure.Integrations.OpenApi;
using Infrastructure.Integrations.Storages;

using Microsoft.AspNetCore.Builder;

namespace Infrastructure.Integrations;

/// <summary>Composition root for outbound integrations and cross-cutting host features.</summary>
public static class IntegrationComposition
{
    /// <summary>
    /// Registers HTTP clients, background jobs, notifications, storage, observability, and OpenAPI.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static WebApplicationBuilder AddIntegration(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddHttpClients();
        builder.AddBackgroundJobs();
        builder.AddNotifications();
        builder.AddStorage();
        builder.AddObservability();
        builder.AddOpenApiDocumentation();

        return builder;
    }

    /// <summary>Adds the integration middleware and endpoint mappings to the pipeline.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <c>null</c>.</exception>
    public static WebApplication UseIntegration(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseObservability();
        app.UseStorage();
        app.UseBackgroundJobs();
        app.UseOpenApiDocumentation();

        return app;
    }
}
