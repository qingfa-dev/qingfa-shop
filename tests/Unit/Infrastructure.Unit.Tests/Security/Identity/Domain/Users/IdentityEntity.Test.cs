using Infrastructure.Security.Identity.Domain.Users.Claims;
using Infrastructure.Security.Identity.Domain.Users.Keys;
using Infrastructure.Security.Identity.Domain.Users.Logins;
using Infrastructure.Security.Identity.Domain.Users.Tokens;

namespace Infrastructure.Unit.Tests.Security.Identity.Domain.Users;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Users")]
public sealed class IdentityEntityTest
{
    [Fact(DisplayName = "Identity child entities should be materializable")]
    public void ChildEntities_ShouldBeMaterializable()
    {
        var claim = new UserClaim { UserId = Guid.NewGuid(), ClaimType = "type", ClaimValue = "value" };
        var passkey = new UserPasskey { UserId = Guid.NewGuid() };
        var login = new UserLogin { UserId = Guid.NewGuid(), LoginProvider = "provider", ProviderKey = "key" };
        var token = new UserToken { UserId = Guid.NewGuid(), LoginProvider = "provider", Name = "name" };

        claim.UserId.Should().NotBe(Guid.Empty);
        claim.ClaimType.Should().Be("type");
        passkey.UserId.Should().NotBe(Guid.Empty);
        login.LoginProvider.Should().Be("provider");
        token.Name.Should().Be("name");
    }
}
