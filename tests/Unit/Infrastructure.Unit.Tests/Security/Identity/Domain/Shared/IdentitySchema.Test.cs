using Infrastructure.Security.Identity.Domain.Shared;

namespace Infrastructure.Unit.Tests.Security.Identity.Domain.Shared;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Schema")]
public sealed class IdentitySchemaTest
{
    [Fact(DisplayName = "Identity schema should use snake_case physical names")]
    public void Schema_ShouldUseSnakeCaseNames()
    {
        IdentitySchema.Name.Should().Be("identity");
        IdentitySchema.TableNames.Users.Should().Be("users");
        IdentitySchema.TableNames.Roles.Should().Be("roles");
        IdentitySchema.TableNames.UserRoles.Should().Be("user_roles");
        IdentitySchema.TableNames.UserClaims.Should().Be("user_claims");
        IdentitySchema.TableNames.UserLogins.Should().Be("user_logins");
        IdentitySchema.TableNames.UserTokens.Should().Be("user_tokens");
        IdentitySchema.TableNames.RoleClaims.Should().Be("role_claims");
        IdentitySchema.TableNames.RefreshTokens.Should().Be("refresh_tokens");
        IdentitySchema.TableNames.Passkeys.Should().Be("passkeys");
        IdentitySchema.TableNames.PasskeyData.Should().Be("passkey_data");
    }
}
