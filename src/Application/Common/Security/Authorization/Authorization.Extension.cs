using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Application.Common.Security.Authorization.Options;
using Application.Common.Security.Authorization.Permissions.Caches;
using Application.Common.Security.Authorization.Permissions.Services;
using Application.Common.Security.Authorization.Policies;
using Application.Common.Security.Authorization.Requirements;

namespace Application.Common.Security.Authorization;

// Boundary: DI registration — wires the provider-neutral authorization pipeline into the host.

/// <summary>Registers and applies the application's authorization pipeline.</summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Registers authorization options, the permission cache, the permission service,
    /// the permission policy provider, and the permission requirement handler.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static WebApplicationBuilder AddApplicationAuthorization(
        this WebApplicationBuilder builder)
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
        builder.Services.AddSingleton<IPermissionCache, PermissionCache>();

        // Initialize: cache-first permission resolution that merges role and direct claims.
        builder.Services.AddScoped<IPermissionService, PermissionService>();

        // Initialize: framework authorization services before replacing the policy provider.
        builder.Services.AddAuthorization();

        // Initialize: resolve canonical permission identifiers to permission requirements on demand.
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        // Initialize: evaluate permission requirements through the permission service.
        builder.Services.AddScoped<IAuthorizationHandler, PermissionRequirementAuthorizationHandler>();

        return builder;
    }

    /// <summary>Adds the authorization middleware to the request pipeline.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <c>null</c>.</exception>
    public static WebApplication UseApplicationAuthorization(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseAuthorization();
        return app;
    }
}
