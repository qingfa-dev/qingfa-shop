using Application.Common.Persistence.Capabilities;
using Application.Common.Persistence.Repositories;
using Application.Common.Querying.Mapping;

using Core.Common.Domain.Entities;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.QueryTranslation;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Persistence.EntityFramework.Repositories;

/// <summary>
/// EF Core implementation of the persistent repository pattern.
/// Bridges the provider-neutral repository interfaces to EF Core's DbContext.
/// Split into partial classes by concern: Read, Write, Delete, Search, Projection, Streaming.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity key type.</typeparam>
public partial class EfCoreRepository<TEntity, TKey> : IPersistentRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    protected readonly EfCoreDbContext _context;
    private readonly IQueryTranslator<TEntity> _translator;
    private readonly QueryMap<TEntity> _map;

    /// <summary>
    /// What query shapes this provider can translate (expressions, search, ranking, etc.).
    /// Used by callers to check feature support before constructing queries.
    /// </summary>
    public IQueryCapabilities Capabilities { get; }

    public EfCoreRepository(
        EfCoreDbContext context,
        IQueryTranslator<TEntity> translator,
        IQueryMapProvider mapProvider,
        IQueryCapabilities capabilities)
    {
        _context = context;
        _translator = translator;
        Capabilities = capabilities;

        // Guard: fail fast at DI resolution rather than at the first query, so the missing
        // map is discovered when the repository is constructed, not deep in a request pipeline.
        if (!mapProvider.TryGet<TEntity>(out QueryMap<TEntity>? map))
        {
            throw new InvalidOperationException(
                $"No query map is registered for '{typeof(TEntity).Name}'. " +
                "Register a QueryMap via IQueryMapProvider before resolving the repository.");
        }

        _map = map!;
    }

    /// <summary>
    /// Translates EF Core and database provider exceptions to granular persistence errors.
    /// Isolated from PersistenceExceptionTranslator: repository-level errors use RepositoryOutcome
    /// (not UnitOfWorkOutcome), since repository operations are not within a unit-of-work transaction.
    /// </summary>
    private static Error TranslateException(Exception ex)
    {
        // PostgreSQL-specific: inspect the inner PostgresException for precise SQLSTATE codes
        if (ex.InnerException is Npgsql.PostgresException pgEx)
        {
            return TranslatePostgresException(pgEx);
        }

        // EF Core concurrency exception
        if (ex is DbUpdateConcurrencyException)
        {
            return RepositoryOutcome.Failure.ConcurrencyConflict;
        }

        // EF Core update exception (constraint violations, etc.)
        if (ex is DbUpdateException dbEx)
        {
            return TranslateDbUpdateException(dbEx);
        }

        // Timeout
        if (ex is TimeoutException)
        {
            return RepositoryOutcome.Failure.Timeout;
        }

        // Cancellation
        if (ex is OperationCanceledException ocEx)
        {
            return ocEx.CancellationToken.IsCancellationRequested
                ? RepositoryOutcome.Failure.Cancelled
                : RepositoryOutcome.Failure.Timeout;
        }

        // InvalidOperationException with timeout message
        if (ex is InvalidOperationException ioe &&
            ioe.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            return RepositoryOutcome.Failure.Timeout;
        }

        // LINQ expression translation failure
        if (ex is InvalidOperationException ioe2 &&
            (ioe2.Message.Contains("translate", StringComparison.OrdinalIgnoreCase) ||
             ioe2.Message.Contains("LINQ", StringComparison.OrdinalIgnoreCase)))
        {
            return RepositoryOutcome.Failure.ExpressionTranslationFailed(ioe2.Message);
        }

        // Generic InvalidOperationException
        if (ex is InvalidOperationException ioe3)
        {
            return RepositoryOutcome.Failure.InvalidOperationFor(ioe3.Message);
        }

        // Unknown
        return RepositoryOutcome.Failure.Unknown;
    }

    /// <summary>
    /// Translates PostgreSQL SQLSTATE codes to repository-level persistence errors.
    /// Uses RepositoryOutcome rather than UnitOfWorkOutcome for consistency with the repository pattern.
    /// </summary>
    static Error TranslatePostgresException(Npgsql.PostgresException ex) => ex.SqlState switch
    {
        // Integrity constraint violations
        "23505" => RepositoryOutcome.Failure.DuplicateKey,
        "23503" => RepositoryOutcome.Failure.ForeignKeyViolation,
        "23514" => RepositoryOutcome.Failure.CheckConstraintViolation,
        "23502" => RepositoryOutcome.Failure.NotNullViolation,

        // Lock/wait deadlocks
        "40P01" => RepositoryOutcome.Failure.DeadlockDetected,

        // Serialization failures
        "40001" => RepositoryOutcome.Failure.SerializationFailure,

        // Connection exceptions
        "08001" => RepositoryOutcome.Failure.ConnectionRefused,
        "08006" => RepositoryOutcome.Failure.ConnectionLost,

        // Invalid authorization specification
        "28000" => RepositoryOutcome.Failure.LoginFailed,

        // Cannot connect now (server shutting down, etc.)
        "08004" => RepositoryOutcome.Failure.ConnectionFailed,

        // Other PostgreSQL errors
        _ => RepositoryOutcome.Failure.Unknown,
    };

    /// <summary>
    /// Fallback for DbUpdateException when no PostgresException is found in the inner exception chain.
    /// Uses message keyword matching to infer the constraint type violated.
    /// </summary>
    static Error TranslateDbUpdateException(DbUpdateException ex)
    {
        string message = ex.Message.ToLowerInvariant();
        if (message.Contains("unique") || message.Contains("duplicate"))
            return RepositoryOutcome.Failure.DuplicateKey;
        if (message.Contains("foreign key") || message.Contains("reference constraint"))
            return RepositoryOutcome.Failure.ForeignKeyViolation;
        if (message.Contains("check constraint"))
            return RepositoryOutcome.Failure.CheckConstraintViolation;
        if (message.Contains("not null") || message.Contains("null value"))
            return RepositoryOutcome.Failure.NotNullViolation;
        return RepositoryOutcome.Failure.SaveChangesFailed(ex.Message);
    }

    /// <summary>
    /// Detaches any tracked instance that shares the supplied entity's key.
    /// EF Core rejects a second instance with the same key, so the stale tracked copy must be
    /// detached before the caller's detached entity can be attached and updated.
    /// </summary>
    /// <remarks>
    /// Reads use <c>AsNoTracking</c>, so update and delete receive detached copies.
    /// If the same key is still tracked (for example, after a create in the same
    /// scope), EF rejects a second instance with that key. Detaching the stale copy
    /// lets the caller's instance become the single tracked instance.
    /// </remarks>
    /// <param name="entity">The detached entity that will be attached.</param>
    private void DetachTrackedCopy(TEntity entity)
    {
        List<EntityEntry<TEntity>> stale = _context.ChangeTracker
            .Entries<TEntity>()
            .Where(entry => !ReferenceEquals(entry.Entity, entity) &&
                EqualityComparer<TKey>.Default.Equals(entry.Entity.Id, entity.Id))
            .ToList();

        foreach (EntityEntry<TEntity> entry in stale)
        {
            entry.State = EntityState.Detached;
        }
    }
}
