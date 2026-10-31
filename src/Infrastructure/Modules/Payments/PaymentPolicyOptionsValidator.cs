using System.Text.RegularExpressions;

using Infrastructure.Payments.Configuration;

using Microsoft.Extensions.Options;

namespace Infrastructure.Modules.Payments;

/// <summary>Validates the per-currency payment policy at startup.</summary>
public sealed partial class PaymentPolicyOptionsValidator : IValidateOptions<PaymentPolicyOptions>
{
    /// <summary>The shortest accepted intent lifetime.</summary>
    public static readonly TimeSpan MinLifetime = TimeSpan.FromMinutes(1);

    /// <summary>The longest accepted intent lifetime.</summary>
    public static readonly TimeSpan MaxLifetime = TimeSpan.FromDays(1);

    /// <summary>The largest accepted expiry grace.</summary>
    public static readonly TimeSpan MaxGrace = TimeSpan.FromHours(1);

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, PaymentPolicyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (options.MaximumAmountsByCurrency.Count == 0)
            failures.Add($"{PaymentPolicyOptions.SectionName}:MaximumAmountsByCurrency must declare at least one currency.");

        foreach (KeyValuePair<string, decimal> limit in options.MaximumAmountsByCurrency)
        {
            if (!CurrencyCode().IsMatch(limit.Key))
                failures.Add(
                    $"{PaymentPolicyOptions.SectionName}:MaximumAmountsByCurrency key '{limit.Key}' is not an ISO 4217 code.");

            if (limit.Value <= 0)
                failures.Add(
                    $"{PaymentPolicyOptions.SectionName}:MaximumAmountsByCurrency['{limit.Key}'] must be greater than zero.");
        }

        if (options.HostedCheckoutIntentLifetime < MinLifetime
            || options.HostedCheckoutIntentLifetime > MaxLifetime)
            failures.Add(
                $"{PaymentPolicyOptions.SectionName}:HostedCheckoutIntentLifetime must be between 00:01:00 and 1.00:00:00.");

        if (options.ProviderExpiryGracePeriod < TimeSpan.Zero
            || options.ProviderExpiryGracePeriod > MaxGrace)
            failures.Add(
                $"{PaymentPolicyOptions.SectionName}:ProviderExpiryGracePeriod must be between 00:00:00 and 01:00:00.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    /// <summary>Matches a three-letter uppercase ISO 4217 code.</summary>
    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyCode();
}
