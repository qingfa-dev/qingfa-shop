using FluentValidation.Results;

using Microsoft.Extensions.Configuration;

using Infrastructure.Integrations.Backgrounds.Options;

namespace Infrastructure.Unit.Tests.Integrations.Backgrounds.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "BackgroundJobs")]
public class BackgroundJobSettingValidatorTests
{
    private readonly BackgroundJobSettingValidator _validator = new(new ConfigurationBuilder().Build());

    [Fact(DisplayName = "Valid defaults should pass validation")]
    public void ValidDefaults_ShouldPassValidation()
    {
        BackgroundJobSetting model = new();

        ValidationResult result = _validator.Validate(model);

        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "Empty DashboardPath should fail with DashboardPathRequired")]
    public void DashboardPath_Empty_ShouldFailWithDashboardPathRequired()
    {
        BackgroundJobSetting model = new() { DashboardPath = string.Empty };

        ValidationResult result = _validator.Validate(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "BackgroundJobs.DashboardPath.Required");
    }

    [Fact(DisplayName = "DashboardPath exceeding max length should fail with DashboardPathTooLong")]
    public void DashboardPath_ExceedingMaxLength_ShouldFailWithDashboardPathTooLong()
    {
        BackgroundJobSetting model = new() { DashboardPath = new string('a', 2049) };

        ValidationResult result = _validator.Validate(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "BackgroundJobs.DashboardPath.TooLong");
    }

    [Fact(DisplayName = "DashboardPath within max length should pass validation")]
    public void DashboardPath_WithinMaxLength_ShouldPassValidation()
    {
        BackgroundJobSetting model = new() { DashboardPath = new string('a', 2048) };

        ValidationResult result = _validator.Validate(model);

        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "CachingEnabled=true with Aspire connection string should pass")]
    public void CachingEnabled_True_WithAspireConnectionString_ShouldPass()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Cache"] = "redis://localhost:6379"
            })
            .Build();

        BackgroundJobSetting model = new() { CachingEnabled = true };
        BackgroundJobSettingValidator validator = new(configuration);

        ValidationResult result = validator.Validate(model);

        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "CachingEnabled=true with default connection string should pass")]
    public void CachingEnabled_True_WithDefaultConnectionString_ShouldPass()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultCaching"] = "redis://localhost:6379"
            })
            .Build();

        BackgroundJobSetting model = new() { CachingEnabled = true };
        BackgroundJobSettingValidator validator = new(configuration);

        ValidationResult result = validator.Validate(model);

        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "CachingEnabled=true without connection string should fail")]
    public void CachingEnabled_True_WithoutConnectionString_ShouldFail()
    {
        var configuration = new ConfigurationBuilder().Build();

        BackgroundJobSetting model = new() { CachingEnabled = true };
        BackgroundJobSettingValidator validator = new(configuration);

        ValidationResult result = validator.Validate(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "BackgroundJobs.Caching.ConnectionStringMissing");
    }
}
