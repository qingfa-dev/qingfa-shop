namespace Core.BuildingBlocks.Modeling.Concerns.Lifecycle.Creatable;

/// <summary>Base concern for creation audit tracking.</summary>
public interface ICreatable
{
    /// <summary>Gets or sets the UTC timestamp when the entity was created.</summary>
    DateTimeOffset CreatedAtUtc { get; set; }
    /// <summary>Gets or sets the actor who created the entity.</summary>
    string? CreatedBy { get; set; }
}