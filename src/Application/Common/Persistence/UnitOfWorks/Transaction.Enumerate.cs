namespace Application.Common.Persistence.UnitOfWorks;

public enum TransactionState
{
    Active,
    Committed,
    RolledBack,
    RollbackOnly,
    Disposed
}