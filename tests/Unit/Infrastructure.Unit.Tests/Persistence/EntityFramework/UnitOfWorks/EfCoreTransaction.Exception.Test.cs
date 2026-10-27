using Application.Common.Persistence.UnitOfWorks;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.UnitOfWorks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Npgsql;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.UnitOfWorks;

/// <summary>
/// Branch tests for <see cref="EfCoreTransaction"/> exception translation using a
/// mocked provider transaction; the context itself is a real InMemory context.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class EfCoreTransactionExceptionTest
{
    private static (EfCoreDbContext Context, EfCoreTransaction Transaction) CreateTransaction(
        IDbContextTransaction inner)
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new UowTestDbContext(options);
        return (context, new EfCoreTransaction(context, inner, readOnly: false));
    }

    private static IDbContextTransaction FailingCommit(Exception exception)
    {
        var transaction = new Mock<IDbContextTransaction>();
        transaction
            .Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
        return transaction.Object;
    }

    private static IDbContextTransaction FailingRollback(Exception exception)
    {
        var transaction = new Mock<IDbContextTransaction>();
        transaction
            .Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
        transaction
            .Setup(t => t.RollbackAsync())
            .ThrowsAsync(exception);
        return transaction.Object;
    }

    private static PostgresException Postgres(string sqlState) =>
        new("boom", "ERROR", "ERROR", sqlState);

    #region CommitAsync translation

    public static TheoryData<Exception, string> CommitFailures => new()
    {
        { new DbUpdateConcurrencyException("conflict"), TransactionOutcome.Codes.ConcurrencyConflict },
        { new DbUpdateException("unique violation"), TransactionOutcome.Codes.DuplicateKey },
        { new DbUpdateException("duplicate value"), TransactionOutcome.Codes.DuplicateKey },
        { new DbUpdateException("foreign key constraint"), TransactionOutcome.Codes.ForeignKeyViolation },
        { new DbUpdateException("reference constraint"), TransactionOutcome.Codes.ForeignKeyViolation },
        { new DbUpdateException("check constraint"), TransactionOutcome.Codes.CheckConstraintViolation },
        { new DbUpdateException("not null column"), TransactionOutcome.Codes.NotNullViolation },
        { new DbUpdateException("null value"), TransactionOutcome.Codes.NotNullViolation },
        { new DbUpdateException("something else"), TransactionOutcome.Codes.SaveChangesFailed },
        { new TimeoutException(), TransactionOutcome.Codes.StoreTimeout },
        { new OperationCanceledException(), TransactionOutcome.Codes.StoreTimeout },
        { new InvalidOperationException("command timeout"), TransactionOutcome.Codes.StoreTimeout },
        { new InvalidOperationException("cannot translate LINQ"), TransactionOutcome.Codes.ExpressionTranslationFailed },
        { new InvalidOperationException("translate failure"), TransactionOutcome.Codes.ExpressionTranslationFailed },
        { new InvalidOperationException("broken state"), TransactionOutcome.Codes.InvalidOperation },
        { new Exception("unexpected"), TransactionOutcome.Codes.StoreUnknown },
    };

    [Theory(DisplayName = "CommitAsync should translate provider exceptions")]
    [MemberData(nameof(CommitFailures))]
    public async Task CommitAsync_WhenCommitFails_ShouldTranslate(Exception exception, string expectedCode)
    {
        // Arrange
        var (_, transaction) = CreateTransaction(FailingCommit(exception));

        // Act
        Result result = await transaction.CommitAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(expectedCode);

        await transaction.DisposeAsync();
    }

    [Fact(DisplayName = "CommitAsync with a cancelled token should report cancellation")]
    public async Task CommitAsync_WhenCancelled_ShouldReturnCancelled()
    {
        // Arrange
        var inner = new Mock<IDbContextTransaction>();
        inner
            .Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        var (_, transaction) = CreateTransaction(inner.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result result = await transaction.CommitAsync(cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(TransactionOutcome.Codes.StoreCancelled);

        await transaction.DisposeAsync();
    }

    [Fact(DisplayName = "CommitAsync should translate a wrapped PostgreSQL unique violation")]
    public async Task CommitAsync_WhenDbUpdateWrapsUniqueViolation_ShouldReturnDuplicateKey()
    {
        // Arrange
        var exception = new DbUpdateException("wrapper", Postgres("23505"));
        var (_, transaction) = CreateTransaction(FailingCommit(exception));

        // Act
        Result result = await transaction.CommitAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(TransactionOutcome.Codes.DuplicateKey);

        await transaction.DisposeAsync();
    }

    [Fact(DisplayName = "CommitAsync should translate a direct PostgreSQL failure")]
    public async Task CommitAsync_WhenPostgresExceptionThrown_ShouldTranslateSqlState()
    {
        // Arrange
        var (_, transaction) = CreateTransaction(FailingCommit(Postgres("23503")));

        // Act
        Result result = await transaction.CommitAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(TransactionOutcome.Codes.ForeignKeyViolation);

        await transaction.DisposeAsync();
    }

    public static TheoryData<string, string> SqlStates => new()
    {
        { "23505", TransactionOutcome.Codes.DuplicateKey },
        { "23503", TransactionOutcome.Codes.ForeignKeyViolation },
        { "23514", TransactionOutcome.Codes.CheckConstraintViolation },
        { "23502", TransactionOutcome.Codes.NotNullViolation },
        { "40P01", TransactionOutcome.Codes.DeadlockDetected },
        { "40001", TransactionOutcome.Codes.SerializationFailure },
        { "08001", TransactionOutcome.Codes.ConnectionRefused },
        { "08006", TransactionOutcome.Codes.ConnectionLost },
        { "28000", TransactionOutcome.Codes.LoginFailed },
        { "08004", TransactionOutcome.Codes.ConnectionFailed },
        { "99999", TransactionOutcome.Codes.StoreUnknown },
    };

    [Theory(DisplayName = "CommitAsync should translate every PostgreSQL SQLSTATE used by the transaction")]
    [MemberData(nameof(SqlStates))]
    public async Task CommitAsync_WhenWrappedPostgresExceptionThrown_ShouldTranslateSqlState(
        string sqlState, string expectedCode)
    {
        // Arrange
        var exception = new InvalidOperationException("wrapped", Postgres(sqlState));
        var (_, transaction) = CreateTransaction(FailingCommit(exception));

        // Act
        Result result = await transaction.CommitAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);

        await transaction.DisposeAsync();
    }

    #endregion

    #region RollbackAsync translation

    [Fact(DisplayName = "RollbackAsync should translate a wrapped PostgreSQL failure")]
    public async Task RollbackAsync_WhenPostgresFailureThrown_ShouldTranslate()
    {
        // Arrange
        var (_, transaction) = CreateTransaction(
            FailingRollback(new InvalidOperationException("wrapped", Postgres("40001"))));

        // Act
        Result result = await transaction.RollbackAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(TransactionOutcome.Codes.SerializationFailure);

        await transaction.DisposeAsync();
    }

    [Fact(DisplayName = "RollbackAsync should translate a generic failure")]
    public async Task RollbackAsync_WhenGenericFailureThrown_ShouldTranslate()
    {
        // Arrange
        var (_, transaction) = CreateTransaction(FailingRollback(new TimeoutException()));

        // Act
        Result result = await transaction.RollbackAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(TransactionOutcome.Codes.StoreTimeout);

        await transaction.DisposeAsync();
    }

    [Fact(DisplayName = "RollbackAsync with a cancelled token should report cancellation")]
    public async Task RollbackAsync_WhenCancelled_ShouldReturnCancelled()
    {
        // Arrange
        var (_, transaction) = CreateTransaction(FailingRollback(new OperationCanceledException()));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result result = await transaction.RollbackAsync(cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(TransactionOutcome.Codes.StoreCancelled);

        await transaction.DisposeAsync();
    }

    #endregion

    #region Isolation level and disposal

    [Fact(DisplayName = "DisposeAsync should swallow rollback failures")]
    public async Task DisposeAsync_WhenRollbackFails_ShouldStillDispose()
    {
        // Arrange
        var (_, transaction) = CreateTransaction(FailingRollback(new InvalidOperationException("no rollback")));

        // Act
        await transaction.DisposeAsync();

        // Assert
        transaction.State.Should().Be(TransactionState.Disposed);
    }

    #endregion
}
