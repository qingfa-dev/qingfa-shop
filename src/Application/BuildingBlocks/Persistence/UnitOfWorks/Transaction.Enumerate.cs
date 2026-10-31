namespace Application.BuildingBlocks.Persistence.UnitOfWorks;

public enum TransactionState
{
    Active,
    Committed,
    RolledBack,
    RollbackOnly,
    Disposed
}