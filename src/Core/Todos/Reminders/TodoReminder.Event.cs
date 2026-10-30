using System.Text.Json.Serialization;

using Core.Common.Domain.Events;
using Core.Common.Monad.Metadata;

namespace Core.Todos.Reminders;

/// <summary>Raised when a reminder is scheduled.</summary>
public sealed record ReminderScheduled(
    Guid ReminderId,
    Guid TodoId,
    Guid TenantId,
    DateTimeOffset RemindAtUtc,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a reminder is snoozed.</summary>
public sealed record ReminderSnoozed(
    Guid ReminderId,
    Guid TodoId,
    Guid TenantId,
    DateTimeOffset RemindAtUtc,
    int SnoozeCount,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a reminder is dismissed.</summary>
public sealed record ReminderDismissed(
    Guid ReminderId,
    Guid TodoId,
    Guid TenantId,
    DateTimeOffset RemindAtUtc,
    DateTimeOffset DismissedAtUtc,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);
