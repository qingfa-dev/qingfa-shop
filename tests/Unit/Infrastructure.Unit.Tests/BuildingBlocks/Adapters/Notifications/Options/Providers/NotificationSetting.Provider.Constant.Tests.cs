namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Options.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public class ProviderSettingConstantTests
{
    [Fact(DisplayName = "Defaults should have expected values")]
    public void Defaults_ShouldHaveExpectedValues()
    {
        ProviderSettingConstant.Defaults.Enabled.Should().BeTrue();
        ProviderSettingConstant.Defaults.Priority.Should().Be(1);
        ProviderSettingConstant.Defaults.RetryCount.Should().Be(3);
        ProviderSettingConstant.Defaults.TimeoutSeconds.Should().Be(30);
    }

    [Fact(DisplayName = "DefaultTimeout should be 30 seconds")]
    public void DefaultTimeout_ShouldBe30Seconds()
    {
        ProviderSettingConstant.DefaultTimeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact(DisplayName = "Constraints should have expected ranges")]
    public void Constraints_ShouldHaveExpectedRanges()
    {
        ProviderSettingConstant.Constraints.MinPriority.Should().Be(1);
        ProviderSettingConstant.Constraints.MaxPriority.Should().Be(100);
        ProviderSettingConstant.Constraints.MinRetryCount.Should().Be(0);
        ProviderSettingConstant.Constraints.MaxRetryCount.Should().Be(10);
        ProviderSettingConstant.Constraints.MinTimeoutSeconds.Should().Be(1);
        ProviderSettingConstant.Constraints.MaxTimeoutSeconds.Should().Be(300);
    }
}
