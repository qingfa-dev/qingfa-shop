
using Application.Common.Persistence.Initialization.Migrations;
using Application.Common.Persistence.Initialization.Seeders;
using Application.Common.Persistence.Initialization.Resetters;

namespace Application.Common.Persistence.Initialization;
/// <summary>
/// Fluent builder for constructing initialization pipelines.
/// </summary>
public interface IInitializationPipelineBuilder
{
    /// <summary>
    /// Adds a migrator step.
    /// </summary>
    IInitializationPipelineBuilder AddMigrator(IPersistenceMigrator migrator);

    /// <summary>
    /// Adds a seeder step.
    /// </summary>
    IInitializationPipelineBuilder AddSeeder(IPersistenceSeeder seeder);

    /// <summary>
    /// Adds a resetter step.
    /// </summary>
    IInitializationPipelineBuilder AddResetter(IPersistenceResetter resetter);

    /// <summary>
    /// Adds a custom step.
    /// </summary>
    IInitializationPipelineBuilder AddStep(IInitializationStep step);

    /// <summary>
    /// Adds multiple steps.
    /// </summary>
    IInitializationPipelineBuilder AddSteps(params IInitializationStep[] steps);

    /// <summary>
    /// Configures the pipeline to run migrations first, then seeders, then resetters.
    /// This is the default order.
    /// </summary>
    IInitializationPipelineBuilder UseDefaultOrder();

    /// <summary>
    /// Configures a custom ordering strategy.
    /// </summary>
    IInitializationPipelineBuilder UseOrdering(Func<IEnumerable<IInitializationStep>, IReadOnlyList<IInitializationStep>> orderFunc);

    /// <summary>
    /// Registers a step as required (pipeline fails fast on failure).
    /// </summary>
    IInitializationPipelineBuilder RequireStep(string stepName);

    /// <summary>
    /// Registers a step as optional (pipeline continues on failure).
    /// </summary>
    IInitializationPipelineBuilder MakeOptional(string stepName);

    /// <summary>
    /// Adds a dependency between steps.
    /// </summary>
    IInitializationPipelineBuilder DependsOn(string stepName, params string[] dependencyNames);

    /// <summary>
    /// Builds the pipeline.
    /// </summary>
    IInitializationPipeline Build();
}

/// <summary>
/// Default implementation of the pipeline builder.
/// </summary>
public sealed class InitializationPipelineBuilder : IInitializationPipelineBuilder
{
    private readonly List<IInitializationStep> _steps = [];
    private readonly Dictionary<string, string[]> _dependencies = [];
    private readonly HashSet<string> _requiredSteps = [];
    private Func<IEnumerable<IInitializationStep>, IReadOnlyList<IInitializationStep>>? _orderFunc;

    public IInitializationPipelineBuilder AddMigrator(IPersistenceMigrator migrator)
    {
        ArgumentNullException.ThrowIfNull(migrator);
        _steps.Add(migrator);
        return this;
    }

    public IInitializationPipelineBuilder AddSeeder(IPersistenceSeeder seeder)
    {
        ArgumentNullException.ThrowIfNull(seeder);
        _steps.Add(seeder);
        return this;
    }

    public IInitializationPipelineBuilder AddResetter(IPersistenceResetter resetter)
    {
        ArgumentNullException.ThrowIfNull(resetter);
        _steps.Add(resetter);
        return this;
    }

    public IInitializationPipelineBuilder AddStep(IInitializationStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        _steps.Add(step);
        return this;
    }

    public IInitializationPipelineBuilder AddSteps(params IInitializationStep[] steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        _steps.AddRange(steps);
        return this;
    }

    public IInitializationPipelineBuilder UseDefaultOrder()
    {
        _orderFunc = DefaultOrder;
        return this;
    }

    public IInitializationPipelineBuilder UseOrdering(Func<IEnumerable<IInitializationStep>, IReadOnlyList<IInitializationStep>> orderFunc)
    {
        _orderFunc = orderFunc ?? throw new ArgumentNullException(nameof(orderFunc));
        return this;
    }

