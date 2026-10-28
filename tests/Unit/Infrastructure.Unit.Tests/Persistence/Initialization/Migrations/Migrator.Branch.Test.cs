using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Migrations;
using Application.Common.Persistence.UnitOfWorks;

using Infrastructure.Persistence.EntityFramework.Initialization;
using Infrastructure.Persistence.Initialization;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

using Npgsql;

namespace Infrastructure.Unit.Tests.Persistence.Initialization;

/// <summary>
/// Branch tests for <see cref="EfCoreMigrator{TContext}"/> using a disposable SQLite
/// database that actually has a migration.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.Initialization.Migrations")]
public sealed class EfCoreMigratorBranchTest : IAsyncLifetime
{
    private string _databasePath = string.Empty;

    /// <inheritdoc />
    public ValueTask InitializeAsync()
    {
        _databasePath = $"Data Source=MigratorBranch_{Guid.NewGuid():N}.db";
        return default;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        var options = new DbContextOptionsBuilder<MigratableDbContext>()
            .UseSqlite(_databasePath)
            .Options;
        await using var context = new MigratableDbContext(options);
        await context.Database.EnsureDeletedAsync();
    }

    private MigratableDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<MigratableDbContext>()
            .UseSqlite(_databasePath)
            .Options);

    private static EfCoreInitializationContext CreateInitializationContext() =>
        new(
            new Mock<System.Data.IDbConnection>().Object,
            new Mock<IUnitOfWork>().Object,
            new Mock<ILogger>().Object);

    [Fact(DisplayName = "Version should return the newest migration identifier")]
    public void Version_WithMigrations_ShouldReturnNewestIdentifier()
    {
        using var context = CreateContext();
        var migrator = new EfCoreMigrator<MigratableDbContext>(
            context, new Mock<ILogger>().Object);

        migrator.Version.Should().Be(MigratableDbContextMigration.Id);
    }

    [Fact(DisplayName = "ExecuteAsync should apply pending migrations")]
    public async Task ExecuteAsync_WithPendingMigrations_ShouldApplyThem()
    {
        await using var context = CreateContext();
        var migrator = new EfCoreMigrator<MigratableDbContext>(
            context, new Mock<ILogger>().Object);

        Result result = await migrator.ExecuteAsync(CreateInitializationContext());

        result.IsSuccess.Should().BeTrue();
        (await context.Database.GetAppliedMigrationsAsync()).Should()
            .Contain(MigratableDbContextMigration.Id);
    }

    [Fact(DisplayName = "PreviewAsync should describe every pending migration")]
    public async Task PreviewAsync_WithPendingMigrations_ShouldDescribeThem()
    {
        await using var context = CreateContext();
        var migrator = new EfCoreMigrator<MigratableDbContext>(
            context, new Mock<ILogger>().Object);

        Result<IReadOnlyList<InitializationOperation>> result =
            await migrator.PreviewAsync(CreateInitializationContext());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Type.Should().Be(InitializationOperationType.SchemaChange);
        result.Value[0].CommandText.Should().Be(MigratableDbContextMigration.Id);
        result.Value[0].Description.Should().Contain("Apply migration");
    }

    [Fact(DisplayName = "ExecuteAsync should return a failure for a non-transient migration error")]
    public async Task ExecuteAsync_WhenMigrationFails_ShouldReturnFailure()
    {
        await using var context = CreateContext();
        await context.Database.OpenConnectionAsync();
        await using (var command = context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "CREATE TABLE \"entities\" (\"Id\" INTEGER NOT NULL PRIMARY KEY);";
            await command.ExecuteNonQueryAsync();
        }

        var migrator = new EfCoreMigrator<MigratableDbContext>(
            context, new Mock<ILogger>().Object);

        Result result = await migrator.ExecuteAsync(CreateInitializationContext());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == MigrationOutcome.Failure.MigrationFailed("test").Code);
    }

    [Fact(DisplayName = "ExecuteAsync should retry a transient migration failure")]
    public async Task ExecuteAsync_WhenMigrationIsTransient_ShouldRetryAndFail()
    {
        await using var context = CreateTransientContext();
        var migrator = new EfCoreMigrator<TransientFailDbContext>(
            context, new Mock<ILogger>().Object, maxRetryAttempts: 1);

        Result result = await migrator.ExecuteAsync(CreateInitializationContext());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == MigrationOutcome.Failure.MigrationFailed("test").Code);
    }

    [Fact(DisplayName = "ExecuteAsync should translate cancellation during backoff")]
    public async Task ExecuteAsync_WhenCancelledDuringBackoff_ShouldReturnCancelled()
    {
        await using var context = CreateTransientContext();
        var migrator = new EfCoreMigrator<TransientFailDbContext>(
            context, new Mock<ILogger>().Object, maxRetryAttempts: 5);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));

        Result result = await migrator.ExecuteAsync(
            CreateInitializationContext(),
            cancellation.Token);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == MigrationOutcome.Failure.Cancelled("test").Code);
    }

    [Fact(DisplayName = "ExecuteAsync should translate an already-cancelled token")]
    public async Task ExecuteAsync_WhenAlreadyCancelled_ShouldReturnCancelled()
    {
        await using var context = CreateContext();
        var migrator = new EfCoreMigrator<MigratableDbContext>(
            context, new Mock<ILogger>().Object);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        Result result = await migrator.ExecuteAsync(
            CreateInitializationContext(),
            cancellation.Token);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == MigrationOutcome.Failure.Cancelled("test").Code);
    }

    [Fact(DisplayName = "PreviewAsync should translate an already-cancelled token")]
    public async Task PreviewAsync_WhenAlreadyCancelled_ShouldReturnCancelled()
    {
        await using var context = CreateContext();
        var migrator = new EfCoreMigrator<MigratableDbContext>(
            context, new Mock<ILogger>().Object);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        Result<IReadOnlyList<InitializationOperation>> result =
            await migrator.PreviewAsync(CreateInitializationContext(), cancellation.Token);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == MigrationOutcome.Failure.Cancelled("test").Code);
    }

    [Theory(DisplayName = "IsTransient should recognize PostgreSQL concurrency failures")]
    [InlineData(PostgresErrorCodes.DeadlockDetected)]
    [InlineData(PostgresErrorCodes.SerializationFailure)]
    public void IsTransient_WithPostgresConcurrencyFailure_ShouldReturnTrue(string sqlState)
    {
        using var context = CreateContext();
        var migrator = new EfCoreMigrator<MigratableDbContext>(
            context, new Mock<ILogger>().Object);
        var exception = new PostgresException("concurrency", "ERROR", "ERROR", sqlState);

        migrator.IsTransient(exception).Should().BeTrue();
    }

    [Fact(DisplayName = "IsTransient should inspect the inner exception chain")]
    public void IsTransient_WithWrappedTimeout_ShouldReturnTrue()
    {
        using var context = CreateContext();
        var migrator = new EfCoreMigrator<MigratableDbContext>(
            context, new Mock<ILogger>().Object);
        var exception = new InvalidOperationException("wrapped", new TimeoutException());

        migrator.IsTransient(exception).Should().BeTrue();
    }

    private TransientFailDbContext CreateTransientContext() =>
        new(new DbContextOptionsBuilder<TransientFailDbContext>()
            .UseSqlite($"Data Source=MigratorTransient_{Guid.NewGuid():N}.db")
            .Options);
}

/// <summary>Context with one applicable migration.</summary>
public sealed class MigratableDbContext(DbContextOptions<MigratableDbContext> options)
    : DbContext(options)
{
    /// <summary>The migration identifier used by the tests.</summary>
    public const string MigrationId = "20260101000000_Initial";
}

/// <summary>The initial migration for <see cref="MigratableDbContext"/>.</summary>
[DbContext(typeof(MigratableDbContext))]
[Migration(MigratableDbContext.MigrationId)]
public sealed class MigratableDbContextMigration : Migration
{
    /// <summary>The migration identifier used by the tests.</summary>
    public const string Id = MigratableDbContext.MigrationId;

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "entities",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_entities", entity => entity.Id));
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "entities");
}

/// <summary>Context whose only migration always throws a transient failure.</summary>
public sealed class TransientFailDbContext(DbContextOptions<TransientFailDbContext> options)
    : DbContext(options);

/// <summary>A migration that raises a transient timeout.</summary>
[DbContext(typeof(TransientFailDbContext))]
[Migration("20260101000001_Transient")]
public sealed class TransientFailMigration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) =>
        throw new TimeoutException("transient migration failure");

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
