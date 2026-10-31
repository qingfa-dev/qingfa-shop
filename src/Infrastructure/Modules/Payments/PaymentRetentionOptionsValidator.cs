using Infrastructure.Payments.Configuration;

using Microsoft.Extensions.Options;

namespace Infrastructure.Modules.Payments;

/// <summary>Validates the retention configuration at startup.</summary>
public sealed class PaymentRetentionOptionsValidator : IValidateOptions<PaymentRetentionOptions>
{
    /// <summary>Shortest accepted webhook retention.</summary>
    public const int MinRetentionDays = 1;

    /// <summary>Longest accepted webhook retention.</summary>
    public const int MaxRetentionDays = 365;

    /// <summary>Shortest accepted sweep interval.</summary>
    public const int MinSweepHours = 1;

    /// <summary>Longest accepted sweep interval.</summary>
    public const int MaxSweepHours = 24;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, PaymentRetentionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (options.WebhookEventRetentionDays is < MinRetentionDays or > MaxRetentionDays)
            failures.Add(
                $"{PaymentRetentionOptions.SectionName}:WebhookEventRetentionDays must be between {MinRetentionDays} and {MaxRetentionDays}.");

        if (options.SweepIntervalHours is < MinSweepHours or > MaxSweepHours)
            failures.Add(
                $"{PaymentRetentionOptions.SectionName}:SweepIntervalHours must be between {MinSweepHours} and {MaxSweepHours}.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
