namespace Core.Common.Monad.Results;

public static partial class ResultExtension
{
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

    #region TryAsync<TValue>

    // Legacy overload: accepts a Func<Task<TValue>>.
    public static Task<Result<TValue>> TryAsync<TValue>(
        this Func<Task<TValue>>? operation,
        Func<Exception, Error>? errorFactory = null,
        CancellationToken cancellationToken = default)
    {
        Func<CancellationToken, Task<TValue>>? adaptedOperation =
            operation is null ? null : _ => operation();

        return TryAsyncCore<TValue>(adaptedOperation, errorFactory, cancellationToken);
    }

    // CancellationToken-aware overload.
    public static Task<Result<TValue>> TryAsync<TValue>(
        this Func<CancellationToken, Task<TValue>>? operation,
        Func<Exception, Error>? errorFactory = null,
        CancellationToken cancellationToken = default)
        => TryAsyncCore<TValue>(operation, errorFactory, cancellationToken);

    private static async Task<Result<TValue>> TryAsyncCore<TValue>(
        Func<CancellationToken, Task<TValue>>? operation,
        Func<Exception, Error>? errorFactory,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (operation is null)
        {
            return Result<TValue>.Fail(
                ResultOutcome.Failure.Flow.CallbackMissing(nameof(TryAsync)));
        }

        try
        {
            var task = operation(cancellationToken);

            if (task is null)
            {
                return Result<TValue>.Fail(
                    ResultOutcome.Failure.Flow.TaskMissing(nameof(TryAsync)));
            }

            var value = await task.WaitAsync(cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            return Result<TValue>.Ok(value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Result<TValue>.Fail(
                CreateExceptionError(exception, errorFactory, nameof(TryAsync)));
        }
    }

    #endregion

    #region TryAsync (non-generic)

    // Legacy overload: accepts a Func<Task>.
    public static Task<Result> TryAsync(
        this Func<Task>? operation,
        Func<Exception, Error>? errorFactory = null,
        CancellationToken cancellationToken = default)
    {
        Func<CancellationToken, Task>? adaptedOperation = operation is null
            ? null
            : async _ => await operation().ConfigureAwait(false);

        return TryAsyncCore(adaptedOperation, errorFactory, cancellationToken);
    }

    // CancellationToken-aware overload.
    public static Task<Result> TryAsync(
        this Func<CancellationToken, Task>? operation,
        Func<Exception, Error>? errorFactory = null,
        CancellationToken cancellationToken = default)
        => TryAsyncCore(operation, errorFactory, cancellationToken);

    private static async Task<Result> TryAsyncCore(
        Func<CancellationToken, Task>? operation,
        Func<Exception, Error>? errorFactory,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (operation is null)
        {
            return Result.Fail(
                ResultOutcome.Failure.Flow.CallbackMissing(nameof(TryAsync)));
        }

        try
        {
            var task = operation(cancellationToken);

            if (task is null)
            {
                return Result.Fail(
                    ResultOutcome.Failure.Flow.TaskMissing(nameof(TryAsync)));
            }

            await task.WaitAsync(cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            return Result.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Result.Fail(
                CreateExceptionError(exception, errorFactory, nameof(TryAsync)));
        }
    }

    #endregion
}
