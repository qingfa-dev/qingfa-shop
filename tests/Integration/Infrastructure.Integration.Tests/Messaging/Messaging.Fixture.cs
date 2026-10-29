using Application.Common.Messaging.Serialization;
using Application.Common.Persistence.UnitOfWorks;

using Core.Todos.Items;

using Infrastructure.Integrations.Messaging.Serialization;
using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.Options;
using Infrastructure.Persistence.EntityFramework.Outbox;
using Infrastructure.Persistence.EntityFramework.UnitOfWorks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Respawn;

using Testcontainers.PostgreSql;

namespace Infrastructure.Integration.Tests.Messaging;

/// <summary>
/// Boots a real PostgreSQL container with the messaging persistence stack so outbox and inbox
/// stores are exercised against the same provider and transaction semantics as production.
/// </summary>
public sealed class MessagingFixture : IAsyncLifetime
{
#pragma warning disable TC001 // PostgreSqlBuilder obsolete constructor
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("testdb_messaging")
        .WithUsername("test")
        .WithPassword("test")
        .Build();
#pragma warning restore TC001

    /// <summary>The root service provider with the messaging stack registered.</summary>
    public IServiceProvider Services { get; private set; } = null!;

    /// <summary>Current scope for test services.</summary>
    public IServiceScope? CurrentScope { get; private set; }

    /// <summary>The application context resolved from the current scope.</summary>
    public ApplicationDbContext DbContext => CurrentScope?.ServiceProvider
        .GetRequiredService<ApplicationDbContext>()
        ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The outbox store resolved from the current scope.</summary>
    public EfCoreOutboxStore OutboxStore => CurrentScope?.ServiceProvider
        .GetRequiredService<EfCoreOutboxStore>()
        ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The unit of work resolved from the current scope.</summary>
    public IUnitOfWork UnitOfWork => CurrentScope?.ServiceProvider
        .GetRequiredService<IUnitOfWork>()
        ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>The domain event serializer resolved from the current scope.</summary>
    public IDomainEventSerializer DomainSerializer => CurrentScope?.ServiceProvider
        .GetRequiredService<IDomainEventSerializer>()
        ?? throw new InvalidOperationException("Scope not initialized");

    /// <summary>Respawner for cleaning messaging tables between tests.</summary>
    public Respawner Respawner { get; private set; } = null!;

    /// <summary>The connection string for the PostgreSQL container.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(ConnectionString, npgsql =>
            {
                npgsql
                    .UsePersistenceMigrationHistory()
                    .CommandTimeout(30);
            }));

        services.AddScoped<IUnitOfWork>(sp =>
            new EfCoreUnitOfWork(sp.GetRequiredService<ApplicationDbContext>()));
        services.AddScoped<EfCoreOutboxStore>(sp =>
            new EfCoreOutboxStore(sp.GetRequiredService<ApplicationDbContext>()));

        // Serialize: the same serializer the dispatcher resolves, over the real event assemblies.
        var eventTypes = new EventTypeRegistry(
            typeof(TodoCreated).Assembly,
            typeof(Infrastructure.AssemblyMarker).Assembly);
        var serializer = new JsonMessageSerializer(eventTypes);
        services.AddSingleton<IEventTypeRegistry>(eventTypes);
        services.AddSingleton(serializer);
        services.AddSingleton<IDomainEventSerializer>(serializer);
        services.AddSingleton<IIntegrationEventSerializer>(serializer);

        Services = services.BuildServiceProvider();

        // Create: EnsureCreated is enough here; the migration is exercised separately.
        using (IServiceScope scope = Services.CreateScope())
        {
            ApplicationDbContext context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        using var connection = new Npgsql.NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        Respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            TablesToIgnore = [new Respawn.Graph.Table("__EFMigrationsHistory", "todo")],
            SchemasToInclude = ["messaging", "todo"],
        });
    }

    /// <summary>Creates a new scope for the next test.</summary>
    public void CreateScope()
    {
        CurrentScope?.Dispose();
        CurrentScope = Services.CreateScope();
    }

    /// <summary>Cleans every messaging and todo row and clears the current change tracker.</summary>
    public async Task CleanAsync()
    {
        try
        {
            if (CurrentScope is not null)
            {
                ApplicationDbContext context = CurrentScope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                context.ChangeTracker.Clear();
            }

            using var connection = new Npgsql.NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await Respawner.ResetAsync(connection);
        }
        catch
        {
            // Ignore: cleanup failures must not mask the actual test result.
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