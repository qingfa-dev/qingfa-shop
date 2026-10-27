using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace Application.Unit.Tests.Common.Composition;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "ApplicationComposition")]
public class ApplicationCompositionTest
{
    [Fact(DisplayName = "AddApplication should register MediatR and Mapster services")]
    public void AddApplication_ShouldRegisterRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplication();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IMediator>().Should().NotBeNull();
    }

    [Fact(DisplayName = "AddApplication should throw when services is null")]
    public void AddApplication_WithNullServices_ShouldThrow()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)services!).AddApplication());
    }

    [Fact(DisplayName = "UseApplication should warm up MediatR services")]
    public void UseApplication_ShouldWarmUpMediator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplication();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act
        var action = () => serviceProvider.UseApplication();

        // Assert
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "UseApplication should return the same provider for chaining")]
    public void UseApplication_ShouldReturnSameProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplication();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act
        var result = serviceProvider.UseApplication();

        // Assert
        result.Should().BeSameAs(serviceProvider);
    }

    [Fact(DisplayName = "UseApplication should throw when provider is null")]
    public void UseApplication_WithNullProvider_ShouldThrow()
    {
        // Arrange
        IServiceProvider? provider = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => provider!.UseApplication());
    }

    [Fact(DisplayName = "AddApplication with additional assemblies should register handlers")]
    public void AddApplication_WithAdditionalAssemblies_ShouldRegisterHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(AssemblyMarker).Assembly;

        // Act
        services.AddApplication(assembly);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IMediator>().Should().NotBeNull();
    }
}
