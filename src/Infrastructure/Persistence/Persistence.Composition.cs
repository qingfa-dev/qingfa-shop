using Application.Common.Persistence;
using Application.Common.Persistence.Repositories;

using Infrastructure.Persistence.EntityFramework;
using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.Repositories;
using Infrastructure.Persistence.Todos;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        where TContext : EfCoreDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Register: Query maps first so AddEfCorePersistence picks up the pre-configured registry.
        services.RegisterQueryMaps();

        // Register: EF Core persistence with the specified DbContext.
        services.AddEfCorePersistence<TContext>(configuration);

        // Register: Generic repositories for all entity types.
        services.RegisterRepositories();

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
        where TContext : EfCoreDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionString);

        // Register: Query maps first so AddEfCorePersistence picks up the pre-configured registry.
        services.RegisterQueryMaps();

        // Register: EF Core persistence with the specified DbContext.
        services.AddEfCorePersistence<TContext>(connectionString, provider);

        // Register: Generic repositories for all entity types.
        services.RegisterRepositories();

        return services;
    }

    /// <summary>
    /// Registers generic EF Core repositories for all known entity types.
    /// </summary>
    private static void RegisterRepositories(this IServiceCollection services)
    {
        // Register: Open-generic repository so any IEntity is resolvable.
        services.AddScoped(typeof(IPersistentRepository<,>), typeof(EfCoreRepository<,>));
        services.AddScoped(typeof(IProjectionRepository<,>), typeof(EfCoreRepository<,>));
        services.AddScoped(typeof(IStreamableRepository<,>), typeof(EfCoreRepository<,>));
        services.AddScoped(typeof(IMappableRepository<,>), typeof(EfCoreRepository<,>));
    }

    /// <summary>
    /// Registers query maps for all queryable entities in the QueryMapRegistry.
    /// The registry is resolved from DI after it is registered by AddEfCorePersistence.
    /// </summary>
    private static void RegisterQueryMaps(this IServiceCollection services)
    {
        // The QueryMapRegistry is registered as a singleton by AddEfCorePersistence.
        // We configure it via a hosted service that runs at startup, or we can
        // register it directly here before AddEfCorePersistence overwrites it.
        // Since AddEfCorePersistence uses TryAddSingleton, we register first.
        var registry = new Application.Common.Querying.Mapping.QueryMapRegistry();

        // Register: Todo query map.
        registry.Register(TodoQueryMap.Create());

        // Register: TodoList query map.
        registry.Register(TodoListQueryMap.Create());

        // Register: TodoCategory query map.
        registry.Register(TodoCategoryQueryMap.Create());

        // Replace the default singleton with our pre-configured registry.
        services.AddSingleton(registry);
    }
}
