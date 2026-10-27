
using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Migrations;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Initialization;

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
        string name = "migrator:efcore",
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
        var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
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
                var delayMs = (int)Math.Min(1000 * Math.Pow(2, attempt), 10000);
                _logger.LogWarning(
                    ex,
                    "Transient migration failure for '{MigratorName}' on attempt {Attempt}; retrying in {DelayMs}ms.",
                    Name, attempt, delayMs);
                await Task.Delay(delayMs, cancellationToken);
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
        var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
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
    /// Timeouts and deadlocks are transient; other errors are not.
    /// </summary>
    public bool IsTransient(Exception exception)
    {
        return exception switch
        {
            TimeoutException => true,
            InvalidOperationException when exception.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase) => true,
            _ => false
        };
    }
}
