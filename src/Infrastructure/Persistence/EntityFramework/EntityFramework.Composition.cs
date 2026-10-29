using Application.Common.Events;
using Application.Common.Messaging.Serialization;
using Application.Common.Persistence.Capabilities;
using Application.Common.Persistence.UnitOfWorks;
using Application.Common.Querying.Mapping;

using Core.Common.Domain.Events;

using Infrastructure.Events;
using Infrastructure.Integrations.Messaging.Serialization;
using Infrastructure.Persistence.EntityFramework.Capabilities;
using Infrastructure.Persistence.EntityFramework.Interceptors;
using Infrastructure.Persistence.EntityFramework.Options;
using Infrastructure.Persistence.EntityFramework.Outbox;
using Infrastructure.Persistence.EntityFramework.QueryTranslation;
using Infrastructure.Persistence.EntityFramework.UnitOfWorks;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.Persistence.EntityFramework;

/// <summary>Registers the EF Core persistence stack: DbContext, QueryTranslator, and UnitOfWork.</summary>
public static class EntityFrameworkComposition
{
    /// <summary>Adds EF Core persistence services to the service collection.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configuration">Configuration holding the <c>EfCore</c> section and <c>ConnectionStrings</c>.</param>
    /// <typeparam name="TContext">The derived DbContext type.</typeparam>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// The connection string name is missing or the named connection string is not found.
    /// </exception>
    public static WebApplicationBuilder AddEfCorePersistence<TContext>(
        this WebApplicationBuilder builder,
        IConfiguration configuration)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        IServiceCollection services = builder.Services;

        // Bind settings from configuration section
        PersistenceSetting setting = configuration
            .GetSection(PersistenceConstants.SectionName)
            .Get<PersistenceSetting>() ?? new PersistenceSetting();

        // Validate: fail fast with inline validation (including connection string resolution)
        Result<PersistenceSetting> validationResult = PersistenceSettingValidation
            .ValidateWithConnectionString(setting, configuration);
        if (validationResult.IsFailure)
        {
            throw new InvalidOperationException(
                string.Join("; ", PersistenceSettingValidation.Describe(validationResult)));
        }

        // Register settings
        services.Configure<PersistenceSetting>(o =>
        {
            // The settings are bound from configuration
        });
        services.TryAddSingleton(setting);

        // Register domain-event publication. MediatR is optional so migration-only hosts
        // can compose persistence without an application mediator.
        RegisterDomainEventPublisher(services);
        RegisterEventSerialization(services);

        // Register DbContext with provider-specific options
        services.AddDbContext<TContext>(
            (provider, optionsBuilder) =>
            {
                PersistenceSetting opt = provider.GetRequiredService<PersistenceSetting>();
                IConfiguration config = provider.GetRequiredService<IConfiguration>();
                string? cs = config.GetConnectionString(opt.ConnectionStringName!);

                optionsBuilder
                    .EnableSensitiveDataLogging(opt.EnableSensitiveDataLogging)
                    .EnableDetailedErrors(opt.EnableDetailedErrors)
                    .ConfigureWarnings(w => w
                        .Default(WarningBehavior.Throw)
                        .Log(CoreEventId.SensitiveDataLoggingEnabledWarning));

                // Register concern interceptors (lifecycle, versionable, domain events).
                optionsBuilder.Apply(
                    provider.GetRequiredService<IDomainEventSerializer>(),
                    provider.GetService<ILogger<OutboxInterceptor>>());

                switch (opt.Provider.ToLowerInvariant())
                {
                    case "postgresql":
                        optionsBuilder.UseNpgsql(cs, npgsqlOptions =>
                        {
                            npgsqlOptions
                                .UsePersistenceMigrationHistory()
                                .CommandTimeout(opt.CommandTimeoutSeconds)
                                .EnableRetryOnFailure(
                                    opt.MaxRetryAttempts,
                                    TimeSpan.FromMilliseconds(opt.RetryBaseDelayMs),
                                    null);
                        });
                        break;

                    default:
                        throw new NotSupportedException($"Provider '{opt.Provider}' is not supported.");
                }
            },
            ServiceLifetime.Scoped,
            ServiceLifetime.Scoped);

        // Register QueryMapRegistry and IQueryMapProvider (caller should populate it)
        services.TryAddSingleton<QueryMapRegistry>();
        services.TryAddSingleton<IQueryMapProvider>(sp => sp.GetRequiredService<QueryMapRegistry>());

