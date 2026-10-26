namespace Application.Common.Persistence.UnitOfWorks;
/// <summary>Transaction validation failures.</summary>
public static class TransactionOutcome
{
    public static class Failure
    {
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
    }

    /// <summary>Stable codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
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
    }
}