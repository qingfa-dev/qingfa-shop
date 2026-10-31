namespace Core.BuildingBlocks.Modeling.Concerns.Foundation.Versionable;

/// <summary>Base concern for entities that carry a version number.</summary>
public interface IVersionable
{
    /// <summary>Gets or sets the version number for the entity.</summary>
    long Version { get; set; }
}