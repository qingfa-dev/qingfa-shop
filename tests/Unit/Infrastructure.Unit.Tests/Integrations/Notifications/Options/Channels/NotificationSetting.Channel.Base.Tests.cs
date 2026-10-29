namespace Infrastructure.Unit.Tests.Integrations.Notifications.Options.Channels;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public class ChannelNotificationSettingBaseTests
{
    private sealed class TestChannel : ChannelSettingBase
    {
        public static new string Section => "TestChannel";
    }

    [Fact(DisplayName = "Default Enabled should be true")]
    public void Default_Enabled_ShouldBeTrue()
    {
        TestChannel channel = new();
        channel.Enabled.Should().BeTrue();
    }

    [Fact(DisplayName = "Section should return the overridden value")]
    public void Section_ShouldReturnOverriddenValue()
    {
        TestChannel.Section.Should().Be("TestChannel");
    }

    [Fact(DisplayName = "Should implement IChannelNotificationSetting")]
    public void Should_Implement_IChannelNotificationSetting()
    {
        TestChannel channel = new();
        channel.Should().BeAssignableTo<ChannelSettingBase>();
    }
}
