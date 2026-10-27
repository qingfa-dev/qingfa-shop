using Application.Common.Persistence.Capabilities;
using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Common.Querying.Mapping;

using Core.Todos;

using Infrastructure.Persistence.EntityFramework.Capabilities;
using Infrastructure.Persistence.EntityFramework.QueryTranslation;
using Infrastructure.Persistence.EntityFramework.Repositories;
using Infrastructure.Persistence.EntityFramework.UnitOfWorks;
using Infrastructure.Persistence.Todos;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Respawn;

using Testcontainers.PostgreSql;

namespace Infrastructure.Integration.Tests.Persistence.Todos;

/// <summary>
/// Boots a real PostgreSQL container with TodoDbContext, repositories, and UnitOfWork
/// so integration tests exercise the full Todos persistence pipeline.
/// </summary>
public sealed class TodoEfCoreFixture : IAsyncLifetime
{
#pragma warning disable TC001 // PostgreSqlBuilder obsolete constructor
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("testdb_todos")
        .WithUsername("test")
        .WithPassword("test")
        .Build();
#pragma warning restore TC001

    /// <summary>The root service provider with the full EF Core stack registered.</summary>
    public IServiceProvider Services { get; private set; } = null!;

    /// <summary>Current scope for test services.</summary>
    public IServiceScope? CurrentScope { get; private set; }

    /// <summary>The Todo DbContext resolved from the current scope.</summary>
    public TodoDbContext DbContext => CurrentScope?.ServiceProvider.GetRequiredService<TodoDbContext>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The repository for Todo.</summary>
    public IPersistentRepository<Todo, Guid> TodoRepository => CurrentScope?.ServiceProvider.GetRequiredService<IPersistentRepository<Todo, Guid>>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The repository for TodoList.</summary>
    public IPersistentRepository<TodoList, Guid> TodoListRepository => CurrentScope?.ServiceProvider.GetRequiredService<IPersistentRepository<TodoList, Guid>>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The repository for TodoCategory.</summary>
    public IPersistentRepository<TodoCategory, Guid> TodoCategoryRepository => CurrentScope?.ServiceProvider.GetRequiredService<IPersistentRepository<TodoCategory, Guid>>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The UnitOfWork.</summary>
    public IUnitOfWork UnitOfWork => CurrentScope?.ServiceProvider.GetRequiredService<IUnitOfWork>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The query map for Todo.</summary>
    public QueryMap<Todo> TodoQueryMap { get; private set; } = null!;

    /// <summary>The query map for TodoList.</summary>
    public QueryMap<TodoList> TodoListQueryMap { get; private set; } = null!;

    /// <summary>The query map for TodoCategory.</summary>
    public QueryMap<TodoCategory> TodoCategoryQueryMap { get; private set; } = null!;

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

        // Register TodoDbContext
        services.AddDbContext<TodoDbContext>(options =>
            options.UseNpgsql(ConnectionString, npgsql =>
            {
                npgsql.CommandTimeout(30);
            }));

        // Register QueryMaps
        TodoQueryMap = Infrastructure.Persistence.Todos.TodoQueryMap.Create();
        TodoListQueryMap = Infrastructure.Persistence.Todos.TodoListQueryMap.Create();
        TodoCategoryQueryMap = Infrastructure.Persistence.Todos.TodoCategoryQueryMap.Create();

        var registry = new QueryMapRegistry();
        registry.Register(TodoQueryMap);
        registry.Register(TodoListQueryMap);
        registry.Register(TodoCategoryQueryMap);
        services.AddSingleton(registry);
        services.AddSingleton<IQueryMapProvider>(registry);

        // Register QueryTranslators
        services.AddSingleton<IQueryTranslator<Todo>, QueryTranslationEfCore<Todo>>();
        services.AddSingleton<IQueryTranslator<TodoList>, QueryTranslationEfCore<TodoList>>();
        services.AddSingleton<IQueryTranslator<TodoCategory>, QueryTranslationEfCore<TodoCategory>>();

        // Register Query Capabilities
        services.AddSingleton<IQueryCapabilities, EfCoreQueryCapabilities>();

        // Register Repositories
        services.AddScoped<IPersistentRepository<Todo, Guid>>(sp =>
            new EfCoreRepository<Todo, Guid>(
                sp.GetRequiredService<TodoDbContext>(),
                sp.GetRequiredService<IQueryTranslator<Todo>>(),
                sp.GetRequiredService<IQueryMapProvider>(),
                sp.GetRequiredService<IQueryCapabilities>()));

        services.AddScoped<IPersistentRepository<TodoList, Guid>>(sp =>
            new EfCoreRepository<TodoList, Guid>(
                sp.GetRequiredService<TodoDbContext>(),
                sp.GetRequiredService<IQueryTranslator<TodoList>>(),
                sp.GetRequiredService<IQueryMapProvider>(),
                sp.GetRequiredService<IQueryCapabilities>()));

        services.AddScoped<IPersistentRepository<TodoCategory, Guid>>(sp =>
            new EfCoreRepository<TodoCategory, Guid>(
                sp.GetRequiredService<TodoDbContext>(),
                sp.GetRequiredService<IQueryTranslator<TodoCategory>>(),
                sp.GetRequiredService<IQueryMapProvider>(),
                sp.GetRequiredService<IQueryCapabilities>()));

        // Register UnitOfWork
        services.AddScoped<IUnitOfWork>(sp =>
            new EfCoreUnitOfWork(sp.GetRequiredService<TodoDbContext>()));

        Services = services.BuildServiceProvider();

        // Create the schema using a temporary scope
        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        // Create respawner for cleaning tables
        using var connection = new Npgsql.NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        Respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            TablesToIgnore = [new Respawn.Graph.Table("ef_migrations_history", "public"), new Respawn.Graph.Table("__EFMigrationsHistory", "public")],
            SchemasToInclude = new[] { "public" }
        });
    }

    /// <summary>Creates a new scope for the next test.</summary>
    public void CreateScope()
    {
        CurrentScope?.Dispose();
        CurrentScope = Services.CreateScope();
    }

    /// <summary>Cleans all test data between tests using Respawn checkpoint.</summary>
    public async Task CleanAsync()
    {
        try
        {
            if (CurrentScope != null)
            {
                var context = CurrentScope.ServiceProvider.GetRequiredService<TodoDbContext>();
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
