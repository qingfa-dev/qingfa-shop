using FluentValidation;

namespace Infrastructure.Integrations.Messaging.Options;

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
            .WithErrorCode("Messaging.Transport.Unknown")
            .WithMessage("Messaging:Transport must be a known transport.");

        RuleFor(setting => setting.ExchangeName)
            .NotEmpty()
            .WithErrorCode("Messaging.ExchangeName.Required")
            .WithMessage("Messaging:ExchangeName is required.");

        RuleFor(setting => setting.QueuePrefix)
            .NotEmpty()
            .WithErrorCode("Messaging.QueuePrefix.Required")
            .WithMessage("Messaging:QueuePrefix is required.");

        RuleFor(setting => setting.PrefetchCount)
            .InclusiveBetween(1, MessagingDefaults.Constraints.PrefetchCountMax)
            .WithErrorCode("Messaging.PrefetchCount.OutOfRange")
            .WithMessage(
                $"Messaging:PrefetchCount must be between 1 and {MessagingDefaults.Constraints.PrefetchCountMax}.");

        RuleFor(setting => setting.MaxConcurrency)
            .InclusiveBetween(1, MessagingDefaults.Constraints.MaxConcurrencyMax)
            .WithErrorCode("Messaging.MaxConcurrency.OutOfRange")
            .WithMessage(
                $"Messaging:MaxConcurrency must be between 1 and {MessagingDefaults.Constraints.MaxConcurrencyMax}.");

        RuleFor(setting => setting.MaxAttempts)
            .InclusiveBetween(1, MessagingDefaults.Constraints.MaxAttemptsMax)
            .WithErrorCode("Messaging.MaxAttempts.OutOfRange")
            .WithMessage(
                $"Messaging:MaxAttempts must be between 1 and {MessagingDefaults.Constraints.MaxAttemptsMax}.");

        RuleFor(setting => setting.RetryDelaySeconds)
            .InclusiveBetween(1, MessagingDefaults.Constraints.RetryDelaySecondsMax)
            .WithErrorCode("Messaging.RetryDelaySeconds.OutOfRange")
            .WithMessage(
                $"Messaging:RetryDelaySeconds must be between 1 and {MessagingDefaults.Constraints.RetryDelaySecondsMax}.");
    }
}
