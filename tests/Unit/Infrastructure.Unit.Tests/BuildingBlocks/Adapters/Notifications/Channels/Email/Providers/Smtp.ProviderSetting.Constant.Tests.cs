namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Channels.Email.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class SmtpProviderSettingConstantTests
{
    [Fact(DisplayName = "Defaults should have expected values")]
    public void Defaults_ShouldHaveExpectedValues()
    {
        SmtpProviderSettingConstant.Defaults.Section.Should().Be("Notification:Channels:Email:Providers:Smtp");
        SmtpProviderSettingConstant.Defaults.Priority.Should().Be(1);
        SmtpProviderSettingConstant.Defaults.Host.Should().Be("localhost");
        SmtpProviderSettingConstant.Defaults.Port.Should().Be(25);
        SmtpProviderSettingConstant.Defaults.EnableSsl.Should().BeFalse();
        SmtpProviderSettingConstant.Defaults.UseDefaultCredentials.Should().BeTrue();
    }

    [Fact(DisplayName = "Constraints should have expected limits")]
    public void Constraints_ShouldHaveExpectedLimits()
    {
        SmtpProviderSettingConstant.Constraints.PortMin.Should().Be(1);
        SmtpProviderSettingConstant.Constraints.PortMax.Should().Be(65535);
        SmtpProviderSettingConstant.Constraints.HostMaxLength.Should().Be(256);
        SmtpProviderSettingConstant.Constraints.UsernameMaxLength.Should().Be(128);
        SmtpProviderSettingConstant.Constraints.PasswordMaxLength.Should().Be(256);
    }

    [Fact(DisplayName = "Patterns should have expected regex")]
    public void Patterns_ShouldHaveExpectedValues()
    {
        SmtpProviderSettingConstant.Patterns.HostName.Should().NotBeNull();
        SmtpProviderSettingConstant.Patterns.HostName.ToString().Should().Be(@"^([a-zA-Z0-9]([a-zA-Z0-9\-]*[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$|^localhost$|^\d{1,3}(\.\d{1,3}){3}$");
    }
}
