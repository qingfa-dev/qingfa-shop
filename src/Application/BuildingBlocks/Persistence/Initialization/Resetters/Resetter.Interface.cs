namespace Application.BuildingBlocks.Persistence.Initialization.Resetters;
/// <summary>
/// Scope of a database reset operation.
/// </summary>
public enum ResetScope
{
    /// <summary>Delete all data (truncate tables), keep schema.</summary>
    DataOnly,

    /// <summary>Drop and recreate schema (tables, indexes, constraints), keep no data.</summary>
    SchemaOnly,

    /// <summary>Full reset: drop schema and data, then recreate schema.</summary>
    Full,
}

/// <summary>
/// Specialized step for database resetting.
/// Inherits common metadata and execution from <see cref="IInitializationStep"/>.
/// </summary>
public interface IPersistenceResetter : IInitializationStep
{
    /// <summary>
    /// Gets the scope of this resetter.
    /// Determines what gets dropped/truncated.
    /// </summary>
    ResetScope Scope { get; }

    /// <summary>
    /// Gets the tables/schemas this resetter affects.
    /// Empty means "all" (subject to Scope).
    /// </summary>
    string[] Targets { get; }

    /// <summary>
    /// Whether to disable foreign key checks during reset (for DataOnly).
    /// Use with caution — only for trusted environments.
    /// </summary>
    bool DisableForeignKeys { get; }
}