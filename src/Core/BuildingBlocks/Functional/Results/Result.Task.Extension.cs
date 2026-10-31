namespace Core.BuildingBlocks.Functional.Results;

public static partial class ResultExtension
{
    #region Then

    /// <summary>
    /// Continues the chain with a non-generic result while keeping the current value.
    /// </summary>
    /// <typeparam name="TValue">The value carried by the chain.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="next">The next asynchronous step.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current result, or the next failure.</returns>
    public static async Task<Result<TValue>> ThenAsync<TValue>(
        this Result<TValue> result,
        Func<TValue, CancellationToken, Task<Result>> next,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        cancellationToken.ThrowIfCancellationRequested();

        if (result.IsFailure)
        {
            return result;
        }

        ArgumentNullException.ThrowIfNull(next);

        Result outcome = await next(result.Value, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return outcome.IsFailure
            ? Result<TValue>.Fail(outcome.Errors, outcome.StatusCode)
            : result;
    }

    #endregion

    #region Awaitable Chains

    /// <summary>Continues a pending chain with a synchronous step.</summary>
    /// <typeparam name="TValue">The value carried by the chain.</typeparam>
    /// <typeparam name="TOut">The value produced by the next step.</typeparam>
    /// <param name="chain">The pending chain.</param>
    /// <param name="next">The next step.</param>
    /// <returns>The next result, or the current failure.</returns>
    public static async Task<Result<TOut>> BindAsync<TValue, TOut>(
        this Task<Result<TValue>> chain,
        Func<TValue, Result<TOut>> next)
    {
        ArgumentNullException.ThrowIfNull(chain);

        Result<TValue> current = await chain.ConfigureAwait(false);
        return current.Bind(next);
    }

    /// <summary>Continues a pending chain with an asynchronous step.</summary>
    /// <typeparam name="TValue">The value carried by the chain.</typeparam>
    /// <typeparam name="TOut">The value produced by the next step.</typeparam>
    /// <param name="chain">The pending chain.</param>
    /// <param name="next">The next step.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The next result, or the current failure.</returns>
    public static async Task<Result<TOut>> BindAsync<TValue, TOut>(
        this Task<Result<TValue>> chain,
        Func<TValue, CancellationToken, Task<Result<TOut>>> next,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chain);

        Result<TValue> current = await chain.ConfigureAwait(false);
        return await current.BindAsync(next, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Projects the value of a pending chain.</summary>
    /// <typeparam name="TValue">The value carried by the chain.</typeparam>
    /// <typeparam name="TOut">The projected value.</typeparam>
    /// <param name="chain">The pending chain.</param>
    /// <param name="mapper">The projection.</param>
    /// <returns>The projected result, or the current failure.</returns>
    public static async Task<Result<TOut>> MapAsync<TValue, TOut>(
        this Task<Result<TValue>> chain,
        Func<TValue, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(chain);

        Result<TValue> current = await chain.ConfigureAwait(false);
        return current.Map(mapper);
    }

    /// <summary>Continues a pending chain with a non-generic step.</summary>
    /// <typeparam name="TValue">The value carried by the chain.</typeparam>
    /// <param name="chain">The pending chain.</param>
    /// <param name="next">The next step.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current result, or the next failure.</returns>
    public static async Task<Result<TValue>> ThenAsync<TValue>(
        this Task<Result<TValue>> chain,
        Func<TValue, CancellationToken, Task<Result>> next,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chain);

        Result<TValue> current = await chain.ConfigureAwait(false);
        return await current.ThenAsync(next, cancellationToken).ConfigureAwait(false);
    }

    #endregion
}
