namespace Application.Modules.Payments.Ports;

/// <summary>
/// Exposes the configured payment policies enforced by Application.
/// </summary>
/// <remarks>
/// This contract contains application-facing policy values only.
/// Infrastructure is responsible for binding, validating, and providing
/// the configuration values.
///
/// Amount limits are expressed in the minor-unit-aware decimal format
/// used by the application's payment amount model, in the specified currency.
/// Currency codes use ISO 4217.
/// </remarks>
public interface IPaymentPolicy
{
    /// <summary>
    /// Gets the maximum permitted payment amount for each supported currency.
    /// Keys are normalized ISO 4217 currency codes.
    /// </summary>
    IReadOnlyDictionary<string, decimal> MaximumAmountsByCurrency { get; }

    /// <summary>
    /// Gets the lifetime of a newly created hosted-checkout intent.
    /// </summary>
    TimeSpan HostedCheckoutIntentLifetime { get; }

    /// <summary>
    /// Gets the additional tolerance applied when evaluating provider-side
    /// expiration and delayed payment events.
    /// </summary>
    TimeSpan ProviderExpiryGracePeriod { get; }
}
