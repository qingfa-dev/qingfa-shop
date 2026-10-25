namespace Core.Common.Monad.Results;

public static partial class ResultExtension
{
    #region Ensure

    public static Result Ensure(
        this Result result,
        Func<bool>? predicate,
        Error? error)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
            return result;

        if (predicate is null)
        {
            return Result.Fail(
                ResultOutcome.Failure.Flow.CallbackMissing(nameof(Ensure)));
        }

        if (error is null)
        {
            return Result.Fail(
                ResultOutcome.Failure.Flow.ErrorMissing(nameof(Ensure)));
        }

        return predicate()
            ? result
            : Result.Fail(error).WithMetadataFrom(result);
    }

    public static Result<TValue> Ensure<TValue>(
        this Result<TValue> result,
        Func<TValue, bool>? predicate,
        Error? error)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
            return result;

        if (predicate is null)
        {
            return Result<TValue>.Fail(
                ResultOutcome.Failure.Flow.CallbackMissing(nameof(Ensure)));
        }

        if (error is null)
        {
            return Result<TValue>.Fail(
                ResultOutcome.Failure.Flow.ErrorMissing(nameof(Ensure)));
        }

        return predicate(result.Value)
            ? result
            : Result<TValue>.Fail(error).WithMetadataFrom(result);
    }

    /// <summary>
    /// Runs a validation against the result's value and fails the result when the
    /// validation failed.
    /// </summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <param name="validation">The validation to run against the result's value.</param>
    /// <returns>
    /// The result when the validation succeeds; otherwise a failure carrying the
    /// validation's errors and status code.
    /// </returns>
    public static Result<TValue> Ensure<TValue>(
        this Result<TValue> result,
        Func<TValue, Result> validation)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
        {
            return result;
        }

        ArgumentNullException.ThrowIfNull(validation);

        var outcome = validation(result.Value);

        if (outcome is null)
        {
            return Result<TValue>.Fail(
                ResultOutcome.Failure.Flow.ResultMissing(nameof(Ensure)));
        }

        // Assign: The failed validation is authoritative for the chain's errors and status code.
        return outcome.IsSuccess
            ? result
            : Result<TValue>.Fail(outcome.Errors, outcome.StatusCode)
                .WithMetadataFrom(result);
    }

    #endregion

    #region EnsureAsync

    public static Task<Result<TValue>> EnsureAsync<TValue>(
        this Result<TValue> result,
        Func<TValue, Task<bool>>? predicate,
        Error? error,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return EnsureAsyncCore(
            result,
            predicate is null ? null : (value, _) => predicate(value),
            error,
            cancellationToken);
    }

    public static Task<Result<TValue>> EnsureAsync<TValue>(
        this Result<TValue> result,
        Func<TValue, CancellationToken, Task<bool>>? predicate,
        Error? error,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return EnsureAsyncCore(result, predicate, error, cancellationToken);
    }

    private static async Task<Result<TValue>> EnsureAsyncCore<TValue>(
        Result<TValue> result,
        Func<TValue, CancellationToken, Task<bool>>? predicate,
        Error? error,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (result.IsFailure)
            return result;

        if (predicate is null)
        {
            return Result<TValue>.Fail(
                    ResultOutcome.Failure.Flow.CallbackMissing(nameof(EnsureAsync)))
                .WithMetadataFrom(result);
        }

        if (error is null)
        {
            return Result<TValue>.Fail(
                    ResultOutcome.Failure.Flow.ErrorMissing(nameof(EnsureAsync)))
                .WithMetadataFrom(result);
        }

        var task = predicate(result.Value, cancellationToken);

        if (task is null)
        {
            return Result<TValue>.Fail(
                    ResultOutcome.Failure.Flow.TaskMissing(nameof(EnsureAsync)))
                .WithMetadataFrom(result);
        }

        var isValid = await task.WaitAsync(cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        return isValid ? result : Result<TValue>.Fail(error).WithMetadataFrom(result);
    }

    #endregion
}
