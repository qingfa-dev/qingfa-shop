using Infrastructure.BuildingBlocks.Adapters.Security.Authentication;
using Infrastructure.BuildingBlocks.Adapters.Security.Authorization;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.BuildingBlocks.Adapters.Security;

/// <summary>
/// Registers the HTTP-backed request contexts and token services that application handlers depend on.
/// </summary>
public static class SecurityComposition
{
    /// <summary>
    /// Adds the authentication contexts and token services.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configuration">Configuration holding the token sections.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configuration"/> is <c>null</c>.
    /// </exception>
    public static WebApplicationBuilder AddSecurity(
        this WebApplicationBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.AddAuthentication(configuration);
        builder.AddAuthorizationPipeline();
        return builder;
    }

    /// <summary>Adds authentication and authorization middleware to the request pipeline.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseSecurity(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseAuthentication();
        app.UseAuthorizationPipeline();
        return app;
    }
}
