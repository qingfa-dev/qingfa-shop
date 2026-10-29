namespace Infrastructure.Unit.Tests.Integrations.Notifications.Options.Channels;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public class ChannelConstantTests
{
    [Fact(DisplayName = "Defaults.Enabled should be true")]
    public void Defaults_Enabled_ShouldBeTrue()
    {
        ChannelConstant.Defaults.Enabled.Should().BeTrue();
    }
}
