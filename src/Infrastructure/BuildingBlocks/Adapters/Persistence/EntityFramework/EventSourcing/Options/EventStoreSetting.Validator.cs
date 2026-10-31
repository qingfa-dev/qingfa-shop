using FluentValidation;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing.Options;

/// <summary>Validates <see cref="EventStoreSetting"/>.</summary>
public sealed class EventStoreSettingValidator : AbstractValidator<EventStoreSetting>
{
    public EventStoreSettingValidator()
    {
        RuleFor(setting => setting.Provider).NotEmpty();
        RuleFor(setting => setting.BatchSize).GreaterThan(0);
        RuleFor(setting => setting.SnapshotInterval).GreaterThan(0);
        RuleFor(setting => setting.PollingIntervalSeconds).GreaterThan(0);
    }
}
