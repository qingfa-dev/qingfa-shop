using Infrastructure.BuildingBlocks.Adapters.OpenApi;

using Microsoft.AspNetCore.Builder;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.OpenApi;

/// <summary>Unit tests for the OpenAPI composition extensions.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Integration")]
public sealed class OpenApiCompositionTest
{
    [Fact(DisplayName = "AddOpenApiDocumentation should return the same builder and register services")]
    public void AddOpenApiDocumentation_ShouldReturnSameBuilderAndRegisterServices()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        WebApplicationBuilder result = builder.AddOpenApiDocumentation();

        result.Should().BeSameAs(builder);
        builder.Services.Any(service =>
            service.ServiceType.FullName?.Contains("OpenApi", StringComparison.Ordinal) == true)
            .Should().BeTrue();
    }

    [Fact(DisplayName = "UseOpenApiDocumentation should return the same application")]
    public void UseOpenApiDocumentation_ShouldReturnSameApplication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddOpenApiDocumentation();
        WebApplication app = builder.Build();

        WebApplication result = app.UseOpenApiDocumentation();

        result.Should().BeSameAs(app);
    }
}
