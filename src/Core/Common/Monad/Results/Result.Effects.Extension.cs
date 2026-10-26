namespace Core.Common.Monad.Results;

public static partial class ResultExtension
{
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
}
