namespace Application.BuildingBlocks.Persistence;

/// <summary>Schemas and tables owned by the platform and its modules.</summary>
public interface IPersistenceSchemaCatalog
{
    /// <summary>Gets every managed schema, in reset order.</summary>
    IReadOnlyList<string> Schemas { get; }

    /// <summary>Gets every managed table name used by data-only resets.</summary>
    IReadOnlyList<string> Tables { get; }
}
