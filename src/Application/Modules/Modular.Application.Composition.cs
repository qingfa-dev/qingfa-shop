using Application.BuildingBlocks.Persistence;
using Application.BuildingBlocks.Security.Authorization.Registry;
using Application.Modules.PermissionCatalog;
using Application.Modules.Payments.Permissions;
using Application.Modules.Persistence;
using Application.Modules.Todos.Persistence;
using Application.Modules.Todos.Permissions;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Modules;

/// <summary>Composition root for application business modules.</summary>
public static class ModularApplicationComposition
{
    /// <summary>Registers the application modules.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddModularApplication(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Register: Todos contributes its permissions and persistence schema to the
        // composite catalogs that platform code consumes.
        builder.Services.AddSingleton<TodoPermissionCatalog>();
        builder.Services.AddSingleton<IPermissionCatalogContributor>(
            provider => provider.GetRequiredService<TodoPermissionCatalog>());
        builder.Services.AddSingleton<IPermissionCatalog, ApplicationPermissionCatalog>();

        builder.Services.AddSingleton<TodoPersistenceSchemaCatalog>();
        builder.Services.AddSingleton<IPersistenceSchemaCatalogContributor>(
            provider => provider.GetRequiredService<TodoPersistenceSchemaCatalog>());
        builder.Services.AddSingleton<IPersistenceSchemaCatalog, ApplicationPersistenceSchemaCatalog>();

        // Register: Payments contributes its permissions to the composite catalog.
        builder.Services.AddSingleton<PaymentPermissionCatalog>();
        builder.Services.AddSingleton<IPermissionCatalogContributor>(
            provider => provider.GetRequiredService<PaymentPermissionCatalog>());

        return builder;
    }

    /// <summary>Runs module startup hooks.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseModularApplication(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app;
    }
}
