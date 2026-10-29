using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Application.Common.Security.Authorization.Options;
using Application.Common.Security.Authorization.Permissions.Caches;
using Application.Common.Security.Authorization.Permissions.Services;
using Application.Common.Security.Authorization.Permissions.Store;
using Application.Common.Security.Authorization.Registry;

using Infrastructure.Security.Authorization.Permissions.Caches;
using Infrastructure.Security.Authorization.Permissions.Registry;
using Infrastructure.Security.Authorization.Permissions.Services;
using Infrastructure.Security.Authorization.Permissions.Store;
using Infrastructure.Security.Authorization.Policies;
using Infrastructure.Security.Authorization.Requirements;

namespace Infrastructure.Security.Authorization;

/// <summary>Composition root for the authorization pipeline and its EF Core-backed storage.</summary>
public static class AuthorizationComposition
{
    /// <summary>
    /// Registers authorization options, the permission cache/service, the policy provider,
    /// the requirement handler, the EF Core store, and the metadata registry.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static WebApplicationBuilder AddAuthorizationPipeline(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Validate: authorization settings must satisfy the validator at startup.
        builder.Services.AddOptions<AuthzSetting>()
            .BindConfiguration(AuthzSetting.SectionName)
            .Validate(
                options => new AuthzSettingValidator().Validate(options).IsValid,
                "Authorization settings are invalid.")
            .ValidateOnStart();

        // Cache: permission lookups backed by ICacheService with tag-based invalidation.
        builder.Services.TryAddSingleton<IPermissionCache, PermissionCache>();

        // Initialize: cache-first permission resolution that merges role and direct claims.
        builder.Services.TryAddScoped<IPermissionService, PermissionService>();

        // Initialize: framework authorization services before replacing the policy provider.
        builder.Services.AddAuthorization();

        // Policy: resolve canonical and multi-permission policy names on demand.
        builder.Services.Replace(ServiceDescriptor.Singleton<
            IAuthorizationPolicyProvider,
            PermissionPolicyProvider>());

        // Handler: evaluate permission requirements through the permission service.
        builder.Services.AddScoped<IAuthorizationHandler, PermissionRequirementAuthorizationHandler>();

        // Storage: EF Core permission claims, repository access, and the metadata catalog.
        builder.AddAuthorizationStore();

        return builder;
    }

    /// <summary>Adds the authorization middleware to the request pipeline.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <c>null</c>.</exception>
    public static WebApplication UseAuthorizationPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseAuthorization();
        return app;
    }

    /// <summary>Adds the permission store and the Scrutor-discovered metadata registry.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static WebApplicationBuilder AddAuthorizationStore(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddScoped<IPermissionStore, PermissionStoreService>();
        builder.Services.TryAddSingleton(TimeProvider.System);

        // Discover: the registry is stateful and shared, so it is a singleton even though
        // each refresh creates its own scope for the scoped EF store.
        builder.Services.Scan(scan => scan
            .FromAssemblyOf<PermissionMetadataRegistry>()
            .AddClasses(classes => classes
                .AssignableTo<IPermissionMetadataRegistry>()
                .Where(type => type is { IsAbstract: false, IsInterface: false }))
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        return builder;
    }
}
