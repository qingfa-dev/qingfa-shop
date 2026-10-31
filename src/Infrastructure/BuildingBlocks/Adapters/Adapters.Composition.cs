using Infrastructure.BuildingBlocks.Adapters.BackgroundJobs;
using Infrastructure.BuildingBlocks.Adapters.Http;
using Infrastructure.BuildingBlocks.Adapters.Messaging;
using Infrastructure.BuildingBlocks.Adapters.Notifications;
using Infrastructure.BuildingBlocks.Adapters.Observability;
using Infrastructure.BuildingBlocks.Adapters.OpenApi;
using Infrastructure.BuildingBlocks.Adapters.Storage;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.BuildingBlocks.Adapters;

/// <summary>Composition root for outbound adapters and cross-cutting host features.</summary>
public static class AdaptersComposition
{
    /// <summary>
    /// Registers HTTP clients, background jobs, messaging, notifications, storage, observability, and OpenAPI.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <param name="messagingModules">Messaging topology contributors.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static WebApplicationBuilder AddAdapters(
        this WebApplicationBuilder builder,
        IConfiguration configuration,
        IEnumerable<IMessagingModule> messagingModules)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(messagingModules);

        builder.AddHttpClients();
        builder.AddBackgroundJobs();
        builder.AddMessaging(configuration, messagingModules);
        builder.AddNotifications();
        builder.AddStorage();
        builder.AddObservability();
        builder.AddOpenApiDocumentation();

        return builder;
    }

    /// <summary>Adds the adapter middleware and endpoint mappings to the pipeline.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <c>null</c>.</exception>
    public static WebApplication UseAdapters(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseObservability();
        app.UseStorage();
        app.UseBackgroundJobs();
        app.UseMessaging();
        app.UseOpenApiDocumentation();

        return app;
    }
}
