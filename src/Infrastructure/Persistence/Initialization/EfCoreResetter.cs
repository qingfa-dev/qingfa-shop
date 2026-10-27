
using System.Data;

using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Resetters;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    /// <param name="name">The resetter name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="order">The execution order.</param>
    /// <param name="disableForeignKeys">Whether to disable FK checks during reset.</param>
    public EfCoreResetter(
        TContext context,
        ILogger logger,
        ResetScope scope = ResetScope.Full,
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
        Targets = [];
        DisableForeignKeys = disableForeignKeys;
    }

    /// <inheritdoc />
    public async Task<Result> ExecuteAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Starting database reset with scope '{Scope}'.", Scope);

            switch (Scope)
            {
                case ResetScope.DataOnly:
                    await ResetDataAsync(context, cancellationToken);
                    break;
                case ResetScope.SchemaOnly:
                    await ResetSchemaAsync(context, cancellationToken);
                    break;
                case ResetScope.Full:
                    await ResetSchemaAsync(context, cancellationToken);
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
                    Description = "Drop and recreate schema",
                    Type = InitializationOperationType.SchemaChange,
                    EstimatedRowsAffected = null
                });
                break;
            case ResetScope.Full:
                operations.Add(new InitializationOperation
                {
                    StepName = Name,
                    Description = "Drop schema, recreate schema",
                    Type = InitializationOperationType.SchemaChange,
                    EstimatedRowsAffected = null
                });
                break;
        }

        return Task.FromResult(Result<IReadOnlyList<InitializationOperation>>.Ok(operations));
    }

    /// <summary>Resets data by truncating all tables.</summary>
    private async Task ResetDataAsync(IInitializationContext context, CancellationToken cancellationToken)
    {
        var connection = context.Connection;
        if (connection.State != ConnectionState.Open)
            connection.Open();

        // Disable FK checks for PostgreSQL
        if (DisableForeignKeys)
        {
            using var disableCmd = connection.CreateCommand();
            disableCmd.CommandText = "SET session_replication_role = 'replica';";
            disableCmd.CommandTimeout = 30;
            disableCmd.ExecuteNonQuery();
        }

        // Truncate tables in dependency order
        var tables = new[]
        {
            "todos",
            "todo_lists",
            "todo_categories"
        };

        foreach (var table in tables)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"TRUNCATE TABLE {table} RESTART IDENTITY CASCADE;";
            cmd.CommandTimeout = 30;
            cmd.ExecuteNonQuery();
            _logger.LogInformation("Truncated table '{Table}'.", table);
        }

        // Re-enable FK checks
        if (DisableForeignKeys)
        {
            using var enableCmd = connection.CreateCommand();
            enableCmd.CommandText = "SET session_replication_role = 'origin';";
            enableCmd.CommandTimeout = 30;
            enableCmd.ExecuteNonQuery();
        }
    }

    /// <summary>Resets schema by dropping and recreating.</summary>
    private async Task ResetSchemaAsync(IInitializationContext context, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Dropping and recreating database schema.");
        await _context.Database.EnsureDeletedAsync(cancellationToken);
        await _context.Database.EnsureCreatedAsync(cancellationToken);
        _logger.LogInformation("Database schema recreated.");
    }
}
