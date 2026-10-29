using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Seeders;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.Initialization;

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    /// <param name="builder">The host builder.</param>
    /// <typeparam name="TContext">The derived DbContext type.</typeparam>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddPersistenceInitialization<TContext>(
        this WebApplicationBuilder builder)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        IServiceCollection services = builder.Services;

        // Guard: repeated composition calls must not register duplicate seeders.
        if (services.Any(descriptor => descriptor.ServiceType == typeof(IPersistenceInitializer)))
        {
            return builder;
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

        return builder;
    }

    /// <summary>Adds the initialization pipeline hook for the host.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UsePersistenceInitialization(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Initialization runs through UseInfrastructure; this hook keeps composition symmetric.
        return app;
    }
}
