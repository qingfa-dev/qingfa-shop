using Application.Common.Caching;
using Application.Common.Persistence.Initialization;

using Infrastructure.Caching;
using Infrastructure.Persistence;
using Infrastructure.Persistence.EntityFramework.Contexts;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure;

/// <summary>
/// Composition root for the Infrastructure layer.
/// Registers all infrastructure services: persistence, caching, repositories, and query maps.
/// </summary>
public static class InfrastructureComposition
{
    /// <summary>
    /// Adds all infrastructure services to the service collection.
    /// This includes EF Core persistence with ApplicationDbContext, caching, repositories,
    /// unit of work, and query map registrations.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <param name="configuration">Configuration holding the EfCore and Caching sections.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Register: Persistence services (EF Core, repositories, query maps).
        services.AddPersistence<ApplicationDbContext>(configuration);

        // Register: Caching facade (HybridCache over L1/L2 tiers).
        services.AddCaching(configuration);

        return services;
    }

    /// <summary>
    /// Adds infrastructure services with an explicit connection string (useful for testing).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="provider">The database provider (default: "PostgreSQL").</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string provider = "PostgreSQL")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionString);

        // Register: Persistence services (EF Core, repositories, query maps).
        services.AddPersistence<ApplicationDbContext>(connectionString, provider);

        // Register: Null cache service for testing scenarios.
        services.TryAddSingleton<ICacheService, Caching.Services.NullCacheService>();

        return services;
    }

    #region Pipeline Configuration

    /// <summary>
    /// Initializes Infrastructure layer services at startup.
    /// Applies pending database migrations and, when requested, runs seeders.
    /// Call this after building the host to ensure the database is ready.
    /// </summary>
    /// <param name="provider">The application's service provider.</param>
    /// <param name="autoMigrate">Whether to apply pending migrations automatically.</param>
    /// <param name="seed">Whether to run the registered seeders after migrating.</param>
    /// <returns>The same provider for chaining.</returns>
    /// <exception cref="InvalidOperationException">Initialization returned a failure.</exception>
    public static IServiceProvider UseInfrastructure(
        this IServiceProvider provider,
        bool autoMigrate = true,
        bool seed = false)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (!autoMigrate && !seed)
        {
            return provider;
        }

        using var scope = provider.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IPersistenceInitializer>();
        var result = initializer
            .InitializeAsync(runMigrations: autoMigrate, runSeeders: seed)
            .GetAwaiter()
            .GetResult();

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Database initialization failed: {string.Join("; ", result.Errors.Select(error => error.Message))}");
        }

        return provider;
    }

    #endregion
}
