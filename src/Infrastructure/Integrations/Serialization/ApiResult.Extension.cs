using Core.Common.Monad.Results;

using Microsoft.AspNetCore.Http;

namespace Infrastructure.Integrations.Serialization;

/// <summary>Maps a monadic result to the shared HTTP result.</summary>
public static class ApiResultExtension
{
    /// <summary>Writes a typed result with its own status code.</summary>
    public static IResult ToApiResult<T>(
        this Result<T> result,
        Func<T, string>? location = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        string? uri = result.IsSuccess
            && result.StatusCode == StatusCodes.Status201Created
            && location is not null
            ? location(result.Value)
            : null;

        return new ApiResult<T>(result, uri);
    }

    /// <summary>Writes a value-less result with its own status code.</summary>
    public static IResult ToApiResult(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new ApiResult(result);
    }
}
