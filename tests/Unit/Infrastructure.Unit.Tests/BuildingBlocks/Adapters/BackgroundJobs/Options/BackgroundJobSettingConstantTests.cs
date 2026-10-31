using Infrastructure.BuildingBlocks.Adapters.BackgroundJobs.Options;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.BackgroundJobs.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "BackgroundJobs")]
public class BackgroundJobSettingConstantTests
{
    [Fact(DisplayName = "Defaults.Enabled should be true")]
    public void Defaults_Enabled_ShouldBeTrue()
    {
        BackgroundJobDefaults.Defaults.Enabled.Should().BeTrue();
    }

    [Fact(DisplayName = "Defaults.DashboardPath should be '/jobs'")]
    public void Defaults_DashboardPath_ShouldBeJobs()
    {
        BackgroundJobDefaults.Defaults.DashboardPath.Should().Be("/jobs");
    }

    [Fact(DisplayName = "Defaults.CachingEnabled should be false")]
    public void Defaults_CachingEnabled_ShouldBeFalse()
    {
        BackgroundJobDefaults.Defaults.CachingEnabled.Should().BeFalse();
    }

    [Fact(DisplayName = "Constraints.DashboardPathMaxLength should be 2048")]
    public void Constraints_DashboardPathMaxLength_ShouldBe2048()
    {
        BackgroundJobDefaults.Constraints.DashboardPathMaxLength.Should().Be(2048);
    }

    [Fact(DisplayName = "Environments.Development should be 'Development'")]
    public void Environments_Development_ShouldBeDevelopment()
    {
        BackgroundJobDefaults.Environments.Development.Should().Be("Development");
    }
}
