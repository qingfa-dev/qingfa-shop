using Infrastructure.Integrations.OpenApi;

using Microsoft.AspNetCore.Builder;

namespace Infrastructure.Integrations;

public static class IntegrationComposition
{
    #region Service Registration

    public static WebApplicationBuilder AddIntegration(
        this WebApplicationBuilder builder)
    {
        builder.AddOpenApiDocumentation();

        return builder;
    }

    #endregion

    #region Pipeline Configuration

    public static WebApplication UseIntegration(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseOpenApiDocumentation();

        return app;
    }

    #endregion
}