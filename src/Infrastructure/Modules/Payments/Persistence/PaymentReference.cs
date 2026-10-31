namespace Infrastructure.Modules.Payments.Persistence;

/// <summary>A provider reference to payment mapping written synchronously on create.</summary>
public sealed class PaymentReference
{
    /// <summary>The row id.</summary>
    public Guid Id { get; set; }

    /// <summary>The provider name.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>The provider reference.</summary>
    public string ProviderReference { get; set; } = string.Empty;

    /// <summary>The payment id.</summary>
    public Guid PaymentId { get; set; }

    /// <summary>When the mapping was written.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
