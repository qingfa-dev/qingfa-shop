namespace Infrastructure.Unit.Tests.Integrations.Notifications.Channels.Sms.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class SmsChannelSettingConstantTests
{
    [Fact(DisplayName = "Defaults should have expected values")]
    public void Defaults_ShouldHaveExpectedValues()
    {
        SmsChannelSettingConstant.Defaults.Section.Should().Be("Notification:Channels:Sms");
    }

    [Fact(DisplayName = "Constraints should have expected limits")]
    public void Constraints_ShouldHaveExpectedLimits()
    {
        SmsChannelSettingConstant.Constraints.DefaultSenderNumberMaxLength.Should().Be(20);
    }

    [Fact(DisplayName = "Patterns should have expected regex")]
    public void Patterns_ShouldHaveExpectedValues()
    {
        SmsChannelSettingConstant.Patterns.SenderNumber.Should().Be(@"^\+[1-9]\d{1,14}$");
    }
}
