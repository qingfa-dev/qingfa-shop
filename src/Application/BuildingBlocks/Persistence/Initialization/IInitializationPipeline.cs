namespace Application.BuildingBlocks.Persistence.Initialization;

/// <summary>
/// Pipeline for composing and executing initialization steps.
/// Handles dependency resolution, ordering, and execution with progress reporting.
/// </summary>
public interface IInitializationPipeline
{
    /// <summary>
    /// Adds a step to the pipeline.
    /// </summary>
    /// <param name="step">The step to add.</param>
    /// <returns>This pipeline for chaining.</returns>
    IInitializationPipeline AddStep(IInitializationStep step);

    /// <summary>
    /// Adds multiple steps to the pipeline.
    /// </summary>
    IInitializationPipeline AddSteps(params IInitializationStep[] steps);

    /// <summary>
    /// Adds multiple steps to the pipeline.
    /// </summary>
    IInitializationPipeline AddSteps(IEnumerable<IInitializationStep> steps);

    /// <summary>
    /// Removes a step by name.
    /// </summary>
    IInitializationPipeline RemoveStep(string stepName);

    /// <summary>
    /// Gets all registered steps in execution order (topologically sorted).
    /// </summary>
    IReadOnlyList<IInitializationStep> GetOrderedSteps();

    /// <summary>
    /// Validates the pipeline (checks for cycles, missing dependencies).
    /// </summary>
    Result Validate();

    /// <summary>
    /// Executes the pipeline.
    /// </summary>
    Task<Result> ExecuteAsync(
        IInitializationContext context,
        IProgress<InitializationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dry-run: previews all steps without executing.
    /// </summary>
    Task<Result<InitializationPreview>> PreviewAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Progress information during pipeline execution.
/// </summary>
public readonly record struct InitializationProgress
{
    /// <summary>Name of the currently executing step.</summary>
    public required string CurrentStepName { get; init; }

    /// <summary>Display name of the current step.</summary>
    public required string CurrentStepDisplayName { get; init; }

    /// <summary>Index of current step (0-based).</summary>
    public required int CurrentStepIndex { get; init; }

    /// <summary>Total number of steps.</summary>
    public required int TotalSteps { get; init; }

    /// <summary>Current step status.</summary>
    public required InitializationStepStatus Status { get; init; }

    /// <summary>Optional message from the step.</summary>
    public string? Message { get; init; }

    /// <summary>Start time of the pipeline.</summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>Optional: estimated time remaining.</summary>
    public TimeSpan? EstimatedTimeRemaining { get; init; }
}

/// <summary>Status of a step during execution.</summary>
public enum InitializationStepStatus
{
    /// <summary>Step is pending execution.</summary>
    Pending,

    /// <summary>Step is currently running.</summary>
    Running,

    /// <summary>Step completed successfully.</summary>
    Completed,

    /// <summary>Step failed but pipeline continues (IsRequired=false).</summary>
    FailedSkipped,

    /// <summary>Step failed and pipeline stopped (IsRequired=true).</summary>
    FailedStopped,

    /// <summary>Step was skipped (e.g., dependency failed).</summary>
    Skipped,
}

/// <summary>
/// Result of a dry-run preview.
/// </summary>
public readonly record struct InitializationPreview
{
    /// <summary>All operations from all steps in execution order.</summary>
    public required IReadOnlyList<InitializationOperation> Operations { get; init; }

    /// <summary>Total estimated operations count.</summary>
    public int TotalOperations => Operations.Count;

    /// <summary>Operations grouped by type.</summary>
    public IReadOnlyDictionary<InitializationOperationType, int> OperationsByType =>
        Operations.GroupBy(o => o.Type).ToDictionary(g => g.Key, g => g.Count());

    /// <summary>Steps that would execute.</summary>
    public required IReadOnlyList<string> StepNames { get; init; }

    /// <summary>Whether any step has Required=true (pipeline would stop on failure).</summary>
    public bool HasRequiredSteps => StepNames.Any(n => false); // Placeholder - needs step access
}