using System.Reflection;

using Application.BuildingBlocks.Persistence;
using Application.BuildingBlocks.Security.Authorization.Registry;
using Application.BuildingBlocks.UseCases.Mediation;

using FluentValidation;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Application.BuildingBlocks;

/// <summary>Composition root for the reusable Application BuildingBlocks.</summary>
public static class BuildingBlockApplicationComposition
{
    /// <summary>Registers mediation, pipeline behaviors, mapping, and validators.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="additionalAssemblies">Additional assemblies to scan for handlers and validators.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddBuildingBlockApplication(
        this WebApplicationBuilder builder,
        params Assembly[] additionalAssemblies)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Register: platform catalogs that module contributions extend.
        builder.Services.AddSingleton<PlatformPermissionCatalog>();
        builder.Services.AddSingleton<IPermissionCatalogContributor>(
            provider => provider.GetRequiredService<PlatformPermissionCatalog>());
        builder.Services.AddSingleton<IPermissionCatalog>(
            provider => provider.GetRequiredService<PlatformPermissionCatalog>());

        builder.Services.AddSingleton<PlatformPersistenceSchemaCatalog>();
        builder.Services.AddSingleton<IPersistenceSchemaCatalogContributor>(
            provider => provider.GetRequiredService<PlatformPersistenceSchemaCatalog>());
        builder.Services.AddSingleton<IPersistenceSchemaCatalog>(
            provider => provider.GetRequiredService<PlatformPersistenceSchemaCatalog>());

        // Register: MediatR with the shared pipeline behaviors.
        builder.AddMediators(additionalAssemblies);

        // Register: validators from the host, this assembly, and any extra assembly.
        builder.Services.AddValidatorsFromAssembly(Assembly.GetEntryAssembly());
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        foreach (Assembly assembly in additionalAssemblies)
            builder.Services.AddValidatorsFromAssembly(assembly);

        return builder;
    }

    /// <summary>Warms the BuildingBlocks services at startup.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseBuildingBlockApplication(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseMediators();
        return app;
    }
}
