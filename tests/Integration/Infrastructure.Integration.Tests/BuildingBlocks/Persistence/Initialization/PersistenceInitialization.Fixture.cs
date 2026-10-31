using Application.BuildingBlocks.EventSourcing;
using Application.BuildingBlocks.Messaging.Serialization;
using Application.BuildingBlocks.Persistence;
using Application.BuildingBlocks.Persistence.Initialization;
using Application.BuildingBlocks.Persistence.UnitOfWorks;
using Application.BuildingBlocks.Security.Authorization.Registry;
using Application.Modules.Persistence;
using Application.Modules.PermissionCatalog;
using Application.Modules.Todos.Persistence;
using Application.Modules.Todos.Permissions;

using Core.Modules.Todos.Items;

using Infrastructure.BuildingBlocks.Adapters.Messaging.Serialization;
using Infrastructure.BuildingBlocks.Adapters.Persistence;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing.Options;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Options;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.UnitOfWorks;

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using Testcontainers.PostgreSql;

namespace Infrastructure.Integration.Tests.BuildingBlocks.Persistence.Initialization;

/// <summary>
/// Boots a real PostgreSQL container with the Todo migrations, seeders, resetter,
/// and persistence initializer so the final database-initialization step is
/// exercised against the same schema production applies.
/// </summary>
public sealed class PersistenceInitializationFixture : IAsyncLifetime
{
#pragma warning disable TC001 // PostgreSqlBuilder obsolete constructor
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("testdb_initialization")
        .WithUsername("test")
        .WithPassword("test")
        .Build();
#pragma warning restore TC001

    /// <summary>The root service provider with the persistence initialization stack.</summary>
    public IServiceProvider Services { get; private set; } = null!;

    /// <summary>The connection string for the PostgreSQL container.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        services.AddLogging();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(ConnectionString, npgsql => npgsql
                .UsePersistenceMigrationHistory()
                .CommandTimeout(30)));

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IUnitOfWork>(sp => new EfCoreUnitOfWork(sp.GetRequiredService<ApplicationDbContext>()));

        // Register: the platform and Todos catalog contributions production composes,
        // so the seeder and resetter consume the same permission and schema sets.
        services.AddSingleton<PlatformPermissionCatalog>();
        services.AddSingleton<IPermissionCatalogContributor>(
            provider => provider.GetRequiredService<PlatformPermissionCatalog>());
        services.AddSingleton<TodoPermissionCatalog>();
        services.AddSingleton<IPermissionCatalogContributor>(
            provider => provider.GetRequiredService<TodoPermissionCatalog>());
        services.AddSingleton<IPermissionCatalog, ApplicationPermissionCatalog>();

        services.AddSingleton<PlatformPersistenceSchemaCatalog>();
        services.AddSingleton<IPersistenceSchemaCatalogContributor>(
            provider => provider.GetRequiredService<PlatformPersistenceSchemaCatalog>());
        services.AddSingleton<IPersistenceSchemaCatalog>(
            provider => provider.GetRequiredService<PlatformPersistenceSchemaCatalog>());
        services.AddSingleton<TodoPersistenceSchemaCatalog>();
        services.AddSingleton<IPersistenceSchemaCatalogContributor>(
            provider => provider.GetRequiredService<TodoPersistenceSchemaCatalog>());
        services.AddSingleton<IPersistenceSchemaCatalog>(
            provider => provider.GetRequiredService<TodoPersistenceSchemaCatalog>());
        services.AddSingleton<IPersistenceSchemaCatalog, ApplicationPersistenceSchemaCatalog>();

        // Register: the persistence initializer and module seeders through the same
        // composition root production uses, so wiring defects surface here too.
        builder.AddPersistenceInitialization<ApplicationDbContext>();
        RegisterEventStoreCollaborators(services);

        Services = services.BuildServiceProvider();
    }

    /// <summary>Registers the collaborators the todo backfill seeder needs to be constructed.</summary>
    private static void RegisterEventStoreCollaborators(IServiceCollection services)
    {
        // Initialize: the same serializer and stores production composes; the backfill
        // setting stays disabled by default, so initialization remains a no-op.
        var eventTypes = new EventTypeRegistry(
            typeof(TodoCreated).Assembly,
            typeof(Infrastructure.AssemblyMarker).Assembly);
        var serializer = new JsonMessageSerializer(eventTypes);

        services.AddSingleton(serializer);
        services.AddSingleton<IDomainEventSerializer>(serializer);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(new EventStoreSetting());
        services.AddScoped<IEventStore>(sp => new PostgresEventStore(
            sp.GetRequiredService<ApplicationDbContext>(),
            serializer,
            TimeProvider.System));
        services.AddScoped<ISnapshotStore>(sp =>
            new EfCoreSnapshotStore(sp.GetRequiredService<ApplicationDbContext>()));
        services.AddScoped<IProjectionCheckpointStore>(sp =>
            new EfCoreProjectionCheckpointStore(sp.GetRequiredService<ApplicationDbContext>()));
    }

    /// <summary>Creates a scope for one database operation.</summary>
    public IServiceScope CreateScope() => Services.CreateScope();

    /// <summary>Gets the initializer from a new scope.</summary>
    public IPersistenceInitializer CreateInitializer(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IPersistenceInitializer>();

    /// <summary>Drops the database so the next operation starts from nothing.</summary>
    public async Task DropDatabaseAsync()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureDeletedAsync();
    }

    /// <summary>Applies all EF Core migrations to the container database.</summary>
    public async Task MigrateAsync()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }

    /// <summary>Runs a callback inside a fresh scope and disposes the scope afterwards.</summary>
    public async Task<T> WithDbContextAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(context);
    }

    /// <summary>Counts rows in a table, returning zero when the table does not exist.</summary>
    public async Task<int> CountRowsAsync(string table)
    {
        if (!await TableExistsAsync(table))
        {
            return 0;
        }

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {Qualify(table)};";
        var value = await command.ExecuteScalarAsync();
        return Convert.ToInt32(value);
    }

    /// <summary>Determines whether a table exists in the configured schema.</summary>
    public async Task<bool> TableExistsAsync(string table)
    {
        (string schema, string name) = Split(table);

        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = @schema AND table_name = @table
                );
                """;
            command.Parameters.AddWithValue("schema", schema);
            command.Parameters.AddWithValue("table", name);
            var value = await command.ExecuteScalarAsync();
            return Convert.ToBoolean(value);
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidCatalogName)
        {
            // A Full reset drops the database; treat that as "no tables".
            return false;
        }
    }

    private static (string Schema, string Table) Split(string table)
    {
        string[] parts = table.Split('.', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2
            ? (parts[0].Trim('"'), parts[1].Trim('"'))
            : ("todo", table);
    }

    private static string Qualify(string table)
    {
        (string schema, string name) = Split(table);
        return $"\"{schema}\".\"{name}\"";
    }

    /// <summary>Determines whether the container database can be reached.</summary>
    public async Task<bool> CanConnectAsync()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Database.CanConnectAsync();
    }

    /// <summary>Reads a scalar string from an arbitrary SQL query.</summary>
    public async Task<string?> ScalarAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync();
        return value is null or DBNull ? null : value.ToString();
    }

    /// <summary>Executes arbitrary SQL, used to arrange edge cases.</summary>
    public async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
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
