namespace Infrastructure.Unit.Tests.Integrations.Notifications.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public class NotificationSettingConstantTests
{
    [Fact(DisplayName = "Defaults should have expected values")]
    public void Defaults_ShouldHaveExpectedValues()
    {
        NotificationSettingConstant.Defaults.ApplicationName.Should().Be("ReSys Shop");
        NotificationSettingConstant.Defaults.SupportEmail.Should().Be("support@resys.shop");
        NotificationSettingConstant.Defaults.SupportPhone.Should().Be("+1-000-000-0000");
        NotificationSettingConstant.Defaults.CustomerSupportLink.Should().Be("https://resys.shop/support");
        NotificationSettingConstant.Defaults.ApplicationUrl.Should().Be("https://resys.shop");
        NotificationSettingConstant.Defaults.UnsubscribeUrl.Should().Be("https://resys.shop/unsubscribe");
        NotificationSettingConstant.Defaults.SurveyUrl.Should().Be("https://resys.shop/survey");
    }

    [Fact(DisplayName = "Constraints should have expected limits")]
    public void Constraints_ShouldHaveExpectedLimits()
    {
        NotificationSettingConstant.Constraints.MinApplicationNameLength.Should().Be(1);
        NotificationSettingConstant.Constraints.MaxApplicationNameLength.Should().Be(100);
    }

    [Fact(DisplayName = "Patterns should have expected phone regex")]
    public void Patterns_ShouldHaveExpectedValues()
    {
        NotificationSettingConstant.Patterns.PhoneNumber.Should().Be(@"^\+?[\d\s\-()]+$");
    }
}
