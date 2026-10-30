using Application.Common.EventSourcing;
using Application.Common.Messaging.Outbox;
using Application.Common.Messaging.Serialization;
using Application.Common.Persistence.Capabilities;
using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Common.Querying.Mapping;
using Application.Common.Security.Authentication.Contexts;
using Application.Features.Todos.Abstractions;
using Application.Features.Todos.Items.Models;

using Core.Common.Domain.EventSourcing;
using Core.Common.Monad.Results;
using Core.Todos.Categories;
using Core.Todos.Items;
using Core.Todos.Lists;

using Infrastructure.Integrations.Messaging.Serialization;
using Infrastructure.Persistence.EntityFramework.Capabilities;
using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.EventSourcing;
using Infrastructure.Persistence.EntityFramework.EventSourcing.Options;
using Infrastructure.Persistence.EntityFramework.Interceptors;
using Infrastructure.Persistence.EntityFramework.Options;
using Infrastructure.Persistence.EntityFramework.Outbox;
using Infrastructure.Persistence.EntityFramework.Projections;
using Infrastructure.Persistence.EntityFramework.QueryTranslation;
using Infrastructure.Persistence.EntityFramework.Repositories;
using Infrastructure.Persistence.EntityFramework.UnitOfWorks;
using Infrastructure.Persistence.Schemas.Todos.Queries;
using Infrastructure.Services.Todos;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Respawn;

using Testcontainers.PostgreSql;

namespace Infrastructure.Integration.Tests.Persistence.Todos;

