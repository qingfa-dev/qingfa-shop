namespace Core.Common.Monad.Results;

public static partial class ResultExtension
{
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
}
