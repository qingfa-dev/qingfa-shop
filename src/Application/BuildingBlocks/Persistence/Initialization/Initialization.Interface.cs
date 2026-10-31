
using Application.BuildingBlocks.Persistence.Initialization.Resetters;

namespace Application.BuildingBlocks.Persistence.Initialization;
/// <summary>
/// High-level initializer that orchestrates migrations, seeding, and resetting.
/// This is the main entry point — use the pipeline for advanced scenarios.
/// </summary>
public interface IPersistenceInitializer
{
    /// <summary>
    /// Initializes the database.
    /// </summary>
    /// <param name="runMigrations">Whether to run migrations.</param>
    /// <param name="runSeeders">Whether to run seeders.</param>
    /// <param name="runResetters">Whether to run resetters (schema/data reset).</param>
    /// <param name="resetScope">Scope of reset if resetters run.</param>
    /// <param name="progress">Optional progress reporting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> InitializeAsync(
        bool runMigrations = true,
        bool runSeeders = true,
        bool runResetters = false,
        ResetScope resetScope = ResetScope.Full,
        IProgress<InitializationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the pipeline for advanced composition (dry-run, custom ordering, etc.).
    /// </summary>
    IInitializationPipeline GetPipeline();
}