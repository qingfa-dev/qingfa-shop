using Infrastructure.BuildingBlocks;
using Infrastructure.BuildingBlocks.Adapters;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Composition;

/// <summary>Unit tests for the adapter composition root.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Adapters")]
public sealed class AdaptersCompositionTest
{
    [Fact(DisplayName = "AddAdapters should return the same builder and register OpenAPI services")]
    public void AddAdapters_ShouldReturnSameBuilderAndRegisterOpenApi()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        WebApplicationBuilder result = builder.AddAdapters(builder.Configuration, InfrastructureModuleRegistrations.Empty.Messaging);

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

    [Fact(DisplayName = "UseAdapters should return the same application")]
    public void UseAdapters_ShouldReturnSameApplication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddAdapters(builder.Configuration, InfrastructureModuleRegistrations.Empty.Messaging);
        WebApplication app = builder.Build();

        WebApplication result = app.UseAdapters();

        result.Should().BeSameAs(app);
    }

    [Fact(DisplayName = "AddAdapters should reject a null builder")]
    public void AddAdapters_WithNullBuilder_ShouldThrow()
    {
        WebApplicationBuilder? builder = null;
        IConfiguration configuration = new ConfigurationBuilder().Build();

        Action act = () => builder!.AddAdapters(configuration, InfrastructureModuleRegistrations.Empty.Messaging);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "UseAdapters should reject a null application")]
    public void UseAdapters_WithNull_ShouldThrow()
    {
        WebApplication? app = null;

        Action act = () => app!.UseAdapters();

        act.Should().Throw<ArgumentNullException>();
    }
}
