using Application.Common.Persistence.Capabilities;
using Application.Common.Persistence.UnitOfWorks;
using Application.Common.Querying.Mapping;

using Infrastructure.Persistence.EntityFramework.Capabilities;
using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.Options;
using Infrastructure.Persistence.EntityFramework.QueryTranslation;
using Infrastructure.Persistence.EntityFramework.UnitOfWorks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.Persistence.EntityFramework;

/// <summary>Registers the EF Core persistence stack: DbContext, QueryTranslator, and UnitOfWork.</summary>
public static class EntityFrameworkComposition
{
    /// <summary>Adds EF Core persistence services to the service collection.</summary>
    /// <param name="services">The host's service collection.</param>
    /// <param name="configuration">Configuration holding the <c>EfCore</c> section and <c>ConnectionStrings</c>.</param>
    /// <typeparam name="TContext">The derived DbContext type.</typeparam>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// The connection string name is missing or the named connection string is not found.
    /// </exception>
    public static IServiceCollection AddEfCorePersistence<TContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TContext : EfCoreDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

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
                    .ConfigureWarnings(w => w.Default(WarningBehavior.Throw));

                switch (opt.Provider.ToLowerInvariant())
                {
                    case "postgresql":
                        optionsBuilder.UseNpgsql(cs, npgsqlOptions =>
                        {
                            npgsqlOptions
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
        services.AddScoped<EfCoreDbContext>(sp => sp.GetRequiredService<TContext>());

        // Register UnitOfWork
        services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();

        return services;
    }

    /// <summary>
    /// Configures EF Core DbContext directly with a connection string.
    /// Useful for testing scenarios where configuration is not available.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="provider">The database provider (default: "PostgreSQL").</param>
    /// <typeparam name="TContext">The derived DbContext type.</typeparam>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddEfCorePersistence<TContext>(
        this IServiceCollection services,
        string connectionString,
        string provider = "PostgreSQL")
        where TContext : EfCoreDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionString);

        PersistenceSetting setting = new()
        {
            Provider = provider
        };

        services.TryAddSingleton(setting);

        services.AddDbContext<TContext>(
            (sp, optionsBuilder) =>
            {
                optionsBuilder
                    .EnableSensitiveDataLogging(setting.EnableSensitiveDataLogging)
                    .EnableDetailedErrors(setting.EnableDetailedErrors)
                    .ConfigureWarnings(w => w.Default(WarningBehavior.Throw));

                switch (setting.Provider.ToLowerInvariant())
                {
                    case "postgresql":
                        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
                        {
                            npgsqlOptions
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
        services.AddScoped<EfCoreDbContext>(sp => sp.GetRequiredService<TContext>());

        services.TryAddSingleton<QueryMapRegistry>();
        services.TryAddSingleton<IQueryMapProvider>(sp => sp.GetRequiredService<QueryMapRegistry>());
        services.AddScoped(typeof(IQueryTranslator<>), typeof(QueryTranslationEfCore<>));

        // Register: Query capabilities — singleton per provider
        services.TryAddSingleton<IQueryCapabilities, EfCoreQueryCapabilities>();

        services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();

        return services;
    }

    
}