        // Register QueryTranslator as scoped per entity type
        // The caller registers specific entity translators via the registry
        services.AddScoped(typeof(IQueryTranslator<>), typeof(QueryTranslationEfCore<>));

        // Register: Query capabilities — singleton per provider
        services.TryAddSingleton<IQueryCapabilities, EfCoreQueryCapabilities>();

        // Register: Base DbContext type so EfCoreUnitOfWork can resolve it.
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());

        // Register UnitOfWork
        services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();

        return builder;
    }

    /// <summary>
    /// Configures EF Core DbContext directly with a connection string.
    /// Useful for testing scenarios where configuration is not available.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="provider">The database provider (default: "PostgreSQL").</param>
    /// <typeparam name="TContext">The derived DbContext type.</typeparam>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddEfCorePersistence<TContext>(
        this WebApplicationBuilder builder,
        string connectionString,
        string provider = "PostgreSQL")
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionString);

        IServiceCollection services = builder.Services;

        PersistenceSetting setting = new()
        {
            Provider = provider
        };

        services.TryAddSingleton(setting);

        // Register domain-event publication. MediatR is optional so migration-only hosts
        // can compose persistence without an application mediator.
        RegisterDomainEventPublisher(services);
        RegisterEventSerialization(services);

        services.AddDbContext<TContext>(
            (sp, optionsBuilder) =>
            {
                optionsBuilder
                    .EnableSensitiveDataLogging(setting.EnableSensitiveDataLogging)
                    .EnableDetailedErrors(setting.EnableDetailedErrors)
                    .ConfigureWarnings(w => w
                        .Default(WarningBehavior.Throw)
                        .Log(CoreEventId.SensitiveDataLoggingEnabledWarning));

                // Register concern interceptors (lifecycle, versionable, domain events).
                optionsBuilder.Apply(
                    sp.GetRequiredService<IDomainEventSerializer>(),
                    sp.GetService<ILogger<OutboxInterceptor>>());

                switch (setting.Provider.ToLowerInvariant())
                {
                    case "postgresql":
                        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
                        {
                            npgsqlOptions
                                .UsePersistenceMigrationHistory()
                                .CommandTimeout(setting.CommandTimeoutSeconds)
                                .EnableRetryOnFailure(
                                    setting.MaxRetryAttempts,
                                    TimeSpan.FromMilliseconds(setting.RetryBaseDelayMs),
                                    null);
                        });
                        break;

                    default:
                        throw new NotSupportedException($"Provider '{setting.Provider}' is not supported.");
                }
            },
            ServiceLifetime.Scoped,
            ServiceLifetime.Scoped);

        // Register: Base DbContext type so EfCoreUnitOfWork can resolve it.
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());

        services.TryAddSingleton<QueryMapRegistry>();
        services.TryAddSingleton<IQueryMapProvider>(sp => sp.GetRequiredService<QueryMapRegistry>());
        services.AddScoped(typeof(IQueryTranslator<>), typeof(QueryTranslationEfCore<>));

        // Register: Query capabilities — singleton per provider
        services.TryAddSingleton<IQueryCapabilities, EfCoreQueryCapabilities>();

        services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();

        return builder;
    }

    /// <summary>Adds the EF Core persistence pipeline hook for the host.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseEfCorePersistence(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // DbContexts are request-scoped; no middleware is required for EF Core.
        return app;
    }

    /// <summary>Registers the event serializers and the type registry used by the outbox.</summary>
    /// <param name="services">The host's service collection.</param>
    private static void RegisterEventSerialization(IServiceCollection services)
    {
        services.TryAddSingleton<IEventTypeRegistry>(_ => new EventTypeRegistry(typeof(DomainEvent).Assembly));
        services.TryAddSingleton<JsonMessageSerializer>();
        services.TryAddSingleton<IDomainEventSerializer>(sp => sp.GetRequiredService<JsonMessageSerializer>());
        services.TryAddSingleton<IIntegrationEventSerializer>(sp => sp.GetRequiredService<JsonMessageSerializer>());
    }

    /// <summary>
    /// Registers the domain-event publisher, falling back to a no-op publisher when the host
    /// did not compose MediatR (for example, a migration-only process).
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    private static void RegisterDomainEventPublisher(IServiceCollection services)
    {
        services.TryAddScoped<IDomainEventPublisher>(sp =>
            sp.GetService<IPublisher>() is { } publisher
                ? new DomainEventPublisher(publisher)
                : NullDomainEventPublisher.Instance);
    }
}
