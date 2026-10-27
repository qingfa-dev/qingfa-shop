namespace Application.Common.Persistence.UnitOfWorks;
/// <summary>
/// Guard utilities for validating transaction and unit of work values against <see cref="TransactionConstant"/> limits.
/// </summary>
/// <remarks>
/// Returns <see cref="Result"/> instead of throwing, enabling monadic validation composition.
/// </remarks>
public static class TransactionGuard
{
    #region Command Timeout

    /// <summary>
    /// Validates a command timeout in seconds.
    /// </summary>
    /// <param name="timeoutSeconds">The timeout to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateCommandTimeout(
        int timeoutSeconds,
        string parameterName = "timeoutSeconds")
    {
        if (timeoutSeconds < TransactionConstant.Limits.MinCommandTimeoutSeconds)
        {
            return Result.Fail(TransactionOutcome.Failure.CommandTimeoutTooSmall(parameterName));
        }

        if (timeoutSeconds > TransactionConstant.Limits.MaxCommandTimeoutSeconds)
        {
            return Result.Fail(TransactionOutcome.Failure.CommandTimeoutTooLarge(parameterName));
        }

        return Result.Ok();
    }

    #endregion

    #region Retry Policy

    /// <summary>
    /// Validates max retry attempts.
    /// </summary>
    /// <param name="maxAttempts">The max attempts to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateMaxRetryAttempts(
        int maxAttempts,
        string parameterName = "maxAttempts")
    {
        if (maxAttempts < 0)
        {
            return Result.Fail(TransactionOutcome.Failure.MaxRetryAttemptsNegative(parameterName));
        }

        if (maxAttempts > TransactionConstant.Limits.MaxRetryAttempts)
        {
            return Result.Fail(TransactionOutcome.Failure.MaxRetryAttemptsExceedsMax(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates retry base delay in milliseconds.
    /// </summary>
    /// <param name="baseDelayMs">The base delay to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateRetryBaseDelay(
        int baseDelayMs,
        string parameterName = "baseDelayMs")
    {
        if (baseDelayMs < 0)
        {
            return Result.Fail(TransactionOutcome.Failure.RetryBaseDelayNegative(parameterName));
        }

        if (baseDelayMs > TransactionConstant.Limits.MaxRetryBaseDelayMs)
        {
            return Result.Fail(TransactionOutcome.Failure.RetryBaseDelayExceedsMax(parameterName));
        }

        return Result.Ok();
    }

    #endregion

    #region Connection Pool

    /// <summary>
    /// Validates connection pool min size.
    /// </summary>
    /// <param name="minSize">The min size to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidatePoolMinSize(
        int minSize,
        string parameterName = "minSize")
    {
        if (minSize < 0)
        {
            return Result.Fail(TransactionOutcome.Failure.PoolMinSizeNegative(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates connection pool max size.
    /// </summary>
    /// <param name="maxSize">The max size to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidatePoolMaxSize(
        int maxSize,
        string parameterName = "maxSize")
    {
        if (maxSize < 0)
        {
            return Result.Fail(TransactionOutcome.Failure.PoolMaxSizeNegative(parameterName));
        }

        if (maxSize > TransactionConstant.Limits.MaxPoolSize)
        {
            return Result.Fail(TransactionOutcome.Failure.PoolMaxSizeExceedsMax(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates connection pool sizes together (min less than or equal to max).
    /// </summary>
    /// <param name="minSize">The min size.</param>
    /// <param name="maxSize">The max size.</param>
    /// <param name="minParameterName">The min parameter name.</param>
    /// <param name="maxParameterName">The max parameter name.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidatePoolSizes(
        int minSize,
        int maxSize,
        string minParameterName = "minSize",
        string maxParameterName = "maxSize")
    {
        var results = new List<Result>
        {
            ValidatePoolMinSize(minSize, minParameterName),
            ValidatePoolMaxSize(maxSize, maxParameterName)
        };

        if (minSize > maxSize)
        {
            results.Add(Result.Fail(
                TransactionOutcome.Failure.PoolMinExceedsMax(minParameterName, maxParameterName)));
        }

        return ResultExtension.Combine([.. results]);
    }

    #endregion

    #region Isolation Level

    /// <summary>
    /// Validates an isolation level string.
    /// </summary>
    /// <param name="isolationLevel">The isolation level to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateIsolationLevel(
        string? isolationLevel,
        string parameterName = "isolationLevel")
    {
        if (isolationLevel is null)
        {
            return Result.Ok(); // Null means use default
        }

        var validLevels = new[]
        {
            TransactionConstant.IsolationLevel.ReadUncommitted,
            TransactionConstant.IsolationLevel.ReadCommitted,
            TransactionConstant.IsolationLevel.RepeatableRead,
            TransactionConstant.IsolationLevel.Serializable,
            TransactionConstant.IsolationLevel.Snapshot,
            TransactionConstant.IsolationLevel.Unspecified
        };

        if (!validLevels.Contains(isolationLevel))
        {
            return Result.Fail(TransactionOutcome.Failure.IsolationLevelInvalid(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates a System.Data.IsolationLevel enum value.
    /// </summary>
    /// <param name="isolationLevel">The isolation level to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateIsolationLevel(
        System.Data.IsolationLevel? isolationLevel,
        string parameterName = "isolationLevel")
    {
        if (isolationLevel is null)
        {
            return Result.Ok();
        }

        var validLevels = new[]
        {
            System.Data.IsolationLevel.ReadUncommitted,
            System.Data.IsolationLevel.ReadCommitted,
            System.Data.IsolationLevel.RepeatableRead,
            System.Data.IsolationLevel.Serializable,
            System.Data.IsolationLevel.Snapshot,
            System.Data.IsolationLevel.Unspecified
        };

        if (!validLevels.Contains(isolationLevel.Value))
        {
            return Result.Fail(TransactionOutcome.Failure.IsolationLevelInvalid(parameterName));
        }

        return Result.Ok();
    }

    #endregion

    #region Transaction State

    /// <summary>
    /// Validates a transaction state string.
    /// </summary>
    /// <param name="state">The state to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateState(
        string? state,
        string parameterName = "state")
    {
        if (state is null)
        {
            return Result.Fail(TransactionOutcome.Failure.StateNull(parameterName));
        }

        var validStates = new[]
        {
            TransactionConstant.State.Active,
            TransactionConstant.State.Committed,
            TransactionConstant.State.RolledBack,
            TransactionConstant.State.RollbackOnly,
            TransactionConstant.State.Failed
        };

        if (!validStates.Contains(state))
        {
            return Result.Fail(TransactionOutcome.Failure.StateInvalid(parameterName));
        }

        return Result.Ok();
    }

    #endregion
}