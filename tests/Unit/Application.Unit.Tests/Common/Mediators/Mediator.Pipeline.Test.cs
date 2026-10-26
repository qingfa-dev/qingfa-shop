using Application.Common.Mediators;
using Application.Common.Mediators.Behaviors.Logging;
using Application.UnitTests.Common.Mediators.Fixtures;

using Core.Common.Monad.Results;

using FluentValidation;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Unit.Tests.Common.Mediators;

/// <summary>
/// Proves the registered pipeline actually runs, end to end through the container.
/// </summary>
/// <remarks>
/// The behavior unit tests exercise each behavior in isolation. These tests close the
/// remaining gap: a constrained open generic is only applied when the closed response
/// type satisfies the constraint, so the pipeline must be observed through a real
/// <see cref="IMediator"/> resolution rather than assumed from the registration.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Mediators")]
public class MediatorPipelineTest
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        // Initialize: a single logger instance per closed type, so the test can read
        // what the behaviors logged.
        services.AddSingleton(typeof(ILogger<>), typeof(TestLogger<>));

        // Initialize: validators live outside AddMediators; the host registers them
        // through AddApplication.
        services.AddValidatorsFromAssemblyContaining<TestRequestValidator>();

        services.AddMediators(typeof(TestRequestHandler).Assembly);
        return services.BuildServiceProvider();
    }

    [Fact(DisplayName = "The pipeline should run the exception behavior for a throwing handler")]
    public async Task Send_WhenTheHandlerThrows_ShouldReturnAFailureResult()
    {
        using ServiceProvider provider = BuildProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        Result result = await mediator.Send(new TestRequestThrowing());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("TestRequestThrowing.Unexpected");
    }

    [Fact(DisplayName = "The pipeline should run the validation behavior for an invalid request")]
    public async Task Send_WhenTheRequestIsInvalid_ShouldReturnAValidationFailure()
    {
        using ServiceProvider provider = BuildProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        Result result = await mediator.Send(new TestRequest { Value = string.Empty });

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(422);

        // The rule name identifies what failed; the field lives in metadata.
        result.Errors.Should().Contain(e => e.Code == "NotEmptyValidator");
        result.Errors.Should().Contain(e => Equals(e.Metadata["Field"], "Value"));
    }

    [Fact(DisplayName = "The pipeline should run the logging behavior for a successful request")]
    public async Task Send_WhenTheRequestIsValid_ShouldReturnSuccessAndLog()
    {
        using ServiceProvider provider = BuildProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        Result result = await mediator.Send(new TestRequest { Value = "valid" });

        result.IsSuccess.Should().BeTrue();

        var logger = (TestLogger<LoggingBehavior<TestRequest, Result>>)
            provider.GetRequiredService<ILogger<LoggingBehavior<TestRequest, Result>>>();

        logger.Entries.Should().NotBeEmpty();
    }
}