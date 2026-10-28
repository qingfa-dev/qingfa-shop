using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Common.Querying.Mapping;

using Infrastructure.Persistence.EntityFramework.Capabilities;
using Infrastructure.Persistence.EntityFramework.Repositories;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Repositories;

/// <summary>
/// Branch tests for <see cref="EfCoreRepository{TEntity,TKey}"/> exception
/// translation. The context throws from <c>Set&lt;T&gt;()</c> so each provider
/// exception reaches the shared translator without a database.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class EfCoreRepositoryTranslationTest
{
    private static EfCoreRepository<UnitTestProduct, Guid> CreateRepository(Exception exception)
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new ThrowingSetDbContext(options, exception);
        var translator = new MockQueryTranslator<UnitTestProduct>();
        var provider = EfCoreRepositoryTestFixture.CreateMapProvider(
            EfCoreRepositoryTestFixture.CreateQueryMap());

        return new EfCoreRepository<UnitTestProduct, Guid>(
            context, translator, provider, new EfCoreQueryCapabilities());
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

    [Theory(DisplayName = "AddAsync should translate every exception shape")]
    [MemberData(nameof(TranslationCases))]
    public async Task AddAsync_WhenContextThrows_ShouldTranslate(Exception exception, string expectedCode)
    {
        // Arrange
        var repository = CreateRepository(exception);
        UnitTestProduct product = EfCoreRepositoryTestFixture.CreateProduct("Widget", 1m);

        // Act
        Result result = await repository.AddAsync(product);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Theory(DisplayName = "AddAsync with a cancelled token should report cancellation")]
    [InlineData(true, "UnitOfWork.Store.Cancelled")]
    [InlineData(false, "UnitOfWork.Store.Timeout")]
    public async Task AddAsync_WhenOperationCancelled_ShouldHonorTheToken(
        bool cancelled, string expectedCode)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        if (cancelled)
        {
            cts.Cancel();
        }

        var exception = cancelled
            ? new OperationCanceledException(cts.Token)
            : new OperationCanceledException();
        var repository = CreateRepository(exception);
        UnitTestProduct product = EfCoreRepositoryTestFixture.CreateProduct("Widget", 1m);

        // Act
        Result result = await repository.AddAsync(product, cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact(DisplayName = "Constructor should reject an entity without a registered query map")]
    public void Constructor_WhenMapIsMissing_ShouldThrow()
    {
        // Arrange
        using UnitTestDbContext context = EfCoreRepositoryTestFixture.CreateContext();
        var translator = new MockQueryTranslator<UnitTestProduct>();
        var emptyProvider = new QueryMapRegistry();
        var capabilities = new EfCoreQueryCapabilities();

        // Act
        Action act = () => _ = new EfCoreRepository<UnitTestProduct, Guid>(
            context, translator, emptyProvider, capabilities);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No query map is registered for 'UnitTestProduct'*");
    }

    public static TheoryData<string, string> SqlStates => new()
    {
        { "23505", UnitOfWorkOutcome.Codes.DuplicateKey },
        { "23503", UnitOfWorkOutcome.Codes.ForeignKeyViolation },
        { "23514", UnitOfWorkOutcome.Codes.CheckConstraintViolation },
        { "23502", UnitOfWorkOutcome.Codes.NotNullViolation },
        { "40P01", TransactionOutcome.Codes.DeadlockDetected },
        { "40001", TransactionOutcome.Codes.SerializationFailure },
        { "08001", UnitOfWorkOutcome.Codes.ConnectionRefused },
        { "08006", UnitOfWorkOutcome.Codes.ConnectionLost },
        { "28000", UnitOfWorkOutcome.Codes.LoginFailed },
        { "08004", UnitOfWorkOutcome.Codes.ConnectionFailed },
        { "99999", UnitOfWorkOutcome.Codes.StoreUnknown },
    };

    [Theory(DisplayName = "AddAsync should translate every PostgreSQL SQLSTATE")]
    [MemberData(nameof(SqlStates))]
    public async Task AddAsync_WhenWrappedPostgresExceptionThrown_ShouldTranslateSqlState(
        string sqlState, string expectedCode)
    {
        // Arrange
        var exception = new InvalidOperationException("wrapped", Postgres(sqlState));
        var repository = CreateRepository(exception);
        UnitTestProduct product = EfCoreRepositoryTestFixture.CreateProduct("Widget", 1m);

        // Act
        Result result = await repository.AddAsync(product);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    /// <summary>Context whose <c>Set&lt;T&gt;()</c> always throws.</summary>
    private sealed class ThrowingSetDbContext(
        DbContextOptions options,
        Exception exception) : UnitTestDbContext(options)
    {
        public override DbSet<TEntity> Set<TEntity>() where TEntity : class =>
            throw exception;
    }
}
