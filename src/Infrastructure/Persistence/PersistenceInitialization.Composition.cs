using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Seeders;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.Initialization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

/// <summary>
/// Composition root for database initialization: migrator, seeders, and resetter.
/// </summary>
public static class PersistenceInitializationComposition
{
    /// <summary>
    /// Registers the EF Core persistence initializer and every module seeder
    /// discovered in the Infrastructure assembly.
    /// Call this after the DbContext and <c>IUnitOfWork</c> are registered.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <typeparam name="TContext">The derived DbContext type.</typeparam>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddPersistenceInitialization<TContext>(
        this IServiceCollection services)
        where TContext : EfCoreDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        // Guard: repeated composition calls must not register duplicate seeders.
        if (services.Any(descriptor => descriptor.ServiceType == typeof(IPersistenceInitializer)))
        {
            return services;
        }

        // Register: non-generic ILogger used by the initializer/resetter/migrator.
        // MS DI only registers ILogger<T>; this keeps the public constructors simple.
        services.TryAddSingleton<ILogger>(sp =>
            sp.GetRequiredService<ILoggerFactory>().CreateLogger("Infrastructure.Persistence.Initialization"));

        // Register: the orchestrator as scoped, matching the DbContext lifetime.
        services.TryAddScoped<IPersistenceInitializer, EfCorePersistenceInitializer<TContext>>();

        // Discover: every module seeder in the Infrastructure assembly. Scrutor
        // registers each implementation under every interface it implements, so
        // both IInitializationStep and IPersistenceSeeder resolve automatically.
        services.Scan(scan => scan
            .FromAssemblyOf<EfCoreDbContext>()
            .AddClasses(classes => classes
                .AssignableTo<IPersistenceSeeder>()
                .Where(type => type is { IsAbstract: false, IsInterface: false }))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}
