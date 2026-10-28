namespace Application.Common.Persistence.UnitOfWorks;

/// <summary>
/// Transaction failures. Owns transaction validation, lifecycle state, transaction
/// concurrency, and read-only enforcement used by <c>EfCoreTransaction</c>.
/// </summary>
/// <remarks>
/// Write constraints, store connections, and exception translation belong to
/// <see cref="UnitOfWorkOutcome"/>. Callers use those members directly instead of
/// duplicating them here.
/// </remarks>
public static class TransactionOutcome
{
    public static class Failure
    {
        // ---- Validation ----

        public static Error CommandTimeoutTooSmall(string parameterName = "timeoutSeconds") => Error.Validation(
            Codes.CommandTimeoutTooSmall,
            $"{parameterName} must be at least {TransactionConstant.Limits.MinCommandTimeoutSeconds} second(s).");

        public static Error CommandTimeoutTooLarge(string parameterName = "timeoutSeconds") => Error.Validation(
            Codes.CommandTimeoutTooLarge,
            $"{parameterName} cannot exceed {TransactionConstant.Limits.MaxCommandTimeoutSeconds} seconds.");

        public static Error MaxRetryAttemptsNegative(string parameterName = "maxAttempts") => Error.Validation(
            Codes.MaxRetryAttemptsNegative, $"{parameterName} cannot be negative.");

        public static Error MaxRetryAttemptsExceedsMax(string parameterName = "maxAttempts") => Error.Validation(
            Codes.MaxRetryAttemptsExceedsMax,
            $"{parameterName} cannot exceed {TransactionConstant.Limits.MaxRetryAttempts}.");

        public static Error RetryBaseDelayNegative(string parameterName = "baseDelayMs") => Error.Validation(
            Codes.RetryBaseDelayNegative, $"{parameterName} cannot be negative.");

        public static Error RetryBaseDelayExceedsMax(string parameterName = "baseDelayMs") => Error.Validation(
            Codes.RetryBaseDelayExceedsMax,
            $"{parameterName} cannot exceed {TransactionConstant.Limits.MaxRetryBaseDelayMs} ms.");

        public static Error PoolMinSizeNegative(string parameterName = "minSize") => Error.Validation(
            Codes.PoolMinSizeNegative, $"{parameterName} cannot be negative.");

        public static Error PoolMaxSizeNegative(string parameterName = "maxSize") => Error.Validation(
            Codes.PoolMaxSizeNegative, $"{parameterName} cannot be negative.");

        public static Error PoolMaxSizeExceedsMax(string parameterName = "maxSize") => Error.Validation(
            Codes.PoolMaxSizeExceedsMax,
            $"{parameterName} cannot exceed {TransactionConstant.Limits.MaxPoolSize}.");

        public static Error PoolMinExceedsMax(string minParameterName = "minSize", string maxParameterName = "maxSize") => Error.Validation(
            Codes.PoolMinExceedsMax,
            $"{minParameterName} cannot exceed {maxParameterName}.");

        public static Error IsolationLevelInvalid(string parameterName = "isolationLevel") => Error.Validation(
            Codes.IsolationLevelInvalid,
            $"{parameterName} must be one of: {TransactionConstant.IsolationLevel.ReadUncommitted}, {TransactionConstant.IsolationLevel.ReadCommitted}, {TransactionConstant.IsolationLevel.RepeatableRead}, {TransactionConstant.IsolationLevel.Serializable}, {TransactionConstant.IsolationLevel.Snapshot}, {TransactionConstant.IsolationLevel.Unspecified}.");

        public static Error StateNull(string parameterName = "state") => Error.Validation(
            Codes.StateNull, $"{parameterName} cannot be null.");

        public static Error StateInvalid(string parameterName = "state") => Error.Validation(
            Codes.StateInvalid,
            $"{parameterName} must be one of: {TransactionConstant.State.Active}, {TransactionConstant.State.Committed}, {TransactionConstant.State.RolledBack}, {TransactionConstant.State.RollbackOnly}, {TransactionConstant.State.Failed}.");

        // ---- Transaction state ----

        public static Error TransactionNested => Error.Conflict(
            Codes.TransactionNested, "A transaction is already open.");

        public static Error TransactionRequired => Error.Conflict(
            Codes.TransactionRequired, "No transaction is open.");

        public static Error TransactionAlreadyCompleted => Error.Conflict(
            Codes.TransactionAlreadyCompleted,
            "The transaction has already been committed or rolled back.");

        public static Error TransactionRollbackOnly => Error.Conflict(
            Codes.TransactionRollbackOnly,
            "The transaction is marked rollback-only and cannot be committed.");

        // ---- Transaction concurrency ----

        public static Error DeadlockDetected => Error.Conflict(
            Codes.DeadlockDetected, "The transaction was deadlocked and cannot proceed.");

        public static Error SerializationFailure => Error.Conflict(
            Codes.SerializationFailure,
            "The transaction failed due to a serialization conflict. Retry the operation.");

        /// <summary>Write attempted in a read-only transaction.</summary>
        public static Error ReadOnlyViolation => Error.Conflict(
            Codes.ReadOnlyViolation, "The transaction is read-only; writes are not permitted.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        // ---- Validation ----
        public const string CommandTimeoutTooSmall      = "Transaction.CommandTimeout.TooSmall";
        public const string CommandTimeoutTooLarge      = "Transaction.CommandTimeout.TooLarge";
        public const string MaxRetryAttemptsNegative    = "Transaction.MaxRetryAttempts.Negative";
        public const string MaxRetryAttemptsExceedsMax  = "Transaction.MaxRetryAttempts.ExceedsMax";
        public const string RetryBaseDelayNegative      = "Transaction.RetryBaseDelay.Negative";
        public const string RetryBaseDelayExceedsMax    = "Transaction.RetryBaseDelay.ExceedsMax";
        public const string PoolMinSizeNegative         = "Transaction.Pool.MinSize.Negative";
        public const string PoolMaxSizeNegative         = "Transaction.Pool.MaxSize.Negative";
        public const string PoolMaxSizeExceedsMax       = "Transaction.Pool.MaxSize.ExceedsMax";
        public const string PoolMinExceedsMax           = "Transaction.Pool.MinExceedsMax";
        public const string IsolationLevelInvalid       = "Transaction.IsolationLevel.Invalid";
        public const string StateNull                   = "Transaction.State.Null";
        public const string StateInvalid                = "Transaction.State.Invalid";

        // ---- Transaction state ----
        public const string TransactionNested           = "Transaction.Nested";
        public const string TransactionRequired         = "Transaction.Required";
        public const string TransactionAlreadyCompleted = "Transaction.AlreadyCompleted";
        public const string TransactionRollbackOnly     = "Transaction.RollbackOnly";

        // ---- Transaction concurrency ----
        public const string DeadlockDetected     = "Transaction.Concurrency.Deadlock";
        public const string SerializationFailure = "Transaction.Concurrency.SerializationFailure";
        public const string ReadOnlyViolation    = "Transaction.Write.ReadOnlyViolation";
    }
}
