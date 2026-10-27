namespace Application.Common.Persistence.UnitOfWorks;

/// <summary>
/// Transaction failures. Covers validation, transaction state, write constraints,
/// store connections, and exception translation outcomes used by <c>EfCoreTransaction</c>.
/// </summary>
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

        // ---- Write constraints ----

        public static Error DuplicateKey => Error.Conflict(
            Codes.DuplicateKey, "The entity conflicts with an existing row.");

        public static Error DuplicateKeyFor(string constraint) => Error.Conflict(
            Codes.DuplicateKey,
            $"The entity conflicts with an existing row (constraint '{constraint}').");

        public static Error DuplicateKeyFor<T>() => Error.Conflict(
            Codes.DuplicateKey, $"'{typeof(T).Name}' conflicts with an existing row.");

        public static Error ForeignKeyViolation => Error.Conflict(
            Codes.ForeignKeyViolation, "The operation violates a foreign key constraint.");

        public static Error ForeignKeyViolationFor(string constraint) => Error.Conflict(
            Codes.ForeignKeyViolation,
            $"The operation violates a foreign key constraint ('{constraint}').");

        public static Error CheckConstraintViolation => Error.Conflict(
            Codes.CheckConstraintViolation, "The operation violates a check constraint.");

        public static Error CheckConstraintViolationFor(string constraint) => Error.Conflict(
            Codes.CheckConstraintViolation,
            $"The operation violates a check constraint ('{constraint}').");

        public static Error NotNullViolation => Error.Conflict(
            Codes.NotNullViolation, "A required column cannot accept a null value.");

        public static Error NotNullViolationFor(string column) => Error.Conflict(
            Codes.NotNullViolation, $"The column '{column}' cannot accept a null value.");

        public static Error ConcurrencyConflict => Error.Conflict(
            Codes.ConcurrencyConflict, "The entity was modified by another request.");

        public static Error ConcurrencyConflictFor<T>(object? key = null) => Error.Conflict(
            Codes.ConcurrencyConflict,
            key is null
                ? $"'{typeof(T).Name}' was modified by another request."
                : $"'{typeof(T).Name}' with key '{key}' was modified by another request.");

        public static Error DeadlockDetected => Error.Conflict(
            Codes.DeadlockDetected, "The transaction was deadlocked and cannot proceed.");

        public static Error SerializationFailure => Error.Conflict(
            Codes.SerializationFailure,
            "The transaction failed due to a serialization conflict. Retry the operation.");

        /// <summary>SaveChanges failed for a non-conflict, non-unique reason</summary>
        public static Error SaveChangesFailed(string reason) => Error.InternalServerError(
            Codes.SaveChangesFailed, $"SaveChanges failed: {reason}");

        /// <summary>Write attempted in a read-only transaction.</summary>
        public static Error ReadOnlyViolation => Error.Conflict(
            Codes.ReadOnlyViolation, "The transaction is read-only; writes are not permitted.");

        // ---- Store connections ----

        public static Error ConnectionFailed => Error.ServiceUnavailable(
            Codes.ConnectionFailed, "The connection to the persistence store failed.");

        public static Error ConnectionRefused => Error.ServiceUnavailable(
            Codes.ConnectionRefused, "The persistence store refused the connection.");

        public static Error ConnectionLost => Error.ServiceUnavailable(
            Codes.ConnectionLost, "The connection to the persistence store was lost.");

        public static Error LoginFailed => Error.ServiceUnavailable(
            Codes.LoginFailed, "Authentication to the persistence store failed.");

        public static Error Timeout => Error.ServiceUnavailable(
            Codes.StoreTimeout, "The persistence operation timed out.");

        public static Error Cancelled => Error.ServiceUnavailable(
            Codes.StoreCancelled, "The persistence operation was cancelled.");

        public static Error Unknown => Error.InternalServerError(
            Codes.StoreUnknown, "The persistence store failed.");

        // ---- Query expression translation ----

        public static Error ExpressionTranslationFailed(string reason) => Error.InternalServerError(
            Codes.ExpressionTranslationFailed, $"Could not translate the query expression: {reason}");

        public static Error InvalidOperation => Error.InternalServerError(
            Codes.InvalidOperation, "The operation is invalid in its current state.");

        public static Error InvalidOperationFor(string reason) => Error.InternalServerError(
            Codes.InvalidOperation, $"The operation is invalid: {reason}");
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

        // ---- Write constraints ----
        public const string DuplicateKey         = "Transaction.Entity.DuplicateKey";
        public const string ForeignKeyViolation  = "Transaction.Entity.ForeignKeyViolation";
        public const string CheckConstraintViolation = "Transaction.Entity.CheckConstraintViolation";
        public const string NotNullViolation     = "Transaction.Entity.NotNullViolation";
        public const string ConcurrencyConflict  = "Transaction.Concurrency.Conflict";
        public const string DeadlockDetected     = "Transaction.Concurrency.Deadlock";
        public const string SerializationFailure = "Transaction.Concurrency.SerializationFailure";
        public const string SaveChangesFailed    = "Transaction.Write.SaveChangesFailed";
        public const string ReadOnlyViolation    = "Transaction.Write.ReadOnlyViolation";

        // ---- Store connections ----
        public const string ConnectionFailed = "Transaction.Store.ConnectionFailed";
        public const string ConnectionRefused = "Transaction.Store.ConnectionRefused";
        public const string ConnectionLost = "Transaction.Store.ConnectionLost";
        public const string LoginFailed = "Transaction.Store.LoginFailed";
        public const string StoreTimeout = "Transaction.Store.Timeout";
        public const string StoreCancelled = "Transaction.Store.Cancelled";
        public const string StoreUnknown = "Transaction.Store.Unknown";

        // ---- Query expression translation ----
        public const string ExpressionTranslationFailed = "Transaction.Query.ExpressionTranslationFailed";
        public const string InvalidOperation          = "Transaction.Operation.Invalid";
    }
}