/// <summary>
/// Boots a real PostgreSQL container with the event store, projections, and repositories
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

    /// <summary>The root service provider with the full event-sourced stack registered.</summary>
    public IServiceProvider Services { get; private set; } = null!;

    /// <summary>Current scope for test services.</summary>
    public IServiceScope? CurrentScope { get; private set; }

    /// <summary>The Todo DbContext resolved from the current scope.</summary>
    public ApplicationDbContext DbContext => CurrentScope?.ServiceProvider.GetRequiredService<ApplicationDbContext>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The event-sourced repository for the Todo stream.</summary>
    public IEventSourcedRepository<Todo, Guid> TodoEvents => CurrentScope?.ServiceProvider.GetRequiredService<IEventSourcedRepository<Todo, Guid>>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The persistent repository for the Todo read model.</summary>
    public IPersistentRepository<TodoReadModel, Guid> TodoReadRepository => CurrentScope?.ServiceProvider.GetRequiredService<IPersistentRepository<TodoReadModel, Guid>>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The mapping repository for the Todo read model.</summary>
    public IMappableRepository<TodoReadModel, Guid> TodoMapperRepository => CurrentScope?.ServiceProvider.GetRequiredService<IMappableRepository<TodoReadModel, Guid>>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The projection repository for the Todo read model.</summary>
    public IProjectionRepository<TodoReadModel, Guid> TodoProjectionRepository => CurrentScope?.ServiceProvider.GetRequiredService<IProjectionRepository<TodoReadModel, Guid>>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The repository for TodoList.</summary>
    public IPersistentRepository<TodoList, Guid> TodoListRepository => CurrentScope?.ServiceProvider.GetRequiredService<IPersistentRepository<TodoList, Guid>>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The mapping repository for TodoList.</summary>
    public IMappableRepository<TodoList, Guid> TodoListMapperRepository => CurrentScope?.ServiceProvider.GetRequiredService<IMappableRepository<TodoList, Guid>>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The projection repository for TodoList.</summary>
    public IProjectionRepository<TodoList, Guid> TodoListProjectionRepository => CurrentScope?.ServiceProvider.GetRequiredService<IProjectionRepository<TodoList, Guid>>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The repository for TodoCategory.</summary>
    public IPersistentRepository<TodoCategory, Guid> TodoCategoryRepository => CurrentScope?.ServiceProvider.GetRequiredService<IPersistentRepository<TodoCategory, Guid>>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The mapping repository for TodoCategory.</summary>
    public IMappableRepository<TodoCategory, Guid> TodoCategoryMapperRepository => CurrentScope?.ServiceProvider.GetRequiredService<IMappableRepository<TodoCategory, Guid>>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The projection repository for TodoCategory.</summary>
    public IProjectionRepository<TodoCategory, Guid> TodoCategoryProjectionRepository => CurrentScope?.ServiceProvider.GetRequiredService<IProjectionRepository<TodoCategory, Guid>>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The event store.</summary>
    public IEventStore EventStore => CurrentScope?.ServiceProvider.GetRequiredService<IEventStore>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The snapshot store.</summary>
    public ISnapshotStore Snapshots => CurrentScope?.ServiceProvider.GetRequiredService<ISnapshotStore>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The projection runner.</summary>
    public ProjectionRunner Runner => CurrentScope?.ServiceProvider.GetRequiredService<ProjectionRunner>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The UnitOfWork.</summary>
    public IUnitOfWork UnitOfWork => CurrentScope?.ServiceProvider.GetRequiredService<IUnitOfWork>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The cross-aggregate reference cleaner.</summary>
    public ITodoReferenceCleaner ReferenceCleaner => CurrentScope?.ServiceProvider.GetRequiredService<ITodoReferenceCleaner>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The query map provider used by search handlers.</summary>
    public IQueryMapProvider QueryMapProvider => CurrentScope?.ServiceProvider.GetRequiredService<IQueryMapProvider>() ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The ambient tenant and user the handlers read.</summary>
    public IntegrationExecutionContext Ambient { get; } = new();

    /// <summary>The frozen clock the handlers read.</summary>
    public IntegrationClock Clock { get; } = new(DateTimeOffset.UtcNow.Date);

    /// <summary>The query map for the Todo read model.</summary>
    public QueryMap<TodoReadModel> TodoQueryMap { get; private set; } = null!;

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

        // Initialize: the same serializer and event types the production stores resolve.
        var eventTypes = new EventTypeRegistry(
            typeof(TodoCreated).Assembly,
            typeof(Infrastructure.AssemblyMarker).Assembly);
        var serializer = new JsonMessageSerializer(eventTypes);

        // Register ApplicationDbContext
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString, npgsql =>
            {
                npgsql
                    .UsePersistenceMigrationHistory()
                    .CommandTimeout(30);
            });

            // Mirror: register only the version-token interceptor; lifecycle and domain-event
            // behavior stays out of this shared fixture (see the delete-semantics tests).
            options.AddInterceptors(new VersionableInterceptor());
        });
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Register QueryMaps
        TodoQueryMap = TodoReadModelQueryMap.Create();
        TodoListQueryMap = Infrastructure.Persistence.Schemas.Todos.Queries.TodoListQueryMap.Create();
        TodoCategoryQueryMap = Infrastructure.Persistence.Schemas.Todos.Queries.TodoCategoryQueryMap.Create();

        var registry = new QueryMapRegistry();
        registry.Register(TodoQueryMap);
        registry.Register(TodoListQueryMap);
        registry.Register(TodoCategoryQueryMap);
        services.AddSingleton(registry);
        services.AddSingleton<IQueryMapProvider>(registry);

        // Register QueryTranslators
        services.AddSingleton<IQueryTranslator<TodoReadModel>, QueryTranslationEfCore<TodoReadModel>>();
        services.AddSingleton<IQueryTranslator<TodoList>, QueryTranslationEfCore<TodoList>>();
        services.AddSingleton<IQueryTranslator<TodoCategory>, QueryTranslationEfCore<TodoCategory>>();

        // Register Query Capabilities
        services.AddSingleton<IQueryCapabilities, EfCoreQueryCapabilities>();

        // Register Repositories
        services.AddScoped<IPersistentRepository<TodoReadModel, Guid>>(sp =>
            new EfCoreRepository<TodoReadModel, Guid>(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<IQueryTranslator<TodoReadModel>>(),
                sp.GetRequiredService<IQueryMapProvider>(),
                sp.GetRequiredService<IQueryCapabilities>()));

        services.AddScoped<IPersistentRepository<TodoList, Guid>>(sp =>
            new EfCoreRepository<TodoList, Guid>(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<IQueryTranslator<TodoList>>(),
                sp.GetRequiredService<IQueryMapProvider>(),
                sp.GetRequiredService<IQueryCapabilities>()));

        services.AddScoped<IPersistentRepository<TodoCategory, Guid>>(sp =>
            new EfCoreRepository<TodoCategory, Guid>(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<IQueryTranslator<TodoCategory>>(),
                sp.GetRequiredService<IQueryMapProvider>(),
                sp.GetRequiredService<IQueryCapabilities>()));

        // Register: the same EF Core repository satisfies the mapping and projection contracts.
        services.AddScoped<IMappableRepository<TodoReadModel, Guid>>(sp =>
            (IMappableRepository<TodoReadModel, Guid>)sp.GetRequiredService<IPersistentRepository<TodoReadModel, Guid>>());
        services.AddScoped<IProjectionRepository<TodoReadModel, Guid>>(sp =>
            (IProjectionRepository<TodoReadModel, Guid>)sp.GetRequiredService<IPersistentRepository<TodoReadModel, Guid>>());

        services.AddScoped<IMappableRepository<TodoList, Guid>>(sp =>
            (IMappableRepository<TodoList, Guid>)sp.GetRequiredService<IPersistentRepository<TodoList, Guid>>());
        services.AddScoped<IProjectionRepository<TodoList, Guid>>(sp =>
            (IProjectionRepository<TodoList, Guid>)sp.GetRequiredService<IPersistentRepository<TodoList, Guid>>());

        services.AddScoped<IMappableRepository<TodoCategory, Guid>>(sp =>
            (IMappableRepository<TodoCategory, Guid>)sp.GetRequiredService<IPersistentRepository<TodoCategory, Guid>>());
        services.AddScoped<IProjectionRepository<TodoCategory, Guid>>(sp =>
            (IProjectionRepository<TodoCategory, Guid>)sp.GetRequiredService<IPersistentRepository<TodoCategory, Guid>>());

        // Register UnitOfWork
        services.AddScoped<IUnitOfWork>(sp =>
            new EfCoreUnitOfWork(sp.GetRequiredService<ApplicationDbContext>()));

        // Register the event-sourcing collaborators the todo stream and projection resolve.
        services.AddScoped<IOutboxStore>(sp =>
            new EfCoreOutboxStore(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<TimeProvider>()));
        services.AddScoped<IEventStore>(sp =>
            new PostgresEventStore(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<IDomainEventSerializer>(),
                sp.GetRequiredService<TimeProvider>()));
        services.AddScoped<ISnapshotStore>(sp =>
            new EfCoreSnapshotStore(sp.GetRequiredService<ApplicationDbContext>()));
        services.AddScoped<IProjectionCheckpointStore>(sp =>
            new EfCoreProjectionCheckpointStore(sp.GetRequiredService<ApplicationDbContext>()));
        services.AddSingleton(new EventStoreSetting());
        services.AddSingleton<ISnapshotPolicy>(new IntervalSnapshotPolicy(10));
        services.AddScoped<IAggregateFactory<Todo>, TodoFactory>();
        services.AddScoped<IEventSourcedRepository<Todo, Guid>>(sp =>
            new EventSourcedRepository<Todo, Guid>(
                sp.GetRequiredService<IEventStore>(),
                sp.GetRequiredService<ISnapshotStore>(),
                sp.GetRequiredService<ISnapshotPolicy>(),
                sp.GetRequiredService<IAggregateFactory<Todo>>(),
                sp.GetRequiredService<IDomainEventSerializer>(),
                sp.GetRequiredService<IOutboxStore>(),
                sp.GetRequiredService<IUnitOfWork>(),
                sp.GetRequiredService<TimeProvider>(),
                NullLogger<EventSourcedRepository<Todo, Guid>>.Instance));
        services.AddScoped<IProjection, TodoProjection>();
        services.AddScoped<ProjectionRunner>();

        services.AddSingleton<IEventTypeRegistry>(eventTypes);
        services.AddSingleton(serializer);
        services.AddSingleton<IDomainEventSerializer>(serializer);
        services.AddSingleton<IIntegrationEventSerializer>(serializer);

        // Register the cross-aggregate reference cleaner and its ambient context.
        services.AddScoped<ITodoReferenceCleaner, TodoReferenceCleaner>();
        services.AddSingleton<TimeProvider>(Clock);

        Services = services.BuildServiceProvider();

        // Create the schema using a temporary scope
        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        // Create respawner for cleaning tables
        using var connection = new Npgsql.NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        Respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            TablesToIgnore = [new Respawn.Graph.Table("__EFMigrationsHistory", "todo")],
            SchemasToInclude = ["todo", "messaging"]
        });
    }

    /// <summary>Creates a new scope for the next test.</summary>
    public void CreateScope()
    {
        CurrentScope?.Dispose();
        CurrentScope = Services.CreateScope();
    }

    /// <summary>Runs one projection pass so handler writes are visible to read-side queries.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The projection outcome.</returns>
    public Task<Result> ProjectAsync(CancellationToken cancellationToken = default)
        => Runner.RunOnceAsync(cancellationToken);

    /// <summary>Cleans all test data between tests using Respawn checkpoint.</summary>
    public async Task CleanAsync()
    {
        try
        {
            if (CurrentScope != null)
            {
                var context = CurrentScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
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

/// <summary>A tenant and user context that integration tests can change directly.</summary>
public sealed class IntegrationExecutionContext : ITenantContext, IUserContext
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <inheritdoc />
    public string? UserId { get; set; }

    /// <inheritdoc />
    public string? UserName { get; set; }

    /// <inheritdoc />
    public string? Email { get; set; }

    /// <inheritdoc />
    public bool IsAuthenticated { get; set; } = true;

    /// <inheritdoc />
    public string? IpAddress { get; set; }

    /// <inheritdoc />
    public string? Device { get; set; }
}

/// <summary>A clock frozen at a controllable instant for integration tests.</summary>
/// <param name="utcNow">The initial instant.</param>
public sealed class IntegrationClock(DateTimeOffset utcNow) : TimeProvider
{
    /// <summary>Gets or sets the instant returned by the clock.</summary>
    public DateTimeOffset UtcNow { get; set; } = utcNow;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => UtcNow;
}
