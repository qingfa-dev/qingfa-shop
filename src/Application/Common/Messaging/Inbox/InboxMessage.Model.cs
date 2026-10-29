namespace Application.Common.Messaging.Inbox;

/// <summary>A message already processed by one consumer.</summary>
public sealed class InboxMessage
{
    public Guid MessageId { get; init; }
    public string Consumer { get; init; } = string.Empty;
    public DateTimeOffset ProcessedOnUtc { get; init; }
}
