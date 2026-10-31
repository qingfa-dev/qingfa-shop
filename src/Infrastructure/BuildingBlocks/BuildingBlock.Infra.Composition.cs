using Infrastructure.BuildingBlocks.Adapters;
using Infrastructure.BuildingBlocks.Adapters.Caching;
using Infrastructure.BuildingBlocks.Adapters.Persistence;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;
using Infrastructure.BuildingBlocks.Adapters.Security;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.BuildingBlocks;

/// <summary>Composition root for reusable Infrastructure BuildingBlocks.</summary>
public static class BuildingBlockInfraComposition
{
    /// <summary>Registers persistence, caching, security, and the adapter pipeline.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <param name="modules">Module contributions.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddBuildingBlockInfrastructure(
        this WebApplicationBuilder builder,
        IConfiguration configuration,
        InfrastructureModuleRegistrations modules)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(modules);

        builder.AddPersistence<ApplicationDbContext>(configuration);
        builder.AddCaching(configuration);
        builder.AddSecurity(configuration);
        builder.AddOutbox(configuration);
        builder.AddEventStore(configuration, modules.EventStores);
        builder.AddAdapters(configuration, modules.Messaging);
        return builder;
    }

    /// <summary>Adds the BuildingBlocks pipeline hooks.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseBuildingBlockInfrastructure(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseCaching();
        app.UsePersistence();
        app.UsePersistenceInitialization();
        app.UseAdapters();
        return app;
    }
}
