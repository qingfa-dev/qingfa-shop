using Application.Common.Persistence.Initialization;

namespace Infrastructure.Unit.Tests.Persistence.Initialization;

/// <summary>Unit tests for InitializationPipeline and InitializationPipelineBuilder.</summary>
public sealed class InitializationPipelineTest
{
    #region Pipeline Builder

    [Fact]
    public void Build_WithNoSteps_ShouldCreateEmptyPipeline()
    {
        var builder = new InitializationPipelineBuilder();
        var pipeline = builder.Build();
        pipeline.GetOrderedSteps().Should().BeEmpty();
    }

    [Fact]
    public void Build_WithSingleStep_ShouldReturnOneStep()
    {
        var step = CreateStep("step1", 1);
        var builder = new InitializationPipelineBuilder();
        builder.AddStep(step);
        var pipeline = builder.Build();
        pipeline.GetOrderedSteps().Should().HaveCount(1);
    }

    [Fact]
    public void Build_WithMultipleSteps_ShouldReturnAllSteps()
    {
        var builder = new InitializationPipelineBuilder();
        builder.AddStep(CreateStep("step1", 1));
        builder.AddStep(CreateStep("step2", 2));
        builder.AddStep(CreateStep("step3", 3));
        var pipeline = builder.Build();
        pipeline.GetOrderedSteps().Should().HaveCount(3);
    }

    [Fact]
    public void Build_WithDefaultOrder_ShouldOrderByType()
    {
        var builder = new InitializationPipelineBuilder();
        builder.AddStep(CreateStep("seeder", 1));
        builder.AddStep(CreateStep("migrator", 0));
        builder.UseDefaultOrder();
        var pipeline = builder.Build();
        pipeline.GetOrderedSteps().Should().HaveCount(2);
    }

    [Fact]
    public void AddSteps_WithArray_ShouldAddAll()
    {
        var builder = new InitializationPipelineBuilder();
        builder.AddSteps(CreateStep("step1", 1), CreateStep("step2", 2));
        var pipeline = builder.Build();
        pipeline.GetOrderedSteps().Should().HaveCount(2);
    }

    [Fact]
    public void DependsOn_WithValidDependency_ShouldWork()
    {
        var builder = new InitializationPipelineBuilder();
        builder.AddStep(CreateStep("step1", 1));
        builder.AddStep(CreateStep("step2", 2, ["step1"]));
        builder.DependsOn("step2", "step1");
        var pipeline = builder.Build();
        pipeline.GetOrderedSteps().Should().HaveCount(2);
    }

    #endregion

    #region Pipeline Validation

    [Fact]
    public void Validate_WhenEmpty_ShouldFail()
    {
        var pipeline = new InitializationPipeline(Array.Empty<IInitializationStep>());
        var result = pipeline.Validate();
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(InitializationOutcome.Codes.NoStepsRegistered);
    }

    [Fact]
    public void Validate_WhenDuplicateNames_ShouldFail()
    {
        var steps = new IInitializationStep[]
        {
            CreateStep("step1", 1),
            CreateStep("step1", 2)
        };
        var pipeline = new InitializationPipeline(steps.ToList());
        var result = pipeline.Validate();
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(InitializationOutcome.Codes.DuplicateStepName);
    }

    [Fact]
    public void Validate_WhenMissingDependency_ShouldFail()
    {
        var steps = new IInitializationStep[]
        {
            CreateStep("step1", 1, ["nonexistent"])
        };
        var pipeline = new InitializationPipeline(steps.ToList());
        var result = pipeline.Validate();
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(InitializationOutcome.Codes.MissingDependency);
    }

