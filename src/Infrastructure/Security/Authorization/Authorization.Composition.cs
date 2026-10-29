using Application.Common.Security.Authorization.Permissions.Store;
using Application.Common.Security.Authorization.Registry;

using Infrastructure.Security.Authorization.Permissions.Registry;
using Infrastructure.Security.Authorization.Permissions.Store;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.Security.Authorization;

/// <summary>Registers EF Core-backed authorization storage and the permission metadata catalog.</summary>
public static class AuthorizationComposition
{
    /// <summary>Adds the permission store and the Scrutor-discovered metadata registry.</summary>
    /// <param name="services">The host's service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddAuthorizationStore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IPermissionStore, PermissionStoreService>();
        services.TryAddSingleton(TimeProvider.System);

        // Discover: the registry is stateful and shared, so it is a singleton even though
        // each refresh creates its own scope for the scoped EF store.
        services.Scan(scan => scan
            .FromAssemblyOf<PermissionMetadataRegistry>()
            .AddClasses(classes => classes
                .AssignableTo<IPermissionMetadataRegistry>()
                .Where(type => type is { IsAbstract: false, IsInterface: false }))
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        return services;
    }
}
