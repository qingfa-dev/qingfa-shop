namespace Core.Common.Monad.Results;

public static partial class ResultExtension
{
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
}
