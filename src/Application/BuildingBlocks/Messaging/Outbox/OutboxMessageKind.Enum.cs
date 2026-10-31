namespace Application.BuildingBlocks.Messaging.Outbox;

/// <summary>Identifies whether an outbox message carries a domain or integration event.</summary>
public enum OutboxMessageKind
{
    Domain = 1,
    Integration = 2,
}
