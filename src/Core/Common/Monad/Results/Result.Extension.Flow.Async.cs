namespace Core.Common.Monad.Results;

public static partial class ResultExtension
{
    #region MatchAsync

    public static Task<TResult?> MatchAsync<TValue, TResult>(
        this Result<TValue> result,
        Func<TValue, Task<TResult>>? onSuccess,
        Func<List<Error>, Task<TResult>>? onFailure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return MatchAsyncCore<TValue, TResult>(
            result,
            onSuccess is null ? null : (value, _) => onSuccess(value),
            onFailure is null ? null : (errors, _) => onFailure(errors),
            cancellationToken);
    }

    public static Task<TResult?> MatchAsync<TValue, TResult>(
        this Result<TValue> result,
        Func<TValue, CancellationToken, Task<TResult>>? onSuccess,
        Func<List<Error>, CancellationToken, Task<TResult>>? onFailure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return MatchAsyncCore<TValue, TResult>(
            result, onSuccess, onFailure, cancellationToken);
    }

    private static async Task<TResult?> MatchAsyncCore<TValue, TResult>(
        Result<TValue> result,
        Func<TValue, CancellationToken, Task<TResult>>? onSuccess,
        Func<List<Error>, CancellationToken, Task<TResult>>? onFailure,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var task = result.IsSuccess
            ? onSuccess?.Invoke(result.Value, cancellationToken)
            : onFailure?.Invoke(result.Errors, cancellationToken);

        if (task is null)
            return default;

        var value = await task.WaitAsync(cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        return value;
    }

    #endregion

    #region FoldAsync

    public static Task<TResult?> FoldAsync<TValue, TResult>(
        this Result<TValue> result,
        Func<TValue, Task<TResult>>? onSuccess,
        Func<List<Error>, Task<TResult>>? onFailure,
        CancellationToken cancellationToken = default)
        => MatchAsync<TValue, TResult>(result, onSuccess, onFailure, cancellationToken);

    public static Task<TResult?> FoldAsync<TValue, TResult>(
        this Result<TValue> result,
        Func<TValue, CancellationToken, Task<TResult>>? onSuccess,
        Func<List<Error>, CancellationToken, Task<TResult>>? onFailure,
        CancellationToken cancellationToken = default)
        => MatchAsync<TValue, TResult>(result, onSuccess, onFailure, cancellationToken);

    #endregion

    #region MapAsync

    public static Task<Result<TOut>> MapAsync<TValue, TOut>(
        this Result<TValue> result,
        Func<TValue, Task<TOut>>? mapper,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        Func<TValue, CancellationToken, Task<TOut>>? adaptedMapper =
            mapper is null ? null : (value, _) => mapper(value);

        return MapAsyncCore<TValue, TOut>(result, adaptedMapper, cancellationToken);
    }

    public static Task<Result<TOut>> MapAsync<TValue, TOut>(
        this Result<TValue> result,
        Func<TValue, CancellationToken, Task<TOut>>? mapper,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return MapAsyncCore<TValue, TOut>(result, mapper, cancellationToken);
    }

    private static async Task<Result<TOut>> MapAsyncCore<TValue, TOut>(
        Result<TValue> result,
        Func<TValue, CancellationToken, Task<TOut>>? mapper,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (result.IsFailure)
        {
            return Result<TOut>.Fail(result.Errors, result.StatusCode)
                .WithMetadataFrom(result);
        }

        if (mapper is null)
        {
            return Result<TOut>.Fail(
                    ResultOutcome.Failure.Flow.CallbackMissing(nameof(MapAsync)))
                .WithMetadataFrom(result);
        }

        var task = mapper(result.Value, cancellationToken);

        if (task is null)
        {
            return Result<TOut>.Fail(
                    ResultOutcome.Failure.Flow.TaskMissing(nameof(MapAsync)))
                .WithMetadataFrom(result);
        }

        var value = await task.WaitAsync(cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        return Result<TOut>.Ok(value, result.StatusCode).WithMetadataFrom(result);
    }

    #endregion

    #region BindAsync

    public static Task<Result<TOut>> BindAsync<TValue, TOut>(
        this Result<TValue> result,
        Func<TValue, Task<Result<TOut>>>? binder,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        Func<TValue, CancellationToken, Task<Result<TOut>>>? adaptedBinder =
            binder is null ? null : (value, _) => binder(value);

        return BindAsyncCore<TValue, TOut>(result, adaptedBinder, cancellationToken);
    }

    public static Task<Result<TOut>> BindAsync<TValue, TOut>(
        this Result<TValue> result,
        Func<TValue, CancellationToken, Task<Result<TOut>>>? binder,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return BindAsyncCore<TValue, TOut>(result, binder, cancellationToken);
    }

    private static async Task<Result<TOut>> BindAsyncCore<TValue, TOut>(
        Result<TValue> result,
        Func<TValue, CancellationToken, Task<Result<TOut>>>? binder,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (result.IsFailure)
        {
            return Result<TOut>.Fail(result.Errors, result.StatusCode)
                .WithMetadataFrom(result);
        }

        if (binder is null)
        {
            return Result<TOut>.Fail(
                    ResultOutcome.Failure.Flow.CallbackMissing(nameof(BindAsync)))
                .WithMetadataFrom(result);
        }

        var task = binder(result.Value, cancellationToken);

        if (task is null)
        {
            return Result<TOut>.Fail(
                    ResultOutcome.Failure.Flow.TaskMissing(nameof(BindAsync)))
                .WithMetadataFrom(result);
        }

        var next = await task.WaitAsync(cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        if (next is null)
        {
            return Result<TOut>.Fail(
                    ResultOutcome.Failure.Flow.ResultMissing(nameof(BindAsync)))
                .WithMetadataFrom(result);
        }

        return next.WithMetadataFrom(result);
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

    #region RecoverAsync

    public static Task<Result<TValue>> RecoverAsync<TValue>(
        this Result<TValue> result,
        Func<List<Error>, Task<TValue>>? recovery,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return RecoverAsyncCore(
            result,
            recovery is null ? null : (errors, _) => recovery(errors),
            cancellationToken);
    }

    public static Task<Result<TValue>> RecoverAsync<TValue>(
        this Result<TValue> result,
        Func<List<Error>, CancellationToken, Task<TValue>>? recovery,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return RecoverAsyncCore(result, recovery, cancellationToken);
    }

    private static async Task<Result<TValue>> RecoverAsyncCore<TValue>(
        Result<TValue> result,
        Func<List<Error>, CancellationToken, Task<TValue>>? recovery,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (result.IsSuccess)
            return result;

        if (recovery is null)
        {
            return Result<TValue>.Fail(
                    ResultOutcome.Failure.Flow.CallbackMissing(nameof(RecoverAsync)))
                .WithMetadataFrom(result);
        }

        var task = recovery(result.Errors, cancellationToken);

        if (task is null)
        {
            return Result<TValue>.Fail(
                    ResultOutcome.Failure.Flow.TaskMissing(nameof(RecoverAsync)))
                .WithMetadataFrom(result);
        }

        var value = await task.WaitAsync(cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        return Result<TValue>.Ok(value).WithMetadataFrom(result);
    }

    #endregion

    #region OrElseAsync

    public static Task<Result<TValue>> OrElseAsync<TValue>(
        this Result<TValue> result,
        Func<List<Error>, Task<Result<TValue>>>? fallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return OrElseAsyncCore(
            result,
            fallback is null ? null : (errors, _) => fallback(errors),
            cancellationToken);
    }

    public static Task<Result<TValue>> OrElseAsync<TValue>(
        this Result<TValue> result,
        Func<List<Error>, CancellationToken, Task<Result<TValue>>>? fallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return OrElseAsyncCore(result, fallback, cancellationToken);
    }

    private static async Task<Result<TValue>> OrElseAsyncCore<TValue>(
        Result<TValue> result,
        Func<List<Error>, CancellationToken, Task<Result<TValue>>>? fallback,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (result.IsSuccess)
            return result;

        if (fallback is null)
        {
            return Result<TValue>.Fail(
                    ResultOutcome.Failure.Flow.CallbackMissing(nameof(OrElseAsync)))
                .WithMetadataFrom(result);
        }

        var task = fallback(result.Errors, cancellationToken);

        if (task is null)
        {
            return Result<TValue>.Fail(
                    ResultOutcome.Failure.Flow.TaskMissing(nameof(OrElseAsync)))
                .WithMetadataFrom(result);
        }

        var recovered = await task.WaitAsync(cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        if (recovered is null)
        {
            return Result<TValue>.Fail(
                    ResultOutcome.Failure.Flow.ResultMissing(nameof(OrElseAsync)))
                .WithMetadataFrom(result);
        }

        return recovered.WithMetadataFrom(result);
    }

    #endregion

    #region TapAsync

    public static Task<Result<TValue>> TapAsync<TValue>(
        this Result<TValue> result,
        Func<TValue, Task>? action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return TapAsyncCore(
            result,
            action is null ? null : (value, _) => action(value),
            cancellationToken);
    }

    public static Task<Result<TValue>> TapAsync<TValue>(
        this Result<TValue> result,
        Func<TValue, CancellationToken, Task>? action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return TapAsyncCore(result, action, cancellationToken);
    }

    private static async Task<Result<TValue>> TapAsyncCore<TValue>(
        Result<TValue> result,
        Func<TValue, CancellationToken, Task>? action,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (result.IsSuccess && action is not null)
        {
            var task = action(result.Value, cancellationToken);

            // A missing callback task is treated as a no-op, preserving the result.
            if (task is null)
                return result;

            await task.WaitAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        return result;
    }

    #endregion

    #region TapErrorAsync

    public static Task<Result<TValue>> TapErrorAsync<TValue>(
        this Result<TValue> result,
        Func<List<Error>, Task>? action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return TapErrorAsyncCore(
            result,
            action is null ? null : (errors, _) => action(errors),
            cancellationToken);
    }

    public static Task<Result<TValue>> TapErrorAsync<TValue>(
        this Result<TValue> result,
        Func<List<Error>, CancellationToken, Task>? action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return TapErrorAsyncCore(result, action, cancellationToken);
    }

    private static async Task<Result<TValue>> TapErrorAsyncCore<TValue>(
        Result<TValue> result,
        Func<List<Error>, CancellationToken, Task>? action,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (result.IsFailure && action is not null)
        {
            var task = action(result.Errors, cancellationToken);

            // A missing callback task is treated as a no-op, preserving the result.
            if (task is null)
                return result;

            await task.WaitAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        return result;
    }

    #endregion

    #region SwitchAsync

    public static Task SwitchAsync(
        this Result result,
        Func<Task>? onSuccess,
        Func<List<Error>, Task>? onFailure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return SwitchAsyncCore(
            result,
            onSuccess is null ? null : _ => onSuccess(),
            onFailure is null ? null : (errors, _) => onFailure(errors),
            cancellationToken);
    }

    public static Task SwitchAsync(
        this Result result,
        Func<CancellationToken, Task>? onSuccess,
        Func<List<Error>, CancellationToken, Task>? onFailure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return SwitchAsyncCore(result, onSuccess, onFailure, cancellationToken);
    }

    private static async Task SwitchAsyncCore(
        Result result,
        Func<CancellationToken, Task>? onSuccess,
        Func<List<Error>, CancellationToken, Task>? onFailure,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var task = result.IsSuccess
            ? onSuccess?.Invoke(cancellationToken)
            : onFailure?.Invoke(result.Errors, cancellationToken);

        if (task is null)
            return;

        await task.WaitAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
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