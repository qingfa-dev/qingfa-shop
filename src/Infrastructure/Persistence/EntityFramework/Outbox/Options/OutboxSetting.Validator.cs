using FluentValidation;

namespace Infrastructure.Persistence.EntityFramework.Outbox.Options;

/// <summary>Validates <see cref="OutboxSetting"/>.</summary>
public sealed class OutboxSettingValidator : AbstractValidator<OutboxSetting>
{
    /// <summary>Creates the validator.</summary>
    public OutboxSettingValidator()
    {
        RuleFor(setting => setting.BatchSize).GreaterThan(0);
        RuleFor(setting => setting.PollingIntervalSeconds).GreaterThan(0);
        RuleFor(setting => setting.MaxAttempts).GreaterThan(0);
        RuleFor(setting => setting.BackoffSeconds).GreaterThanOrEqualTo(0);
        RuleFor(setting => setting.MaxBackoffSeconds).GreaterThanOrEqualTo(0);
        RuleFor(setting => setting.LeaseSeconds).GreaterThan(0);
        RuleFor(setting => setting.RetentionDays).GreaterThanOrEqualTo(0);
        RuleFor(setting => setting.MaxAgeSeconds).GreaterThan(0);
    }
}
