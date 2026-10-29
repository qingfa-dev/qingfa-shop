namespace Application.Common.Messaging.Outbox;

/// <summary>A durable message staged in the same transaction as the state change.</summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public OutboxMessageKind Kind { get; init; } = OutboxMessageKind.Domain;
    public string Type { get; init; } = string.Empty;
    public int Version { get; init; } = 1;
    public string Payload { get; init; } = "{}";
    public string Metadata { get; init; } = "{}";
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset AvailableOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LockedUntilUtc { get; set; }
    public Guid? LeaseId { get; set; }
    public DateTimeOffset? ProcessedOnUtc { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public Guid? CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public Guid? TenantId { get; init; }
}