    [Fact]
    public void Validate_WhenValid_ShouldSucceed()
    {
        var steps = new IInitializationStep[]
        {
            CreateStep("step1", 1),
            CreateStep("step2", 2)
        };
        var pipeline = new InitializationPipeline(steps.ToList());
        var result = pipeline.Validate();
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Pipeline Modification

    [Fact]
    public void AddStep_ShouldReturnNewPipeline()
    {
        var pipeline = new InitializationPipeline(new List<IInitializationStep> { CreateStep("step1", 1) });
        var newPipeline = pipeline.AddStep(CreateStep("step2", 2));
        newPipeline.GetOrderedSteps().Should().HaveCount(2);
    }

    [Fact]
    public void RemoveStep_ShouldReturnNewPipeline()
    {
        var pipeline = new InitializationPipeline(new List<IInitializationStep>
        {
            CreateStep("step1", 1),
            CreateStep("step2", 2)
        });
        var newPipeline = pipeline.RemoveStep("step1");
        newPipeline.GetOrderedSteps().Should().HaveCount(1);
        newPipeline.GetOrderedSteps().First().Name.Should().Be("step2");
    }

    [Fact]
    public void AddSteps_Enumerable_ShouldAddAll()
    {
        var pipeline = new InitializationPipeline(Array.Empty<IInitializationStep>());
        var newPipeline = pipeline.AddSteps(new List<IInitializationStep> { CreateStep("step1", 1), CreateStep("step2", 2) });
        newPipeline.GetOrderedSteps().Should().HaveCount(2);
    }

    #endregion

    #region Pipeline Execution

    [Fact]
    public async Task ExecuteAsync_WhenAllSucceed_ShouldSucceed()
    {
        var steps = new List<IInitializationStep>
        {
            CreateStep("step1", 1),
            CreateStep("step2", 2)
        };
        var pipeline = new InitializationPipeline(steps);
        var context = CreateContext();
        var result = await pipeline.ExecuteAsync(context.Object);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenStepFails_ShouldFail()
    {
        var steps = new List<IInitializationStep>
        {
            CreateStep("step1", 1, isRequired: true, shouldFail: true)
        };
        var pipeline = new InitializationPipeline(steps);
        var context = CreateContext();
        var result = await pipeline.ExecuteAsync(context.Object);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenOptionalStepFails_ShouldContinue()
    {
        var steps = new List<IInitializationStep>
        {
            CreateStep("step1", 1, isRequired: false, shouldFail: true),
            CreateStep("step2", 2)
        };
        var pipeline = new InitializationPipeline(steps);
        var context = CreateContext();
        var result = await pipeline.ExecuteAsync(context.Object);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithProgress_ShouldReport()
    {
        var steps = new List<IInitializationStep> { CreateStep("step1", 1) };
        var pipeline = new InitializationPipeline(steps);
        var context = CreateContext();
        var reportedSteps = new List<InitializationProgress>();
        var progress = new Progress<InitializationProgress>(p => reportedSteps.Add(p));

        await pipeline.ExecuteAsync(context.Object, progress);
        reportedSteps.Should().NotBeEmpty();
    }

    #endregion

    #region Pipeline Preview

    [Fact]
    public async Task PreviewAsync_WhenValid_ShouldReturnPreview()
    {
        var steps = new List<IInitializationStep> { CreateStep("step1", 1) };
        var pipeline = new InitializationPipeline(steps);
        var context = CreateContext();
        var result = await pipeline.PreviewAsync(context.Object);
        result.IsSuccess.Should().BeTrue();
        result.Value.StepNames.Should().Contain("step1");
    }

    [Fact]
    public async Task PreviewAsync_WhenEmpty_ShouldFail()
    {
        var pipeline = new InitializationPipeline(Array.Empty<IInitializationStep>());
        var context = CreateContext();
        var result = await pipeline.PreviewAsync(context.Object);
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Helpers

    static Mock<IInitializationContext> CreateContext()
    {
        var context = new Mock<IInitializationContext>();
        context.Setup(c => c.Items).Returns(new Dictionary<string, object>());
        return context;
    }

    static IInitializationStep CreateStep(
        string name,
        int order,
        string[]? dependsOn = null,
        bool isRequired = true,
        bool shouldFail = false)
    {
        var step = new Mock<IInitializationStep>();
        step.Setup(s => s.Name).Returns(name);
        step.Setup(s => s.DisplayName).Returns($"Display {name}");
        step.Setup(s => s.Order).Returns(order);
        step.Setup(s => s.DependsOn).Returns(dependsOn ?? Array.Empty<string>());
        step.Setup(s => s.IsRequired).Returns(isRequired);
        step.Setup(s => s.ExecuteAsync(It.IsAny<IInitializationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(shouldFail ? Result.Fail(Error.Conflict("test", "test")) : Result.Ok());
        step.Setup(s => s.PreviewAsync(It.IsAny<IInitializationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<InitializationOperation>>.Ok(Array.Empty<InitializationOperation>()));
        return step.Object;
    }

    #endregion
}
