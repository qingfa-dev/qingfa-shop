using Core.Common.Domain.Entities;
using Core.Todos.Reminders;

namespace Application.Features.Todos.Reminders.Models;

/// <summary>Fields shared by reminder requests and responses.</summary>
public abstract record TodoReminderParameterModel
{
    /// <summary>Gets or sets the UTC instant the reminder is due.</summary>
    public DateTimeOffset RemindAtUtc { get; set; }
}

/// <summary>Write model accepted by reminder commands.</summary>
public record TodoReminderRequestModel : TodoReminderParameterModel;

/// <summary>Full reminder response model.</summary>
public record TodoReminderResponseModel : TodoReminderParameterModel
{
    /// <summary>Gets or sets the reminder identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the todo the reminder belongs to.</summary>
    public Guid TodoId { get; set; }

    /// <summary>Gets or sets the tenant that owns the reminder.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the reminder lifecycle state.</summary>
    public ReminderStatus Status { get; set; }

    /// <summary>Gets or sets how many times the reminder was snoozed.</summary>
    public int SnoozeCount { get; set; }

    /// <summary>Gets or sets the UTC instant the reminder was dismissed.</summary>
    public DateTimeOffset? DismissedAtUtc { get; set; }

    /// <summary>Gets or sets the stream version the read model was projected from.</summary>
    public long Version { get; set; }
}

/// <summary>Compact reminder response used by search results.</summary>
public record TodoReminderSummaryResponseModel : TodoReminderParameterModel
{
    /// <summary>Gets or sets the reminder identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the todo the reminder belongs to.</summary>
    public Guid TodoId { get; set; }

    /// <summary>Gets or sets the reminder lifecycle state.</summary>
    public ReminderStatus Status { get; set; }

    /// <summary>Gets or sets how many times the reminder was snoozed.</summary>
    public int SnoozeCount { get; set; }

    /// <summary>Gets or sets the UTC instant the reminder was dismissed.</summary>
    public DateTimeOffset? DismissedAtUtc { get; set; }

    /// <summary>Gets or sets the stream version the read model was projected from.</summary>
    public long Version { get; set; }
}

/// <summary>Projection/read model for a todo reminder.</summary>
public record TodoReminderModel : IEntity<Guid>
{
    /// <summary>Gets or sets the reminder identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the todo the reminder belongs to.</summary>
    public Guid TodoId { get; set; }

    /// <summary>Gets or sets the tenant that owns the reminder.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the UTC instant the reminder is due.</summary>
    public DateTimeOffset RemindAtUtc { get; set; }

    /// <summary>Gets or sets the reminder lifecycle state.</summary>
    public ReminderStatus Status { get; set; }

    /// <summary>Gets or sets how many times the reminder was snoozed.</summary>
    public int SnoozeCount { get; set; }

    /// <summary>Gets or sets the UTC instant the reminder was dismissed.</summary>
    public DateTimeOffset? DismissedAtUtc { get; set; }

    /// <summary>Gets or sets the stream version the read model was projected from.</summary>
    public long Version { get; set; }
}
