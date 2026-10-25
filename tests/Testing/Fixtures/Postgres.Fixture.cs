using Npgsql;

using Respawn;

using Testcontainers.PostgreSql;

using Xunit;

namespace Testing.Fixtures;

/// <summary>
/// Boots one real PostgreSQL container for a test collection and resets its
/// state between tests with Respawn.
/// </summary>
/// <remarks>
/// Fixture, not a suite: consumers declare
/// <c>[CollectionDefinition(..., ICollectionFixture&lt;PostgresFixture&gt;)]</c> and inject
/// this type. The Api has no database yet, so this fixture carries no domain
/// schema; a consumer creates the tables it needs and calls <see cref="ResetAsync"/>
/// afterwards. Respawner is built on the first reset so it sees those tables.
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string Image = "postgres:17-alpine";
    private const string UnavailableMessage =
        "The PostgreSQL container is not running; guard with ContainerRuntime.IsAvailable first.";

    private PostgreSqlContainer? _container;
    private Respawner? _respawner;

    /// <summary>Connection string for the running container's database.</summary>
    /// <exception cref="InvalidOperationException">The container never started.</exception>
    public string ConnectionString => _container is null
        ? throw new InvalidOperationException(UnavailableMessage)
        : _container.GetConnectionString();

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        // Validate: with no engine the collection still initialises, so dependent
        // tests can skip via ContainerRuntime.SkipReason instead of failing in the
        // fixture before they ever run.
        if (!ContainerRuntime.IsAvailable)
        {
            return;
        }

        ContainerRuntime.ConfigureProcessEnvironment();

        // Initialize: the builder validates the endpoint in Build(), so the container
        // is constructed only once an engine is known to exist.
        _container = new PostgreSqlBuilder(Image)
            .WithDatabase("qingfa_tests")
            .WithUsername("qingfa")
            .WithPassword("qingfa")
            .Build();

        await _container.StartAsync();
    }

    /// <summary>
    /// Deletes all data in the included schemas, resetting sequences, so the next
    /// test starts from an empty database.
    /// </summary>
    /// <remarks>
    /// The Respawner snapshots the schema when first created. Create tables
    /// before the first <see cref="ResetAsync"/> call, or a later table is left
    /// out of the delete plan.
    /// </remarks>
    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        _respawner ??= await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
        });

        await _respawner.ResetAsync(connection);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}