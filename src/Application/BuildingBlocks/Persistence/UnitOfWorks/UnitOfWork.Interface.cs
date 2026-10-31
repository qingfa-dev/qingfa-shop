using System.Data;

namespace Application.BuildingBlocks.Persistence.UnitOfWorks;

/// <summary>Owns commits and transactions for the current scope.
/// The single commit point for every <c>AddAsync</c>/<c>UpdateAsync</c>/<c>RemoveAsync</c> on any repository.</summary>
public interface IUnitOfWork
{
    /// <summary>True when the tracker has pending changes (EF Core) or the adapter has staged work (Dapper).</summary>
    bool HasChanges { get; }

    Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<Result<ITransaction>> BeginTransactionAsync(
        IsolationLevel? isolationLevel = null,
        bool readOnly = false,
        CancellationToken cancellationToken = default);

    // ---- Scoped helpers — commit on success, rollback on failure/cancel/throw.

    Task<Result> ExecuteAsync(
        Func<CancellationToken, Task> work,
        CancellationToken cancellationToken = default);

    Task<Result<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> work,
        CancellationToken cancellationToken = default);

    Task<Result> ExecuteInTransactionAsync(
        Func<ITransaction, CancellationToken, Task> work,
        IsolationLevel? isolationLevel = null,
        bool readOnly = false,
        CancellationToken cancellationToken = default);

    Task<Result<T>> ExecuteInTransactionAsync<T>(
        Func<ITransaction, CancellationToken, Task<T>> work,
        IsolationLevel? isolationLevel = null,
        bool readOnly = false,
        CancellationToken cancellationToken = default);
}