using Application.Common.Persistence.Repositories;
using Application.Common.Querying.Mapping;
using Application.Features.Todos.Abstractions;

using Infrastructure.Persistence.EntityFramework;
using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.Persistence;

/// <summary>
/// Composition root for the Persistence layer.
/// Registers all persistence services: EF Core context, repositories, query maps, and unit of work.
/// </summary>
public static class PersistenceComposition
{
    /// <summary>
    /// Adds all persistence services to the service collection.
    /// This includes EF Core persistence with the specified DbContext, generic repositories,
    /// and query map registrations.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <param name="configuration">Configuration holding the EfCore section and ConnectionStrings.</param>
    /// <typeparam name="TContext">The derived DbContext type.</typeparam>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddPersistence<TContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Enforce: Query maps must be registered before EF Core so the registry is available
        // when the DbContext is configured and repositories resolve their maps.
        services.RegisterQueryMaps();

        // Call: Configure EF Core persistence with the specified DbContext and settings.
        services.AddEfCorePersistence<TContext>(configuration);

        // Create: Open-generic repository implementations for all entity types.
        services.RegisterRepositories();

        // Create: Database initialization pipeline (migrator + seeders + resetter).
        services.AddPersistenceInitialization<TContext>();

        return services;
    }

    /// <summary>
    /// Adds persistence services with an explicit connection string (useful for testing).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="provider">The database provider (default: "PostgreSQL").</param>
    /// <typeparam name="TContext">The derived DbContext type.</typeparam>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddPersistence<TContext>(
        this IServiceCollection services,
        string connectionString,
        string provider = "PostgreSQL")
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionString);

        // Enforce: Query maps must be registered before EF Core so the registry is available
        // when the DbContext is configured and repositories resolve their maps.
        services.RegisterQueryMaps();

        // Call: Configure EF Core persistence with explicit connection string and provider.
        services.AddEfCorePersistence<TContext>(connectionString, provider);

        // Create: Open-generic repository implementations for all entity types.
        services.RegisterRepositories();

        // Create: Database initialization pipeline (migrator + seeders + resetter).
        services.AddPersistenceInitialization<TContext>();

        return services;
    }

    /// <summary>
    /// Registers generic EF Core repositories for all known entity types.
    /// </summary>
    private static void RegisterRepositories(this IServiceCollection services)
    {
        // Create: Open-generic repository so any IEntity is resolvable.
        services.AddScoped(typeof(IPersistentRepository<,>), typeof(EfCoreRepository<,>));
        services.AddScoped(typeof(IProjectionRepository<,>), typeof(EfCoreRepository<,>));
        services.AddScoped(typeof(IStreamableRepository<,>), typeof(EfCoreRepository<,>));
        services.AddScoped(typeof(IMappableRepository<,>), typeof(EfCoreRepository<,>));

    }

    /// <summary>
    /// Discovers every <see cref="IQueryMapDefinition"/> in the persistence
    /// assembly and builds the shared <see cref="QueryMapRegistry"/> from them.
    /// </summary>
    private static void RegisterQueryMaps(this IServiceCollection services)
    {
        // Discover: query map definitions live next to their maps, so a new map
        // only needs a definition class to join the registry.
        services.Scan(scan => scan
            .FromAssemblyOf<EfCoreDbContext>()
            .AddClasses(classes => classes
                .AssignableTo<IQueryMapDefinition>()
                .Where(type => type is { IsAbstract: false, IsInterface: false }))
            .As<IQueryMapDefinition>()
            .WithSingletonLifetime());

        // Register: the registry is composed from the discovered definitions and
        // shared as a singleton with the provider interface.
        services.TryAddSingleton(sp => new QueryMapRegistry(
            sp.GetServices<IQueryMapDefinition>()));
        services.TryAddSingleton<IQueryMapProvider>(sp =>
            sp.GetRequiredService<QueryMapRegistry>());
    }
}
