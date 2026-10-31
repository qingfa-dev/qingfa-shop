using Infrastructure.Payments.Configuration;

using Microsoft.Extensions.Options;

namespace Infrastructure.Modules.Payments;

/// <summary>Validates the reconciliation configuration at startup.</summary>
public sealed class PaymentReconciliationOptionsValidator : IValidateOptions<PaymentReconciliationOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, PaymentReconciliationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (options.IntervalMinutes is < PaymentReconciliationConstant.MinIntervalMinutes
                or > PaymentReconciliationConstant.MaxIntervalMinutes)
            failures.Add(
                $"{PaymentReconciliationOptions.SectionName}:IntervalMinutes must be between {PaymentReconciliationConstant.MinIntervalMinutes} and {PaymentReconciliationConstant.MaxIntervalMinutes}.");

        if (options.MinimumAgeMinutes is < PaymentReconciliationConstant.MinAgeMinutes
                or > PaymentReconciliationConstant.MaxAgeMinutes)
            failures.Add(
                $"{PaymentReconciliationOptions.SectionName}:MinimumAgeMinutes must be between {PaymentReconciliationConstant.MinAgeMinutes} and {PaymentReconciliationConstant.MaxAgeMinutes}.");

        if (options.BatchSize is < PaymentReconciliationConstant.MinBatchSize
                or > PaymentReconciliationConstant.MaxBatchSize)
            failures.Add(
                $"{PaymentReconciliationOptions.SectionName}:BatchSize must be between {PaymentReconciliationConstant.MinBatchSize} and {PaymentReconciliationConstant.MaxBatchSize}.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
