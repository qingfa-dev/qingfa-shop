using Infrastructure.Persistence.EntityFramework.Options;

namespace Infrastructure.Tests.Persistence.EntityFramework.Options;

[Trait("Category", "Unit")]
public sealed class EfCoreOptionTest
{
    [Fact(DisplayName = "Default Values Should Match Constants")]
    public void Default_ShouldMatchConstants()
    {
        var option = new EfCoreOption();

        option.ConnectionString.Should().BeNull();
        option.Provider.Should().Be(EfCoreSettingConstant.DefaultProvider);
        option.EnableSensitiveDataLogging.Should().BeFalse();
        option.EnableDetailedErrors.Should().BeFalse();
        option.CommandTimeoutSeconds.Should().Be(EfCoreSettingConstant.DefaultCommandTimeoutSeconds);
        option.MaxRetryAttempts.Should().Be(EfCoreSettingConstant.DefaultMaxRetryAttempts);
        option.RetryBaseDelayMs.Should().Be(EfCoreSettingConstant.DefaultRetryBaseDelayMs);
        option.UsePooling.Should().BeTrue();
        option.MinPoolSize.Should().Be(EfCoreSettingConstant.DefaultMinPoolSize);
        option.MaxPoolSize.Should().Be(EfCoreSettingConstant.DefaultMaxPoolSize);
    }

    [Theory(DisplayName = "Provider Should Accept PostgreSQL And SQL Server")]
    [InlineData("PostgreSQL")]
    [InlineData("SqlServer")]
    [InlineData("postgresql")]
    [InlineData("sqlserver")]
    public void Provider_ShouldAcceptCaseInsensitive(string provider)
    {
        var option = new EfCoreOption { Provider = provider };
        option.Provider.Should().Be(provider);
    }

    [Fact(DisplayName = "Can Override All Options")]
    public void CanOverrideAllOptions()
    {
        var option = new EfCoreOption
        {
            ConnectionString = "Host=localhost;Database=test",
            Provider = "SqlServer",
            EnableSensitiveDataLogging = true,
            EnableDetailedErrors = true,
            CommandTimeoutSeconds = 60,
            MaxRetryAttempts = 5,
            RetryBaseDelayMs = 200,
            UsePooling = false,
            MinPoolSize = 5,
            MaxPoolSize = 200
        };

        option.ConnectionString.Should().Be("Host=localhost;Database=test");
        option.Provider.Should().Be("SqlServer");
        option.EnableSensitiveDataLogging.Should().BeTrue();
        option.EnableDetailedErrors.Should().BeTrue();
        option.CommandTimeoutSeconds.Should().Be(60);
        option.MaxRetryAttempts.Should().Be(5);
        option.RetryBaseDelayMs.Should().Be(200);
        option.UsePooling.Should().BeFalse();
        option.MinPoolSize.Should().Be(5);
        option.MaxPoolSize.Should().Be(200);
    }
}