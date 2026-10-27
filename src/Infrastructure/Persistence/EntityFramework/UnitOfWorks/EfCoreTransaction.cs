using System.Data;

using Application.Common.Persistence.UnitOfWorks;

using Infrastructure.Persistence.EntityFramework.Contexts;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence.EntityFramework.UnitOfWorks;

/// <summary>
/// EF Core implementation of a unit of work transaction.
/// Wraps IDbContextTransaction and ensures rollback on dispose if not committed.
/// </summary>
public sealed class EfCoreTransaction : ITransaction
{
    private readonly EfCoreDbContext _context;
    private readonly IDbContextTransaction _transaction;
    private readonly IsolationLevel _isolationLevel;

    private TransactionState _state = TransactionState.Active;
    private bool _disposed;

    internal EfCoreTransaction(
        EfCoreDbContext context,
        IDbContextTransaction transaction,
        bool readOnly)
    {
        _context = context;
        _transaction = transaction;
        IsReadOnly = readOnly;

        // Captured up front: reading it from a completed/disposed DbTransaction throws.
        _isolationLevel = transaction.GetDbTransaction().IsolationLevel;
    }

    /// <inheritdoc />
    /// <summary>
    /// Current state of the transaction (Active, RollbackOnly, Committed, RolledBack, Disposed).
    /// </summary>
    public TransactionState State => _state;

    /// <inheritdoc />
    /// <summary>
    /// Isolation level captured at transaction creation, before the underlying DbTransaction completes or disposes.
    /// </summary>
    public IsolationLevel IsolationLevel => _isolationLevel;

    /// <inheritdoc />
    /// <summary>
    /// True when the transaction is read-only; write operations are rejected and SaveChanges is skipped on commit.
    /// </summary>
    public bool IsReadOnly { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Saves pending changes via <c>SaveChangesAsync</c> and then commits the database transaction.
    /// Callers should not call <c>SaveChangesAsync</c> separately before committing.
    /// If saving or committing fails, the transaction is moved to <see cref="TransactionState.RollbackOnly"/>
    /// (PostgreSQL aborts the transaction after a failed statement), so the only valid next step is rollback.
    /// </remarks>
    public async Task<Result> CommitAsync(CancellationToken cancellationToken = default)
    {
        if (CheckActive() is { } inactive)
            return Result.Fail(inactive);

        if (_state == TransactionState.RollbackOnly)
            return Result.Fail(TransactionOutcome.Failure.TransactionRollbackOnly);

        // Read-only transactions must not persist changes.
        if (IsReadOnly && _context.ChangeTracker.HasChanges())
            return Result.Fail(TransactionOutcome.Failure.ReadOnlyViolation);

        try
        {
            // Flush: EF Core does not auto-flush pending changes on transaction commit,
            // so SaveChangesAsync must be called explicitly to write staged entities to the database.
            if (!IsReadOnly)
                await _context.SaveChangesAsync(cancellationToken);

            await _transaction.CommitAsync(cancellationToken);
            _state = TransactionState.Committed;
            return Result.Ok();
        }
        catch (Exception ex)
        {
            // The transaction can no longer be committed; only rollback is valid.
            _state = TransactionState.RollbackOnly;
            return Result.Fail(PersistenceExceptionTranslator.Translate(ex, cancellationToken));
        }
    }

    /// <inheritdoc />
    public async Task<Result> RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (CheckActive() is { } inactive)
            return Result.Fail(inactive);

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
            _state = TransactionState.RolledBack;
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(PersistenceExceptionTranslator.Translate(ex, cancellationToken));
        }
    }

    /// <inheritdoc />
    public Result MarkRollbackOnly()
    {
        if (CheckActive() is { } inactive)
            return Result.Fail(inactive);

        _state = TransactionState.RollbackOnly;
        return Result.Ok();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Compensate: rollback if not already committed or rolled back.
        if (_state is TransactionState.Active or TransactionState.RollbackOnly)
        {
            try
            {
                await _transaction.RollbackAsync(CancellationToken.None);
            }
            catch
            {
                // Ignore rollback failures during disposal.
            }
        }

        _state = TransactionState.Disposed;
        await _transaction.DisposeAsync();
    }

    /// <summary>
    /// Returns an error when the transaction is not in an active or rollback-only state.
    /// Prevents operations on a transaction that has already been committed, rolled back, or disposed.
    /// </summary>
    private Error? CheckActive() =>
        _state is TransactionState.Active or TransactionState.RollbackOnly
            ? null
            : TransactionOutcome.Failure.TransactionAlreadyCompleted;
}