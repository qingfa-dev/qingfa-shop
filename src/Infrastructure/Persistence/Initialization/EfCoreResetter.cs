
using System.Data;
using System.Data.Common;

using Application.Common.Persistence;
using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Resetters;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Npgsql;

namespace Infrastructure.Persistence.Initialization;

/// <summary>
/// EF Core implementation of <see cref="IPersistenceResetter"/>.
/// Resets database schema and/or data using EF Core and raw SQL.
/// </summary>
/// <typeparam name="TContext">The derived DbContext type.</typeparam>
public sealed class EfCoreResetter<TContext> : IPersistenceResetter
    where TContext : DbContext
{
    private readonly TContext _context;
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
    /// <param name="scope">The reset scope.</param>
    /// <param name="targets">The tables to reset; empty means every table.</param>
    /// <param name="name">The resetter name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="order">The execution order.</param>
    /// <param name="disableForeignKeys">Whether to disable FK checks during reset.</param>
    public EfCoreResetter(
        TContext context,
        ILogger logger,
        ResetScope scope = ResetScope.Full,
        string[]? targets = null,
        string name = "resetter:efcore",
        string? displayName = null,
        int order = ResetterConstant.Limits.DefaultOrder,
        bool disableForeignKeys = true)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

            _logger.LogInformation("Database reset completed successfully for '{ResetterName}'.", Name);
            return Result.Ok();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Fail(SchemaResetOutcome.Failure.Cancelled);
        }
        catch (Exception ex)
        {
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
        }

        return Task.FromResult(Result<IReadOnlyList<InitializationOperation>>.Ok(operations));
    }

    /// <summary>Resets data by truncating the requested tables that exist.</summary>
    private async Task ResetDataAsync(IInitializationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IDbConnection connection = context.Connection;
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        bool isPostgres = IsPostgres();
        HashSet<string> existing = await GetExistingTablesAsync(connection, cancellationToken);

        string[] requested = Targets.Length > 0 ? Targets : DefaultTables;
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

    /// <summary>Drops every configured schema object while keeping the database itself.</summary>
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

                foreach (string schema in Schemas)
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

        _logger.LogInformation("Database schema recreated.");
    }

    /// <summary>Drops the database so the next migration run recreates it.</summary>
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

    private bool IsPostgres() =>
        _context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

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

        foreach (string schema in Schemas)
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
            foreach (string schema in Schemas)
            {
                if (existing.Contains($"{schema}.{target}"))
                {
                    tables.Add((schema, target));
                }
            }
        }

        return tables;
    }

    private static string Qualify(string schema, string table) =>
        string.IsNullOrEmpty(schema)
            ? Quote(table)
            : $"{Quote(schema)}.{Quote(table)}";

    private static string Quote(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private static IReadOnlyList<string> Schemas => PersistenceConstant.Schema.Names;

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

    private static readonly string[] DefaultTables =
    [
        "todos",
        "todo_lists",
        "todo_categories"
    ];
}
