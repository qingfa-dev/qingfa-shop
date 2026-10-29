using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Unit.Tests.Common.Composition;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "ApplicationComposition")]
public class ApplicationCompositionTest
{
    [Fact(DisplayName = "AddApplication should register MediatR services")]
    public void AddApplication_ShouldRegisterRequiredServices()
    {
        // Arrange
        WebApplicationBuilder builder = CreateBuilder();

        // Act
        WebApplicationBuilder returned = builder.AddApplication();
        using ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        returned.Should().BeSameAs(builder);
        serviceProvider.GetService<IMediator>().Should().NotBeNull();
    }

    [Fact(DisplayName = "AddApplication should throw when builder is null")]
    public void AddApplication_WithNullBuilder_ShouldThrow()
    {
        // Arrange
        WebApplicationBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddApplication());
    }

    [Fact(DisplayName = "UseApplication should warm up MediatR services")]
    public void UseApplication_ShouldWarmUpMediator()
    {
        // Arrange
        using WebApplication app = CreateApplication();

        // Act
        var action = () => app.UseApplication();

        // Assert
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "UseApplication should return the same application for chaining")]
    public void UseApplication_ShouldReturnSameApplication()
    {
        // Arrange
        using WebApplication app = CreateApplication();

        // Act
        WebApplication result = app.UseApplication();

        // Assert
        result.Should().BeSameAs(app);
    }

    [Fact(DisplayName = "UseApplication should throw when application is null")]
    public void UseApplication_WithNullApplication_ShouldThrow()
    {
        // Arrange
        WebApplication? app = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => app!.UseApplication());
    }

    private static WebApplicationBuilder CreateBuilder(string environment = "Testing") =>
        WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environment,
        });

    private static WebApplication CreateApplication(string environment = "Testing")
    {
        WebApplicationBuilder builder = CreateBuilder(environment);
        builder.AddApplication();
        return builder.Build();
    }
}
