namespace Infrastructure.Modules.Payments.Adapters.PayPal;

/// <summary>The PayPal REST environment.</summary>
public enum PayPalEnvironment
{
    /// <summary>The sandbox.</summary>
    Sandbox = 0,

    /// <summary>Production.</summary>
    Live = 1
}
