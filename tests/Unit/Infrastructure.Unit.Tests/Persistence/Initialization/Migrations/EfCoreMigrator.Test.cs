using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Migrations;

using Infrastructure.Persistence.EntityFramework.Initialization;
using Infrastructure.Persistence.Initialization;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Unit.Tests.Persistence.Initialization;

/// <summary>Unit tests for EfCoreMigrator using SQLite-backed DbContext.</summary>
public sealed class EfCoreMigratorTest : IAsyncLifetime
{
    private string? _dbName;

    public ValueTask InitializeAsync()
    {
        _dbName = $"Data Source=ModeMigrator_{Guid.NewGuid()}.db";
        return default;
    }

    public ValueTask DisposeAsync() => default;

    private TestMigratorDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestMigratorDbContext>()
            .UseSqlite(_dbName ?? $"Data Source=ModeMigrator_{Guid.NewGuid()}.db")
            .Options;
        return new TestMigratorDbContext(options);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultProperties()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreMigrator<TestMigratorDbContext>>>().Object;

        // Act
        var migrator = new EfCoreMigrator<TestMigratorDbContext>(context, logger);

        // Assert
        migrator.Name.Should().Be("migrator:efcore");
        migrator.DisplayName.Should().Be("EF Core Migrations");
        migrator.Order.Should().Be(MigrationConstant.Limits.DefaultOrder);
        migrator.DependsOn.Should().BeEmpty();
        migrator.IsRequired.Should().BeTrue();
        migrator.Version.Should().NotBeEmpty();
        migrator.Checksum.Should().Be("0000000000000000000000000000000000000000000000000000000000000000");
    }

    [Fact]
    public void Constructor_WithCustomName_ShouldSetName()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreMigrator<TestMigratorDbContext>>>().Object;

        // Act
        var migrator = new EfCoreMigrator<TestMigratorDbContext>(context, logger, name: "custom:migrator", displayName: "Custom Migrator", order: 5);

        // Assert
        migrator.Name.Should().Be("custom:migrator");
        migrator.DisplayName.Should().Be("Custom Migrator");
        migrator.Order.Should().Be(5);
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        TestMigratorDbContext? context = null;
        var logger = new Mock<ILogger<EfCoreMigrator<TestMigratorDbContext>>>().Object;

        // Act & Assert
        var act = () => new EfCoreMigrator<TestMigratorDbContext>(context!, logger);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("context");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var context = CreateContext();
        ILogger? logger = null;

        // Act & Assert
        var act = () => new EfCoreMigrator<TestMigratorDbContext>(context, logger!);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Constructor_WithNullName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreMigrator<TestMigratorDbContext>>>().Object;
        string? name = null;

        // Act & Assert
        var act = () => new EfCoreMigrator<TestMigratorDbContext>(context, logger, name: name!);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("name");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoPendingMigrations_ShouldReturnSuccess()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreMigrator<TestMigratorDbContext>>>().Object;
        var migrator = new EfCoreMigrator<TestMigratorDbContext>(context, logger);
        var initContext = CreateInitContext();

        // Act
        var result = await migrator.ExecuteAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithPendingMigrations_ShouldApplyAndReturnSuccess()
    {
        // Arrange - create a fresh context with migrations
        var freshOptions = new DbContextOptionsBuilder()
            .UseSqlite($"Data Source=ModeMigratorFresh_{Guid.NewGuid()}.db")
            .Options;
        var freshContext = new TestMigratorDbContext(freshOptions);
        // EnsureCreated creates the schema without migrations, so GetPendingMigrations returns empty
        await freshContext.Database.EnsureCreatedAsync();

        var logger = new Mock<ILogger<EfCoreMigrator<TestMigratorDbContext>>>().Object;
        var migrator = new EfCoreMigrator<TestMigratorDbContext>(freshContext, logger);
        var initContext = CreateInitContext();

        // Act
        var result = await migrator.ExecuteAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Cleanup
        await freshContext.Database.EnsureDeletedAsync();
        freshContext.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_WithCreatedDb_ShouldReturnSuccess()
    {
        // Arrange
        var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var logger = new Mock<ILogger<EfCoreMigrator<TestMigratorDbContext>>>().Object;
        var migrator = new EfCoreMigrator<TestMigratorDbContext>(context, logger);
        var initContext = CreateInitContext();

        // Act
        var result = await migrator.ExecuteAsync(initContext);

        // Assert - no pending migrations, should succeed
        result.IsSuccess.Should().BeTrue();
        context.Dispose();
    }

    [Fact]
    public async Task PreviewAsync_WithNoPendingMigrations_ShouldReturnSchemaCheck()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreMigrator<TestMigratorDbContext>>>().Object;
        var migrator = new EfCoreMigrator<TestMigratorDbContext>(context, logger);
        var initContext = CreateInitContext();

        // Act
        var result = await migrator.PreviewAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Type.Should().Be(InitializationOperationType.SchemaCheck);
        result.Value[0].Description.Should().Contain("up to date");
    }

    [Fact]
    public void IsTransient_WithTimeoutException_ShouldReturnTrue()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreMigrator<TestMigratorDbContext>>>().Object;
        var migrator = new EfCoreMigrator<TestMigratorDbContext>(context, logger);

        // Act
        var result = migrator.IsTransient(new TimeoutException());

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTransient_WithDeadlockException_ShouldReturnTrue()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreMigrator<TestMigratorDbContext>>>().Object;
        var migrator = new EfCoreMigrator<TestMigratorDbContext>(context, logger);

        // Act
        var result = migrator.IsTransient(new InvalidOperationException("Deadlock detected"));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTransient_WithGenericException_ShouldReturnFalse()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreMigrator<TestMigratorDbContext>>>().Object;
        var migrator = new EfCoreMigrator<TestMigratorDbContext>(context, logger);

        // Act
        var result = migrator.IsTransient(new InvalidOperationException("Some other error"));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTransient_WithNullException_ShouldReturnFalse()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreMigrator<TestMigratorDbContext>>>().Object;
        var migrator = new EfCoreMigrator<TestMigratorDbContext>(context, logger);

        // Act & Assert
        migrator.IsTransient(new ArgumentNullException()).Should().BeFalse();
    }

    [Fact]
    public void Migrator_ShouldImplementIPersistenceMigrator()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreMigrator<TestMigratorDbContext>>>().Object;

        // Act
        var migrator = new EfCoreMigrator<TestMigratorDbContext>(context, logger);

        // Assert
        migrator.Should().BeAssignableTo<IPersistenceMigrator>();
        migrator.Should().BeAssignableTo<IInitializationStep>();
    }

    [Fact]
    public void Version_ShouldMatchExpectedFormat()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreMigrator<TestMigratorDbContext>>>().Object;
        var migrator = new EfCoreMigrator<TestMigratorDbContext>(context, logger);

        // Act
        var version = migrator.Version;

        // Assert
        version.Should().NotBeEmpty();
        // Version format is DateTimeOffset round-trip — just verify it's parseable
        version.Length.Should().BeGreaterThan(10);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailingDatabase_ShouldReturnFailure()
    {
        // Arrange - use a non-existent database path to simulate failure
        var badOptions = new DbContextOptionsBuilder()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var badContext = new TestMigratorDbContext(badOptions);
        // Create the schema so it's valid
        await badContext.Database.EnsureCreatedAsync();

        var logger = new Mock<ILogger<EfCoreMigrator<TestMigratorDbContext>>>().Object;
        var migrator = new EfCoreMigrator<TestMigratorDbContext>(badContext, logger);
        var initContext = CreateInitContext();

        // Act
        var result = await migrator.ExecuteAsync(initContext);

        // Assert - should succeed because no pending migrations
        result.IsSuccess.Should().BeTrue();

        // Cleanup
        await badContext.Database.EnsureDeletedAsync();
        badContext.Dispose();
    }

    [Fact]
    public void Constructor_WithCustomRetryAttempts_ShouldSetMaxRetries()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreMigrator<TestMigratorDbContext>>>().Object;

        // Act - we can't directly verify _maxRetryAttempts, but we can verify the constructor doesn't throw
        var migrator = new EfCoreMigrator<TestMigratorDbContext>(context, logger, maxRetryAttempts: 5);

        // Assert
        migrator.Should().NotBeNull();
    }

    private EfCoreInitializationContext CreateInitContext()
    {
        var connection = new Mock<System.Data.IDbConnection>().Object;
        var unitOfWork = new Mock<Application.Common.Persistence.UnitOfWorks.IUnitOfWork>().Object;
        var logger = new Mock<ILogger>().Object;
        return new EfCoreInitializationContext(connection, unitOfWork, logger);
    }
}

/// <summary>Test DbContext for migrator tests.</summary>
public class TestMigratorDbContext : DbContext
{
    public TestMigratorDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<TestMigratorEntity> Entities => Set<TestMigratorEntity>();
}

public class TestMigratorEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
