using Core.Common.Monad.Results;

namespace Web.Api.Endpoints;

/// <summary>Maps a monadic <see cref="Result"/> to an HTTP response.</summary>
public static class ApiResult
{
    /// <summary>Maps a typed result to its success response, or to ProblemDetails on failure.</summary>
    /// <typeparam name="T">The success payload type.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="success">Builds the success response; defaults to <c>200 OK</c> with the value.</param>
    /// <returns>The HTTP result.</returns>
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? success = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? success is null ? Results.Ok(result.Value) : success(result.Value)
            : Problem(result);
    }

    /// <summary>Maps a value-less result to <c>204 No Content</c>, or to ProblemDetails on failure.</summary>
    /// <param name="result">The result to map.</param>
    /// <returns>The HTTP result.</returns>
    public static IResult ToHttpResult(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? Results.NoContent() : Problem(result);
    }

    private static IResult Problem(Result result)
        => Results.Problem(
            title: result.Errors[0].Code,
            detail: result.Errors[0].Message,
            statusCode: result.StatusCode);
}
