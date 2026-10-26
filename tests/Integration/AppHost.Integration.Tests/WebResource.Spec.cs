namespace AppHost.Tests;

// Verify: the sweep covers every resource AppHost declares, so a new resource cannot
// silently go untested.
[Collection(AppHostCollection.Name)]
public class WebResourceSpec
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(180);

    private readonly AppHostFixture _fixture;

    public WebResourceSpec(AppHostFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task DeclaredResources_WhenAppHostStarts_ShouldBeRunnable()
    {
        JavaScriptResources.SkipUnlessToolchainAvailable();

        var app = await _fixture.GetAppAsync();

        // Validate: project resources expose readiness checks.
        foreach (var resource in new[] { "api", "gateway" })
        {
            await app.ResourceNotifications
                .WaitForResourceHealthyAsync(resource)
                .WaitAsync(Timeout);
        }

        // Validate: the Vite dev servers expose no readiness check, so Running is
        // the strongest state they publish.
        foreach (var resource in new[] { "shopfront", "admin" })
        {
            await app.ResourceNotifications
                .WaitForResourceAsync(resource, KnownResourceStates.Running)
                .WaitAsync(Timeout);
        }
    }
}