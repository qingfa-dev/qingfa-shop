using Infrastructure.Integrations;

using Microsoft.AspNetCore.Builder;

namespace Infrastructure.Unit.Tests.Integrations;

/// <summary>Unit tests for the integration composition root.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Integration")]
public sealed class IntegrationCompositionTest
{
    [Fact(DisplayName = "AddIntegration should return the same builder and register OpenAPI services")]
    public void AddIntegration_ShouldReturnSameBuilderAndRegisterOpenApi()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        WebApplicationBuilder result = builder.AddIntegration();

        result.Should().BeSameAs(builder);
        builder.Services.Any(service =>
            service.ServiceType.FullName?.Contains("OpenApi", StringComparison.Ordinal) == true)
            .Should().BeTrue();
        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType.FullName != null &&
            descriptor.ServiceType.FullName.Contains("INotificationService", StringComparison.Ordinal));
        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType.FullName != null &&
            descriptor.ServiceType.FullName.Contains("IStorageService", StringComparison.Ordinal));
        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType.FullName != null &&
            descriptor.ServiceType.FullName.Contains("CorrelationIdPropagationHandler", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "UseIntegration should return the same application")]
    public void UseIntegration_ShouldReturnSameApplication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddIntegration();
        WebApplication app = builder.Build();

        WebApplication result = app.UseIntegration();

        result.Should().BeSameAs(app);
    }

    [Fact(DisplayName = "AddIntegration should reject a null builder")]
    public void AddIntegration_WithNullBuilder_ShouldThrow()
    {
        WebApplicationBuilder? builder = null;

        Action act = () => builder!.AddIntegration();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "UseIntegration should reject a null application")]
    public void UseIntegration_WithNull_ShouldThrow()
    {
        WebApplication? app = null;

        Action act = () => app!.UseIntegration();

        act.Should().Throw<ArgumentNullException>();
    }
}
