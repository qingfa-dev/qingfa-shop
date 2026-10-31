using FluentValidation.Results;

using Infrastructure.BuildingBlocks.Adapters.Messaging.Options;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Messaging;

/// <summary>Unit tests for <see cref="MessagingSettingValidator"/>.</summary>
/// <remarks>
/// The validator owns shape and bound checks only. A RabbitMQ transport without a connection
/// string is rejected by the composition root, because it resolves the string from configuration.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Messaging")]
public sealed class MessagingSettingValidatorTest
{
    private readonly MessagingSettingValidator _validator = new();

    private void AssertFailsWith(
        ValidationResult result,
        string propertyName,
        Error expected)
    {
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == propertyName);
        ValidationFailure failure = result.Errors.Single(error => error.PropertyName == propertyName);
        failure.ErrorCode.Should().Be(expected.Code);
        failure.ErrorMessage.Should().Be(expected.Message);
    }

    #region Valid

    [Fact(DisplayName = "Defaults should validate and match the documented values")]
    public void Defaults_ShouldValidateAndMatchDocumentedValues()
    {
        // Arrange
        MessagingSetting model = new();

        // Act
        ValidationResult result = _validator.Validate(model);

        // Assert
        result.IsValid.Should().BeTrue();
        model.Enabled.Should().BeTrue();
        model.Transport.Should().Be(MessagingTransport.InMemory);
        model.ExchangeName.Should().Be("qingfa.events");
        model.QueuePrefix.Should().Be("qingfa");
        model.PrefetchCount.Should().Be(16);
        model.MaxConcurrency.Should().Be(8);
        model.MaxAttempts.Should().Be(5);
        model.RetryDelaySeconds.Should().Be(2);
    }

    [Fact(DisplayName = "InMemory transport should validate")]
    public void InMemoryTransport_ShouldValidate()
    {
        // Arrange
        MessagingSetting model = new() { Transport = MessagingTransport.InMemory };

        // Act
        ValidationResult result = _validator.Validate(model);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "RabbitMq transport should validate without a connection string")]
    public void RabbitMqTransport_WithoutConnectionString_ShouldValidate()
    {
        // Arrange
        MessagingSetting model = new() { Transport = MessagingTransport.RabbitMq };

        // Act
        ValidationResult result = _validator.Validate(model);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "PrefetchCount bounds should be inclusive")]
    public void PrefetchCount_Boundaries_ShouldValidate()
    {
        MessagingSetting minimum = new()
        {
            PrefetchCount = MessagingDefaults.Constraints.PrefetchCountMin
        };
        MessagingSetting maximum = new()
        {
            PrefetchCount = MessagingDefaults.Constraints.PrefetchCountMax
        };

        _validator.Validate(minimum).IsValid.Should().BeTrue();
        _validator.Validate(maximum).IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "MaxConcurrency bounds should be inclusive")]
    public void MaxConcurrency_Boundaries_ShouldValidate()
    {
        MessagingSetting minimum = new()
        {
            MaxConcurrency = MessagingDefaults.Constraints.MaxConcurrencyMin
        };
        MessagingSetting maximum = new()
        {
            MaxConcurrency = MessagingDefaults.Constraints.MaxConcurrencyMax
        };

        _validator.Validate(minimum).IsValid.Should().BeTrue();
        _validator.Validate(maximum).IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "MaxAttempts bounds should be inclusive")]
    public void MaxAttempts_Boundaries_ShouldValidate()
    {
        MessagingSetting minimum = new()
        {
            MaxAttempts = MessagingDefaults.Constraints.MaxAttemptsMin
        };
        MessagingSetting maximum = new()
        {
            MaxAttempts = MessagingDefaults.Constraints.MaxAttemptsMax
        };

        _validator.Validate(minimum).IsValid.Should().BeTrue();
        _validator.Validate(maximum).IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "RetryDelaySeconds bounds should be inclusive")]
    public void RetryDelaySeconds_Boundaries_ShouldValidate()
    {
        MessagingSetting minimum = new()
        {
            RetryDelaySeconds = MessagingDefaults.Constraints.RetryDelaySecondsMin
        };
        MessagingSetting maximum = new()
        {
            RetryDelaySeconds = MessagingDefaults.Constraints.RetryDelaySecondsMax
        };

        _validator.Validate(minimum).IsValid.Should().BeTrue();
        _validator.Validate(maximum).IsValid.Should().BeTrue();
    }

    #endregion

    #region Invalid

    [Fact(DisplayName = "Unknown transport should fail validation")]
    public void UnknownTransport_ShouldFail()
    {
        // Arrange
        MessagingSetting model = new() { Transport = (MessagingTransport)99 };

        // Act
        ValidationResult result = _validator.Validate(model);

        // Assert
        AssertFailsWith(
            result,
            nameof(MessagingSetting.Transport),
            MessagingSettingOutcome.Failure.TransportUnknown);
    }

    [Theory(DisplayName = "Non-positive PrefetchCount should fail validation")]
    [InlineData(0)]
    [InlineData(-1)]
    public void PrefetchCount_NonPositive_ShouldFail(int prefetchCount)
    {
        // Arrange
        MessagingSetting model = new() { PrefetchCount = prefetchCount };

        // Act
        ValidationResult result = _validator.Validate(model);

        // Assert
        AssertFailsWith(
            result,
            nameof(MessagingSetting.PrefetchCount),
            MessagingSettingOutcome.Failure.PrefetchCountOutOfRange);
    }

    [Fact(DisplayName = "PrefetchCount above the broker maximum should fail validation")]
    public void PrefetchCount_AboveBrokerMaximum_ShouldFail()
    {
        // Arrange
        MessagingSetting model = new()
        {
            PrefetchCount = MessagingDefaults.Constraints.PrefetchCountMax + 1
        };

        // Act
        ValidationResult result = _validator.Validate(model);

        // Assert
        AssertFailsWith(
            result,
            nameof(MessagingSetting.PrefetchCount),
            MessagingSettingOutcome.Failure.PrefetchCountOutOfRange);
    }

    [Theory(DisplayName = "Non-positive MaxConcurrency should fail validation")]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxConcurrency_NonPositive_ShouldFail(int maxConcurrency)
    {
        // Arrange
        MessagingSetting model = new() { MaxConcurrency = maxConcurrency };

        // Act
        ValidationResult result = _validator.Validate(model);

        // Assert
        AssertFailsWith(
            result,
            nameof(MessagingSetting.MaxConcurrency),
            MessagingSettingOutcome.Failure.MaxConcurrencyOutOfRange);
    }

    [Fact(DisplayName = "MaxConcurrency above the configured maximum should fail validation")]
    public void MaxConcurrency_AboveMaximum_ShouldFail()
    {
        // Arrange
        MessagingSetting model = new()
        {
            MaxConcurrency = MessagingDefaults.Constraints.MaxConcurrencyMax + 1
        };

        // Act
        ValidationResult result = _validator.Validate(model);

        // Assert
        AssertFailsWith(
            result,
            nameof(MessagingSetting.MaxConcurrency),
            MessagingSettingOutcome.Failure.MaxConcurrencyOutOfRange);
    }

    [Theory(DisplayName = "Non-positive MaxAttempts should fail validation")]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxAttempts_NonPositive_ShouldFail(int maxAttempts)
    {
        // Arrange
        MessagingSetting model = new() { MaxAttempts = maxAttempts };

        // Act
        ValidationResult result = _validator.Validate(model);

        // Assert
        AssertFailsWith(
            result,
            nameof(MessagingSetting.MaxAttempts),
            MessagingSettingOutcome.Failure.MaxAttemptsOutOfRange);
    }

    [Fact(DisplayName = "MaxAttempts above the configured maximum should fail validation")]
    public void MaxAttempts_AboveMaximum_ShouldFail()
    {
        // Arrange
        MessagingSetting model = new()
        {
            MaxAttempts = MessagingDefaults.Constraints.MaxAttemptsMax + 1
        };

        // Act
        ValidationResult result = _validator.Validate(model);

        // Assert
        AssertFailsWith(
            result,
            nameof(MessagingSetting.MaxAttempts),
            MessagingSettingOutcome.Failure.MaxAttemptsOutOfRange);
    }

    [Theory(DisplayName = "Non-positive RetryDelaySeconds should fail validation")]
    [InlineData(0)]
    [InlineData(-1)]
    public void RetryDelaySeconds_NonPositive_ShouldFail(int retryDelaySeconds)
    {
        // Arrange
        MessagingSetting model = new() { RetryDelaySeconds = retryDelaySeconds };

        // Act
        ValidationResult result = _validator.Validate(model);

        // Assert
        AssertFailsWith(
            result,
            nameof(MessagingSetting.RetryDelaySeconds),
            MessagingSettingOutcome.Failure.RetryDelaySecondsOutOfRange);
    }

    [Fact(DisplayName = "RetryDelaySeconds above the configured maximum should fail validation")]
    public void RetryDelaySeconds_AboveMaximum_ShouldFail()
    {
        // Arrange
        MessagingSetting model = new()
        {
            RetryDelaySeconds = MessagingDefaults.Constraints.RetryDelaySecondsMax + 1
        };

        // Act
        ValidationResult result = _validator.Validate(model);

        // Assert
        AssertFailsWith(
            result,
            nameof(MessagingSetting.RetryDelaySeconds),
            MessagingSettingOutcome.Failure.RetryDelaySecondsOutOfRange);
    }

    [Theory(DisplayName = "Blank exchange name should fail validation")]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankExchangeName_ShouldFail(string exchangeName)
    {
        // Arrange
        MessagingSetting model = new() { ExchangeName = exchangeName };

        // Act
        ValidationResult result = _validator.Validate(model);

        // Assert
        AssertFailsWith(
            result,
            nameof(MessagingSetting.ExchangeName),
            MessagingSettingOutcome.Failure.ExchangeNameRequired);
    }

    [Theory(DisplayName = "Blank queue prefix should fail validation")]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankQueuePrefix_ShouldFail(string queuePrefix)
    {
        // Arrange
        MessagingSetting model = new() { QueuePrefix = queuePrefix };

        // Act
        ValidationResult result = _validator.Validate(model);

        // Assert
        AssertFailsWith(
            result,
            nameof(MessagingSetting.QueuePrefix),
            MessagingSettingOutcome.Failure.QueuePrefixRequired);
    }

    #endregion
}
