namespace Application.Modules.Payments.Ports;

/// <summary>The configured payment policy the slices enforce.</summary>
/// <remarks>
/// Limits live in configuration, but the slices must not depend on an Infrastructure options type.
/// This port carries only the values the application needs.
/// </remarks>
public interface IPaymentPolicy
{
    /// <summary>The largest amount a single payment may request.</summary>
    decimal MaxAmount { get; }

    /// <summary>How long a hosted-checkout intent stays payable.</summary>
    TimeSpan IntentLifetime { get; }

    /// <summary>Extra time allowed before expiry is enforced, absorbing provider clock skew.</summary>
    TimeSpan ExpiryGrace { get; }
}
