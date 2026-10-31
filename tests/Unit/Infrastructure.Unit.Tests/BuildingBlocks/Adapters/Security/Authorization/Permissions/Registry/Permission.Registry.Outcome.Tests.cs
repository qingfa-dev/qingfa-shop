using Application.BuildingBlocks.Security.Authorization.Registry;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Authorization.Permissions.Registry;

[Trait("Category", "Unit")]
[Trait("Module", "Authorization")]
[Trait("Feature", "PermissionRegistry")]
public sealed class PermissionRegistryOutcomeTests
{
    [Fact(DisplayName = "RefreshFailed should be a dependency error")]
    public void RefreshFailed_ShouldBeServiceUnavailable()
    {
        Error error = PermissionRegistryOutcome.Failure.RefreshFailed();

        error.Code.Should().Be("PermissionRegistry.RefreshFailed");
        error.Status.Should().Be(503);
    }

    [Fact(DisplayName = "RefreshFailed should include the exception type")]
    public void RefreshFailed_ShouldIncludeExceptionType()
    {
        Error error = PermissionRegistryOutcome.Failure.RefreshFailed(
            new TimeoutException("timed out"));

        error.Message.Should().Contain("TimeoutException");
    }
}
