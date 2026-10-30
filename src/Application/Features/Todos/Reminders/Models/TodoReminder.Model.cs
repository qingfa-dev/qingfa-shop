using Core.Common.Domain.Entities;
using Core.Todos.Reminders;

namespace Application.Features.Todos.Reminders.Models;

/// <summary>Projection/read model for a todo reminder.</summary>
public record TodoReminderModel : IEntity<Guid>
{
    public Guid Id { get; set; }
    public Guid TodoId { get; set; }
    public Guid TenantId { get; set; }
    public DateTimeOffset RemindAtUtc { get; set; }
    public ReminderStatus Status { get; set; }
    public int SnoozeCount { get; set; }
    public DateTimeOffset? DismissedAtUtc { get; set; }
    public long Version { get; set; }
}
