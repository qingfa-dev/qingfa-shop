using Application.BuildingBlocks.Persistence.UnitOfWorks;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.UnitOfWorks;

/// <summary>
/// Materializes a classified <see cref="PersistenceFailure"/> into the single outcome type
/// that owns its meaning.
/// </summary>
/// <remarks>
/// Transaction lifecycle, deadlock, serialization, and read-only failures resolve to
/// <see cref="TransactionOutcome"/>. Write constraints, store connections, and execution
/// failures resolve to <see cref="UnitOfWorkOutcome"/>. The originating exception is attached
/// as metadata so the same failure remains traceable after it becomes an <see cref="Error"/>.
/// </remarks>
internal static class PersistenceFailureMapper
{
    /// <summary>Projects the failure into its owning outcome.</summary>
    /// <param name="failure">The classified failure.</param>
    /// <returns>The matching outcome error, with exception metadata when available.</returns>
    public static Error ToOutcome(this PersistenceFailure failure)
    {
        Error error = failure.Kind switch
        {
            // ---- Transaction-owned ----
            PersistenceFailureKind.DeadlockDetected => TransactionOutcome.Failure.DeadlockDetected,
            PersistenceFailureKind.SerializationFailure => TransactionOutcome.Failure.SerializationFailure,
            PersistenceFailureKind.ReadOnlyViolation => TransactionOutcome.Failure.ReadOnlyViolation,
            PersistenceFailureKind.TransactionRollbackOnly => TransactionOutcome.Failure.TransactionRollbackOnly,

            // ---- Unit-of-work-owned: write constraints ----
            PersistenceFailureKind.ConcurrencyConflict => UnitOfWorkOutcome.Failure.ConcurrencyConflict,
            PersistenceFailureKind.DuplicateKey => Detail(
                failure.Detail,
                static detail => UnitOfWorkOutcome.Failure.DuplicateKeyFor(detail),
                UnitOfWorkOutcome.Failure.DuplicateKey),
            PersistenceFailureKind.ForeignKeyViolation => Detail(
                failure.Detail,
                static detail => UnitOfWorkOutcome.Failure.ForeignKeyViolationFor(detail),
                UnitOfWorkOutcome.Failure.ForeignKeyViolation),
            PersistenceFailureKind.CheckConstraintViolation => Detail(
                failure.Detail,
                static detail => UnitOfWorkOutcome.Failure.CheckConstraintViolationFor(detail),
                UnitOfWorkOutcome.Failure.CheckConstraintViolation),
            PersistenceFailureKind.NotNullViolation => Detail(
                failure.Detail,
                static detail => UnitOfWorkOutcome.Failure.NotNullViolationFor(detail),
                UnitOfWorkOutcome.Failure.NotNullViolation),

            // ---- Unit-of-work-owned: store ----
            PersistenceFailureKind.ConnectionFailed => UnitOfWorkOutcome.Failure.ConnectionFailed,
            PersistenceFailureKind.ConnectionRefused => UnitOfWorkOutcome.Failure.ConnectionRefused,
            PersistenceFailureKind.ConnectionLost => UnitOfWorkOutcome.Failure.ConnectionLost,
            PersistenceFailureKind.LoginFailed => UnitOfWorkOutcome.Failure.LoginFailed,
            PersistenceFailureKind.Timeout => UnitOfWorkOutcome.Failure.Timeout,
            PersistenceFailureKind.Cancelled => UnitOfWorkOutcome.Failure.Cancelled,

            // ---- Unit-of-work-owned: execution ----
            PersistenceFailureKind.SaveChangesFailed => UnitOfWorkOutcome.Failure.SaveChangesFailed(
                failure.Detail ?? "The persistence store rejected the change."),
            PersistenceFailureKind.ExpressionTranslationFailed => UnitOfWorkOutcome.Failure.ExpressionTranslationFailed(
                failure.Detail ?? "The query could not be translated."),
            PersistenceFailureKind.InvalidOperation => Detail(
                failure.Detail,
                static detail => UnitOfWorkOutcome.Failure.InvalidOperationFor(detail),
                UnitOfWorkOutcome.Failure.InvalidOperation),

            _ => UnitOfWorkOutcome.Failure.Unknown,
        };

        return failure.WithExceptionMetadata(error);
    }

    /// <summary>
    /// Projects a non-actionable SaveChanges failure into the single write error while keeping
    /// the originating exception metadata.
    /// </summary>
    /// <param name="failure">The classified failure.</param>
    /// <param name="reason">The provider message to include in the error.</param>
    /// <returns>The write failure with exception metadata when available.</returns>
    public static Error ToSaveChangesError(this PersistenceFailure failure, string reason) =>
        failure.WithExceptionMetadata(UnitOfWorkOutcome.Failure.SaveChangesFailed(reason));

    private static Error WithExceptionMetadata(this PersistenceFailure failure, Error error) =>
        failure.ExceptionType is null
            ? error
            : error
                .WithMetadata(ErrorConstant.Metadata.ExceptionType, failure.ExceptionType)
                .WithMetadata(ErrorConstant.Metadata.ExceptionMessage, failure.ExceptionMessage ?? string.Empty);

    private static Error Detail(string? detail, Func<string, Error> withDetail, Error withoutDetail) =>
        detail is null ? withoutDetail : withDetail(detail);
}
