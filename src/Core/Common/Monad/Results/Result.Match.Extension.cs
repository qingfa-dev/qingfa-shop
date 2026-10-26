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
}
