using Core.Modules.Payments.Intents;

namespace Application.Modules.Payments.Disputes.Models;

/// <summary>The projected dispute row.</summary>
public sealed class DisputeReadModel
{
    /// <summary>The dispute id.</summary>
    public Guid Id { get; set; }

    /// <summary>The payment id.</summary>
    public Guid PaymentId { get; set; }

    /// <summary>The provider dispute reference.</summary>
    public string ProviderReference { get; set; } = string.Empty;

    /// <summary>The disputed amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>The currency code.</summary>
    public string Currency { get; set; } = PaymentConstant.Defaults.Currency;

    /// <summary>The provider reason.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>The dispute status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>When the provider opened the dispute.</summary>
    public DateTime OpenedAt { get; set; }

    /// <summary>When the dispute was resolved, if it has been.</summary>
    public DateTime? ResolvedAt { get; set; }
}
