using Application.BuildingBlocks.Persistence;
using Application.BuildingBlocks.Persistence.Capabilities;
using Application.BuildingBlocks.Querying.Mapping;

using Core.BuildingBlocks.Functional.Results;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Capabilities;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Options;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Repositories;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.UnitOfWorks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Respawn;

using Testcontainers.PostgreSql;

namespace Infrastructure.Integration.Tests.BuildingBlocks.Persistence;

/// <summary>
/// Boots a real PostgreSQL container with EF Core, the repository, and UnitOfWork
/// so integration tests exercise the same pipeline as production.
/// </summary>
public sealed class EfCoreFixture : IAsyncLifetime
{
#pragma warning disable TC001 // PostgreSqlBuilder obsolete constructor
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("testdb")
        .WithUsername("test")
        .WithPassword("test")
        .Build();
#pragma warning restore TC001

    /// <summary>The root service provider with the full EF Core stack registered.</summary>
    public IServiceProvider Services { get; private set; } = null!;

    /// <summary>Current scope for test services.</summary>
    public IServiceScope? CurrentScope { get; private set; }

    /// <summary>The test DbContext resolved from the current scope.</summary>
    public TestDbContext DbContext => CurrentScope?.ServiceProvider.GetRequiredService<TestDbContext>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The repository for TestProduct.</summary>
    public EfCoreRepository<TestProduct, Guid> Repository => CurrentScope?.ServiceProvider.GetRequiredService<EfCoreRepository<TestProduct, Guid>>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The UnitOfWork.</summary>
    public EfCoreUnitOfWork UnitOfWork => CurrentScope?.ServiceProvider.GetRequiredService<EfCoreUnitOfWork>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The query map for TestProduct.</summary>
    public QueryMap<TestProduct> QueryMap { get; private set; } = null!;

    /// <summary>Respawner for cleaning tables between tests.</summary>
    public Respawner Respawner { get; private set; } = null!;

    /// <summary>The connection string for the PostgreSQL container.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        // Start the PostgreSQL container
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        // Build service collection
        var services = new ServiceCollection();
        services.AddLogging();

        // Register EF Core
        services.AddDbContext<TestDbContext>(options =>
            options.UseNpgsql(ConnectionString, npgsql =>
            {
                npgsql
                    .UsePersistenceMigrationHistory()
                    .CommandTimeout(30);
            }));

        // Register QueryMap
        Result<QueryMap<TestProduct>> mapResult = QueryMapBuilder<TestProduct>.Create()
            .Map("Id", p => p.Id, b => b.Sortable(false).Selectable(false).Searchable(false))
            .Map("Name", p => p.Name, b => b.Searchable().Sortable())
            .Map("Description", p => p.Description, b => b.Searchable())
            .Map("Price", p => p.Price, b => b.Sortable())
            .Map("Stock", p => p.Stock, b => b.Sortable())
            .Map("CreatedAt", p => p.CreatedAt, b => b.Sortable())
            .Map("IsActive", p => p.IsActive)
            .Build();

        if (mapResult.IsFailure)
            throw new InvalidOperationException("Failed to build QueryMap", null);

        QueryMap = mapResult.Value;

        var registry = new QueryMapRegistry();
        registry.Register(QueryMap);
        services.AddSingleton(registry);
        services.AddSingleton<IQueryMapProvider>(registry);

        // Register QueryTranslator
        services.AddSingleton<IQueryTranslator<TestProduct>, QueryTranslationEfCore<TestProduct>>();

        // Register Query Capabilities
        services.AddSingleton<IQueryCapabilities, EfCoreQueryCapabilities>();

        // Register Repository
        services.AddScoped<EfCoreRepository<TestProduct, Guid>>(sp =>
            new EfCoreRepository<TestProduct, Guid>(
                sp.GetRequiredService<TestDbContext>(),
                sp.GetRequiredService<IQueryTranslator<TestProduct>>(),
                sp.GetRequiredService<IQueryMapProvider>(),
                sp.GetRequiredService<IQueryCapabilities>()));

        // Register UnitOfWork
        services.AddScoped<EfCoreUnitOfWork>(sp =>
            new EfCoreUnitOfWork(sp.GetRequiredService<TestDbContext>()));

        Services = services.BuildServiceProvider();

        // Create the schema using a temporary scope
        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        // Create respawner for cleaning tables (after schema exists)
        using var connection = new Npgsql.NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        Respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            TablesToIgnore = [new Respawn.Graph.Table("__EFMigrationsHistory", "public")],
            SchemasToInclude = [PersistenceConstant.Schema.Default]
        });
    }

    /// <summary>Creates a new scope for the next test.</summary>
    public void CreateScope()
    {
        CurrentScope?.Dispose();
        CurrentScope = Services.CreateScope();
    }

    /// <summary>
    /// Cleans all test data between tests using Respawn checkpoint.
    /// Also clears the DbContext change tracker to avoid stale tracked entities.
    /// </summary>
    public async Task CleanAsync()
    {
        try
        {
            if (CurrentScope != null)
            {
                var context = CurrentScope.ServiceProvider.GetRequiredService<TestDbContext>();
                context.ChangeTracker.Clear();
            }

            using var connection = new Npgsql.NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await Respawner.ResetAsync(connection);
        }
        catch
        {
            // Ignore cleanup failures to avoid masking the actual test results
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Services is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }

        await _container.StopAsync();
    }
}
