
using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Resetters;
using Application.Common.Persistence.UnitOfWorks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Initialization;

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

    /// <summary>
    /// Creates a new persistence initializer.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="steps">Pre-registered initialization steps (seeders, etc.).</param>
    public EfCorePersistenceInitializer(
        TContext context,
        IUnitOfWork unitOfWork,
        ILogger logger,
        IEnumerable<IInitializationStep> steps)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
    }

    /// <inheritdoc />
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

            var pipeline = GetPipeline();
            var allSteps = new List<IInitializationStep>();

            // Add migrator first
            if (runMigrations)
            {
                var migrator = new EfCoreMigrator<TContext>(_context, _logger);
                allSteps.Add(migrator);
                PersistenceInitializerLoggers.RunningMigrators(_logger, 1);
            }
            else
            {
                PersistenceInitializerLoggers.MigrationsSkipped(_logger);
            }

            // Add resetter if requested
            if (runResetters)
            {
                var resetter = new EfCoreResetter<TContext>(_context, _logger, resetScope);
                allSteps.Add(resetter);
            }

            // Add seeders
            if (runSeeders)
            {
                var seederSteps = _steps.ToList();
                allSteps.AddRange(seederSteps);
                PersistenceInitializerLoggers.RunningSeeders(_logger, seederSteps.Count);
            }
            else
            {
                PersistenceInitializerLoggers.SeedersSkipped(_logger);
            }

            // Build and execute pipeline
            var connection = _context.Database.GetDbConnection();
            var initContext = new EfCoreInitializationContext(connection, _unitOfWork, _logger, cancellationToken);

            var builder = new InitializationPipelineBuilder();
            foreach (var step in allSteps)
            {
                builder.AddStep(step);
            }
            builder.UseDefaultOrder();
            var builtPipeline = builder.Build();

            var result = await builtPipeline.ExecuteAsync(initContext, progress, cancellationToken);

            if (result.IsSuccess)
            {
                PersistenceInitializerLoggers.Succeeded(_logger);
            }
            else
            {
                var error = result.Errors.FirstOrDefault();
                PersistenceInitializerLoggers.Failed(_logger, error?.Code ?? "Unknown", error?.Message ?? "Unknown error");
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
        foreach (var step in _steps)
        {
            builder.AddStep(step);
        }

        builder.UseDefaultOrder();
        return builder.Build();
    }
}
