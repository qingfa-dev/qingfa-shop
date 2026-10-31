namespace Core.BuildingBlocks.Modeling.Concerns.Foundation.Referenceable;

/// <summary>Base concern for entities that carry a reference string.</summary>
public interface IReferenceable
{
    /// <summary>Gets or sets the reference string for the entity.</summary>
    string Reference { get; set; }
}