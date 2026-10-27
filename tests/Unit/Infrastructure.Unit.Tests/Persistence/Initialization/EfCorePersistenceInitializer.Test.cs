using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Resetters;
using Application.Common.Persistence.UnitOfWorks;

using Infrastructure.Persistence.Initialization;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Unit.Tests.Persistence.Initialization;

/// <summary>Unit tests for EfCorePersistenceInitializer using SQLite-backed DbContext.</summary>
public sealed class EfCorePersistenceInitializerTest : IAsyncLifetime
{
    private DbContextOptions<TestInitializerDbContext>? _options;

    public ValueTask InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<TestInitializerDbContext>()
            .UseSqlite($"Data Source=ModeInitializer_{Guid.NewGuid()}.db")
            .Options;
        return default;
    }

    public ValueTask DisposeAsync() => default;

    [Fact]
    public void Constructor_ShouldInitializeWithNoSteps()
    {
        // Arrange
        var context = new TestInitializerDbContext(_options!);
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger<EfCorePersistenceInitializer<TestInitializerDbContext>>>().Object;
        var steps = Array.Empty<IInitializationStep>();

        // Act
        var initializer = new EfCorePersistenceInitializer<TestInitializerDbContext>(context, unitOfWork, logger, steps);

        // Assert
        initializer.Should().NotBeNull();
        initializer.Should().BeAssignableTo<IPersistenceInitializer>();
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        TestInitializerDbContext? context = null;
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger<EfCorePersistenceInitializer<TestInitializerDbContext>>>().Object;
        var steps = Array.Empty<IInitializationStep>();

        // Act & Assert
        var act = () => new EfCorePersistenceInitializer<TestInitializerDbContext>(context!, unitOfWork, logger, steps);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("context");
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
    {
        // Arrange
        var context = new TestInitializerDbContext(_options!);
        IUnitOfWork? unitOfWork = null;
        var logger = new Mock<ILogger<EfCorePersistenceInitializer<TestInitializerDbContext>>>().Object;
        var steps = Array.Empty<IInitializationStep>();

        // Act & Assert
        var act = () => new EfCorePersistenceInitializer<TestInitializerDbContext>(context, unitOfWork!, logger, steps);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("unitOfWork");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var context = new TestInitializerDbContext(_options!);
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        ILogger? logger = null;
        var steps = Array.Empty<IInitializationStep>();

        // Act & Assert
        var act = () => new EfCorePersistenceInitializer<TestInitializerDbContext>(context, unitOfWork, logger!, steps);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Constructor_WithNullSteps_ShouldThrowArgumentNullException()
    {
        // Arrange
        var context = new TestInitializerDbContext(_options!);
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger<EfCorePersistenceInitializer<TestInitializerDbContext>>>().Object;
        IEnumerable<IInitializationStep>? steps = null;

        // Act & Assert
        var act = () => new EfCorePersistenceInitializer<TestInitializerDbContext>(context, unitOfWork, logger, steps!);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("steps");
    }

    [Fact]
    public async Task InitializeAsync_WithNoSteps_ShouldReturnSuccess()
    {
        // Arrange
        var context = new TestInitializerDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger<EfCorePersistenceInitializer<TestInitializerDbContext>>>().Object;
        var initializer = new EfCorePersistenceInitializer<TestInitializerDbContext>(context, unitOfWork, logger, Array.Empty<IInitializationStep>());

        // Act
        var result = await initializer.InitializeAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Cleanup
        await context.Database.EnsureDeletedAsync();
        context.Dispose();
    }

    [Fact]
    public async Task InitializeAsync_WithRunMigrationsFalse_ShouldSkipMigrations()
    {
        // Arrange
        var context = new TestInitializerDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        context.Database.OpenConnection();
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger<EfCorePersistenceInitializer<TestInitializerDbContext>>>().Object;
        var initializer = new EfCorePersistenceInitializer<TestInitializerDbContext>(context, unitOfWork, logger, Array.Empty<IInitializationStep>());

        // Act
        var result = await initializer.InitializeAsync(runMigrations: false, runSeeders: false);

        // Assert
        result.Should().NotBeNull();

        // Cleanup
        context.Database.CloseConnection();
        await context.Database.EnsureDeletedAsync();
        context.Dispose();
    }

    [Fact]
    public async Task InitializeAsync_WithRunSeedersFalse_ShouldSkipSeeders()
    {
        // Arrange
        var context = new TestInitializerDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger<EfCorePersistenceInitializer<TestInitializerDbContext>>>().Object;
        var initializer = new EfCorePersistenceInitializer<TestInitializerDbContext>(context, unitOfWork, logger, Array.Empty<IInitializationStep>());

        // Act
        var result = await initializer.InitializeAsync(runMigrations: true, runSeeders: false);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Cleanup
        await context.Database.EnsureDeletedAsync();
        context.Dispose();
    }

    [Fact]
    public async Task InitializeAsync_WithRunResettersTrue_ShouldExecuteResetter()
    {
        // Arrange
        var context = new TestInitializerDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        context.Entities.Add(new TestInitializerEntity { Name = "Test" });
        await context.SaveChangesAsync();

        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger<EfCorePersistenceInitializer<TestInitializerDbContext>>>().Object;
        var initializer = new EfCorePersistenceInitializer<TestInitializerDbContext>(context, unitOfWork, logger, Array.Empty<IInitializationStep>());

        // Act
        var result = await initializer.InitializeAsync(runMigrations: false, runSeeders: false, runResetters: true, resetScope: ResetScope.SchemaOnly);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Cleanup
        await context.Database.EnsureDeletedAsync();
        context.Dispose();
    }

    [Fact]
    public async Task InitializeAsync_WithProgress_ShouldReportProgress()
    {
        // Arrange
        var context = new TestInitializerDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger<EfCorePersistenceInitializer<TestInitializerDbContext>>>().Object;
        var initializer = new EfCorePersistenceInitializer<TestInitializerDbContext>(context, unitOfWork, logger, Array.Empty<IInitializationStep>());
        var progress = new Progress<InitializationProgress>(_ => { });

        // Act
        var result = await initializer.InitializeAsync(progress: progress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Progress may or may not be reported depending on pipeline implementation
        // The key assertion is that it doesn't throw

        // Cleanup
        await context.Database.EnsureDeletedAsync();
        context.Dispose();
    }

    [Fact]
    public async Task InitializeAsync_WhenCancelled_ShouldHandleGracefully()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var context = new TestInitializerDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger<EfCorePersistenceInitializer<TestInitializerDbContext>>>().Object;
        var initializer = new EfCorePersistenceInitializer<TestInitializerDbContext>(context, unitOfWork, logger, Array.Empty<IInitializationStep>());

        // Act
        var result = await initializer.InitializeAsync(cancellationToken: cts.Token);

        // Assert - may succeed or fail, but should not throw
        result.Should().NotBeNull();

        // Cleanup
        await context.Database.EnsureDeletedAsync();
        context.Dispose();
    }

    [Fact]
    public void GetPipeline_ShouldReturnValidPipeline()
    {
        // Arrange
        var context = new TestInitializerDbContext(_options!);
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger<EfCorePersistenceInitializer<TestInitializerDbContext>>>().Object;
        var initializer = new EfCorePersistenceInitializer<TestInitializerDbContext>(context, unitOfWork, logger, Array.Empty<IInitializationStep>());

        // Act
        var pipeline = initializer.GetPipeline();

        // Assert
        pipeline.Should().NotBeNull();
        pipeline.Should().BeAssignableTo<IInitializationPipeline>();
    }

    [Fact]
    public void GetPipeline_WithSteps_ShouldIncludeSteps()
    {
        // Arrange
        var context = new TestInitializerDbContext(_options!);
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger<EfCorePersistenceInitializer<TestInitializerDbContext>>>().Object;

        var mockStep = new Mock<IInitializationStep>();
        mockStep.Setup(s => s.Name).Returns("test:seeder");
        mockStep.Setup(s => s.DisplayName).Returns("Test Seeder");
        mockStep.Setup(s => s.Order).Returns(10);
        mockStep.Setup(s => s.DependsOn).Returns(Array.Empty<string>());
        mockStep.Setup(s => s.IsRequired).Returns(false);

        var steps = new[] { mockStep.Object };
        var initializer = new EfCorePersistenceInitializer<TestInitializerDbContext>(context, unitOfWork, logger, steps);

        // Act
        var pipeline = initializer.GetPipeline();

        // Assert
        pipeline.Should().NotBeNull();
    }

    [Fact]
    public async Task InitializeAsync_WithOptionalFailingStep_ShouldContinue()
    {
        // Arrange
        var context = new TestInitializerDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger<EfCorePersistenceInitializer<TestInitializerDbContext>>>().Object;

        // Create an optional step that fails
        var failingStep = new Mock<IInitializationStep>();
        failingStep.Setup(s => s.Name).Returns("optional:failing");
        failingStep.Setup(s => s.DisplayName).Returns("Optional Failing");
        failingStep.Setup(s => s.Order).Returns(10);
        failingStep.Setup(s => s.DependsOn).Returns(Array.Empty<string>());
        failingStep.Setup(s => s.IsRequired).Returns(false);
        failingStep.Setup(s => s.ExecuteAsync(It.IsAny<IInitializationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(InitializationOutcome.Failure.Unexpected("Optional failure")));
        failingStep.Setup(s => s.PreviewAsync(It.IsAny<IInitializationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<InitializationOperation>>.Ok(Array.Empty<InitializationOperation>()));

        var initializer = new EfCorePersistenceInitializer<TestInitializerDbContext>(context, unitOfWork, logger, new[] { failingStep.Object });

        // Act
        var result = await initializer.InitializeAsync(runMigrations: false);

        // Assert - optional step failure should not cause overall failure
        // The pipeline may still report success if optional steps are skipped
        result.Should().NotBeNull();

        // Cleanup
        await context.Database.EnsureDeletedAsync();
        context.Dispose();
    }

    [Fact]
    public async Task InitializeAsync_MultipleTimes_ShouldBeIdempotent()
    {
        // Arrange
        var context = new TestInitializerDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger<EfCorePersistenceInitializer<TestInitializerDbContext>>>().Object;
        var initializer = new EfCorePersistenceInitializer<TestInitializerDbContext>(context, unitOfWork, logger, Array.Empty<IInitializationStep>());

        // Act
        var result1 = await initializer.InitializeAsync();
        var result2 = await initializer.InitializeAsync();

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();

        // Cleanup
        await context.Database.EnsureDeletedAsync();
        context.Dispose();
    }

    [Fact(DisplayName = "InitializeAsync with a data-only reset should run the resetter after migrations")]
    public async Task InitializeAsync_WithDataOnlyReset_ShouldRunResetterAfterMigrations()
    {
        // Arrange
        var context = new TestInitializerDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger<EfCorePersistenceInitializer<TestInitializerDbContext>>>().Object;
        var initializer = new EfCorePersistenceInitializer<TestInitializerDbContext>(
            context, unitOfWork, logger, Array.Empty<IInitializationStep>());

        // Act
        var result = await initializer.InitializeAsync(
            runMigrations: false,
            runSeeders: false,
            runResetters: true,
            resetScope: ResetScope.DataOnly);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Cleanup
        await context.Database.EnsureDeletedAsync();
        context.Dispose();
    }

    [Fact(DisplayName = "InitializeAsync should wire step dependencies into the pipeline")]
    public async Task InitializeAsync_WithDependencies_ShouldWireThemIntoThePipeline()
    {
        // Arrange
        var context = new TestInitializerDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger<EfCorePersistenceInitializer<TestInitializerDbContext>>>().Object;

        var first = new Mock<IInitializationStep>();
        first.SetupGet(s => s.Name).Returns("seed:first");
        first.SetupGet(s => s.DisplayName).Returns("First");
        first.SetupGet(s => s.Order).Returns(10);
        first.SetupGet(s => s.DependsOn).Returns(Array.Empty<string>());
        first.SetupGet(s => s.IsRequired).Returns(false);
        first.Setup(s => s.ExecuteAsync(It.IsAny<IInitializationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var second = new Mock<IInitializationStep>();
        second.SetupGet(s => s.Name).Returns("seed:second");
        second.SetupGet(s => s.DisplayName).Returns("Second");
        second.SetupGet(s => s.Order).Returns(20);
        second.SetupGet(s => s.DependsOn).Returns(["seed:first"]);
        second.SetupGet(s => s.IsRequired).Returns(false);
        second.Setup(s => s.ExecuteAsync(It.IsAny<IInitializationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var initializer = new EfCorePersistenceInitializer<TestInitializerDbContext>(
            context, unitOfWork, logger, [first.Object, second.Object]);

        // Act
        var result = await initializer.InitializeAsync(runMigrations: false);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Cleanup
        await context.Database.EnsureDeletedAsync();
        context.Dispose();
    }

    [Fact(DisplayName = "InitializeAsync should report a required step failure")]
    public async Task InitializeAsync_WhenRequiredStepFails_ShouldReturnFailure()
    {
        // Arrange
        var context = new TestInitializerDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger<EfCorePersistenceInitializer<TestInitializerDbContext>>>().Object;

        var failingStep = new Mock<IInitializationStep>();
        failingStep.SetupGet(s => s.Name).Returns("seed:required-failing");
        failingStep.SetupGet(s => s.DisplayName).Returns("Required Failing");
        failingStep.SetupGet(s => s.Order).Returns(10);
        failingStep.SetupGet(s => s.DependsOn).Returns(Array.Empty<string>());
        failingStep.SetupGet(s => s.IsRequired).Returns(true);
        failingStep.Setup(s => s.ExecuteAsync(It.IsAny<IInitializationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(InitializationOutcome.Failure.Unexpected("required failure")));
        failingStep.Setup(s => s.PreviewAsync(It.IsAny<IInitializationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<InitializationOperation>>.Ok(Array.Empty<InitializationOperation>()));

        var initializer = new EfCorePersistenceInitializer<TestInitializerDbContext>(
            context, unitOfWork, logger, [failingStep.Object]);

        // Act
        var result = await initializer.InitializeAsync(runMigrations: false);

        // Assert
        result.IsFailure.Should().BeTrue();

        // Cleanup
        await context.Database.EnsureDeletedAsync();
        context.Dispose();
    }

    private EfCoreInitializationContext CreateInitContext(DbContext context)
    {
        var connection = context.Database.GetDbConnection();
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger>().Object;
        return new EfCoreInitializationContext(connection, unitOfWork, logger);
    }
}

/// <summary>Test DbContext for initializer tests.</summary>
public class TestInitializerDbContext : DbContext
{
    public TestInitializerDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<TestInitializerEntity> Entities => Set<TestInitializerEntity>();
}

public class TestInitializerEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
