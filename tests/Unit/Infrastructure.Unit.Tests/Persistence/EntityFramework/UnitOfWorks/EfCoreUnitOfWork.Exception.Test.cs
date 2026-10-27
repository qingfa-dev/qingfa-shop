using Application.Common.Persistence.UnitOfWorks;

using Infrastructure.Persistence.EntityFramework.UnitOfWorks;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.UnitOfWorks;

/// <summary>
/// Branch tests for <see cref="EfCoreUnitOfWork"/> exception translation and
/// save-failure paths that the happy-path suite does not reach.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class EfCoreUnitOfWorkExceptionTest
{
    private static UowTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new UowTestDbContext(options);
    }

    private static PostgresException Postgres(string sqlState) =>
        new("boom", "ERROR", "ERROR", sqlState);

    public static TheoryData<Exception, string> TranslationCases => new()
    {
        { new DbUpdateConcurrencyException("conflict"), UnitOfWorkOutcome.Codes.ConcurrencyConflict },
        { new DbUpdateException("unique violation"), UnitOfWorkOutcome.Codes.DuplicateKey },
        { new DbUpdateException("duplicate value"), UnitOfWorkOutcome.Codes.DuplicateKey },
        { new DbUpdateException("foreign key violation"), UnitOfWorkOutcome.Codes.ForeignKeyViolation },
        { new DbUpdateException("reference constraint violation"), UnitOfWorkOutcome.Codes.ForeignKeyViolation },
        { new DbUpdateException("check constraint violation"), UnitOfWorkOutcome.Codes.CheckConstraintViolation },
        { new DbUpdateException("not null violation"), UnitOfWorkOutcome.Codes.NotNullViolation },
        { new DbUpdateException("null value rejected"), UnitOfWorkOutcome.Codes.NotNullViolation },
        { new DbUpdateException("something else"), UnitOfWorkOutcome.Codes.SaveChangesFailed },
        { new TimeoutException(), UnitOfWorkOutcome.Codes.StoreTimeout },
        { new OperationCanceledException(), UnitOfWorkOutcome.Codes.StoreTimeout },
        { new InvalidOperationException("command timeout"), UnitOfWorkOutcome.Codes.StoreTimeout },
        { new InvalidOperationException("cannot translate LINQ"), UnitOfWorkOutcome.Codes.ExpressionTranslationFailed },
        { new InvalidOperationException("translate failure"), UnitOfWorkOutcome.Codes.ExpressionTranslationFailed },
        { new InvalidOperationException("broken state"), UnitOfWorkOutcome.Codes.InvalidOperation },
        { new Exception("unexpected"), UnitOfWorkOutcome.Codes.StoreUnknown },
    };

    [Theory(DisplayName = "ExecuteAsync should translate every exception shape")]
    [MemberData(nameof(TranslationCases))]
    public async Task ExecuteAsync_WhenWorkThrows_ShouldTranslate(Exception exception, string expectedCode)
    {
        // Arrange
        var context = CreateContext();
        var unitOfWork = new EfCoreUnitOfWork(context);

        // Act
        Result result = await unitOfWork.ExecuteAsync(_ => Task.FromException(exception));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    public static TheoryData<string, string> SqlStates => new()
    {
        { "23505", UnitOfWorkOutcome.Codes.DuplicateKey },
        { "23503", UnitOfWorkOutcome.Codes.ForeignKeyViolation },
        { "23514", UnitOfWorkOutcome.Codes.CheckConstraintViolation },
        { "23502", UnitOfWorkOutcome.Codes.NotNullViolation },
        { "40P01", UnitOfWorkOutcome.Codes.DeadlockDetected },
        { "40001", UnitOfWorkOutcome.Codes.SerializationFailure },
        { "08001", UnitOfWorkOutcome.Codes.ConnectionRefused },
        { "08006", UnitOfWorkOutcome.Codes.ConnectionLost },
        { "28000", UnitOfWorkOutcome.Codes.LoginFailed },
        { "08004", UnitOfWorkOutcome.Codes.ConnectionFailed },
        { "99999", UnitOfWorkOutcome.Codes.StoreUnknown },
    };

    [Theory(DisplayName = "ExecuteAsync should translate every PostgreSQL SQLSTATE")]
    [MemberData(nameof(SqlStates))]
    public async Task ExecuteAsync_WhenWrappedPostgresExceptionThrown_ShouldTranslateSqlState(
        string sqlState, string expectedCode)
    {
        // Arrange
        var context = CreateContext();
        var unitOfWork = new EfCoreUnitOfWork(context);
        var exception = new InvalidOperationException("wrapped", Postgres(sqlState));

        // Act
        Result result = await unitOfWork.ExecuteAsync(_ => Task.FromException(exception));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact(DisplayName = "ExecuteAsync<T> should return the save failure when SaveChanges fails")]
    public async Task ExecuteAsyncGeneric_WhenSaveFails_ShouldReturnSaveFailure()
    {
        // Arrange
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new SaveFailingDbContext(options, new InvalidOperationException("save failed"));
        var unitOfWork = new EfCoreUnitOfWork(context);

        // Act
        Result<string> result = await unitOfWork.ExecuteAsync(_ => Task.FromResult("value"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(UnitOfWorkOutcome.Codes.SaveChangesFailed);
    }

    [Fact(DisplayName = "ExecuteAsync<T> with a cancelled token should report cancellation")]
    public async Task ExecuteAsyncGeneric_WhenCancelled_ShouldReturnCancelled()
    {
        // Arrange
        var context = CreateContext();
        var unitOfWork = new EfCoreUnitOfWork(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result<string> result = await unitOfWork.ExecuteAsync<string>(
            _ => Task.FromException<string>(new OperationCanceledException()),
            cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(UnitOfWorkOutcome.Codes.StoreCancelled);
    }

    [Fact(DisplayName = "ExecuteAsync<T> should translate a generic work failure")]
    public async Task ExecuteAsyncGeneric_WhenWorkThrows_ShouldTranslate()
    {
        // Arrange
        var context = CreateContext();
        var unitOfWork = new EfCoreUnitOfWork(context);

        // Act
        Result<string> result = await unitOfWork.ExecuteAsync<string>(
            _ => Task.FromException<string>(new InvalidOperationException("broken state")));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(UnitOfWorkOutcome.Codes.InvalidOperation);
    }

    public static TheoryData<Exception, string> SaveFailures => new()
    {
        { new DbUpdateConcurrencyException("conflict"), UnitOfWorkOutcome.Codes.ConcurrencyConflict },
        { new DbUpdateException("unique violation"), UnitOfWorkOutcome.Codes.DuplicateKey },
        { new DbUpdateException("duplicate value"), UnitOfWorkOutcome.Codes.DuplicateKey },
        { new DbUpdateException("foreign key constraint"), UnitOfWorkOutcome.Codes.SaveChangesFailed },
        { new TimeoutException(), UnitOfWorkOutcome.Codes.StoreTimeout },
        { new InvalidOperationException("broken state"), UnitOfWorkOutcome.Codes.SaveChangesFailed },
    };

    [Theory(DisplayName = "SaveChangesAsync should translate every failure shape")]
    [MemberData(nameof(SaveFailures))]
    public async Task SaveChangesAsync_WhenSaveFails_ShouldTranslate(Exception exception, string expectedCode)
    {
        // Arrange
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new SaveFailingDbContext(options, exception);
        var unitOfWork = new EfCoreUnitOfWork(context);

        // Act
        Result result = await unitOfWork.SaveChangesAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact(DisplayName = "SaveChangesAsync should recognize a wrapped PostgreSQL unique violation")]
    public async Task SaveChangesAsync_WhenWrappedUniqueViolation_ShouldReturnDuplicateKey()
    {
        // Arrange
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var exception = new DbUpdateException("wrapper", Postgres("23505"));
        var context = new SaveFailingDbContext(options, exception);
        var unitOfWork = new EfCoreUnitOfWork(context);

        // Act
        Result result = await unitOfWork.SaveChangesAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(UnitOfWorkOutcome.Codes.DuplicateKey);
    }

    [Fact(DisplayName = "SaveChangesAsync with a cancelled token should report cancellation")]
    public async Task SaveChangesAsync_WhenCancelled_ShouldReturnCancelled()
    {
        // Arrange
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new SaveFailingDbContext(options, new OperationCanceledException());
        var unitOfWork = new EfCoreUnitOfWork(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result result = await unitOfWork.SaveChangesAsync(cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(UnitOfWorkOutcome.Codes.StoreCancelled);
    }

    [Fact(DisplayName = "ExecuteInTransactionAsync should return the begin-transaction failure")]
    public async Task ExecuteInTransactionAsync_WhenBeginFails_ShouldReturnFailure()
    {
        // Arrange
        var options = new DbContextOptionsBuilder()
            .UseSqlite("Data Source=/dev/null/qingfa-unit-test.db")
            .Options;
        var context = new UowTestDbContext(options);
        var unitOfWork = new EfCoreUnitOfWork(context);

        // Act
        Result result = await unitOfWork.ExecuteInTransactionAsync(
            (_, _) => Task.CompletedTask);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "ExecuteInTransactionAsync<T> should return the begin-transaction failure")]
    public async Task ExecuteInTransactionAsyncGeneric_WhenBeginFails_ShouldReturnFailure()
    {
        // Arrange
        var options = new DbContextOptionsBuilder()
            .UseSqlite("Data Source=/dev/null/qingfa-unit-test.db")
            .Options;
        var context = new UowTestDbContext(options);
        var unitOfWork = new EfCoreUnitOfWork(context);

        // Act
        Result<string> result = await unitOfWork.ExecuteInTransactionAsync<string>(
            (_, _) => Task.FromResult("value"));

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    /// <summary>Context whose SaveChanges always fails with the supplied exception.</summary>
    private sealed class SaveFailingDbContext(
        DbContextOptions options,
        Exception exception) : UowTestDbContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<int>(exception);
    }
}
