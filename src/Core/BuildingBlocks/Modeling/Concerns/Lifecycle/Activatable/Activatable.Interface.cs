namespace Core.BuildingBlocks.Modeling.Concerns.Lifecycle.Activatable;

/// <summary>Base concern for activation state tracking.</summary>
public interface IActivatable
{
    /// <summary>Gets or sets a value indicating whether the entity is active.</summary>
    bool IsActive { get; set; }
    /// <summary>Gets or sets the UTC timestamp when the entity was activated.</summary>
    DateTimeOffset? ActivatedAtUtc { get; set; }
    /// <summary>Gets or sets the actor who activated the entity.</summary>
    string? ActivatedBy { get; set; }
}