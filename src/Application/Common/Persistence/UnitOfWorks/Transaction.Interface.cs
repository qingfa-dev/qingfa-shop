using System.Data;

namespace Application.Common.Persistence.UnitOfWorks;

/// <summary>A unit-of-work transaction.</summary>
/// <remarks>Disposing without a successful <see cref="CommitAsync"/> rolls the transaction back.</remarks>
public interface ITransaction : IAsyncDisposable
{
    TransactionState State          { get; }
    IsolationLevel   IsolationLevel { get; }
    bool             IsReadOnly     { get; }

    Task<Result> CommitAsync(CancellationToken cancellationToken = default);
    Task<Result> RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>Marks the transaction so <see cref="CommitAsync"/> fails with
    /// <see cref="Application.Common.Persistence.UnitOfWorks.TransactionOutcome.Failure.TransactionRollbackOnly"/>.</summary>
    /// <returns>A success result, or a failure if the transaction is no longer active.</returns>
    Result MarkRollbackOnly();
}