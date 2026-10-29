using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;


namespace Infrastructure.Integrations.Observability.HealthChecks;

public static class HealthChecksExtension
{
    #region Service Registration

    public static WebApplicationBuilder AddObservabilityHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks();

        var pgConnection = ResolveConnectionString(builder.Configuration,
            "db", "DefaultConnection");
        if (pgConnection is not null)
            builder.Services.AddHealthChecks()
                .AddNpgSql(pgConnection, tags: ["ready", "database"]);

        var redisConnection = ResolveConnectionString(builder.Configuration,
            "redis", "Cache");
        if (redisConnection is not null)
            builder.Services.AddHealthChecks()
                .AddRedis(redisConnection, tags: ["ready", "cache"]);

        return builder;
    }

    #endregion

    #region Pipeline Configuration

    public static WebApplication UseObservabilityHealthChecks(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<ObservabilitySetting>();

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = GetHealthResponseWriter(options)
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = GetHealthResponseWriter(options)
        });

        return app;
    }

    #endregion

    #region Private Helpers

    private static string? ResolveConnectionString(
        ConfigurationManager configuration,
        string aspireResourceName,
        string fallbackName)
    {
        var aspire = configuration.GetConnectionString(aspireResourceName);
        if (!string.IsNullOrEmpty(aspire))
            return aspire;

        var fallback = configuration.GetConnectionString(fallbackName);
        return !string.IsNullOrEmpty(fallback) ? fallback : null;
    }

    private static Func<HttpContext, HealthReport, Task> GetHealthResponseWriter(
        ObservabilitySetting options)
    {
        return options.ExposeDetailedReport
            ? WriteDetailedReport
            : WriteStatusOnly;
    }

    private static Task WriteStatusOnly(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "text/plain";
        return context.Response.WriteAsync(report.Status.ToString());
    }

    private static Task WriteDetailedReport(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration,
            entries = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration
                })
        });
        return context.Response.WriteAsync(json);
    }

    #endregion
}
