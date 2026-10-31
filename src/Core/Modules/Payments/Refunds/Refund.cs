using Core.BuildingBlocks.Domain.ValueObjects.Money;

namespace Core.Modules.Payments.Refunds;

/// <summary>A refund issued against a captured payment.</summary>
public sealed class Refund
{
    private Refund() { }

    /// <summary>The refund identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>The refunded amount.</summary>
    public Money Amount { get; private set; }

    /// <summary>The refund state.</summary>
    public RefundStatus Status { get; private set; }

    /// <summary>The provider refund reference.</summary>
    public string? ProviderReference { get; private set; }

    /// <summary>The operator-supplied reason.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>The failure code when rejected.</summary>
    public string? FailureCode { get; private set; }

    /// <summary>The failure message when rejected.</summary>
    public string? FailureMessage { get; private set; }

    internal static Refund Request(RefundRequested requested) => new()
    {
        Id = requested.RefundId,
        Amount = requested.Amount,
        Status = RefundStatus.Pending,
        Reason = requested.Reason
    };

    internal void MarkSucceeded(string providerReference)
    {
        Status = RefundStatus.Succeeded;
        ProviderReference = providerReference;
    }

    internal void MarkFailed(string failureCode, string? failureMessage)
    {
        Status = RefundStatus.Failed;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
    }
}