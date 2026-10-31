
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

using Application.BuildingBlocks.Persistence;
using Application.BuildingBlocks.Persistence.Initialization;
using Application.BuildingBlocks.Persistence.Initialization.Migrations;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Options;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.Initialization;

/// <summary>
/// EF Core implementation of <see cref="IPersistenceMigrator"/>.
/// Applies pending EF Core migrations with retry logic for transient failures.
/// </summary>
/// <typeparam name="TContext">The derived DbContext type.</typeparam>
public sealed class EfCoreMigrator<TContext> : IPersistenceMigrator
    where TContext : DbContext
{
    private readonly TContext _context;
    private readonly ILogger _logger;
    private readonly int _maxRetryAttempts;

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
    public string Version
    {
        get
        {
            IReadOnlyList<string> migrations = _context.Database.GetMigrations().ToList();
            return migrations.Count > 0
                ? migrations[^1]
                : DateTimeOffset.UtcNow.ToString(MigrationConstant.HistoryTable.VersionFormat);
        }
    }

    /// <inheritdoc />
    public string Checksum => "0000000000000000000000000000000000000000000000000000000000000000";

    /// <summary>
    /// Creates a new EF Core migrator.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The migrator name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="order">The execution order.</param>
    /// <param name="maxRetryAttempts">Maximum retry attempts for transient failures.</param>
    public EfCoreMigrator(
        TContext context,
        ILogger logger,
        string name = MigrationConstant.Names.Default,
        string? displayName = null,
        int order = MigrationConstant.Limits.DefaultOrder,
        int maxRetryAttempts = 3)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DisplayName = displayName ?? "EF Core Migrations";
        Order = order;
        DependsOn = [];
        IsRequired = true;
        _maxRetryAttempts = maxRetryAttempts;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Retry: uses exponential backoff because transient database failures like deadlocks
    /// and lock timeouts resolve quickly when retried after a short delay.
    /// </remarks>
    public async Task<Result> ExecuteAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> pendingMigrations;
        try
        {
            // Preserve: move any legacy history before EF decides which migrations are pending.
            await MoveLegacyMigrationHistoryAsync(cancellationToken);
            pendingMigrations = (await _context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Fail(MigrationOutcome.Failure.Cancelled(Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not read pending migrations for '{MigratorName}'.", Name);
            return Result.Fail(MigrationOutcome.Failure.MigrationFailed(Name));
        }

        if (!pendingMigrations.Any())
        {
            _logger.LogInformation("No pending migrations for '{MigratorName}'.", Name);
            return Result.Ok();
        }

        _logger.LogInformation(
            "Applying {Count} pending migration(s) for '{MigratorName}'.",
            pendingMigrations.Count(), Name);

        int attempt = 0;
        Exception? lastException = null;

        while (attempt < _maxRetryAttempts)
        {
            attempt++;
            try
            {
                await _context.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation(
                    "Migrations succeeded for '{MigratorName}' on attempt {Attempt}.",
                    Name, attempt);
                return Result.Ok();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Translate: caller's cancellation token was triggered.
                return Result.Fail(MigrationOutcome.Failure.Cancelled(Name));
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                // Retry: transient failures (timeout, deadlock) may succeed on retry with exponential backoff.
                lastException = ex;
                var delayMs = (int)Math.Min(MigrationConstant.Limits.RetryBaseDelayMs * Math.Pow(2, attempt), MigrationConstant.Limits.RetryMaxDelayMs);
                _logger.LogWarning(
                    ex,
                    "Transient migration failure for '{MigratorName}' on attempt {Attempt}; retrying in {DelayMs}ms.",
                    Name, attempt, delayMs);

                try
                {
                    await Task.Delay(delayMs, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Translate: cancellation during backoff is still a caller cancellation.
                    return Result.Fail(MigrationOutcome.Failure.Cancelled(Name));
                }
            }
            catch (Exception ex)
            {
                // Fail: non-transient errors are not retried.
                _logger.LogError(
                    ex,
                    "Migration failed for '{MigratorName}' on attempt {Attempt}.",
                    Name, attempt);
                return Result.Fail(MigrationOutcome.Failure.MigrationFailed(Name));
            }
        }

        _logger.LogError(
            lastException,
            "Migration failed for '{MigratorName}' after {Attempts} attempts.",
            Name, _maxRetryAttempts);
        return Result.Fail(MigrationOutcome.Failure.MigrationFailed(Name));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<InitializationOperation>>> PreviewAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> pendingMigrations;
        try
        {
            pendingMigrations = (await _context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<IReadOnlyList<InitializationOperation>>.Fail(
                MigrationOutcome.Failure.Cancelled(Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not preview pending migrations for '{MigratorName}'.", Name);
            return Result<IReadOnlyList<InitializationOperation>>.Fail(
                MigrationOutcome.Failure.MigrationFailed(Name));
        }

        var operations = new List<InitializationOperation>();

        foreach (var migration in pendingMigrations)
        {
            operations.Add(new InitializationOperation
            {
                StepName = Name,
                Description = $"Apply migration: {migration}",
                Type = InitializationOperationType.SchemaChange,
                CommandText = migration,
                EstimatedRowsAffected = null
            });
        }

        if (operations.Count == 0)
        {
            operations.Add(new InitializationOperation
            {
                StepName = Name,
                Description = "No pending migrations. Database is up to date.",
                Type = InitializationOperationType.SchemaCheck,
                EstimatedRowsAffected = 0
            });
        }

        return Result<IReadOnlyList<InitializationOperation>>.Ok(operations);
    }

    /// <inheritdoc />
    /// <summary>
    /// Determines whether an exception is transient and worth retrying.
    /// Timeouts, PostgreSQL deadlocks/serialization failures, and wrapped equivalents
    /// are transient; other errors are not.
    /// </summary>
    public bool IsTransient(Exception exception)
    {
        // Walk: provider exceptions are often wrapped by EF Core or another exception.
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException)
                return true;

            if (current is PostgresException
                {
                    SqlState: PostgresErrorCodes.DeadlockDetected
                        or PostgresErrorCodes.SerializationFailure
                })
            {
                return true;
            }

            if (current is InvalidOperationException &&
                current.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>True if the database provider is PostgreSQL (Npgsql).</summary>
    private bool IsPostgres() =>
        _context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Moves an EF Core migration-history table left behind in another schema into
    /// <see cref="PersistenceConstant.Schema.Default"/>.
    /// </summary>
    /// <remarks>
    /// EF Core reads history only from the configured schema. A deployment whose
    /// history sits in the old module schema would otherwise see every migration as
    /// pending and re-apply it against tables that already exist.
    /// Provider-specific SQL is covered by the PostgreSQL integration suite.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    private async Task MoveLegacyMigrationHistoryAsync(CancellationToken cancellationToken)
    {
        // Skip: only PostgreSQL stores tables in schemas.
        if (!IsPostgres())
            return;

        DbConnection connection = _context.Database.GetDbConnection();
        bool wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
        {
            try
            {
                await connection.OpenAsync(cancellationToken);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidCatalogName)
            {
                // Skip: migrations create the database, so a missing catalog has no history yet.
                return;
            }
        }

        try
        {
            string historyTable = NpgsqlOptionExtensions.MigrationHistoryTable;
            string target = PersistenceConstant.Schema.Default;

            // Discover: history tables the default schema does not own yet.
            await using DbCommand discover = connection.CreateCommand();
            discover.CommandText = """
                SELECT table_schema
                FROM information_schema.tables
                WHERE table_name = @table
                  AND table_schema <> @target
                """;
            discover.Parameters.Add(new NpgsqlParameter("table", historyTable));
            discover.Parameters.Add(new NpgsqlParameter("target", target));

            var legacySchemas = new List<string>();
            await using (DbDataReader reader = await discover.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                    legacySchemas.Add(reader.GetString(0));
            }

            if (legacySchemas.Count == 0)
                return;

            bool targetExists = await HistoryTableExistsAsync(
                connection, target, historyTable, cancellationToken);

            foreach (string schema in legacySchemas)
            {
                if (targetExists)
                {
                    // Guard: moving onto an existing history table would fail; leave
                    // both tables for the operator to reconcile instead of throwing.
                    _logger.LogWarning(
                        "Skipped moving legacy migration history from schema '{Schema}' to '{Target}' because the target table already exists.",
                        schema,
                        target);
                    continue;
                }

                await using DbCommand alter = connection.CreateCommand();
                alter.CommandText =
                    $"ALTER TABLE {Quote(schema)}.{Quote(historyTable)} SET SCHEMA {Quote(target)}";
                await alter.ExecuteNonQueryAsync(cancellationToken);
                targetExists = true;

                _logger.LogInformation(
                    "Moved legacy migration history from schema '{Schema}' to '{Target}'.",
                    schema,
                    target);
            }
        }
        finally
        {
            if (wasClosed)
                await connection.CloseAsync();
        }
    }

    /// <summary>Determines whether the migration-history table exists in a schema.</summary>
    [ExcludeFromCodeCoverage]
    private static async Task<bool> HistoryTableExistsAsync(
        DbConnection connection,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_name = @table
                  AND table_schema = @schema
            );
            """;
        command.Parameters.Add(new NpgsqlParameter("table", table));
        command.Parameters.Add(new NpgsqlParameter("schema", schema));

        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    /// <summary>Quotes an identifier with double quotes, escaping embedded quotes.</summary>
    [ExcludeFromCodeCoverage]
    private static string Quote(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
