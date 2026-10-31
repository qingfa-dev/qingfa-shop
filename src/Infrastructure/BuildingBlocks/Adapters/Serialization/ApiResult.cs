using System.Text.Json;

using Core.BuildingBlocks.Functional.Results;

using Microsoft.AspNetCore.Http;

namespace Infrastructure.BuildingBlocks.Adapters.Serialization;

/// <summary>Writes a value-less <see cref="Result"/> with its own status code.</summary>
public sealed class ApiResult : IResult
{
    private readonly Result _result;

    public ApiResult(Result result) => _result = result;

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        int statusCode = _result.StatusCode == 0 ? StatusCodes.Status400BadRequest : _result.StatusCode;

        if (statusCode == StatusCodes.Status204NoContent)
        {
            httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        return httpContext.Response.WriteAsync(JsonSerializer.Serialize(_result, ApiJson.Options));
    }
}

/// <summary>Writes a typed <see cref="Result{T}"/> with its own status code.</summary>
public sealed class ApiResult<T> : IResult
{
    private readonly Result<T> _result;
    private readonly string? _location;

    public ApiResult(Result<T> result, string? location = null)
    {
        _result = result;
        _location = location;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (_result.IsFailure)
            return new ApiResult(_result.ToResult()).ExecuteAsync(httpContext);

        int statusCode = _result.StatusCode == 0 ? StatusCodes.Status200OK : _result.StatusCode;

        if (statusCode == StatusCodes.Status204NoContent)
        {
            httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        }

        if (statusCode == StatusCodes.Status201Created && _location is not null)
            httpContext.Response.Headers.Location = _location;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        return httpContext.Response.WriteAsync(JsonSerializer.Serialize(_result, ApiJson.Options));
    }
}
