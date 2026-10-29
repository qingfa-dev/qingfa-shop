using Application.Common.Persistence.Repositories;
using Application.Common.Querying.Mapping;

using Infrastructure.Persistence.EntityFramework;
using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.Repositories;

using Microsoft.AspNetCore.Builder;
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
    /// <param name="builder">The host builder.</param>
    /// <param name="configuration">Configuration holding the EfCore section and ConnectionStrings.</param>
    /// <typeparam name="TContext">The derived DbContext type.</typeparam>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddPersistence<TContext>(
        this WebApplicationBuilder builder,
        IConfiguration configuration)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        IServiceCollection services = builder.Services;

        // Enforce: Query maps must be registered before EF Core so the registry is available
        // when the DbContext is configured and repositories resolve their maps.
        services.RegisterQueryMaps();

        // Call: Configure EF Core persistence with the specified DbContext and settings.
        builder.AddEfCorePersistence<TContext>(configuration);

        // Create: Open-generic repository implementations for all entity types.
        services.RegisterRepositories();

        // Create: Database initialization pipeline (migrator + seeders + resetter).
        builder.AddPersistenceInitialization<TContext>();

        return builder;
    }

    /// <summary>
    /// Adds persistence services with an explicit connection string (useful for testing).
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="provider">The database provider (default: "PostgreSQL").</param>
    /// <typeparam name="TContext">The derived DbContext type.</typeparam>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddPersistence<TContext>(
        this WebApplicationBuilder builder,
        string connectionString,
        string provider = "PostgreSQL")
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionString);

        IServiceCollection services = builder.Services;

        // Enforce: Query maps must be registered before EF Core so the registry is available
        // when the DbContext is configured and repositories resolve their maps.
        services.RegisterQueryMaps();

        // Call: Configure EF Core persistence with explicit connection string and provider.
        builder.AddEfCorePersistence<TContext>(connectionString, provider);

        // Create: Open-generic repository implementations for all entity types.
        services.RegisterRepositories();

        // Create: Database initialization pipeline (migrator + seeders + resetter).
        builder.AddPersistenceInitialization<TContext>();

        return builder;
    }

    /// <summary>Adds the persistence pipeline hook for the host.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UsePersistence(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Persistence is request-scoped; there is no middleware to add here.
        return app;
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
