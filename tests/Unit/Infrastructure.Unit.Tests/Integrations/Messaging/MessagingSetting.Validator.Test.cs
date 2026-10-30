using FluentValidation.Results;

using Infrastructure.Integrations.Messaging.Options;

namespace Infrastructure.Unit.Tests.Integrations.Messaging;

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
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(MessagingSetting.Transport));
    }

    [Theory(DisplayName = "Non-positive counts should fail validation")]
    [InlineData(0)]
    [InlineData(-1)]
    public void PrefetchCount_NonPositive_ShouldFail(int prefetchCount)
    {
        // Arrange
        MessagingSetting model = new() { PrefetchCount = prefetchCount };

        // Act
        ValidationResult result = _validator.Validate(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(MessagingSetting.PrefetchCount));
    }

    [Fact(DisplayName = "PrefetchCount above the broker maximum should fail validation")]
    public void PrefetchCount_AboveBrokerMaximum_ShouldFail()
    {
        // Arrange
        MessagingSetting model = new() { PrefetchCount = ushort.MaxValue + 1 };

        // Act
        ValidationResult result = _validator.Validate(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(MessagingSetting.PrefetchCount));
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
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(MessagingSetting.MaxConcurrency));
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
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(MessagingSetting.MaxAttempts));
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
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(MessagingSetting.RetryDelaySeconds));
    }

    [Theory(DisplayName = "Blank exchange and prefix names should fail validation")]
    [InlineData("", "qingfa")]
    [InlineData("qingfa.events", "")]
    [InlineData("   ", "qingfa")]
    [InlineData("qingfa.events", "   ")]
    public void BlankNames_ShouldFail(string exchangeName, string queuePrefix)
    {
        // Arrange
        MessagingSetting model = new() { ExchangeName = exchangeName, QueuePrefix = queuePrefix };

        // Act
        ValidationResult result = _validator.Validate(model);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion
}
