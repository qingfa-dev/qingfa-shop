namespace Core.Common.Monad.Results;

public static partial class ResultExtension
{
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
}
