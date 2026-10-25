namespace Core.Common.Monad.Results;

public static partial class ResultExtension
{
    #region Pattern Matching

    public static TOut? Match<TOut>(
        this Result result,
        Func<TOut>? onSuccess,
        Func<List<Error>, TOut>? onFailure)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
            return onSuccess is null ? default : onSuccess();

        return onFailure is null ? default : onFailure(result.Errors);
    }

    public static TOut? Fold<TOut>(
        this Result result,
        Func<TOut>? onSuccess,
        Func<List<Error>, TOut>? onFailure)
        => result.Match(onSuccess, onFailure);

    #endregion

    #region Map

    public static Result<TOut> Map<TOut>(
        this Result result,
        Func<TOut>? mapper)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
        {
            return Result<TOut>.Fail(result.Errors, result.StatusCode)
                .WithMetadataFrom(result);
        }

        if (mapper is null)
        {
            return Result<TOut>.Fail(
                ResultOutcome.Failure.Flow.CallbackMissing(nameof(Map)));
        }

        return Result<TOut>.Ok(mapper(), result.StatusCode)
            .WithMetadataFrom(result);
    }

    public static Result<TOut> Map<TValue, TOut>(
        this Result<TValue> result,
        Func<TValue, TOut>? mapper)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
        {
            return Result<TOut>.Fail(result.Errors, result.StatusCode)
                .WithMetadataFrom(result);
        }

        if (mapper is null)
        {
            return Result<TOut>.Fail(
                ResultOutcome.Failure.Flow.CallbackMissing(nameof(Map)));
        }

        return Result<TOut>.Ok(mapper(result.Value), result.StatusCode)
            .WithMetadataFrom(result);
    }

    #endregion

    #region MapError

    public static Result MapError(
        this Result result,
        Func<Error, Error>? mapper)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
            return result;

        if (mapper is null)
        {
            return Result.Fail(
                ResultOutcome.Failure.Flow.CallbackMissing(nameof(MapError)));
        }

        var errors = new List<Error>(result.Errors.Count);

        foreach (var error in result.Errors)
        {
            var mapped = mapper(error);

            if (mapped is null)
            {
                return Result.Fail(
                    ResultOutcome.Failure.Flow.ErrorMissing(nameof(MapError)));
            }

            errors.Add(mapped);
        }

        return Result.Failure(errors, result.StatusCode)
            .WithMetadataFrom(result);
    }

    public static Result<TValue> MapError<TValue>(
        this Result<TValue> result,
        Func<Error, Error>? mapper)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
            return result;

        if (mapper is null)
        {
            return Result<TValue>.Fail(
                ResultOutcome.Failure.Flow.CallbackMissing(nameof(MapError)));
        }

        var errors = new List<Error>(result.Errors.Count);

        foreach (var error in result.Errors)
        {
            var mapped = mapper(error);

            if (mapped is null)
            {
                return Result<TValue>.Fail(
                    ResultOutcome.Failure.Flow.ErrorMissing(nameof(MapError)));
            }

            errors.Add(mapped);
        }

        return Result<TValue>.Fail(errors, result.StatusCode)
            .WithMetadataFrom(result);
    }

    #endregion

    #region Bind

    public static Result<TOut> Bind<TOut>(
        this Result result,
        Func<Result<TOut>>? binder)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
        {
            return Result<TOut>.Fail(result.Errors, result.StatusCode)
                .WithMetadataFrom(result);
        }

        if (binder is null)
        {
            return Result<TOut>.Fail(
                ResultOutcome.Failure.Flow.CallbackMissing(nameof(Bind)));
        }

        var bound = binder();

        if (bound is null)
        {
            return Result<TOut>.Fail(
                ResultOutcome.Failure.Flow.ResultMissing(nameof(Bind)));
        }

        return bound.WithMetadataFrom(result);
    }

    public static Result<TOut> Bind<TValue, TOut>(
        this Result<TValue> result,
        Func<TValue, Result<TOut>>? binder)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
        {
            return Result<TOut>.Fail(result.Errors, result.StatusCode)
                .WithMetadataFrom(result);
        }

        if (binder is null)
        {
            return Result<TOut>.Fail(
                ResultOutcome.Failure.Flow.CallbackMissing(nameof(Bind)));
        }

        var bound = binder(result.Value);

        if (bound is null)
        {
            return Result<TOut>.Fail(
                ResultOutcome.Failure.Flow.ResultMissing(nameof(Bind)));
        }

        return bound.WithMetadataFrom(result);
    }

    #endregion

    #region Flatten

    public static Result<TValue> Flatten<TValue>(
        this Result<Result<TValue>> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
        {
            return Result<TValue>.Fail(result.Errors, result.StatusCode)
                .WithMetadataFrom(result);
        }

        var inner = result.Value;

        if (inner is null)
        {
            return Result<TValue>.Fail(
                ResultOutcome.Failure.Flow.ResultMissing(nameof(Flatten)));
        }

        return inner.WithMetadataFrom(result);
    }

    #endregion

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

    #region Recover

    public static Result Recover(
        this Result result,
        Func<List<Error>, Result>? recovery)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
            return result;

        if (recovery is null)
        {
            return Result.Fail(
                ResultOutcome.Failure.Flow.CallbackMissing(nameof(Recover)));
        }

        var recovered = recovery(result.Errors);

        if (recovered is null)
        {
            return Result.Fail(
                ResultOutcome.Failure.Flow.ResultMissing(nameof(Recover)));
        }

        return recovered.WithMetadataFrom(result);
    }

    public static Result<TValue> Recover<TValue>(
        this Result<TValue> result,
        Func<List<Error>, Result<TValue>>? recovery)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
            return result;

        if (recovery is null)
        {
            return Result<TValue>.Fail(
                ResultOutcome.Failure.Flow.CallbackMissing(nameof(Recover)));
        }

        var recovered = recovery(result.Errors);

        if (recovered is null)
        {
            return Result<TValue>.Fail(
                ResultOutcome.Failure.Flow.ResultMissing(nameof(Recover)));
        }

        return recovered.WithMetadataFrom(result);
    }

    #endregion

    #region OrElse

    public static Result OrElse(
        this Result result,
        Func<Result>? fallback)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
            return result;

        if (fallback is null)
        {
            return Result.Fail(
                ResultOutcome.Failure.Flow.CallbackMissing(nameof(OrElse)));
        }

        var recovered = fallback();

        if (recovered is null)
        {
            return Result.Fail(
                ResultOutcome.Failure.Flow.ResultMissing(nameof(OrElse)));
        }

        return recovered.WithMetadataFrom(result);
    }

    public static Result<TValue> OrElse<TValue>(
        this Result<TValue> result,
        Func<Result<TValue>>? fallback)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
            return result;

        if (fallback is null)
        {
            return Result<TValue>.Fail(
                ResultOutcome.Failure.Flow.CallbackMissing(nameof(OrElse)));
        }

        var recovered = fallback();

        if (recovered is null)
        {
            return Result<TValue>.Fail(
                ResultOutcome.Failure.Flow.ResultMissing(nameof(OrElse)));
        }

        return recovered.WithMetadataFrom(result);
    }

    #endregion

    #region Try

    public static Result Try(
        Func<Result>? operation,
        Func<Exception, Error>? errorFactory = null)
    {
        if (operation is null)
        {
            return Result.Fail(
                ResultOutcome.Failure.Flow.CallbackMissing(nameof(Try)));
        }

        try
        {
            var result = operation();

            return result
                ?? Result.Fail(
                    ResultOutcome.Failure.Flow.ResultMissing(nameof(Try)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Result.Fail(
                CreateExceptionError(exception, errorFactory, nameof(Try)));
        }
    }

    public static Result<TValue> Try<TValue>(
        Func<Result<TValue>>? operation,
        Func<Exception, Error>? errorFactory = null)
    {
        if (operation is null)
        {
            return Result<TValue>.Fail(
                ResultOutcome.Failure.Flow.CallbackMissing(nameof(Try)));
        }

        try
        {
            var result = operation();

            return result
                ?? Result<TValue>.Fail(
                    ResultOutcome.Failure.Flow.ResultMissing(nameof(Try)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Result<TValue>.Fail(
                CreateExceptionError(exception, errorFactory, nameof(Try)));
        }
    }

    public static Result<TOut> MapTry<TValue, TOut>(
        this Result<TValue> result,
        Func<TValue, TOut>? mapper,
        Func<Exception, Error>? errorFactory = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
        {
            return Result<TOut>.Fail(result.Errors, result.StatusCode)
                .WithMetadataFrom(result);
        }

        if (mapper is null)
        {
            return Result<TOut>.Fail(
                    ResultOutcome.Failure.Flow.CallbackMissing(nameof(MapTry)))
                .WithMetadataFrom(result);
        }

        try
        {
            return Result<TOut>.Ok(mapper(result.Value), result.StatusCode)
                .WithMetadataFrom(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Result<TOut>.Fail(
                    CreateExceptionError(exception, errorFactory, nameof(MapTry)))
                .WithMetadataFrom(result);
        }
    }

    public static Result<TOut> BindTry<TValue, TOut>(
        this Result<TValue> result,
        Func<TValue, Result<TOut>>? binder,
        Func<Exception, Error>? errorFactory = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
        {
            return Result<TOut>.Fail(result.Errors, result.StatusCode)
                .WithMetadataFrom(result);
        }

        if (binder is null)
        {
            return Result<TOut>.Fail(
                    ResultOutcome.Failure.Flow.CallbackMissing(nameof(BindTry)))
                .WithMetadataFrom(result);
        }

        try
        {
            var bound = binder(result.Value);

            if (bound is null)
            {
                return Result<TOut>.Fail(
                        ResultOutcome.Failure.Flow.ResultMissing(nameof(BindTry)))
                    .WithMetadataFrom(result);
            }

            return bound.WithMetadataFrom(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Result<TOut>.Fail(
                    CreateExceptionError(exception, errorFactory, nameof(BindTry)))
                .WithMetadataFrom(result);
        }
    }

    #endregion

    #region Effects

    public static Result Tap(
        this Result result,
        Action? action)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
            action?.Invoke();

        return result;
    }

    public static Result<TValue> Tap<TValue>(
        this Result<TValue> result,
        Action<TValue>? action)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
            action?.Invoke(result.Value);

        return result;
    }

    public static Result TapError(
        this Result result,
        Action<List<Error>>? action)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
            action?.Invoke(result.Errors);

        return result;
    }

    public static Result<TValue> TapError<TValue>(
        this Result<TValue> result,
        Action<List<Error>>? action)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
            action?.Invoke(result.Errors);

        return result;
    }

    public static Result Switch(
        this Result result,
        Action? onSuccess,
        Action<List<Error>>? onFailure)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
            onSuccess?.Invoke();
        else
            onFailure?.Invoke(result.Errors);

        return result;
    }

    public static Result<TValue> Switch<TValue>(
        this Result<TValue> result,
        Action<TValue>? onSuccess,
        Action<List<Error>>? onFailure)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
            onSuccess?.Invoke(result.Value);
        else
            onFailure?.Invoke(result.Errors);

        return result;
    }

    #endregion

    #region Combination

    public static Result Combine(params Result[] results)
    {
        ArgumentNullException.ThrowIfNull(results);

        if (results.Length == 0)
            return Result.Ok();

        foreach (var result in results)
        {
            if (result is null)
            {
                return Result.Fail(
                    ResultOutcome.Failure.Flow.ResultMissing(nameof(Combine)));
            }
        }

        var failures = results
            .Where(result => result.IsFailure)
            .ToList();

        if (failures.Count == 0)
            return Result.Ok();

        var statusCode = failures[0].StatusCode;

        if (failures.Any(result => result.StatusCode != statusCode))
        {
            return Result.Fail(ResultOutcome.Failure.Errors.MixedStatusCodes);
        }

        var errors = failures.SelectMany(result => result.Errors).ToList();

        return Result.Failure(errors, statusCode);
    }

    #endregion

    #region Extraction

    public static TValue? ValueOr<TValue>(
        this Result<TValue> result,
        TValue? fallback = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? result.Value : fallback;
    }

    public static TValue ValueOrThrow<TValue>(this Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        result.ThrowIfFailure();

        return result.Value;
    }

    public static Result ThrowIfFailure(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
            throw new ResultException(result);

        return result;
    }

    public static Result<TValue> ThrowIfFailure<TValue>(
        this Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
            throw new ResultException(result);

        return result;
    }

    #endregion

    #region Exception Helpers

    private static Error CreateExceptionError(
        Exception exception,
        Func<Exception, Error>? errorFactory,
        string operation)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (errorFactory is null)
        {
            return Error.InternalServerError(
                "result.exception",
                "An unexpected exception occurred.");
        }

        try
        {
            return errorFactory(exception)
                ?? ResultOutcome.Failure.Flow.ExceptionMappingFailed(operation);
        }
        catch (Exception)
        {
            return ResultOutcome.Failure.Flow.ExceptionMappingFailed(operation);
        }
    }

    #endregion
}