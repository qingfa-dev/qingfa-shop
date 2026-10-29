using Microsoft.Extensions.DependencyInjection;

using Infrastructure.Integrations.Http;

namespace Infrastructure.Unit.Tests.Integrations.Http;

[Trait("Category", "Unit")]
[Trait("Feature", "Http")]
public class ExtensionsTests
{
    public sealed class TestClient(HttpClient client)
    {
        public HttpClient Client => client;
    }

    [Fact(DisplayName = "AddTypedHttpClient should register typed client with correct base address")]
    public void AddTypedHttpClient_ShouldRegisterTypedClient()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdPropagationHandler>();
        services.AddTypedHttpClient<TestClient>("https://api.example.com");

        var sp = services.BuildServiceProvider();
        var clientFactory = sp.GetRequiredService<IHttpClientFactory>();

        var client = clientFactory.CreateClient(typeof(TestClient).Name);

        client.BaseAddress.Should().Be("https://api.example.com/");
    }

    [Fact(DisplayName = "AddTypedHttpClient should resolve typed client")]
    public void AddTypedHttpClient_ShouldResolveTypedClient()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdPropagationHandler>();
        services.AddTypedHttpClient<TestClient>("https://api.example.com");

        var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<TestClient>();

        client.Should().NotBeNull();
        client.Client.Should().NotBeNull();
        client.Client.BaseAddress.Should().Be("https://api.example.com/");
    }

    [Fact(DisplayName = "AddTypedHttpClient with attachResilience false should not throw")]
    public void AddTypedHttpClient_WithoutResilience_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdPropagationHandler>();
        services.AddTypedHttpClient<TestClient>("https://api.example.com", attachResilience: false);

        var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<TestClient>();

        client.Should().NotBeNull();
    }
}
