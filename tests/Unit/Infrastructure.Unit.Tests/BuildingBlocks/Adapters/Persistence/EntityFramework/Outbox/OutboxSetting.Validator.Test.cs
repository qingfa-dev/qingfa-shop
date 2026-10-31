using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox.Options;

using FluentValidation.Results;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;

/// <summary>Tests for <see cref="OutboxSettingValidator"/>.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Messaging")]
public sealed class OutboxSettingValidatorTest
{
    private readonly OutboxSettingValidator _validator = new();

    [Fact(DisplayName = "Default outbox settings should be valid")]
    public void Validate_WithDefaults_ShouldBeValid()
    {
        ValidationResult result = _validator.Validate(new OutboxSetting());

        result.IsValid.Should().BeTrue();
    }

    [Theory(DisplayName = "Outbox settings should reject non-positive bounds")]
    [InlineData(nameof(OutboxSetting.BatchSize))]
    [InlineData(nameof(OutboxSetting.PollingIntervalSeconds))]
    [InlineData(nameof(OutboxSetting.MaxAttempts))]
    [InlineData(nameof(OutboxSetting.LeaseSeconds))]
    [InlineData(nameof(OutboxSetting.MaxAgeSeconds))]
    public void Validate_WhenBoundIsNonPositive_ShouldFail(string property)
    {
        var setting = new OutboxSetting();
        typeof(OutboxSetting).GetProperty(property)!.SetValue(setting, 0);

        ValidationResult result = _validator.Validate(setting);

        result.IsValid.Should().BeFalse();
    }
}
