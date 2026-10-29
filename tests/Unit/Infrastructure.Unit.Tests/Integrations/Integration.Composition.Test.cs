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

    [Fact(DisplayName = "UseIntegration should reject a null application")]
    public void UseIntegration_WithNull_ShouldThrow()
    {
        WebApplication? app = null;

        Action act = () => app!.UseIntegration();

        act.Should().Throw<ArgumentNullException>();
    }
}
