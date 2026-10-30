using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

using Infrastructure.Integrations.Serialization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Security.Authentication;

/// <summary>Keeps authentication failures on the shared Result contract.</summary>
public static class AuthenticationResultComposition
{
    /// <summary>Configures the JWT challenge and forbidden responses.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddAuthenticationResultContract(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Configure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                options.Events ??= new JwtBearerEvents();

                options.Events.OnChallenge = async context =>
                {
                    // Preserve: HandleResponse suppresses the framework's challenge,
                    // including its WWW-Authenticate header, so advertise it here.
                    context.Response.Headers.WWWAuthenticate = JwtBearerDefaults.AuthenticationScheme;

                    context.HandleResponse();
                    await Result.Fail(Error.Unauthorized(
                            "Authentication.Required",
                            "Authentication is required."))
                        .ToApiResult()
                        .ExecuteAsync(context.HttpContext);
                };

                options.Events.OnForbidden = context => Result.Fail(Error.Forbidden(
                        "Authorization.Forbidden",
                        "The caller is not allowed to perform this operation."))
                    .ToApiResult()
                    .ExecuteAsync(context.HttpContext);
            });

        return services;
    }
}
