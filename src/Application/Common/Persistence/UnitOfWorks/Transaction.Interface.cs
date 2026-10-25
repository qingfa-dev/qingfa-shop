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
    /// <see cref="PersistenceOutcome.Failure.TransactionRollbackOnly"/>.</summary>
    void MarkRollbackOnly();
}