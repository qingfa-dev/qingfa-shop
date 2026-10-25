namespace Core.Common.Monad.Results;

public static partial class ResultExtension
{
    #region Status

    /// <summary>
    /// Replaces the HTTP status code and returns a new result.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="statusCode">The new HTTP status code.</param>
    /// <returns>A new result with the updated status code.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// </exception>
    public static Result WithStatus(
        this Result result,
        int statusCode)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.CopyWith(
            statusCode: statusCode);
    }

    /// <summary>
    /// Replaces the HTTP status code and returns a new result.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="statusCode">The new HTTP status code.</param>
    /// <returns>A new result with the updated status code.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// </exception>
    public static Result<TValue> WithStatus<TValue>(
        this Result<TValue> result,
        int statusCode)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.CopyWith(
            statusCode: statusCode);
    }

    #endregion
}
