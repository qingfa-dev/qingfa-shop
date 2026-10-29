namespace Infrastructure.Unit.Tests.Integrations.Notifications.Channels.Email.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class SendGridProviderSettingConstantTests
{
    [Fact(DisplayName = "Defaults should have expected values")]
    public void Defaults_ShouldHaveExpectedValues()
    {
        SendGridProviderSettingConstant.Defaults.Section.Should().Be("Notification:Channels:Email:Providers:SendGrid");
        SendGridProviderSettingConstant.Defaults.Priority.Should().Be(2);
    }

    [Fact(DisplayName = "Constraints should have expected limits")]
    public void Constraints_ShouldHaveExpectedLimits()
    {
        SendGridProviderSettingConstant.Constraints.ApiKeyMinLength.Should().Be(10);
        SendGridProviderSettingConstant.Constraints.ApiKeyMaxLength.Should().Be(256);
    }

    [Fact(DisplayName = "Patterns should have expected regex")]
    public void Patterns_ShouldHaveExpectedValues()
    {
        SendGridProviderSettingConstant.Patterns.ApiKey.Should().Be(@"^SG\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+$");
    }
}