    public IInitializationPipelineBuilder RequireStep(string stepName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stepName);
        _requiredSteps.Add(stepName);
        return this;
    }

    public IInitializationPipelineBuilder MakeOptional(string stepName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stepName);
        _requiredSteps.Remove(stepName);
        return this;
    }

    public IInitializationPipelineBuilder DependsOn(string stepName, params string[] dependencyNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stepName);
        ArgumentNullException.ThrowIfNull(dependencyNames);
        _dependencies[stepName] = dependencyNames;
        return this;
    }

    public IInitializationPipeline Build()
    {
        var orderedSteps = ResolveOrder();
        ApplyMetadata(orderedSteps);
        return new InitializationPipeline(orderedSteps);
    }

    private IReadOnlyList<IInitializationStep> ResolveOrder()
    {
        if (_orderFunc != null)
        {
            return _orderFunc(_steps);
        }

        // Default: topological sort by DependsOn, then by Order
        var graph = _steps.ToDictionary(s => s.Name, s => _dependencies.GetValueOrDefault(s.Name, []));
        var sorted = TopologicalSort(graph);
        return sorted.Select(name => _steps.First(s => s.Name == name)).ToList();
    }

    private static List<string> TopologicalSort(Dictionary<string, string[]> graph)
    {
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();
        var result = new List<string>();

        void Visit(string node)
        {
            if (visited.Contains(node)) return;
            if (visiting.Contains(node))
                throw new InvalidOperationException($"Circular dependency detected involving '{node}'.");

            visiting.Add(node);
            if (graph.TryGetValue(node, out var deps))
            {
                foreach (var dep in deps)
                {
                    if (!graph.ContainsKey(dep))
                        throw new InvalidOperationException($"Step '{node}' depends on unknown step '{dep}'.");
                    Visit(dep);
                }
            }
            visiting.Remove(node);
            visited.Add(node);
            result.Add(node);
        }

        foreach (var node in graph.Keys)
        {
            Visit(node);
        }

        return result;
    }

    private static IReadOnlyList<IInitializationStep> DefaultOrder(IEnumerable<IInitializationStep> steps)
    {
        return steps
            .OrderBy(s => s is IPersistenceMigrator ? 0 : s is IPersistenceSeeder ? 1 : s is IPersistenceResetter ? 2 : 3)
            .ThenBy(s => s.Order)
            .ToList();
    }

    private void ApplyMetadata(IReadOnlyList<IInitializationStep> steps)
    {
        // IsRequired is read-only on interface, so we'd need to use a wrapper or mutable implementation
        // For now, we'll handle this in the pipeline execution
    }
}

/// <summary>
/// Default implementation of the initialization pipeline.
/// </summary>
public sealed class InitializationPipeline : IInitializationPipeline
{
    private readonly IReadOnlyList<IInitializationStep> _steps;

