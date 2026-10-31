using System.Data;
using System.Data.Common;

using Application.BuildingBlocks.Persistence;
using Application.BuildingBlocks.Persistence.Initialization.Resetters;
using Application.BuildingBlocks.Persistence.UnitOfWorks;
using Application.Modules.Persistence;
using Application.Modules.Todos.Persistence;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Initialization;
using Infrastructure.BuildingBlocks.Adapters.Persistence.Initialization;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.Initialization.Resetters;

/// <summary>
/// Data-only reset tests on SQLite, covering table discovery, target resolution,
/// deletion, and the empty-schema short circuit.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class EfCoreResetterDataTest : IAsyncLifetime
{
    private static readonly IPersistenceSchemaCatalog s_schemaCatalog =
        new ApplicationPersistenceSchemaCatalog(
            [new PlatformPersistenceSchemaCatalog(), new TodoPersistenceSchemaCatalog()]);

    private string _databasePath = string.Empty;

    /// <inheritdoc />
    public ValueTask InitializeAsync()
    {
        _databasePath = $"Data Source=ResetterData_{Guid.NewGuid():N}.db";
        return default;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        var options = new DbContextOptionsBuilder<TestResetterDbContext>()
            .UseSqlite(_databasePath)
            .Options;
        await using var context = new TestResetterDbContext(options);
        await context.Database.EnsureDeletedAsync();
    }

    private TestResetterDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestResetterDbContext>()
            .UseSqlite(_databasePath)
            .Options;
        return new TestResetterDbContext(options);
    }

    private static void Execute(DbContext context, string sql)
    {
        var connection = context.Database.GetDbConnection();
        bool opened = connection.State != ConnectionState.Open;
        if (opened)
        {
            connection.Open();
        }

        using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();

        if (opened)
        {
            connection.Close();
        }
    }

    private static int Count(DbContext context, string table)
    {
        var connection = context.Database.GetDbConnection();
        bool opened = connection.State != ConnectionState.Open;
        if (opened)
        {
            connection.Open();
        }

        using DbCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM \"{table}\";";
        var count = Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);

        if (opened)
        {
            connection.Close();
        }

        return count;
    }

    private static void CreateTodoTables(TestResetterDbContext context)
    {
        Execute(context, """
            CREATE TABLE "todos" (id TEXT PRIMARY KEY);
            CREATE TABLE "todo_lists" (id TEXT PRIMARY KEY);
            CREATE TABLE "todo_categories" (id TEXT PRIMARY KEY);
            INSERT INTO "todos" (id) VALUES ('a'), ('b');
            INSERT INTO "todo_lists" (id) VALUES ('c'), ('d');
            INSERT INTO "todo_categories" (id) VALUES ('e'), ('f');
            """);
    }

    private static EfCoreInitializationContext CreateInitializationContext(TestResetterDbContext context) =>
        new(
            context.Database.GetDbConnection(),
            new Mock<IUnitOfWork>().Object,
            new Mock<ILogger>().Object);

    [Fact(DisplayName = "DataOnly reset should delete every default table")]
    public async Task ExecuteAsync_DataOnly_ShouldDeleteDefaultTables()
    {
        // Arrange
        using var context = CreateContext();
        CreateTodoTables(context);
        var resetter = new EfCoreResetter<TestResetterDbContext>(
            context, new Mock<ILogger>().Object, s_schemaCatalog, ResetScope.DataOnly);

        // Act
        Result result = await resetter.ExecuteAsync(CreateInitializationContext(context));

        // Assert
        result.IsSuccess.Should().BeTrue();
        Count(context, "todos").Should().Be(0);
        Count(context, "todo_lists").Should().Be(0);
        Count(context, "todo_categories").Should().Be(0);
    }

    [Fact(DisplayName = "DataOnly reset with targets should only delete those tables")]
    public async Task ExecuteAsync_DataOnly_WithTargets_ShouldOnlyDeleteTargets()
    {
        // Arrange
        using var context = CreateContext();
        CreateTodoTables(context);
        var resetter = new EfCoreResetter<TestResetterDbContext>(
            context,
            new Mock<ILogger>().Object,
            s_schemaCatalog,
            ResetScope.DataOnly,
            targets: ["todos", "missing_table"]);

        // Act
        Result result = await resetter.ExecuteAsync(CreateInitializationContext(context));

        // Assert
        result.IsSuccess.Should().BeTrue();
        Count(context, "todos").Should().Be(0);
        Count(context, "todo_lists").Should().Be(2);
        Count(context, "todo_categories").Should().Be(2);
    }

    [Fact(DisplayName = "DataOnly reset should short-circuit when no tables exist")]
    public async Task ExecuteAsync_DataOnly_WhenSchemaIsEmpty_ShouldSucceed()
    {
        // Arrange
        using var context = CreateContext();
        var resetter = new EfCoreResetter<TestResetterDbContext>(
            context, new Mock<ILogger>().Object, s_schemaCatalog, ResetScope.DataOnly);

        // Act
        Result result = await resetter.ExecuteAsync(CreateInitializationContext(context));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "DataOnly reset should tolerate a connection that is not a DbConnection")]
    public async Task ExecuteAsync_DataOnly_WhenConnectionIsNotDbConnection_ShouldSucceed()
    {
        // Arrange
        var connection = new Mock<IDbConnection>();
        connection.SetupGet(c => c.State).Returns(ConnectionState.Open);
        var initContext = new EfCoreInitializationContext(
            connection.Object,
            new Mock<IUnitOfWork>().Object,
            new Mock<ILogger>().Object);

        using var context = CreateContext();
        var resetter = new EfCoreResetter<TestResetterDbContext>(
            context, new Mock<ILogger>().Object, s_schemaCatalog, ResetScope.DataOnly);

        // Act
        Result result = await resetter.ExecuteAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "DataOnly reset should observe cancellation")]
    public async Task ExecuteAsync_DataOnly_WhenCancelled_ShouldFail()
    {
        // Arrange
        using var context = CreateContext();
        CreateTodoTables(context);
        var resetter = new EfCoreResetter<TestResetterDbContext>(
            context, new Mock<ILogger>().Object, s_schemaCatalog, ResetScope.DataOnly);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result result = await resetter.ExecuteAsync(CreateInitializationContext(context), cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Contain("Cancelled");
    }
}
