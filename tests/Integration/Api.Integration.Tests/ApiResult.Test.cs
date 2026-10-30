using System.Text;
using System.Text.Json;

using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

using Microsoft.AspNetCore.Http;

using Infrastructure.Integrations.Serialization;

namespace Api.Integration.Tests;

[Trait("Category", "Unit")]
public sealed class ApiResultTest
{
    [Fact]
    public async Task Created_SetsStatusLocationAndValue()
    {
        ApiResult<int> result = new(Result<int>.Created(7), "/api/things/7");

        HttpContext context = await ExecuteAsync(result);

        context.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        context.Response.Headers.Location.ToString().Should().Be("/api/things/7");

        using JsonDocument document = JsonDocument.Parse(ReadBody(context));
        document.RootElement.GetProperty("value").GetInt32().Should().Be(7);
    }

    [Fact]
    public async Task NoContent_WritesNoBodyOrContentType()
    {
        ApiResult<int> result = new(Result<int>.Ok(0, StatusCodes.Status204NoContent));

        HttpContext context = await ExecuteAsync(result);

        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        context.Response.ContentType.Should().BeNull();
        ReadBody(context).Should().BeEmpty();
    }

    [Fact]
    public async Task BadRequestFailure_WritesFailureBodyWithoutValue()
    {
        ApiResult<int> result = new(Result<int>.Fail(Error.BadRequest("test.bad", "bad")));

        HttpContext context = await ExecuteAsync(result);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        using JsonDocument document = JsonDocument.Parse(ReadBody(context));
        document.RootElement.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        document.RootElement.TryGetProperty("value", out _).Should().BeFalse();
    }

    [Fact]
    public async Task NotFoundFailure_WritesFailureBodyWithoutValue()
    {
        ApiResult<int> result = new(Result<int>.Fail(Error.NotFound("test.notfound", "missing")));

        HttpContext context = await ExecuteAsync(result);

        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        using JsonDocument document = JsonDocument.Parse(ReadBody(context));
        document.RootElement.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        document.RootElement.TryGetProperty("value", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Success_WritesValueInBody()
    {
        ApiResult<int> result = new(Result<int>.Ok(42));

        HttpContext context = await ExecuteAsync(result);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context.Response.Headers.Location.ToString().Should().BeEmpty();

        using JsonDocument document = JsonDocument.Parse(ReadBody(context));
        document.RootElement.GetProperty("value").GetInt32().Should().Be(42);
    }

    [Fact]
    public async Task Location_IsIgnoredUnlessCreated()
    {
        IResult result = Result<int>.Ok(42).ToApiResult(_ => "/api/things/42");

        HttpContext context = await ExecuteAsync(result);

        context.Response.Headers.Location.ToString().Should().BeEmpty();
    }

    private static async Task<HttpContext> ExecuteAsync(IResult result)
    {
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();

        await result.ExecuteAsync(context);

        return context;
    }

    private static string ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;

        using StreamReader reader = new(
            context.Response.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        return reader.ReadToEnd();
    }
}
