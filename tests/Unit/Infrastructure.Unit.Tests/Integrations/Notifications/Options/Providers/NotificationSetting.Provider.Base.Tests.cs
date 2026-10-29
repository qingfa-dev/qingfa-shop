namespace Infrastructure.Unit.Tests.Integrations.Notifications.Options.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public class ProviderNotificationSettingBaseTests
{
    private sealed class TestProvider : BaseProviderSetting
    {
        public static new string Section => "TestProvider";
    }

    [Fact(DisplayName = "Default Enabled should be true")]
    public void Default_Enabled_ShouldBeTrue()
    {
        TestProvider provider = new();
        provider.Enabled.Should().BeTrue();
    }

    [Fact(DisplayName = "Default Priority should be 1")]
    public void Default_Priority_ShouldBe1()
    {
        TestProvider provider = new();
        provider.Priority.Should().Be(1);
    }

    [Fact(DisplayName = "Default RetryCount should be 3")]
    public void Default_RetryCount_ShouldBe3()
    {
        TestProvider provider = new();
        provider.RetryCount.Should().Be(3);
    }

    [Fact(DisplayName = "Default Timeout should be 30 seconds")]
    public void Default_Timeout_ShouldBe30Seconds()
    {
        TestProvider provider = new();
        provider.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact(DisplayName = "SectionName should return the overridden value")]
    public void SectionName_ShouldReturnOverriddenValue()
    {
        TestProvider provider = new();
        TestProvider.Section.Should().Be("TestProvider");
    }

    [Fact(DisplayName = "Should implement IProviderNotificationSetting")]
    public void Should_Implement_IProviderNotificationSetting()
    {
        TestProvider provider = new();
        provider.Should().BeAssignableTo(typeof(IProviderSetting));
    }
}
