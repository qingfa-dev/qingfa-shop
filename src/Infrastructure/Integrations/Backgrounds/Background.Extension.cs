using FluentValidation;

using Hangfire;
using Hangfire.Redis.StackExchange;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Application.Common.Validation.FluentValidation;
using Infrastructure.Integrations.Backgrounds.Options;
using Infrastructure.Caching.Options;

namespace Infrastructure.Integrations.Backgrounds;

public static class BackgroundJobExtensions
{
    private static readonly string[] DefaultQueues = ["default"];

    /// <summary>
    /// Adds background job services to the dependency injection container.
    /// Configures Hangfire with Redis or in-memory storage based on settings.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <returns>The web application builder for chaining.</returns>
    /// <remarks>
    /// This method performs the following operations:
    /// 1. Registers FluentValidation for BackgroundJobSetting
    /// 2. Binds configuration section and enables validation
    /// 3. Configures Hangfire storage (Redis or in-memory)
    /// 4. Sets up Hangfire server with default queues
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
    public static WebApplicationBuilder AddBackgroundJobs(this WebApplicationBuilder builder)
    {
        // Contract: builder != null
        // Boundary: Domain/Infrastructure - DI container configuration

        // Register FluentValidation validator (includes all rules)
        builder.Services.AddSingleton<IValidator<BackgroundJobSetting>, BackgroundJobSettingValidator>();

        // Bind options and enable FluentValidation
        builder.Services.AddOptions<BackgroundJobSetting>()
            .BindConfiguration(BackgroundJobSetting.SectionName)
            .ValidateFluentValidation()
            .ValidateOnStart();

        // No separate IValidateOptions registration needed

        var options = builder.Configuration.GetSection(BackgroundJobSetting.SectionName)
            .Get<BackgroundJobSetting>() ?? new BackgroundJobSetting();

        builder.Services.AddHangfire((_, config) =>
        {
            // AgentHint: Complex Hangfire configuration with conditional storage setup
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

            if (!options.Enabled)
            {
                // Resource: Minimal in-memory storage for disabled mode (registers IBackgroundJobClient)
                config.UseInMemoryStorage();
                return;
            }

            if (options.CachingEnabled)
            {
                // Cache: Redis storage with connection string resolution (TTL not applicable for storage config)
                var (_, connectionString) = ResolveConnectionString(builder.Configuration);
                if (!string.IsNullOrEmpty(connectionString))
                    config.UseRedisStorage(connectionString);
            }
            else
            {
                // Resource: In-memory storage allocation for development/testing
                config.UseInMemoryStorage();
            }
        });

        if (options.Enabled)
        {
            // Resource: Hangfire server resource acquisition
            builder.Services.AddHangfireServer(serverOptions =>
            {
                serverOptions.ServerName = "General-Worker-Server";
                serverOptions.Queues = DefaultQueues;
            });
        }

        return builder;
    }

    /// <summary>
    /// Integrates Hangfire dashboard and middleware into the web application.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    /// <returns>The web application for chaining.</returns>
    /// <remarks>
    /// This method performs the following operations:
    /// 1. Binds background job configuration from app settings
    /// 2. Skips middleware setup if background jobs are disabled
    /// 3. Registers Hangfire dashboard in development environments
    /// </remarks>
    public static WebApplication UseBackgroundJobs(this WebApplication app)
    {
        // Contract: app != null
        // Boundary: API/Presentation - middleware registration

        var options = app.Configuration.GetSection(BackgroundJobSetting.SectionName)
            .Get<BackgroundJobSetting>() ?? new BackgroundJobSetting();

        if (!options.Enabled)
            return app;

        if (app.Environment.IsDevelopment())
        {
            // Call: Register Hangfire dashboard endpoint for monitoring
            app.UseHangfireDashboard(options.DashboardPath);
        }

        return app;
    }

    /// <summary>
    /// Resolves the appropriate Redis connection string from configuration.
    /// Prioritizes Aspire connection strings over default connection strings.
    /// </summary>
    /// <param name="configuration">The configuration to search for connection strings.</param>
    /// <returns>A tuple containing the connection string name and value (null if not found).</returns>
    /// <remarks>
    /// This method implements a fallback strategy:
    /// 1. First checks for Aspire-specific connection string
    /// 2. Falls back to default connection string
    /// 3. Returns Aspire with null value if neither is found
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
    private static (string Name, string? Value) ResolveConnectionString(IConfiguration configuration)
    {
        // Contract: configuration != null
        // Boundary: Infrastructure/Configuration - connection string resolution

        var aspire = configuration.GetConnectionString(CachingSettingConstant.Connections.Aspire);
        if (!string.IsNullOrEmpty(aspire))
            return (CachingSettingConstant.Connections.Aspire, aspire);

        var @default = configuration.GetConnectionString(CachingSettingConstant.Connections.Default);
        if (!string.IsNullOrEmpty(@default))
            return (CachingSettingConstant.Connections.Default, @default);

        // Exception: Return Aspire with null when no valid connection string found
        return (CachingSettingConstant.Connections.Aspire, null);
    }
}