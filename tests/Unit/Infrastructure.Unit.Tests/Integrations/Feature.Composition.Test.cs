using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Infrastructure.Integrations.Backgrounds;
using Infrastructure.Integrations.Http;
using Infrastructure.Integrations.Notifications;
using Infrastructure.Integrations.Observability;
using Infrastructure.Integrations.Observability.Correlation;
using Infrastructure.Integrations.Observability.HealthChecks;
using Infrastructure.Integrations.Observability.Logging;
using Infrastructure.Integrations.Storages;

namespace Infrastructure.Unit.Tests.Integrations;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "IntegrationComposition")]
public sealed class FeatureCompositionTest
{
    [Fact(DisplayName = "AddStorage should register the storage services")]
    public void AddStorage_ShouldRegisterServices()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        WebApplicationBuilder result = builder.AddStorage();

        result.Should().BeSameAs(builder);
        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType.FullName == "Application.Common.Integrations.Storages.Services.IStorageService");
    }

    [Fact(DisplayName = "UseStorage should return the same application")]
    public void UseStorage_ShouldReturnSameApplication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddStorage();
        using WebApplication app = builder.Build();

        app.UseStorage().Should().BeSameAs(app);
    }

    [Fact(DisplayName = "AddHttpClients should register options and correlation")]
    public void AddHttpClients_ShouldRegisterOptions()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        WebApplicationBuilder result = builder.AddHttpClients();

        result.Should().BeSameAs(builder);
        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType.FullName == "Infrastructure.Integrations.Http.CorrelationIdPropagationHandler");
    }

    [Fact(DisplayName = "AddTypedHttpClient should register typed clients")]
    public void AddTypedHttpClient_ShouldRegisterClients()
    {
        var services = new ServiceCollection();

        services.AddTypedHttpClient<IFakeClient>("https://example.test");
        services.AddTypedHttpClient<IFakeClient, FakeClient>("https://example.test");
        services.AddTypedHttpClient<IFakeClient, FakeClient, CorrelationIdPropagationHandler>("https://example.test");

        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IFakeClient));
    }

    [Fact(DisplayName = "AddBackgroundJobs should register Hangfire services")]
    public void AddBackgroundJobs_ShouldRegisterServices()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        WebApplicationBuilder result = builder.AddBackgroundJobs();

        result.Should().BeSameAs(builder);
        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType.FullName != null && descriptor.ServiceType.FullName.Contains("Hangfire", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "UseBackgroundJobs should return the same application when disabled")]
    public void UseBackgroundJobs_WhenDisabled_ShouldReturnSameApplication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddBackgroundJobs();
        using WebApplication app = builder.Build();

        app.UseBackgroundJobs().Should().BeSameAs(app);
    }

    [Fact(DisplayName = "AddObservability should register correlation, logging, and health checks")]
    public void AddObservability_ShouldRegisterServices()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();

        WebApplicationBuilder result = builder.AddObservability();

        result.Should().BeSameAs(builder);
        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ICorrelationContext));
    }

    [Fact(DisplayName = "UseObservability should return the same application")]
    public void UseObservability_ShouldReturnSameApplication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.AddObservability();
        using WebApplication app = builder.Build();

        app.UseObservability().Should().BeSameAs(app);
    }

    [Fact(DisplayName = "Observability pieces should be individually composable")]
    public void ObservabilityPieces_ShouldBeComposable()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(new ObservabilitySetting());

        builder.AddObservabilityCorrelation()
            .AddObservabilityLogging()
            .AddObservabilityHealthChecks()
            .Should().BeSameAs(builder);

        using WebApplication app = builder.Build();
        app.UseObservabilityCorrelation()
            .UseObservabilityLogging()
            .UseObservabilityHealthChecks()
            .Should().BeSameAs(app);
    }

    [Fact(DisplayName = "AddNotifications should register the notification pipeline")]
    public void AddNotifications_ShouldRegisterServices()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        WebApplicationBuilder result = builder.AddNotifications();

        result.Should().BeSameAs(builder);
        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType.FullName == "Application.Common.Integrations.Notifications.Services.INotificationService");
    }

    public interface IFakeClient;

    public sealed class FakeClient : IFakeClient;

    [Fact(DisplayName = "AddHttpClients should configure named clients")]
    public void AddHttpClients_WithConfiguredClient_ShouldRegisterNamedClient()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddHttpContextAccessor();
        builder.Configuration["Http:DefaultTimeoutSeconds"] = "20";
        builder.Configuration["Http:Clients:test:BaseAddress"] = "https://example.test";
        builder.Configuration["Http:Clients:test:TimeoutSeconds"] = "10";

        builder.AddHttpClients();

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();
        using HttpClient client = factory.CreateClient("test");
        client.BaseAddress.Should().Be(new Uri("https://example.test"));
        client.Timeout.Should().Be(TimeSpan.FromSeconds(10));
    }
}
