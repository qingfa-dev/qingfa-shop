using Infrastructure.Integrations.Backgrounds.Options;

namespace Infrastructure.Unit.Tests.Integrations.Backgrounds.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "BackgroundJobs")]
public class BackgroundJobSettingResultTests
{
    [Fact(DisplayName = "DashboardPathRequired error code should be 'BackgroundJobs.DashboardPath.Required'")]
    public void DashboardPathRequired_ErrorCode_ShouldBeCorrect()
    {
        BackgroundJobSettingResult.Failure.DashboardPathRequired.Code.Should().Be("BackgroundJobs.DashboardPath.Required");
    }

    [Fact(DisplayName = "DashboardPathRequired error message should contain 'is required'")]
    public void DashboardPathRequired_ErrorMessage_ShouldBeCorrect()
    {
        BackgroundJobSettingResult.Failure.DashboardPathRequired.Message.Should().Contain("is required");
    }

    [Fact(DisplayName = "DashboardPathTooLong error code should be 'BackgroundJobs.DashboardPath.TooLong'")]
    public void DashboardPathTooLong_ErrorCode_ShouldBeCorrect()
    {
        BackgroundJobSettingResult.Failure.DashboardPathTooLong.Code.Should().Be("BackgroundJobs.DashboardPath.TooLong");
    }

    [Fact(DisplayName = "DashboardPathTooLong error message should contain max length")]
    public void DashboardPathTooLong_ErrorMessage_ShouldContainMaxLength()
    {
        BackgroundJobSettingResult.Failure.DashboardPathTooLong.Message.Should().Contain("2048");
    }

    [Fact(DisplayName = "CachingConnectionStringMissing error code should be 'BackgroundJobs.Caching.ConnectionStringMissing'")]
    public void CachingConnectionStringMissing_ErrorCode_ShouldBeCorrect()
    {
        BackgroundJobSettingResult.Failure.CachingConnectionStringMissing.Code.Should().Be("BackgroundJobs.Caching.ConnectionStringMissing");
    }

    [Fact(DisplayName = "CachingConnectionStringMissing error message should contain 'Redis connection string'")]
    public void CachingConnectionStringMissing_ErrorMessage_ShouldContainRedis()
    {
        BackgroundJobSettingResult.Failure.CachingConnectionStringMissing.Message.Should().Contain("Redis connection string");
    }

    [Fact(DisplayName = "All errors should be Validation errors")]
    public void AllErrors_ShouldBeValidationErrors()
    {
        BackgroundJobSettingResult.Failure.DashboardPathRequired.Status.Should().Be(422);
        BackgroundJobSettingResult.Failure.DashboardPathTooLong.Status.Should().Be(422);
        BackgroundJobSettingResult.Failure.CachingConnectionStringMissing.Status.Should().Be(422);
    }
}
