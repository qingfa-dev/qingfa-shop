using Infrastructure.Persistence.EntityFramework.Options;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class PersistenceSettingValidationTest
{
    private static PersistenceSetting CreateValidSetting() => new()
    {
        ConnectionStringName = "Default",
        Provider = "PostgreSQL",
        CommandTimeoutSeconds = 30,
        MaxRetryAttempts = 3,
        RetryBaseDelayMs = 100,
        UsePooling = true,
        MinPoolSize = 0,
        MaxPoolSize = 100
    };

    #region Valid Options

    [Fact(DisplayName = "Validate with valid settings should succeed")]
    public void Validate_WithValidSettings_ShouldSucceed()
    {
        // Arrange
        var setting = CreateValidSetting();

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation.Validate(setting);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(setting);
    }

    [Fact(DisplayName = "Validate with minimal valid settings should succeed")]
    public void Validate_WithMinimalValidSettings_ShouldSucceed()
    {
        // Arrange
        var setting = new PersistenceSetting
        {
            ConnectionStringName = "Default"
        };

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation.Validate(setting);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Connection String Name

    [Fact(DisplayName = "Validate with null connection string name should fail")]
    public void Validate_WithNullConnectionStringName_ShouldFail()
    {
        // Arrange
        var setting = new PersistenceSetting { ConnectionStringName = null };

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation.Validate(setting);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors.First().Code.Should().Be("EfCore.ConnectionStringName.Required");
    }

    [Fact(DisplayName = "Validate with empty connection string name should fail")]
    public void Validate_WithEmptyConnectionStringName_ShouldFail()
    {
        // Arrange
        var setting = new PersistenceSetting { ConnectionStringName = "" };

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation.Validate(setting);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("EfCore.ConnectionStringName.Required");
    }

    [Fact(DisplayName = "Validate with whitespace connection string name should fail")]
    public void Validate_WithWhitespaceConnectionStringName_ShouldFail()
    {
        // Arrange
        var setting = new PersistenceSetting { ConnectionStringName = "   " };

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation.Validate(setting);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("EfCore.ConnectionStringName.Required");
    }

    #endregion

    #region Provider

    [Fact(DisplayName = "Validate with unsupported provider should fail")]
    public void Validate_WithUnsupportedProvider_ShouldFail()
    {
        // Arrange
        var setting = CreateValidSetting();
        setting.Provider = "SqlServer";

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation.Validate(setting);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("EfCore.Provider.Unsupported");
    }

    [Fact(DisplayName = "Validate with PostgreSQL provider should succeed")]
    public void Validate_WithPostgreSQLProvider_ShouldSucceed()
    {
        // Arrange
        var setting = CreateValidSetting();
        setting.Provider = "PostgreSQL";

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation.Validate(setting);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Command Timeout

    [Fact(DisplayName = "Validate with command timeout below minimum should fail")]
    public void Validate_WithCommandTimeoutBelowMinimum_ShouldFail()
    {
        // Arrange
        var setting = CreateValidSetting();
        setting.CommandTimeoutSeconds = 0;

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation.Validate(setting);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("EfCore.CommandTimeout.OutOfRange");
    }

    [Fact(DisplayName = "Validate with command timeout above maximum should fail")]
    public void Validate_WithCommandTimeoutAboveMaximum_ShouldFail()
    {
        // Arrange
        var setting = CreateValidSetting();
        setting.CommandTimeoutSeconds = 3601;

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation.Validate(setting);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("EfCore.CommandTimeout.OutOfRange");
    }

    [Fact(DisplayName = "Validate with command timeout at boundary should succeed")]
    public void Validate_WithCommandTimeoutAtBoundary_ShouldSucceed()
    {
        // Arrange
        var setting = CreateValidSetting();
        setting.CommandTimeoutSeconds = PersistenceConstants.Constraints.CommandTimeoutSecondsMin;

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation.Validate(setting);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Retry Policy

    [Fact(DisplayName = "Validate with max retry attempts exceeded should fail")]
    public void Validate_WithMaxRetryAttemptsExceeded_ShouldFail()
    {
        // Arrange
        var setting = CreateValidSetting();
        setting.MaxRetryAttempts = PersistenceConstants.Limits.MaxRetryAttemptsLimit + 1;

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation.Validate(setting);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("EfCore.MaxRetryAttempts.Exceeded");
    }

    [Fact(DisplayName = "Validate with retry base delay exceeded should fail")]
    public void Validate_WithRetryBaseDelayExceeded_ShouldFail()
    {
        // Arrange
        var setting = CreateValidSetting();
        setting.RetryBaseDelayMs = PersistenceConstants.Limits.RetryBaseDelayMsLimit + 1;

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation.Validate(setting);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("EfCore.RetryBaseDelay.Exceeded");
    }

    #endregion

    #region Pooling

    [Fact(DisplayName = "Validate with max pool size exceeded should fail")]
    public void Validate_WithMaxPoolSizeExceeded_ShouldFail()
    {
        // Arrange
        var setting = CreateValidSetting();
        setting.MaxPoolSize = PersistenceConstants.Limits.MaxPoolSizeLimit + 1;

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation.Validate(setting);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("EfCore.MaxPoolSize.Exceeded");
    }

    [Fact(DisplayName = "Validate with inverted pool size should fail")]
    public void Validate_WithInvertedPoolSize_ShouldFail()
    {
        // Arrange
        var setting = CreateValidSetting();
        setting.MinPoolSize = 50;
        setting.MaxPoolSize = 10;

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation.Validate(setting);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("EfCore.PoolSize.Inverted");
    }

    #endregion

    #region Multiple Errors

    [Fact(DisplayName = "Validate with multiple errors should collect all errors")]
    public void Validate_WithMultipleErrors_ShouldCollectAllErrors()
    {
        // Arrange
        var setting = new PersistenceSetting
        {
            ConnectionStringName = null,
            Provider = "SqlServer",
            CommandTimeoutSeconds = 0,
            MaxRetryAttempts = 999,
            MinPoolSize = 50,
            MaxPoolSize = 10
        };

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation.Validate(setting);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCountGreaterThan(1);
    }

    #endregion

    #region Connection String Resolution

    [Fact(DisplayName = "ValidateWithConnectionString with invalid static rules should skip resolution")]
    public void ValidateWithConnectionString_WhenStaticRulesFail_ShouldReturnThem()
    {
        // Arrange
        var setting = CreateValidSetting();
        setting.Provider = "SqlServer";
        var config = new ConfigurationBuilder().Build();

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation
            .ValidateWithConnectionString(setting, config);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("EfCore.Provider.Unsupported");
    }

    [Fact(DisplayName = "ValidateWithConnectionString without a Host key should fail for PostgreSQL")]
    public void ValidateWithConnectionString_WhenHostMissing_ShouldFail()
    {
        // Arrange
        var setting = CreateValidSetting();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Database=test;Username=test;Password=test"
            })
            .Build();

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation
            .ValidateWithConnectionString(setting, config);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("EfCore.ConnectionString.Invalid");
    }

    #endregion

    #region Connection Probe

    [Fact(DisplayName = "ValidateWithConnectionProbeAsync should short-circuit on invalid static rules")]
    public async Task ValidateWithConnectionProbeAsync_WhenStaticRulesFail_ShouldReturnThem()
    {
        // Arrange
        var setting = CreateValidSetting();
        setting.Provider = "SqlServer";
        var config = new ConfigurationBuilder().Build();

        // Act
        Result<PersistenceSetting> result = await PersistenceSettingValidation
            .ValidateWithConnectionProbeAsync(setting, config);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("EfCore.Provider.Unsupported");
    }

    [Fact(DisplayName = "ValidateWithConnectionProbeAsync should report an unreachable host")]
    public async Task ValidateWithConnectionProbeAsync_WhenHostRefuses_ShouldFail()
    {
        // Arrange
        var setting = CreateValidSetting();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    "Host=127.0.0.1;Port=1;Timeout=1;Database=test;Username=test;Password=test;Pooling=false"
            })
            .Build();

        // Act
        Result<PersistenceSetting> result = await PersistenceSettingValidation
            .ValidateWithConnectionProbeAsync(setting, config);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("EfCore.Connection.ProbeFailed");
    }

    [Fact(DisplayName = "ValidateWithConnectionProbeAsync should report a malformed connection string")]
    public async Task ValidateWithConnectionProbeAsync_WhenConnectionStringMalformed_ShouldFail()
    {
        // Arrange
        var setting = CreateValidSetting();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;this is not a valid key"
            })
            .Build();

        // Act
        Result<PersistenceSetting> result = await PersistenceSettingValidation
            .ValidateWithConnectionProbeAsync(setting, config);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("EfCore.Connection.ProbeFailed");
    }

    [Fact(DisplayName = "ValidateWithConnectionProbeAsync should report cancellation")]
    public async Task ValidateWithConnectionProbeAsync_WhenCancelled_ShouldFail()
    {
        // Arrange
        var setting = CreateValidSetting();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    "Host=127.0.0.1;Port=1;Timeout=1;Database=test;Username=test;Password=test"
            })
            .Build();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result<PersistenceSetting> result = await PersistenceSettingValidation
            .ValidateWithConnectionProbeAsync(setting, config, cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("EfCore.Connection.ProbeFailed");
    }

    #endregion

    #region Connection String Resolution

    [Fact(DisplayName = "ValidateWithConnectionString with missing connection string should fail")]
    public void ValidateWithConnectionString_WithMissingConnectionString_ShouldFail()
    {
        // Arrange
        var setting = CreateValidSetting();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EfCore:ConnectionStringName"] = "Default"
            })
            .Build();

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation
            .ValidateWithConnectionString(setting, config);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("EfCore.ConnectionString.NotFound");
    }

    [Fact(DisplayName = "ValidateWithConnectionString with valid connection string should succeed")]
    public void ValidateWithConnectionString_WithValidConnectionString_ShouldSucceed()
    {
        // Arrange
        var setting = CreateValidSetting();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test"
            })
            .Build();

        // Act
        Result<PersistenceSetting> result = PersistenceSettingValidation
            .ValidateWithConnectionString(setting, config);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Describe

    [Fact(DisplayName = "Describe should format errors as Code: Message")]
    public void Describe_ShouldFormatErrors()
    {
        // Arrange
        var setting = new PersistenceSetting { ConnectionStringName = null };
        Result<PersistenceSetting> result = PersistenceSettingValidation.Validate(setting);

        // Act
        var descriptions = PersistenceSettingValidation.Describe(result);

        // Assert
        descriptions.Should().ContainSingle();
        descriptions.First().Should().Contain("EfCore.ConnectionStringName.Required");
    }

    #endregion
}
