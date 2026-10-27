using Infrastructure.Persistence.EntityFramework.Options;

namespace Infrastructure.Tests.Persistence.EntityFramework.Options;

[Trait("Category", "Unit")]
public sealed class PersistenceSettingTest
{
    #region Default Values

    [Fact(DisplayName = "Default Values Should Match Constants")]
    public void Default_ShouldMatchConstants()
    {
        var setting = new PersistenceSetting();

        setting.ConnectionStringName.Should().BeNull();
        setting.Provider.Should().Be(PersistenceConstants.Defaults.Provider);
        setting.EnableSensitiveDataLogging.Should().BeFalse();
        setting.EnableDetailedErrors.Should().BeFalse();
        setting.CommandTimeoutSeconds.Should().Be(PersistenceConstants.Defaults.CommandTimeoutSeconds);
        setting.MaxRetryAttempts.Should().Be(PersistenceConstants.Defaults.MaxRetryAttempts);
        setting.RetryBaseDelayMs.Should().Be(PersistenceConstants.Defaults.RetryBaseDelayMs);
        setting.UsePooling.Should().BeTrue();
        setting.MinPoolSize.Should().Be(PersistenceConstants.Defaults.MinPoolSize);
        setting.MaxPoolSize.Should().Be(PersistenceConstants.Defaults.MaxPoolSize);
    }

    #endregion

    #region Provider

    [Theory(DisplayName = "Provider Should Accept Various Values")]
    [InlineData("PostgreSQL")]
    [InlineData("SqlServer")]
    [InlineData("postgresql")]
    [InlineData("sqlserver")]
    [InlineData("MySQL")]
    [InlineData("oracle")]
    public void Provider_ShouldAcceptCaseInsensitive(string provider)
    {
        var setting = new PersistenceSetting { Provider = provider };
        setting.Provider.Should().Be(provider);
    }

    #endregion

    #region CommandTimeout Boundary Values

    [Theory(DisplayName = "CommandTimeoutSeconds should accept boundary values")]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(300)]
    [InlineData(3600)]
    public void CommandTimeoutSeconds_ShouldAcceptBoundaryValues(int value)
    {
        var setting = new PersistenceSetting { CommandTimeoutSeconds = value };
        setting.CommandTimeoutSeconds.Should().Be(value);
    }

    [Theory(DisplayName = "CommandTimeoutSeconds should accept out-of-range values (validation catches them)")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3601)]
    [InlineData(10000)]
    public void CommandTimeoutSeconds_ShouldAcceptOutOfRangeValues(int value)
    {
        var setting = new PersistenceSetting { CommandTimeoutSeconds = value };
        setting.CommandTimeoutSeconds.Should().Be(value);
    }

    #endregion

    #region RetryAttempts Boundary Values

    [Theory(DisplayName = "MaxRetryAttempts should accept boundary values")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(100)]
    public void MaxRetryAttempts_ShouldAcceptBoundaryValues(int value)
    {
        var setting = new PersistenceSetting { MaxRetryAttempts = value };
        setting.MaxRetryAttempts.Should().Be(value);
    }

    #endregion

    #region PoolSize Boundary Values

    [Theory(DisplayName = "PoolSize should accept boundary values")]
    [InlineData(0, 100)]
    [InlineData(1, 1)]
    [InlineData(10, 10)]
    [InlineData(0, 1)]
    [InlineData(50, 200)]
    public void PoolSize_ShouldAcceptBoundaryValues(int min, int max)
    {
        var setting = new PersistenceSetting { MinPoolSize = min, MaxPoolSize = max };
        setting.MinPoolSize.Should().Be(min);
        setting.MaxPoolSize.Should().Be(max);
    }

    [Theory(DisplayName = "PoolSize should accept inverted values (validation catches them)")]
    [InlineData(50, 10)]
    [InlineData(100, 0)]
    public void PoolSize_ShouldAcceptInvertedValues(int min, int max)
    {
        var setting = new PersistenceSetting { MinPoolSize = min, MaxPoolSize = max };
        setting.MinPoolSize.Should().Be(min);
        setting.MaxPoolSize.Should().Be(max);
    }

    #endregion

    #region Override All Options

    [Fact(DisplayName = "Can Override All Options")]
    public void CanOverrideAllOptions()
    {
        var setting = new PersistenceSetting
        {
            ConnectionStringName = "Default",
            Provider = "PostgreSQL",
            EnableSensitiveDataLogging = true,
            EnableDetailedErrors = true,
            CommandTimeoutSeconds = 60,
            MaxRetryAttempts = 5,
            RetryBaseDelayMs = 200,
            UsePooling = false,
            MinPoolSize = 5,
            MaxPoolSize = 200
        };

        setting.ConnectionStringName.Should().Be("Default");
        setting.Provider.Should().Be("PostgreSQL");
        setting.EnableSensitiveDataLogging.Should().BeTrue();
        setting.EnableDetailedErrors.Should().BeTrue();
        setting.CommandTimeoutSeconds.Should().Be(60);
        setting.MaxRetryAttempts.Should().Be(5);
        setting.RetryBaseDelayMs.Should().Be(200);
        setting.UsePooling.Should().BeFalse();
        setting.MinPoolSize.Should().Be(5);
        setting.MaxPoolSize.Should().Be(200);
    }

    #endregion
}
