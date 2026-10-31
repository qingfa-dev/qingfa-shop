namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Channels.Sms.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class SinchProviderSettingConstantTests
{
    [Fact(DisplayName = "Defaults should have expected values")]
    public void Defaults_ShouldHaveExpectedValues()
    {
        SinchProviderSettingConstant.Defaults.Section.Should().Be("Notification:Channels:Sms:Providers:Sinch");
        SinchProviderSettingConstant.Defaults.Priority.Should().Be(1);
    }

    [Fact(DisplayName = "Constraints should have expected limits")]
    public void Constraints_ShouldHaveExpectedLimits()
    {
        SinchProviderSettingConstant.Constraints.ProjectIdMinLength.Should().Be(1);
        SinchProviderSettingConstant.Constraints.ProjectIdMaxLength.Should().Be(64);
        SinchProviderSettingConstant.Constraints.KeyIdMinLength.Should().Be(1);
        SinchProviderSettingConstant.Constraints.KeyIdMaxLength.Should().Be(128);
        SinchProviderSettingConstant.Constraints.KeySecretMinLength.Should().Be(1);
        SinchProviderSettingConstant.Constraints.KeySecretMaxLength.Should().Be(256);
        SinchProviderSettingConstant.Constraints.SenderPhoneNumberMaxLength.Should().Be(20);
    }

    [Fact(DisplayName = "Patterns should have expected regex")]
    public void Patterns_ShouldHaveExpectedValues()
    {
        SinchProviderSettingConstant.Patterns.SenderPhoneNumber.Should().Be(@"^\+[1-9]\d{1,14}$");
    }
}
