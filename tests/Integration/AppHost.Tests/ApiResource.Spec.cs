using AwesomeAssertions;

namespace AppHost.IntegrationTests;

[Collection(AppHostCollection.Name)]
public class ApiResourceSpec
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    private readonly AppHostFixture _fixture;

    public ApiResourceSpec(AppHostFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ApiResource_WhenAppHostStarts_ShouldReturnOkFromHealth()
    {
        JavaScriptResources.SkipUnlessToolchainAvailable();

        var app = await _fixture.GetAppAsync();

        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("api")
            .WaitAsync(Timeout);

        // Initialize: use the HTTP endpoint so the test does not depend on trusting
        // the local development certificate, matching the Compose runtime.
        using var httpClient = app.CreateHttpClient("api", "http");
        using var response = await httpClient.GetAsync("/health").WaitAsync(Timeout);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}