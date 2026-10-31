using System.Net;
using System.Text.Json;

using Testing;
using Testing.Fixtures;

namespace Api.Integration.Tests.BuildingBlocks.Api;

/// <summary>Locks the wire shapes every API endpoint returns.</summary>
[Collection(ApiHostCollection.Name)]
public sealed class ResultContractApiTest : IntegrationTestBase
{
    public ResultContractApiTest(ShopApiFactory factory) : base(factory)
    {
    }

    [Fact(DisplayName = "GET /time should stay a raw payload")]
    public async Task GetTime_ShouldStayRaw()
    {
        using HttpResponseMessage response = await Client.GetAsync("/time");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using JsonDocument document = await ReadJsonAsync(response);

        document.RootElement.TryGetProperty("utcNow", out _).Should().BeTrue();
        document.RootElement.TryGetProperty("isSuccess", out _).Should().BeFalse();
        document.RootElement.TryGetProperty("value", out _).Should().BeFalse();
    }

    [Fact(DisplayName = "An unauthenticated /api request should return a Result body")]
    public async Task UnauthenticatedRequest_ShouldReturn401WithResultBody()
    {
        using HttpResponseMessage response = await Client.GetAsync("/api/todos");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().Should().Contain("Bearer");

        using JsonDocument document = await ReadJsonAsync(response);

        document.RootElement.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        document.RootElement.GetProperty("errors").GetArrayLength().Should().BePositive();
    }

    [Fact(DisplayName = "A failure body should not carry a value property or ProblemDetails")]
    public async Task UnauthenticatedRequest_ShouldNotReturnValueOrProblemDetails()
    {
        using HttpResponseMessage response = await Client.GetAsync("/api/todos");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using JsonDocument document = await ReadJsonAsync(response);

        document.RootElement.TryGetProperty("value", out _).Should().BeFalse();
        document.RootElement.TryGetProperty("title", out _).Should().BeFalse();
        document.RootElement.TryGetProperty("detail", out _).Should().BeFalse();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        return JsonDocument.Parse(json);
    }
}
