using FluentValidation;

namespace Infrastructure.BuildingBlocks.Adapters.Messaging.Options;

/// <summary>Validates the shape and bounds of <see cref="MessagingSetting"/>.</summary>
/// <remarks>
/// A RabbitMQ transport without a connection string is deliberately not rejected here: the
/// composition root resolves the string from three configuration keys and fails there.
/// </remarks>
public sealed class MessagingSettingValidator : AbstractValidator<MessagingSetting>
{
    /// <summary>Creates the validator with the messaging rules.</summary>
    public MessagingSettingValidator()
    {
        RuleFor(setting => setting.Transport)
            .IsInEnum()
            .WithErrorCode(MessagingSettingOutcome.Codes.TransportUnknown)
            .WithMessage(MessagingSettingOutcome.Failure.TransportUnknown.Message);

        RuleFor(setting => setting.ExchangeName)
            .NotEmpty()
            .WithErrorCode(MessagingSettingOutcome.Codes.ExchangeNameRequired)
            .WithMessage(MessagingSettingOutcome.Failure.ExchangeNameRequired.Message);

        RuleFor(setting => setting.QueuePrefix)
            .NotEmpty()
            .WithErrorCode(MessagingSettingOutcome.Codes.QueuePrefixRequired)
            .WithMessage(MessagingSettingOutcome.Failure.QueuePrefixRequired.Message);

        RuleFor(setting => setting.PrefetchCount)
            .InclusiveBetween(
                MessagingDefaults.Constraints.PrefetchCountMin,
                MessagingDefaults.Constraints.PrefetchCountMax)
            .WithErrorCode(MessagingSettingOutcome.Codes.PrefetchCountOutOfRange)
            .WithMessage(MessagingSettingOutcome.Failure.PrefetchCountOutOfRange.Message);

        RuleFor(setting => setting.MaxConcurrency)
            .InclusiveBetween(
                MessagingDefaults.Constraints.MaxConcurrencyMin,
                MessagingDefaults.Constraints.MaxConcurrencyMax)
            .WithErrorCode(MessagingSettingOutcome.Codes.MaxConcurrencyOutOfRange)
            .WithMessage(MessagingSettingOutcome.Failure.MaxConcurrencyOutOfRange.Message);

        RuleFor(setting => setting.MaxAttempts)
            .InclusiveBetween(
                MessagingDefaults.Constraints.MaxAttemptsMin,
                MessagingDefaults.Constraints.MaxAttemptsMax)
            .WithErrorCode(MessagingSettingOutcome.Codes.MaxAttemptsOutOfRange)
            .WithMessage(MessagingSettingOutcome.Failure.MaxAttemptsOutOfRange.Message);

        RuleFor(setting => setting.RetryDelaySeconds)
            .InclusiveBetween(
                MessagingDefaults.Constraints.RetryDelaySecondsMin,
                MessagingDefaults.Constraints.RetryDelaySecondsMax)
            .WithErrorCode(MessagingSettingOutcome.Codes.RetryDelaySecondsOutOfRange)
            .WithMessage(MessagingSettingOutcome.Failure.RetryDelaySecondsOutOfRange.Message);
    }
}
