using Infrastructure.Security.Identity.Options;

namespace Infrastructure.Unit.Tests.Security.Identity.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Configuration")]
public sealed class IdentitySettingConstantTest
{
    [Fact(DisplayName = "Identity password policy should follow the documented OWASP baseline")]
    public void PasswordPolicy_ShouldFollowBaseline()
    {
        IdentitySettingConstant.Password.RequireDigit.Should().BeTrue();
        IdentitySettingConstant.Password.RequireLowercase.Should().BeTrue();
        IdentitySettingConstant.Password.RequireUppercase.Should().BeTrue();
        IdentitySettingConstant.Password.RequireNonAlphanumeric.Should().BeTrue();
        IdentitySettingConstant.Password.RequiredLength.Should().Be(12);
        IdentitySettingConstant.Password.RequiredUniqueChars.Should().Be(3);
    }

    [Fact(DisplayName = "Identity lockout policy should bound failed attempts")]
    public void LockoutPolicy_ShouldBoundFailedAttempts()
    {
        IdentitySettingConstant.Lockout.MaxFailedAccessAttempts.Should().Be(5);
        IdentitySettingConstant.Lockout.DefaultLockoutTimeSpan.Should().Be(TimeSpan.FromMinutes(15));
        IdentitySettingConstant.Lockout.AllowedForNewUsers.Should().BeTrue();
    }

    [Fact(DisplayName = "Identity sign-in and security stamp settings should be explicit")]
    public void SignInAndSecurityStamp_ShouldBeExplicit()
    {
        IdentitySettingConstant.User.RequireUniqueEmail.Should().BeTrue();
        IdentitySettingConstant.SignIn.RequireConfirmedEmail.Should().BeFalse();
        IdentitySettingConstant.SignIn.RequireConfirmedAccount.Should().BeFalse();
        IdentitySettingConstant.SecurityStamp.ValidationInterval.Should().Be(TimeSpan.FromMinutes(30));
        IdentitySettingConstant.DataProtection.ApplicationName.Should().Be("ReSys.Shop");
    }
}
