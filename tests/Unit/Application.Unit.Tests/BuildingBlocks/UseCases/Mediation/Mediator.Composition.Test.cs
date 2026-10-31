using Application.BuildingBlocks.UseCases.Mediation;
using Application.BuildingBlocks.UseCases.Mediation.Behaviors.Exceptions;
using Application.BuildingBlocks.UseCases.Mediation.Behaviors.Logging;
using Application.BuildingBlocks.UseCases.Mediation.Behaviors.Validation;
using Application.Unit.Tests.BuildingBlocks.UseCases.Mediation.Fixtures;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Application.Unit.Tests.BuildingBlocks.UseCases.Mediation;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Mediators")]
public class MediatorCompositionTest
{
    [Fact(DisplayName = "AddMediators should register MediatR services and behaviors")]
    public void AddMediators_ShouldRegisterRequiredServices()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act
        builder.AddMediators();
        using ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        // 1. Check MediatR
        serviceProvider.GetService<IMediator>().Should().NotBeNull();

        // 2. Check Behaviors
        var behaviors = builder.Services
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
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        var additionalAssembly = typeof(TestRequestHandler).Assembly;

        // Act
        builder.AddMediators(additionalAssembly);
        using ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IMediator>().Should().NotBeNull();

        builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(IRequestHandler<TestRequest, Result>));

        // Validate: the host relies on a scoped mediator so it shares the
        // request scope with scoped handler dependencies.
        builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(IMediator) &&
            s.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact(DisplayName = "UseMediators should resolve IMediator to warm up the service")]
    public void UseMediators_ShouldWarmUpMediator()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddMediators();
        using WebApplication app = builder.Build();

        // Act
        var action = () => app.UseMediators();

        // Assert
        action.Should().NotThrow();
    }
    [Fact(DisplayName = "UseMediators should resolve the scoped mediator when scope validation is on")]
    public void UseMediators_WithScopeValidation_ShouldResolve()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.AddMediators();
        using WebApplication app = builder.Build();

        // Act
        var action = () => app.UseMediators();

        // Assert
        action.Should().NotThrow();
    }
}
