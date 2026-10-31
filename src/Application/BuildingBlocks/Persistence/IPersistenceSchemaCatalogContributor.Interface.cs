namespace Application.BuildingBlocks.Persistence;

/// <summary>A module's contribution to the persistence schema catalog.</summary>
public interface IPersistenceSchemaCatalogContributor
{
    /// <summary>Gets the module schemas, in reset order.</summary>
    IReadOnlyList<string> Schemas { get; }

    /// <summary>Gets the module tables used by data-only resets.</summary>
    IReadOnlyList<string> Tables { get; }
}
