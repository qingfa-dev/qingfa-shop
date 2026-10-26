namespace Application.Common.Persistence.Initialization;

/// <summary>
/// Base interface for any initialization step (migration, seeder, resetter).
/// Provides common metadata, ordering, and execution contract.
/// </summary>
public interface IInitializationStep
{
    /// <summary>Unique identifier for this step (e.g., "migrator:add-users-table").</summary>
    string Name { get; }

    /// <summary>Human-readable display name (e.g., "Add Users Table Migration").</summary>
    string DisplayName { get; }

    /// <summary>Execution order — lower values run first. Used when no explicit dependencies.</summary>
    int Order { get; }

    /// <summary>
    /// Names of steps that must complete before this step.
    /// The pipeline builds a dependency graph from this.
    /// </summary>
    string[] DependsOn { get; }

    /// <summary>
    /// If true, pipeline fails fast when this step fails.
    /// If false, failure is recorded but pipeline continues.
    /// </summary>
    bool IsRequired { get; }

    /// <summary>
    /// Executes the initialization step.
    /// </summary>
    /// <param name="context">Shared context providing DB access, logging, etc.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or failure with error details.</returns>
    Task<Result> ExecuteAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dry-run: inspects what would execute without making changes.
    /// Returns the list of operations that would be performed.
    /// </summary>
    /// <param name="context">Shared context (read-only for preview).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Preview result with planned operations or error.</returns>
    Task<Result<IReadOnlyList<InitializationOperation>>> PreviewAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default);
}