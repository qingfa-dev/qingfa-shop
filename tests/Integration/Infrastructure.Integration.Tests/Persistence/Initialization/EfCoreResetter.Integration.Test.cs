using Application.Common.Persistence.Initialization.Resetters;
using Application.Common.Persistence.UnitOfWorks;

using Infrastructure.Persistence.Initialization;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.Schemas.Todos.Seeders;
using Infrastructure.Persistence.EntityFramework.Initialization;

namespace Infrastructure.Integration.Tests.Persistence.Initialization;

/// <summary>
/// Integration tests for <see cref="EfCoreResetter{TContext}"/> against a real
/// PostgreSQL database, including the migration-history contract.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.Initialization.Resetter.PostgreSQL")]
[Collection(PersistenceInitializationCollection.Name)]
public sealed class EfCoreResetterIntegrationTest(PersistenceInitializationFixture fixture) : IAsyncLifetime
{
    private readonly PersistenceInitializationFixture _fixture = fixture;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _fixture.DropDatabaseAsync();
        await _fixture.MigrateAsync();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact(DisplayName = "DataOnly reset should truncate all mapped tables")]
    public async Task ExecuteAsync_DataOnly_ShouldTruncateAllMappedTables()
    {
        // Arrange
        await SeedAsync();
        using var scope = _fixture.CreateScope();
        var resetter = CreateResetter(scope, ResetScope.DataOnly);

        // Act
        var result = await resetter.ExecuteAsync(CreateInitializationContext(scope));

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.CountRowsAsync("todos")).Should().Be(0);
        (await _fixture.CountRowsAsync("todo_lists")).Should().Be(0);
        (await _fixture.CountRowsAsync("todo_categories")).Should().Be(0);
    }

    [Fact(DisplayName = "DataOnly reset should keep the schema and migration history")]
    public async Task ExecuteAsync_DataOnly_ShouldKeepSchemaAndMigrationHistory()
    {
        // Arrange
        await SeedAsync();
        using var scope = _fixture.CreateScope();
        var resetter = CreateResetter(scope, ResetScope.DataOnly);

        // Act
        var result = await resetter.ExecuteAsync(CreateInitializationContext(scope));

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.TableExistsAsync("todos")).Should().BeTrue();
        (await _fixture.TableExistsAsync("__EFMigrationsHistory")).Should().BeTrue();
        (await _fixture.CountRowsAsync("__EFMigrationsHistory")).Should().Be(5);
    }

    [Fact(DisplayName = "DataOnly reset should honor explicit targets")]
    public async Task ExecuteAsync_DataOnly_WithTargets_ShouldOnlyTruncateThoseTables()
    {
        // Arrange
        await SeedAsync();
        using var scope = _fixture.CreateScope();
        var resetter = CreateResetter(scope, ResetScope.DataOnly, targets: ["todos"]);

        // Act
        var result = await resetter.ExecuteAsync(CreateInitializationContext(scope));

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.CountRowsAsync("todos")).Should().Be(0);
        (await _fixture.CountRowsAsync("todo_lists")).Should().Be(3);
        (await _fixture.CountRowsAsync("todo_categories")).Should().Be(4);
    }

    [Fact(DisplayName = "DataOnly reset should succeed on a schema with no tables")]
    public async Task ExecuteAsync_DataOnly_WhenNoTablesExist_ShouldReturnSuccess()
    {
        // Arrange
        await _fixture.ExecuteAsync("DROP SCHEMA todo CASCADE; CREATE SCHEMA todo;");
        using var scope = _fixture.CreateScope();
        var resetter = CreateResetter(scope, ResetScope.DataOnly);

        // Act
        var result = await resetter.ExecuteAsync(CreateInitializationContext(scope));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "DataOnly reset should be idempotent")]
    public async Task ExecuteAsync_DataOnly_WhenAlreadyEmpty_ShouldReturnSuccess()
    {
        // Arrange
        await SeedAsync();
        using var scope = _fixture.CreateScope();
        var resetter = CreateResetter(scope, ResetScope.DataOnly);
        await resetter.ExecuteAsync(CreateInitializationContext(scope));

        // Act
        var result = await resetter.ExecuteAsync(CreateInitializationContext(scope));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "DataOnly reset should reseed cleanly afterwards")]
    public async Task ExecuteAsync_DataOnly_ThenSeed_ShouldRestoreSeedData()
    {
        // Arrange
        await SeedAsync();
        using var scope = _fixture.CreateScope();
        var resetter = CreateResetter(scope, ResetScope.DataOnly);
        await resetter.ExecuteAsync(CreateInitializationContext(scope));

        // Act
        await new TodoCategorySeeder().ExecuteAsync(CreateInitializationContext(scope));
        await new TodoListSeeder().ExecuteAsync(CreateInitializationContext(scope));
        await new TodoSeeder().ExecuteAsync(CreateInitializationContext(scope));

        // Assert
        (await _fixture.CountRowsAsync("todo_categories")).Should().Be(4);
        (await _fixture.CountRowsAsync("todo_lists")).Should().Be(3);
        (await _fixture.CountRowsAsync("todos")).Should().Be(5);
    }

    [Fact(DisplayName = "DataOnly reset should observe cancellation")]
    public async Task ExecuteAsync_DataOnly_WhenCancelled_ShouldReturnCancelledFailure()
    {
        // Arrange
        await SeedAsync();
        using var scope = _fixture.CreateScope();
        var resetter = CreateResetter(scope, ResetScope.DataOnly);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await resetter.ExecuteAsync(CreateInitializationContext(scope), cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Contain("Cancelled");
    }

    [Fact(DisplayName = "SchemaOnly reset should drop all tables and migration history")]
    public async Task ExecuteAsync_SchemaOnly_ShouldDropSchemaObjects()
    {
        // Arrange
        await SeedAsync();
        using var scope = _fixture.CreateScope();
        var resetter = CreateResetter(scope, ResetScope.SchemaOnly);

        // Act
        var result = await resetter.ExecuteAsync(CreateInitializationContext(scope));

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.TableExistsAsync("todos")).Should().BeFalse();
        (await _fixture.TableExistsAsync("todo_lists")).Should().BeFalse();
        (await _fixture.TableExistsAsync("todo_categories")).Should().BeFalse();
        (await _fixture.TableExistsAsync("__EFMigrationsHistory")).Should().BeFalse();
        (await _fixture.CanConnectAsync()).Should().BeTrue();
    }

    [Fact(DisplayName = "SchemaOnly reset should be followed by a successful migration")]
    public async Task ExecuteAsync_SchemaOnly_ThenMigrate_ShouldRecreateSchema()
    {
        // Arrange
        await SeedAsync();
        using var scope = _fixture.CreateScope();
        var resetter = CreateResetter(scope, ResetScope.SchemaOnly);
        await resetter.ExecuteAsync(CreateInitializationContext(scope));

        // Act
        await _fixture.MigrateAsync();

        // Assert
        (await _fixture.TableExistsAsync("todos")).Should().BeTrue();
        (await _fixture.CountRowsAsync("todos")).Should().Be(0);
        (await _fixture.CountRowsAsync("__EFMigrationsHistory")).Should().Be(5);
    }

    [Fact(DisplayName = "SchemaOnly reset should be idempotent")]
    public async Task ExecuteAsync_SchemaOnly_WhenAlreadyEmpty_ShouldReturnSuccess()
    {
        // Arrange
        await _fixture.ExecuteAsync("DROP SCHEMA todo CASCADE; CREATE SCHEMA todo;");
        using var scope = _fixture.CreateScope();
        var resetter = CreateResetter(scope, ResetScope.SchemaOnly);

        // Act
        var result = await resetter.ExecuteAsync(CreateInitializationContext(scope));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Full reset should drop the database")]
    public async Task ExecuteAsync_Full_ShouldDropDatabase()
    {
        // Arrange
        await SeedAsync();
        using var scope = _fixture.CreateScope();
        var resetter = CreateResetter(scope, ResetScope.Full);

        // Act
        var result = await resetter.ExecuteAsync(CreateInitializationContext(scope));

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.CanConnectAsync()).Should().BeFalse();
    }

    [Fact(DisplayName = "Full reset should be followed by a successful migration")]
    public async Task ExecuteAsync_Full_ThenMigrate_ShouldRecreateDatabase()
    {
        // Arrange
        await SeedAsync();
        using var scope = _fixture.CreateScope();
        var resetter = CreateResetter(scope, ResetScope.Full);
        await resetter.ExecuteAsync(CreateInitializationContext(scope));

        // Act
        await _fixture.MigrateAsync();

        // Assert
        (await _fixture.CanConnectAsync()).Should().BeTrue();
        (await _fixture.TableExistsAsync("todos")).Should().BeTrue();
        (await _fixture.CountRowsAsync("__EFMigrationsHistory")).Should().Be(5);
    }

    [Fact(DisplayName = "Resetter should expose explicit targets")]
    public void Constructor_WithTargets_ShouldExposeThem()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("test");

        // Act
        var resetter = new EfCoreResetter<ApplicationDbContext>(
            context, logger, ResetScope.DataOnly, targets: ["todos", "todo_lists"]);

        // Assert
        resetter.Targets.Should().BeEquivalentTo(["todos", "todo_lists"]);
        resetter.Scope.Should().Be(ResetScope.DataOnly);
    }

    private async Task SeedAsync()
    {
        using var scope = _fixture.CreateScope();
        var initContext = CreateInitializationContext(scope);
        await new TodoCategorySeeder().ExecuteAsync(initContext);
        await new TodoListSeeder().ExecuteAsync(initContext);
        await new TodoSeeder().ExecuteAsync(initContext);
    }

    private static EfCoreResetter<ApplicationDbContext> CreateResetter(
        IServiceScope scope,
        ResetScope resetScope,
        string[]? targets = null)
    {
        return new EfCoreResetter<ApplicationDbContext>(
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
            scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("EfCoreResetterTest"),
            resetScope,
            targets: targets);
    }

    private static EfCoreInitializationContext CreateInitializationContext(IServiceScope scope)
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("EfCoreResetterTest");
        return new EfCoreInitializationContext(
            context.Database.GetDbConnection(),
            scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            logger);
    }
}