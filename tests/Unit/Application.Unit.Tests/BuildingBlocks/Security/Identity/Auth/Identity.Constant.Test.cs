using Application.BuildingBlocks.Security.Identity.Auth;

namespace Application.Unit.Tests.BuildingBlocks.Security.Identity.Auth;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Configuration")]
public sealed class IdentityConstantTest
{
    [Fact(DisplayName = "Identity route and provider constants should be stable")]
    public void Constants_ShouldBeStable()
    {
        IdentityConstant.Routes.Base.Should().Be("/identity");
        IdentityConstant.Routes.Register.Should().Be("/register");
        IdentityConstant.Routes.Login.Should().Be("/login");
        IdentityConstant.Providers.Google.Should().Be("Google");
        IdentityConstant.Providers.Microsoft.Should().Be("Microsoft");
        IdentityConstant.Providers.OpenIdConnect.Should().Be("OpenIdConnect");
        IdentityConstant.TokenProviders.Authenticator.Should().Be("Authenticator");
        IdentityConstant.Defaults.DefaultRole.Should().Be("User");
        IdentityConstant.Constraints.RecoveryCodeCount.Should().Be(10);
    }
}
