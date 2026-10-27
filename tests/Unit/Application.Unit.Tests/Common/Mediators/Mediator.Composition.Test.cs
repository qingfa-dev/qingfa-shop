using Application.Common.Mediators;
using Application.Common.Mediators.Behaviors.Exceptions;
using Application.Common.Mediators.Behaviors.Logging;
using Application.Common.Mediators.Behaviors.Validation;
using Application.UnitTests.Common.Mediators.Fixtures;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace Application.Unit.Tests.Common.Mediators;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Mediators")]
public class MediatorCompositionTest
{
    [Fact(DisplayName = "AddMediators should register MediatR services and behaviors")]
    public void AddMediators_ShouldRegisterRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediators();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        // 1. Check MediatR
        serviceProvider.GetService<IMediator>().Should().NotBeNull();

        // 2. Check Behaviors
        var behaviors = services
            .Where(s => s.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(s => s.ImplementationType)
            .ToList();

        behaviors.Should().Contain(typeof(LoggingBehavior<,>));
        behaviors.Should().Contain(typeof(ValidationBehavior<,>));
        behaviors.Should().Contain(typeof(ExceptionMappingBehavior<,>));
    }

    [Fact(DisplayName = "AddMediators should register handlers from additional assemblies")]
    public void AddMediators_WithAdditionalAssemblies_ShouldRegisterHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        var additionalAssembly = typeof(TestRequestHandler).Assembly;

        // Act
        services.AddMediators(additionalAssembly);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IMediator>().Should().NotBeNull();

        services.Should().Contain(s =>
            s.ServiceType == typeof(IRequestHandler<TestRequest, Result>));

        // Validate: the host relies on a scoped mediator so it shares the
        // request scope with scoped handler dependencies.
        services.Should().Contain(s =>
            s.ServiceType == typeof(IMediator) &&
            s.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact(DisplayName = "UseMediators should resolve IMediator to warm up the service")]
    public void UseMediators_ShouldWarmUpMediator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediators();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act
        var action = () => serviceProvider.UseMediators();

        // Assert
        action.Should().NotThrow();
    }
}