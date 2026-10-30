using System.Text.Json.Serialization;

using Application.Common.Messaging.Events;

namespace Application.Features.Todos.Events;

/// <summary>Published when a todo reminder changes state so other processes can react.</summary>
public sealed record TodoReminderChangedIntegration : IntegrationEvent
{
    /// <summary>Gets the identifier of the reminder.</summary>
    public Guid ReminderId { get; init; }

    /// <summary>Gets the identifier of the todo the reminder belongs to.</summary>
    public Guid TodoId { get; init; }

    /// <summary>Gets the tenant the reminder belongs to.</summary>
    public Guid TenantId { get; init; }

    /// <summary>Gets the transition that produced this event.</summary>
    public TodoReminderChange Change { get; init; }

    /// <summary>Gets the UTC instant the reminder is due.</summary>
    public DateTimeOffset RemindAtUtc { get; init; }

    /// <summary>Gets how many times the reminder was snoozed.</summary>
    public int SnoozeCount { get; init; }

    /// <summary>Gets the UTC instant the reminder was dismissed, when it was.</summary>
    public DateTimeOffset? DismissedAtUtc { get; init; }

    /// <inheritdoc />
    [JsonIgnore]
    public override string EventName => "todo.reminder.changed";

    /// <inheritdoc />
    [JsonIgnore]
    public override int Version => 1;
}
