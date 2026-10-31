using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing.Options;

using FluentValidation.Results;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing;

/// <summary>Tests for <see cref="EventStoreSettingValidator"/>.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "EventSourcing")]
public sealed class EventStoreSettingValidatorTest
{
    private readonly EventStoreSettingValidator _validator = new();

    [Fact(DisplayName = "Default event-store settings should be valid")]
    public void Validate_WithDefaults_ShouldBeValid()
    {
        ValidationResult result = _validator.Validate(new EventStoreSetting());

        result.IsValid.Should().BeTrue();
    }

    [Theory(DisplayName = "Event-store settings should reject non-positive bounds")]
    [InlineData(nameof(EventStoreSetting.BatchSize))]
    [InlineData(nameof(EventStoreSetting.SnapshotInterval))]
    [InlineData(nameof(EventStoreSetting.PollingIntervalSeconds))]
    public void Validate_WhenBoundIsNonPositive_ShouldFail(string property)
    {
        var setting = new EventStoreSetting();
        typeof(EventStoreSetting).GetProperty(property)!.SetValue(setting, 0);

        ValidationResult result = _validator.Validate(setting);

        result.IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "Event-store settings should reject an empty provider")]
    public void Validate_WhenProviderIsEmpty_ShouldFail()
    {
        var setting = new EventStoreSetting { Provider = string.Empty };

        ValidationResult result = _validator.Validate(setting);

        result.IsValid.Should().BeFalse();
    }
}
