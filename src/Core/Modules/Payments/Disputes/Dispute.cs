using Core.BuildingBlocks.Domain.ValueObjects.Money;

namespace Core.Modules.Payments.Disputes;

/// <summary>A provider dispute raised against a captured payment.</summary>
public sealed class Dispute
{
    private Dispute() { }

    /// <summary>The dispute identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>The provider dispute reference.</summary>
    public string ProviderReference { get; private set; } = string.Empty;

    /// <summary>The disputed amount.</summary>
    public Money Amount { get; private set; }

    /// <summary>The provider reason code.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>The dispute state.</summary>
    public DisputeStatus Status { get; private set; }

    /// <summary>When the provider opened the dispute.</summary>
    public DateTimeOffset OpenedAt { get; private set; }

    /// <summary>When the dispute was resolved, if it has been.</summary>
    public DateTimeOffset? ResolvedAt { get; private set; }

    /// <summary>Whether the dispute still needs attention.</summary>
    public bool IsOpen => Status is DisputeStatus.Opened;

    internal static Dispute Open(DisputeOpened opened) => new()
    {
        Id = opened.DisputeId,
        ProviderReference = opened.ProviderDisputeReference,
        Amount = opened.Amount,
        Reason = opened.Reason,
        Status = DisputeStatus.Opened,
        OpenedAt = opened.OccurredAtUtc
    };

    internal void Resolve(DisputeOutcome outcome, DateTimeOffset resolvedAt)
    {
        Status = outcome switch
        {
            DisputeOutcome.Lost => DisputeStatus.Lost,
            DisputeOutcome.Accepted => DisputeStatus.Accepted,
            _ => DisputeStatus.Won
        };

        ResolvedAt = resolvedAt;
    }
}
