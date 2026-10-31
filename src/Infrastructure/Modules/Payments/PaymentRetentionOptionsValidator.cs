using Infrastructure.Payments.Configuration;

using Microsoft.Extensions.Options;

namespace Infrastructure.Modules.Payments;

/// <summary>Validates the retention configuration at startup.</summary>
public sealed class PaymentRetentionOptionsValidator : IValidateOptions<PaymentRetentionOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, PaymentRetentionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (options.WebhookEventRetentionDays is < PaymentsConfigurationConstant.MinRetentionDays or > PaymentsConfigurationConstant.MaxRetentionDays)
            failures.Add(
                $"{PaymentRetentionOptions.SectionName}:WebhookEventRetentionDays must be between {PaymentsConfigurationConstant.MinRetentionDays} and {PaymentsConfigurationConstant.MaxRetentionDays}.");

        if (options.SweepIntervalHours is < PaymentsConfigurationConstant.MinSweepHours or > PaymentsConfigurationConstant.MaxSweepHours)
            failures.Add(
                $"{PaymentRetentionOptions.SectionName}:SweepIntervalHours must be between {PaymentsConfigurationConstant.MinSweepHours} and {PaymentsConfigurationConstant.MaxSweepHours}.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
