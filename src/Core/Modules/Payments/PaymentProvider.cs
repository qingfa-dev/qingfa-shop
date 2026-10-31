namespace Core.Modules.Payments;

/// <summary>The external payment provider that settles a payment.</summary>
public enum PaymentProvider
{
    /// <summary>Stripe.</summary>
    Stripe = 0,

    /// <summary>PayPal.</summary>
    PayPal = 1,

    /// <summary>Deterministic in-memory provider used by tests and local development.</summary>
    Fake = 2
}