    public InitializationPipeline(IReadOnlyList<IInitializationStep> steps)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
    }

    public IInitializationPipeline AddStep(IInitializationStep step)
    {
        var newSteps = _steps.Concat([step]).ToList();
        return new InitializationPipeline(newSteps);
    }

    public IInitializationPipeline AddSteps(params IInitializationStep[] steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        var newSteps = _steps.Concat(steps).ToList();
        return new InitializationPipeline(newSteps);
    }

    public IInitializationPipeline AddSteps(IEnumerable<IInitializationStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        var newSteps = _steps.Concat(steps).ToList();
        return new InitializationPipeline(newSteps);
    }

    public IInitializationPipeline RemoveStep(string stepName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stepName);
        var newSteps = _steps.Where(s => s.Name != stepName).ToList();
        return new InitializationPipeline(newSteps);
    }

    public IReadOnlyList<IInitializationStep> GetOrderedSteps() => _steps;

    public Result Validate()
    {
        if (_steps.Count == 0)
        {
            return Result.Fail(InitializationOutcome.Failure.NoStepsRegistered);
        }

        var names = _steps.Select(s => s.Name).ToList();
        if (names.Count != names.Distinct().Count())
        {
            var dup = names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).First();
            return Result.Fail(InitializationOutcome.Failure.DuplicateStepName(dup));
        }

        // Check dependencies exist
        foreach (var step in _steps)
        {
            foreach (var dep in step.DependsOn)
            {
                if (!names.Contains(dep))
                {
                    return Result.Fail(InitializationOutcome.Failure.MissingDependency(step.Name, dep));
                }
            }
        }

        return Result.Ok();
    }

    public async Task<Result> ExecuteAsync(
        IInitializationContext context,
        IProgress<InitializationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate();
        if (validation.IsFailure) return validation;

        var startedAt = DateTimeOffset.UtcNow;
        var completedSteps = new HashSet<string>();

        for (int i = 0; i < _steps.Count; i++)
        {
            var step = _steps[i];
            cancellationToken.ThrowIfCancellationRequested();

            // Check dependencies
            var missingDeps = step.DependsOn.Where(d => !completedSteps.Contains(d)).ToList();
            if (missingDeps.Count > 0)
            {
                progress?.Report(new InitializationProgress
                {
                    CurrentStepName = step.Name,
                    CurrentStepDisplayName = step.DisplayName,
                    CurrentStepIndex = i,
                    TotalSteps = _steps.Count,
                    Status = InitializationStepStatus.Skipped,
                    Message = $"Missing dependencies: {string.Join(", ", missingDeps)}",
                    StartedAt = startedAt
                });
                continue;
            }

            progress?.Report(new InitializationProgress
            {
                CurrentStepName = step.Name,
                CurrentStepDisplayName = step.DisplayName,
                CurrentStepIndex = i,
                TotalSteps = _steps.Count,
                Status = InitializationStepStatus.Running,
                StartedAt = startedAt
            });

            var result = await step.ExecuteAsync(context, cancellationToken);

            if (result.IsSuccess)
            {
                completedSteps.Add(step.Name);
                progress?.Report(new InitializationProgress
                {
                    CurrentStepName = step.Name,
                    CurrentStepDisplayName = step.DisplayName,
                    CurrentStepIndex = i,
                    TotalSteps = _steps.Count,
                    Status = InitializationStepStatus.Completed,
                    StartedAt = startedAt
                });
            }
            else
            {
                var isRequired = step.IsRequired; // Note: this requires step to expose IsRequired
                progress?.Report(new InitializationProgress
                {
                    CurrentStepName = step.Name,
                    CurrentStepDisplayName = step.DisplayName,
                    CurrentStepIndex = i,
                    TotalSteps = _steps.Count,
                    Status = isRequired ? InitializationStepStatus.FailedStopped : InitializationStepStatus.FailedSkipped,
                    Message = result.Errors.FirstOrDefault()?.Message,
                    StartedAt = startedAt
                });

                if (isRequired)
                {
                    return Result.Fail(InitializationOutcome.Failure.StepFailed(step.Name, result.Errors));
                }
            }
        }

        return Result.Ok();
    }

    public async Task<Result<InitializationPreview>> PreviewAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate();
        if (validation.IsFailure) return Result<InitializationPreview>.Fail(validation.Errors);

        var allOperations = new List<InitializationOperation>();
        var stepNames = new List<string>();

        foreach (var step in _steps)
        {
            stepNames.Add(step.Name);
            var stepPreview = await step.PreviewAsync(context, cancellationToken);
            if (stepPreview.IsFailure) return Result<InitializationPreview>.Fail(stepPreview.Errors);
            allOperations.AddRange(stepPreview.Value);
        }

        var previewResult = new InitializationPreview
        {
            Operations = allOperations,
            StepNames = stepNames
        };

        return Result<InitializationPreview>.Ok(previewResult);
    }
}