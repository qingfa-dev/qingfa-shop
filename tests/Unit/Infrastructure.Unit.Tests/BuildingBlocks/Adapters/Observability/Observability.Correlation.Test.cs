using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Infrastructure.BuildingBlocks.Adapters.Observability;
using Infrastructure.BuildingBlocks.Adapters.Observability.Correlation;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Observability;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Observability")]
public sealed class ObservabilityCorrelationTest
{
    [Fact(DisplayName = "CorrelationMiddleware should preserve an incoming correlation id")]
    public async Task InvokeAsync_WithIncomingHeader_ShouldPreserveId()
    {
        var context = new DefaultHttpContext();
        var correlation = new CorrelationContext();
        var services = new ServiceCollection();
        services.AddSingleton<ICorrelationContext>(correlation);
        context.RequestServices = services.BuildServiceProvider();
        context.Request.Headers["X-Correlation-ID"] = "existing-id";

        var middleware = new CorrelationMiddleware(
            _ => Task.CompletedTask,
            new ObservabilitySetting(),
            NullLogger<CorrelationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        correlation.CorrelationId.Should().Be("existing-id");
        context.Response.Headers["X-Correlation-ID"].ToString().Should().Be("existing-id");
    }

    [Fact(DisplayName = "CorrelationMiddleware should generate a correlation id when absent")]
    public async Task InvokeAsync_WithoutHeader_ShouldGenerateId()
    {
        var context = new DefaultHttpContext();
        var correlation = new CorrelationContext();
        var services = new ServiceCollection();
        services.AddSingleton<ICorrelationContext>(correlation);
        context.RequestServices = services.BuildServiceProvider();

        var middleware = new CorrelationMiddleware(
            _ => Task.CompletedTask,
            new ObservabilitySetting(),
            NullLogger<CorrelationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        correlation.CorrelationId.Should().NotBeNullOrWhiteSpace();
        context.Response.Headers["X-Correlation-ID"].ToString().Should().Be(correlation.CorrelationId);
    }
}
