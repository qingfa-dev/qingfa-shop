using Infrastructure.Integrations.Backgrounds.Options;

namespace Infrastructure.Unit.Tests.Integrations.Backgrounds.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "BackgroundJobs")]
public class BackgroundJobSettingTests
{
    [Fact(DisplayName = "Default Enabled should be true")]
    public void Enabled_Default_ShouldBeTrue()
    {
        var setting = new BackgroundJobSetting();
        setting.Enabled.Should().BeTrue();
    }

    [Fact(DisplayName = "Default DashboardPath should be '/jobs'")]
    public void DashboardPath_Default_ShouldBeJobs()
    {
        var setting = new BackgroundJobSetting();
        setting.DashboardPath.Should().Be("/jobs");
    }

    [Fact(DisplayName = "Default CachingEnabled should be false")]
    public void CachingEnabled_Default_ShouldBeFalse()
    {
        var setting = new BackgroundJobSetting();
        setting.CachingEnabled.Should().BeFalse();
    }

    [Fact(DisplayName = "Enabled can be set to true")]
    public void Enabled_SetToTrue_ShouldBeTrue()
    {
        var setting = new BackgroundJobSetting { Enabled = true };
        setting.Enabled.Should().BeTrue();
    }

    [Fact(DisplayName = "Enabled can be set to false")]
    public void Enabled_SetToFalse_ShouldBeFalse()
    {
        var setting = new BackgroundJobSetting { Enabled = false };
        setting.Enabled.Should().BeFalse();
    }

    [Fact(DisplayName = "DashboardPath can be set to custom value")]
    public void DashboardPath_SetToCustom_ShouldBeCustom()
    {
        var customPath = "/custom/jobs";
        var setting = new BackgroundJobSetting { DashboardPath = customPath };
        setting.DashboardPath.Should().Be(customPath);
    }

    [Fact(DisplayName = "CachingEnabled can be set to true")]
    public void CachingEnabled_SetToTrue_ShouldBeTrue()
    {
        var setting = new BackgroundJobSetting { CachingEnabled = true };
        setting.CachingEnabled.Should().BeTrue();
    }

    [Fact(DisplayName = "CachingEnabled can be set to false")]
    public void CachingEnabled_SetToFalse_ShouldBeFalse()
    {
        var setting = new BackgroundJobSetting { CachingEnabled = false };
        setting.CachingEnabled.Should().BeFalse();
    }
}
