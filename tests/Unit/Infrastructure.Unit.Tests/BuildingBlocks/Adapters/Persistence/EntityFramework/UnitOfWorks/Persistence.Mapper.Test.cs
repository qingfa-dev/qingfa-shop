using Application.BuildingBlocks.Persistence.UnitOfWorks;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.UnitOfWorks;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.UnitOfWorks;

/// <summary>
/// Verifies that <see cref="PersistenceFailureMapper"/> routes each failure to the single outcome
/// that owns it and preserves the originating exception as metadata.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class PersistenceFailureMapperTest
{
    [Theory(DisplayName = "Transaction failures map to TransactionOutcome")]
    [InlineData(nameof(PersistenceFailureKind.DeadlockDetected), TransactionOutcome.Codes.DeadlockDetected)]
    [InlineData(nameof(PersistenceFailureKind.SerializationFailure), TransactionOutcome.Codes.SerializationFailure)]
    [InlineData(nameof(PersistenceFailureKind.ReadOnlyViolation), TransactionOutcome.Codes.ReadOnlyViolation)]
    [InlineData(nameof(PersistenceFailureKind.TransactionRollbackOnly), TransactionOutcome.Codes.TransactionRollbackOnly)]
    public void ToOutcome_WhenTransactionFailure_ShouldUseTransactionOutcome(
        string kindName, string expectedCode)
    {
        // Act
        Error error = new PersistenceFailure(Enum.Parse<PersistenceFailureKind>(kindName)).ToOutcome();

        // Assert
        error.Code.Should().Be(expectedCode);
    }

    [Theory(DisplayName = "Write and store failures map to UnitOfWorkOutcome")]
    [InlineData(nameof(PersistenceFailureKind.ConcurrencyConflict), UnitOfWorkOutcome.Codes.ConcurrencyConflict)]
    [InlineData(nameof(PersistenceFailureKind.DuplicateKey), UnitOfWorkOutcome.Codes.DuplicateKey)]
    [InlineData(nameof(PersistenceFailureKind.ForeignKeyViolation), UnitOfWorkOutcome.Codes.ForeignKeyViolation)]
    [InlineData(nameof(PersistenceFailureKind.CheckConstraintViolation), UnitOfWorkOutcome.Codes.CheckConstraintViolation)]
    [InlineData(nameof(PersistenceFailureKind.NotNullViolation), UnitOfWorkOutcome.Codes.NotNullViolation)]
    [InlineData(nameof(PersistenceFailureKind.ConnectionFailed), UnitOfWorkOutcome.Codes.ConnectionFailed)]
    [InlineData(nameof(PersistenceFailureKind.ConnectionRefused), UnitOfWorkOutcome.Codes.ConnectionRefused)]
    [InlineData(nameof(PersistenceFailureKind.ConnectionLost), UnitOfWorkOutcome.Codes.ConnectionLost)]
    [InlineData(nameof(PersistenceFailureKind.LoginFailed), UnitOfWorkOutcome.Codes.LoginFailed)]
    [InlineData(nameof(PersistenceFailureKind.Timeout), UnitOfWorkOutcome.Codes.StoreTimeout)]
    [InlineData(nameof(PersistenceFailureKind.Cancelled), UnitOfWorkOutcome.Codes.StoreCancelled)]
    [InlineData(nameof(PersistenceFailureKind.SaveChangesFailed), UnitOfWorkOutcome.Codes.SaveChangesFailed)]
    [InlineData(nameof(PersistenceFailureKind.ExpressionTranslationFailed), UnitOfWorkOutcome.Codes.ExpressionTranslationFailed)]
    [InlineData(nameof(PersistenceFailureKind.InvalidOperation), UnitOfWorkOutcome.Codes.InvalidOperation)]
    [InlineData(nameof(PersistenceFailureKind.Unknown), UnitOfWorkOutcome.Codes.StoreUnknown)]
    public void ToOutcome_WhenUnitOfWorkFailure_ShouldUseUnitOfWorkOutcome(
        string kindName, string expectedCode)
    {
        // Arrange
        PersistenceFailureKind kind = Enum.Parse<PersistenceFailureKind>(kindName);

        // Act
        Error error = new PersistenceFailure(kind, Detail(kind)).ToOutcome();

        // Assert
        error.Code.Should().Be(expectedCode);
    }

    [Fact(DisplayName = "ToOutcome should attach the originating exception as metadata")]
    public void ToOutcome_WhenClassified_ShouldAttachExceptionMetadata()
    {
        // Arrange
        var exception = new TimeoutException("store timed out");

        // Act
        Error error = PersistenceExceptionTranslator.Classify(exception).ToOutcome();

        // Assert
        error.Code.Should().Be(UnitOfWorkOutcome.Codes.StoreTimeout);
        error.Metadata[ErrorConstant.Metadata.ExceptionType].Should().Be(typeof(TimeoutException).FullName);
        error.Metadata[ErrorConstant.Metadata.ExceptionMessage].Should().Be(exception.Message);
    }

    [Fact(DisplayName = "ToSaveChangesError should attach the originating exception as metadata")]
    public void ToSaveChangesError_WhenClassified_ShouldAttachExceptionMetadata()
    {
        // Arrange
        var exception = new InvalidOperationException("writer failed");
        PersistenceFailure failure = PersistenceExceptionTranslator.Classify(exception);

        // Act
        Error error = failure.ToSaveChangesError(exception.Message);

        // Assert
        error.Code.Should().Be(UnitOfWorkOutcome.Codes.SaveChangesFailed);
        error.Metadata[ErrorConstant.Metadata.ExceptionType].Should().Be(typeof(InvalidOperationException).FullName);
        error.Metadata[ErrorConstant.Metadata.ExceptionMessage].Should().Be(exception.Message);
    }

    private static string? Detail(PersistenceFailureKind kind) => kind switch
    {
        PersistenceFailureKind.DuplicateKey => "UX_Todo_Key",
        PersistenceFailureKind.ForeignKeyViolation => "FK_Todo_List",
        PersistenceFailureKind.CheckConstraintViolation => "CK_Todo_State",
        PersistenceFailureKind.NotNullViolation => "Title",
        PersistenceFailureKind.SaveChangesFailed => "store rejected the change",
        PersistenceFailureKind.ExpressionTranslationFailed => "unsupported member",
        PersistenceFailureKind.InvalidOperation => "invalid state",
        _ => null,
    };
}
