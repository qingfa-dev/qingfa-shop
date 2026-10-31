namespace Application.BuildingBlocks.Persistence.Repositories;

/// <summary>Selects how a delete use case removes an aggregate.</summary>
public enum DeleteMode
{
    /// <summary>Mark the row deleted and keep it for restore and audit.</summary>
    Soft = 0,

    /// <summary>Remove the row permanently.</summary>
    Hard = 1,
}
