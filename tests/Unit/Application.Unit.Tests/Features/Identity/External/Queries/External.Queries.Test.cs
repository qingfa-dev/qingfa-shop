using Application.Features.Identity.Auth;
using Application.Features.Identity.External.Models;
using Application.Features.Identity.External.Queries;

namespace Application.Unit.Tests.Features.Identity.External.Queries;

/// <summary>Unit tests for the external sign-in query handlers.</summary>
public sealed class ExternalQueriesTest
{
    [Fact(DisplayName = "GetProviders should return the configured providers")]
    public async Task GetProviders_ShouldReturnConfiguredProviders()
    {
        var fixture = new ExternalFixture();
        fixture.External.Setup(service => service.GetProviders())
            .Returns(ExternalFixture.Providers);

        Result<IReadOnlyCollection<ExternalProviderModel>> result = await new GetExternalProviders
            .UseCaseHandler(fixture.External.Object)
            .Handle(
                new GetExternalProviders.UseCase(),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Select(provider => provider.Name).Should().Contain(
            IdentityConstant.Providers.Google);
    }

    [Fact(DisplayName = "GetProviders should reject a missing query")]
    public async Task GetProviders_WithoutQuery_ShouldThrow()
    {
        var fixture = new ExternalFixture();
        var handler = new GetExternalProviders.UseCaseHandler(fixture.External.Object);

        Func<Task> act = () => handler.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
