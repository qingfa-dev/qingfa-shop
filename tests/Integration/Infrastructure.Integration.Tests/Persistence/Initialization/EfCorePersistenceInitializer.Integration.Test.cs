using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Migrations;
using Application.Common.Persistence.Initialization.Resetters;
using Application.Common.Persistence.Initialization.Seeders;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.Initialization;
using Infrastructure.Persistence.Initialization;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Integration.Tests.Persistence.Initialization;

/// <summary>
/// Integration tests for <see cref="EfCorePersistenceInitializer{TContext}"/>,
/// covering every migration/seeder/resetter flag combination and their ordering.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.Initialization.Initializer.PostgreSQL")]
[Collection(PersistenceInitializationCollection.Name)]
public sealed class EfCorePersistenceInitializerIntegrationTest(PersistenceInitializationFixture fixture) : IAsyncLifetime
{
    private readonly PersistenceInitializationFixture _fixture = fixture;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _fixture.DropDatabaseAsync();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact(DisplayName = "InitializeAsync should migrate and seed by default")]
    public async Task InitializeAsync_WithDefaults_ShouldMigrateAndSeed()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var initializer = _fixture.CreateInitializer(scope);

        // Act
        var result = await initializer.InitializeAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.TableExistsAsync("todos")).Should().BeTrue();
        (await _fixture.CountRowsAsync("todo_categories")).Should().Be(4);
        (await _fixture.CountRowsAsync("todo_lists")).Should().Be(3);
        (await _fixture.CountRowsAsync("todos")).Should().Be(5);
    }

    [Fact(DisplayName = "InitializeAsync should skip seeders when runSeeders is false")]
    public async Task InitializeAsync_WithRunSeedersFalse_ShouldOnlyMigrate()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var initializer = _fixture.CreateInitializer(scope);

        // Act
        var result = await initializer.InitializeAsync(runSeeders: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.TableExistsAsync("todos")).Should().BeTrue();
        (await _fixture.CountRowsAsync("todos")).Should().Be(0);
        (await _fixture.CountRowsAsync("todo_categories")).Should().Be(0);
    }

    [Fact(DisplayName = "InitializeAsync should skip migrations when runMigrations is false")]
    public async Task InitializeAsync_WithRunMigrationsFalse_ShouldNotCreateSchema()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var initializer = _fixture.CreateInitializer(scope);

        // Act
        var result = await initializer.InitializeAsync(runMigrations: false, runSeeders: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.TableExistsAsync("todos")).Should().BeFalse();
    }

    [Fact(DisplayName = "InitializeAsync with DataOnly reset should migrate, reset, then seed")]
    public async Task InitializeAsync_WithDataOnlyReset_ShouldLeaveSeedData()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var initializer = _fixture.CreateInitializer(scope);
        await initializer.InitializeAsync();

        // Act
        var result = await initializer.InitializeAsync(
            runMigrations: true,
            runSeeders: true,
            runResetters: true,
            resetScope: ResetScope.DataOnly);

        // Assert
        result.IsSuccess.Should().BeTrue(
            string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Message}")));
        (await _fixture.CountRowsAsync("todo_categories")).Should().Be(4);
        (await _fixture.CountRowsAsync("todo_lists")).Should().Be(3);
        (await _fixture.CountRowsAsync("todos")).Should().Be(5);
        (await _fixture.CountRowsAsync("__EFMigrationsHistory")).Should().Be(7);
    }

    [Fact(DisplayName = "InitializeAsync with SchemaOnly reset should reset, migrate, then seed")]
    public async Task InitializeAsync_WithSchemaOnlyReset_ShouldRebuildAndSeed()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var initializer = _fixture.CreateInitializer(scope);
        await initializer.InitializeAsync();

        // Act
        var result = await initializer.InitializeAsync(
            runMigrations: true,
            runSeeders: true,
            runResetters: true,
            resetScope: ResetScope.SchemaOnly);

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.TableExistsAsync("todos")).Should().BeTrue();
        (await _fixture.CountRowsAsync("__EFMigrationsHistory")).Should().Be(7);
        (await _fixture.CountRowsAsync("todo_categories")).Should().Be(4);
        (await _fixture.CountRowsAsync("todo_lists")).Should().Be(3);
        (await _fixture.CountRowsAsync("todos")).Should().Be(5);
    }

    [Fact(DisplayName = "InitializeAsync with Full reset should drop, migrate, then seed")]
    public async Task InitializeAsync_WithFullReset_ShouldRebuildAndSeed()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var initializer = _fixture.CreateInitializer(scope);
        await initializer.InitializeAsync();

        // Act
        var result = await initializer.InitializeAsync(
            runMigrations: true,
            runSeeders: true,
            runResetters: true,
            resetScope: ResetScope.Full);

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.CanConnectAsync()).Should().BeTrue();
        (await _fixture.CountRowsAsync("__EFMigrationsHistory")).Should().Be(7);
        (await _fixture.CountRowsAsync("todos")).Should().Be(5);
    }

    [Fact(DisplayName = "InitializeAsync should be idempotent")]
    public async Task InitializeAsync_RunTwice_ShouldBeIdempotent()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var initializer = _fixture.CreateInitializer(scope);

        // Act
        var first = await initializer.InitializeAsync();
        var second = await initializer.InitializeAsync();

        // Assert
        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        (await _fixture.CountRowsAsync("todo_categories")).Should().Be(4);
        (await _fixture.CountRowsAsync("todo_lists")).Should().Be(3);
        (await _fixture.CountRowsAsync("todos")).Should().Be(5);
    }

    [Fact(DisplayName = "InitializeAsync should report completed progress for each step")]
    public async Task InitializeAsync_WithProgress_ShouldReportCompletedSteps()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var initializer = _fixture.CreateInitializer(scope);
        var reports = new List<InitializationProgress>();
        var progress = new Progress<InitializationProgress>(reports.Add);

        // Act
        var result = await initializer.InitializeAsync(progress: progress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        reports.Should().NotBeEmpty();
        reports.Should().OnlyContain(report => report.TotalSteps == 7);
        reports.Select(report => report.CurrentStepName)
            .Should().Contain("migrator:efcore")
            .And.Contain("seeder:identity-roles")
            .And.Contain("seeder:identity-users")
            .And.Contain("seeder:todo-categories")
            .And.Contain("seeder:todo-lists")
            .And.Contain("seeder:todos")
            .And.Contain("seeder:todo-backfill");
    }

    [Fact(DisplayName = "InitializeAsync should fail without throwing when cancelled")]
    public async Task InitializeAsync_WhenCancelled_ShouldReturnFailure()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var initializer = _fixture.CreateInitializer(scope);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await initializer.InitializeAsync(cancellationToken: cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Fact(DisplayName = "GetPipeline should order the migrator before the seeders")]
    public void GetPipeline_ShouldOrderMigratorBeforeSeeders()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var initializer = _fixture.CreateInitializer(scope);

        // Act
        var steps = initializer.GetPipeline().GetOrderedSteps();

        // Assert
        steps.Should().HaveCount(7);
        steps[0].Should().BeAssignableTo<IPersistenceMigrator>();
        steps.Skip(1).Should().AllBeAssignableTo<IPersistenceSeeder>();
        steps.Select(step => step.Name).Should().ContainInOrder(
            "migrator:efcore",
            "seeder:todo-categories",
            "seeder:todo-lists",
            "seeder:identity-roles",
            "seeder:todos",
            "seeder:todo-backfill",
            "seeder:identity-users");
    }

    [Fact(DisplayName = "GetPipeline preview should describe migrations and seed inserts")]
    public async Task GetPipeline_Preview_ShouldDescribeMigrationsAndSeedInsert()
    {
        // Arrange
        await _fixture.MigrateAsync();
        using var scope = _fixture.CreateScope();
        var initializer = _fixture.CreateInitializer(scope);
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var initContext = new EfCoreInitializationContext(
            context.Database.GetDbConnection(),
            scope.ServiceProvider.GetRequiredService<Application.Common.Persistence.UnitOfWorks.IUnitOfWork>(),
            scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()
                .CreateLogger("InitializerPreviewTest"));

        // Act
        var result = await initializer.GetPipeline().PreviewAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StepNames.Should().Equal(
            "migrator:efcore",
            "seeder:todo-categories",
            "seeder:todo-lists",
            "seeder:identity-roles",
            "seeder:todos",
            "seeder:todo-backfill",
            "seeder:identity-users");
        result.Value.Operations.Should().Contain(op => op.Type == InitializationOperationType.SchemaCheck);
        result.Value.Operations.Should().Contain(op => op.Type == InitializationOperationType.DataChange);
    }
}
