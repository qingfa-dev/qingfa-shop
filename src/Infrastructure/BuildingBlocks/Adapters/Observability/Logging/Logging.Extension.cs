using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.BuildingBlocks.Adapters.Observability.Logging;

public static class LoggingExtension
{
    #region Service Registration

    public static WebApplicationBuilder AddObservabilityLogging(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration
            .GetSection(ObservabilitySetting.SectionName)
            .Get<ObservabilitySetting>() ?? new ObservabilitySetting();

        builder.Logging.SetMinimumLevel(options.MinimumLogLevel);

        builder.Services.AddHttpLogging(logging =>
        {
            logging.LoggingFields = HttpLoggingFields.RequestMethod
                | HttpLoggingFields.RequestPath
                | HttpLoggingFields.ResponseStatusCode
                | HttpLoggingFields.Duration;

            foreach (var header in options.SensitiveHeaders)
                logging.RequestHeaders.Remove(header);
        });

        return builder;
    }

    #endregion

    #region Pipeline Configuration

    public static WebApplication UseObservabilityLogging(this WebApplication app)
    {
        app.UseWhen(
            context => !context.Request.Path.StartsWithSegments("/health"),
            builder => builder.UseHttpLogging());

        return app;
    }

    #endregion
}
