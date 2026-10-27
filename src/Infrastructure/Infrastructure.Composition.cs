using Application.Common.Caching;

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
    /// This includes EF Core persistence with AppDbContext, caching, repositories,
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
    /// Applies pending database migrations and validates connectivity.
    /// Call this after building the host to ensure the database is ready.
    /// </summary>
    /// <param name="provider">The application's service provider.</param>
    /// <param name="autoMigrate">Whether to apply pending migrations automatically.</param>
    /// <returns>The same provider for chaining.</returns>
    public static IServiceProvider UseInfrastructure(
        this IServiceProvider provider,
        bool autoMigrate = true)
    {
        ArgumentNullException.ThrowIfNull(provider);

        // Apply: Database migrations if enabled.
        if (autoMigrate)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Migrate: Apply any pending migrations.
            context.Database.Migrate();
        }

        return provider;
    }

    #endregion
}
