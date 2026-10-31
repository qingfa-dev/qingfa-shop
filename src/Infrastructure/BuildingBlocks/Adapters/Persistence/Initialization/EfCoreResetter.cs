
using System.Data;
using System.Data.Common;

using Application.BuildingBlocks.Persistence;
using Application.BuildingBlocks.Persistence.Initialization;
using Application.BuildingBlocks.Persistence.Initialization.Resetters;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Options;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.Initialization;

/// <summary>
/// EF Core implementation of <see cref="IPersistenceResetter"/>.
/// Resets database schema and/or data using EF Core and raw SQL.
/// </summary>
/// <typeparam name="TContext">The derived DbContext type.</typeparam>
public sealed class EfCoreResetter<TContext> : IPersistenceResetter
    where TContext : DbContext
{
    private readonly TContext _context;
    private readonly IPersistenceSchemaCatalog _schemaCatalog;
    private readonly ILogger _logger;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string DisplayName { get; }

    /// <inheritdoc />
    public int Order { get; }

    /// <inheritdoc />
    public string[] DependsOn { get; }

    /// <inheritdoc />
    public bool IsRequired { get; }

    /// <inheritdoc />
    public ResetScope Scope { get; }

    /// <inheritdoc />
    public string[] Targets { get; }

    /// <inheritdoc />
    public bool DisableForeignKeys { get; }

    /// <summary>
    /// Creates a new EF Core resetter.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="schemaCatalog">The schemas and tables the reset covers.</param>
    /// <param name="scope">The reset scope.</param>
    /// <param name="targets">The tables to reset; empty means every table.</param>
    /// <param name="name">The resetter name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="order">The execution order.</param>
    /// <param name="disableForeignKeys">Whether to disable FK checks during reset.</param>
    public EfCoreResetter(
        TContext context,
        ILogger logger,
        IPersistenceSchemaCatalog schemaCatalog,
        ResetScope scope = ResetScope.Full,
        string[]? targets = null,
        string name = "resetter:efcore",
        string? displayName = null,
        int order = ResetterConstant.Limits.DefaultOrder,
        bool disableForeignKeys = true)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _schemaCatalog = schemaCatalog ?? throw new ArgumentNullException(nameof(schemaCatalog));
        Scope = scope;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DisplayName = displayName ?? "EF Core Database Reset";
        Order = order;
        DependsOn = [];
        IsRequired = false;
        Targets = targets ?? [];
        DisableForeignKeys = disableForeignKeys;
    }

    /// <inheritdoc />
    public async Task<Result> ExecuteAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogWarning("Starting database reset with scope '{Scope}'.", Scope);

            switch (Scope)
            {
                case ResetScope.DataOnly:
                    await ResetDataAsync(context, cancellationToken);
                    break;
                case ResetScope.SchemaOnly:
                    await ResetSchemaAsync(cancellationToken);
                    break;
                case ResetScope.Full:
                    await ResetDatabaseAsync(cancellationToken);
                    break;
                default:
                    return Result.Fail(SchemaResetOutcome.Failure.ResetFailed("Invalid scope"));
            }

            // Reset: a reset invalidates every tracked entity; prevent stale rows from
            // being reused by seeders that run later in the same scope.
            _context.ChangeTracker.Clear();

            _logger.LogInformation("Database reset completed successfully for '{ResetterName}'.", Name);
            return Result.Ok();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Translate: caller's cancellation token was triggered.
            return Result.Fail(SchemaResetOutcome.Failure.Cancelled);
        }
        catch (Exception ex)
        {
            // Translate: any unexpected reset failure to a typed error.
            _logger.LogError(ex, "Database reset failed for '{ResetterName}'.", Name);
            return Result.Fail(SchemaResetOutcome.Failure.ResetFailed(ex.Message));
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<InitializationOperation>>> PreviewAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default)
    {
        var operations = new List<InitializationOperation>();

        switch (Scope)
        {
            case ResetScope.DataOnly:
                operations.Add(new InitializationOperation
                {
                    StepName = Name,
                    Description = "Truncate all tables (data only)",
                    Type = InitializationOperationType.DataChange,
                    EstimatedRowsAffected = null
                });
                break;
            case ResetScope.SchemaOnly:
                operations.Add(new InitializationOperation
                {
                    StepName = Name,
                    Description = "Drop all schema objects and recreate the schema",
                    Type = InitializationOperationType.SchemaChange,
                    EstimatedRowsAffected = null
                });
                break;
            case ResetScope.Full:
                operations.Add(new InitializationOperation
                {
                    StepName = Name,
                    Description = "Drop the database",
                    Type = InitializationOperationType.SchemaChange,
                    EstimatedRowsAffected = null
                });
                break;
            default:
                return Task.FromResult(
                    Result<IReadOnlyList<InitializationOperation>>.Fail(
                        SchemaResetOutcome.Failure.ResetFailed("Invalid scope")));
        }

        return Task.FromResult(Result<IReadOnlyList<InitializationOperation>>.Ok(operations));
    }

    /// <summary>
    /// Resets data by truncating tables, preserving the schema.
    /// Disables foreign key checks on PostgreSQL so table order does not matter.
    /// </summary>
    private async Task ResetDataAsync(IInitializationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IDbConnection connection = context.Connection;
        if (connection.State != ConnectionState.Open)
        {
            // Open: use the async path when the provider exposes a DbConnection so
            // cancellation is honored; fall back to the interface for custom connections.
            if (connection is DbConnection dbConnection)
                await dbConnection.OpenAsync(cancellationToken);
            else
                connection.Open();
        }

        bool isPostgres = IsPostgres();
        HashSet<string> existing = await GetExistingTablesAsync(connection, cancellationToken);

        IReadOnlyList<string> requested = Targets.Length > 0 ? Targets : _schemaCatalog.Tables;
        List<(string Schema, string Table)> tables = ResolveTables(requested, existing, isPostgres);

        if (tables.Count == 0)
        {
            _logger.LogInformation("No tables to truncate; the schema is already empty.");
            return;
        }

        // Disable FK checks for PostgreSQL so table order does not matter.
        if (DisableForeignKeys && isPostgres)
        {
            await ExecuteAsync(
                connection,
                "SET session_replication_role = 'replica';",
                cancellationToken);
        }

        foreach ((string schema, string table) in tables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string qualified = Qualify(schema, table);
            string sql = isPostgres
                ? $"TRUNCATE TABLE {qualified} RESTART IDENTITY CASCADE;"
                : $"DELETE FROM {qualified};";

            await ExecuteAsync(connection, sql, cancellationToken);
            _logger.LogInformation("Truncated table '{Table}'.", qualified);
        }

        if (DisableForeignKeys && isPostgres)
        {
            await ExecuteAsync(
                connection,
                "SET session_replication_role = 'origin';",
                cancellationToken);
        }
    }

    /// <summary>
    /// Drops and recreates every configured schema, keeping the database itself.
    /// For PostgreSQL this drops and recreates schemas; for other providers it drops
    /// and recreates the entire database file or catalog.
    /// </summary>
    private async Task ResetSchemaAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("Dropping and recreating database schema.");

        if (IsPostgres())
        {
            // Execute through the raw connection: schema identifiers cannot be
            // parameterized, and the quoting helper is the only input.
            await _context.Database.OpenConnectionAsync(cancellationToken);

            try
            {
                DbConnection connection = _context.Database.GetDbConnection();

                foreach (string schema in _schemaCatalog.Schemas)
                {
                    string quoted = Quote(schema);
                    await ExecuteAsync(
                        connection,
                        "DROP SCHEMA IF EXISTS " + quoted + " CASCADE;",
                        cancellationToken);
                    await ExecuteAsync(
                        connection,
                        "CREATE SCHEMA " + quoted + ";",
                        cancellationToken);
                }

                // Reset: history now lives in the default schema, which is not a module
                // schema. Drop it explicitly so the next migration run recreates the
                // module schemas instead of treating them as already applied.
                string history = Qualify(
                    PersistenceConstant.Schema.Default,
                    NpgsqlOptionExtensions.MigrationHistoryTable);
                await ExecuteAsync(
                    connection,
                    "DROP TABLE IF EXISTS " + history + " CASCADE;",
                    cancellationToken);
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
        else
        {
            // Providers without PostgreSQL schemas drop the file or catalog instead.
            await _context.Database.EnsureDeletedAsync(cancellationToken);
            await _context.Database.EnsureCreatedAsync(cancellationToken);
        }

        PersistenceSchemaResetterLoggers.SchemaRecreated(_logger);
    }

    /// <summary>
    /// Drops the entire database so the next migration run recreates it from scratch.
    /// Closes the connection and clears the connection pool to avoid dead sessions
    /// being reused after the database is dropped.
    /// </summary>
    private async Task ResetDatabaseAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("Dropping the database.");

        // Close the connection directly: it may have been opened outside EF (seeders
        // use the raw connection), in which case Database.CloseConnectionAsync does
        // not close it. Dropping the database then terminates that live session, and
        // the pool can still hand the dead session to the next step (the migration
        // run), so clear the pool as well to force a fresh connection.
        DbConnection connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Closed)
        {
            await connection.CloseAsync();
        }

        await _context.Database.EnsureDeletedAsync(cancellationToken);

        if (IsPostgres())
        {
            NpgsqlConnection.ClearAllPools();
        }

        _logger.LogInformation("Database dropped.");
    }

    /// <summary>True if the database provider is PostgreSQL (Npgsql).</summary>
    private bool IsPostgres() =>
        _context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Queries the database metadata to find existing tables.
    /// Returns "schema.table" keys for PostgreSQL and bare table names for other providers.
    /// </summary>
    private async Task<HashSet<string>> GetExistingTablesAsync(
        IDbConnection connection,
        CancellationToken cancellationToken)
    {
        // Key: "schema.table" on PostgreSQL so the same table name in two
        // schemas stays distinct; a bare table name on providers without schemas.
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (connection is not DbConnection dbConnection)
        {
            return tables;
        }

        if (!IsPostgres())
        {
            await using DbCommand sqlite = dbConnection.CreateCommand();
            sqlite.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table';";

            await using DbDataReader reader = await sqlite.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                tables.Add(reader.GetString(0));
            }

            return tables;
        }

        foreach (string schema in _schemaCatalog.Schemas)
        {
            await using DbCommand command = dbConnection.CreateCommand();
            command.CommandText = """
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = @schema AND table_type = 'BASE TABLE';
                """;

            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = "schema";
            parameter.Value = schema;
            command.Parameters.Add(parameter);

            await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                tables.Add($"{schema}.{reader.GetString(0)}");
            }
        }

        return tables;
    }

    /// <summary>
    /// Resolves requested table names to (schema, table) pairs, filtering to only existing tables.
    /// Supports qualified names (schema.table) and looks up unqualified names in all configured schemas.
    /// </summary>
    private List<(string Schema, string Table)> ResolveTables(
        IEnumerable<string> requested,
        HashSet<string> existing,
        bool isPostgres)
    {
        var tables = new List<(string Schema, string Table)>();

        foreach (string target in requested)
        {
            string[] parts = target.Split('.', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && isPostgres)
            {
                string schema = parts[0].Trim('"');
                string table = parts[1].Trim('"');
                if (existing.Contains($"{schema}.{table}"))
                {
                    tables.Add((schema, table));
                }

                continue;
            }

            if (!isPostgres)
            {
                if (existing.Contains(target))
                {
                    tables.Add((string.Empty, target));
                }

                continue;
            }

            // Unqualified targets are looked up in every configured schema.
            foreach (string schema in _schemaCatalog.Schemas)
            {
                if (existing.Contains($"{schema}.{target}"))
                {
                    tables.Add((schema, target));
                }
            }
        }

        return tables;
    }

    /// <summary>Builds a fully-qualified table name with proper quoting.</summary>
    private static string Qualify(string schema, string table) =>
        string.IsNullOrEmpty(schema)
            ? Quote(table)
            : $"{Quote(schema)}.{Quote(table)}";

    /// <summary>Quotes an identifier with double quotes, escaping embedded quotes.</summary>
    private static string Quote(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    /// <summary>Executes a raw SQL command on the given connection with a 30-second timeout.</summary>
    private static async Task ExecuteAsync(
        IDbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        if (connection is not DbConnection dbConnection)
        {
            return;
        }

        await using DbCommand command = dbConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 30;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
