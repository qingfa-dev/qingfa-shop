namespace Application.BuildingBlocks.Messaging.Outbox;

/// <summary>Tracks the dispatch lifecycle of an outbox message.</summary>
public enum OutboxMessageStatus
{
    Pending = 1,
    Processed = 2,
    DeadLettered = 3,
}
