
using Application.BuildingBlocks.Persistence;
using Application.BuildingBlocks.Persistence.Initialization;
using Application.BuildingBlocks.Persistence.Initialization.Resetters;
using Application.BuildingBlocks.Persistence.UnitOfWorks;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Initialization;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.Initialization;

/// <summary>
/// EF Core implementation of <see cref="IPersistenceInitializer"/>.
/// Orchestrates migrations, seeding, and resetting using the initialization pipeline.
/// </summary>
/// <typeparam name="TContext">The derived DbContext type.</typeparam>
public sealed class EfCorePersistenceInitializer<TContext> : IPersistenceInitializer
    where TContext : DbContext
{
    private readonly TContext _context;
    private readonly ILogger _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEnumerable<IInitializationStep> _steps;
    private readonly IPersistenceSchemaCatalog _schemaCatalog;

    /// <summary>
    /// Creates a new persistence initializer.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="schemaCatalog">The schemas and tables a reset covers.</param>
    /// <param name="steps">Pre-registered initialization steps (seeders, etc.).</param>
    public EfCorePersistenceInitializer(
        TContext context,
        IUnitOfWork unitOfWork,
        ILogger logger,
        IPersistenceSchemaCatalog schemaCatalog,
        IEnumerable<IInitializationStep> steps)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _schemaCatalog = schemaCatalog ?? throw new ArgumentNullException(nameof(schemaCatalog));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Order: the resetter position depends on scope. Schema reset must run before migrations
    /// (it drops tables and history), while data reset runs after migrations (it truncates
    /// the migrated tables). Seeders always run last so they populate the fresh schema.
    /// </remarks>
    public async Task<Result> InitializeAsync(
        bool runMigrations = true,
        bool runSeeders = true,
        bool runResetters = false,
        ResetScope resetScope = ResetScope.Full,
        IProgress<InitializationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            PersistenceInitializerLoggers.Started(_logger);

            var allSteps = new List<IInitializationStep>();

            // Order: a schema reset must run before migrations because it drops the
            // tables and history; a data reset must run after migrations because it
            // truncates the migrated tables. Seeders always run last.
            EfCoreResetter<TContext>? resetter = null;
            if (runResetters)
            {
                resetter = new EfCoreResetter<TContext>(_context, _logger, _schemaCatalog, resetScope);

                if (resetScope != ResetScope.DataOnly)
                {
                    allSteps.Add(resetter);
                }
            }

            if (runMigrations)
            {
                allSteps.Add(new EfCoreMigrator<TContext>(_context, _logger));
                PersistenceInitializerLoggers.RunningMigrators(_logger, 1);
            }
            else
            {
                PersistenceInitializerLoggers.MigrationsSkipped(_logger);
            }

            if (resetter is not null && resetScope == ResetScope.DataOnly)
            {
                allSteps.Add(resetter);
            }

            if (runSeeders)
            {
                List<IInitializationStep> seederSteps = OrderedSeeders().ToList();
                allSteps.AddRange(seederSteps);
                PersistenceInitializerLoggers.RunningSeeders(_logger, seederSteps.Count);
            }
            else
            {
                PersistenceInitializerLoggers.SeedersSkipped(_logger);
            }

            // Build: insertion order carries the migrate/reset/seed contract, and
            // explicit dependencies order the module seeders even when the DI
            // enumeration order differs.
            if (allSteps.Count == 0)
            {
                PersistenceInitializerLoggers.Succeeded(_logger);
                return Result.Ok();
            }

            var connection = _context.Database.GetDbConnection();
            var initContext = new EfCoreInitializationContext(connection, _unitOfWork, _logger, cancellationToken);

            var builder = new InitializationPipelineBuilder();
            HashSet<string> stepNames = allSteps
                .Select(step => step.Name)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var step in allSteps)
            {
                builder.AddStep(step);

                string[] dependencies = step.DependsOn
                    .Where(stepNames.Contains)
                    .ToArray();

                if (dependencies.Length > 0)
                {
                    builder.DependsOn(step.Name, dependencies);
                }
            }

            Result<IInitializationPipeline> built = builder.TryBuild();
            if (built.IsFailure)
            {
                Error graphError = built.Errors[0];
                PersistenceInitializerLoggers.Failed(_logger, graphError.Code, graphError.Message);
                return Result.Fail(built.Errors);
            }

            var result = await built.Value.ExecuteAsync(initContext, progress, cancellationToken);

            if (result.IsSuccess)
            {
                PersistenceInitializerLoggers.Succeeded(_logger);
            }
            else
            {
                // Result guarantees a failed result has at least one error.
                Error error = result.Errors[0];
                PersistenceInitializerLoggers.Failed(_logger, error.Code, error.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            PersistenceInitializerLoggers.Unexpected(_logger, ex);
            return Result.Fail(InitializationOutcome.Failure.Unexpected(ex.Message));
        }
    }

    /// <inheritdoc />
    public IInitializationPipeline GetPipeline()
    {
        var builder = new InitializationPipelineBuilder();

        // Add migrator
        var migrator = new EfCoreMigrator<TContext>(_context, _logger);
        builder.AddStep(migrator);

        // Add registered steps (seeders, etc.)
        foreach (var step in OrderedSeeders())
        {
            builder.AddStep(step);
        }

        builder.UseDefaultOrder();
        return builder.Build();
    }

    /// <summary>Orders DI-discovered steps deterministically for equal <see cref="IInitializationStep.Order"/> values.</summary>
    /// <returns>The seeders sorted by order, then by stable name.</returns>
    private IEnumerable<IInitializationStep> OrderedSeeders()
        // Validate: the DI scan returns steps in assembly metadata order, which shifts when
        // files move. The explicit name tie-break keeps preview and execution in agreement.
        => _steps
            .OrderBy(step => step.Order)
            .ThenBy(step => step.Name, StringComparer.Ordinal);
}
