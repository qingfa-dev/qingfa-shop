using System.Net;
using System.Net.Http.Json;

using Testing;
using Testing.Fixtures;

namespace Api.Integration.Tests;

[Collection(ApiHostCollection.Name)]
public sealed class ApiSmokeTest : IntegrationTestBase
{
    public ApiSmokeTest(ShopApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetTime_ReturnsSuccess()
    {
        // Act
        using var response = await Client.GetAsync("/time");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<TimeResponse>();
        Assert.NotNull(payload);
    }

    private sealed record TimeResponse(DateTimeOffset UtcNow);
}