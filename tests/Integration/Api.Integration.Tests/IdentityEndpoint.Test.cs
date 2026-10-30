using Application.Common.Security.Authentication.Contexts;

using Core.Common.Monad.Results;

using Web.Api.Endpoints.Identity;

namespace Api.Integration.Tests;

/// <summary>Locks the current-user guard used by the profile and two-factor routes.</summary>
[Trait("Category", "Unit")]
public sealed class IdentityEndpointTest
{
    [Fact(DisplayName = "A valid user identifier should pass through")]
    public void RequireUserId_WithValidIdentifier_ShouldReturnUserId()
    {
        Guid userId = Guid.NewGuid();

        Result<Guid> result = IdentityEndpoint.RequireUserId(
            new StubUserContext(userId.ToString()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(userId);
    }

    [Theory(DisplayName = "A missing or malformed user identifier should produce a 401")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void RequireUserId_WithoutValidIdentifier_ShouldReturnUnauthorized(string? userId)
    {
        Result<Guid> result = IdentityEndpoint.RequireUserId(new StubUserContext(userId));

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(401);
        result.Errors.Should().ContainSingle(
            error => error.Code == "Authentication.UserIdMissing");
    }

    private sealed class StubUserContext(string? userId) : IUserContext
    {
        public string? UserId => userId;

        public string? UserName => null;

        public string? Email => null;

        public bool IsAuthenticated => userId is not null;

        public string? IpAddress => null;

        public string? Device => null;
    }
}
