using Application.Common.EventSourcing;
using Application.Common.Messaging.Serialization;
using Application.Features.Todos.Reminders.Models;

using Core.Common.Domain.Events;
using Core.Todos.Reminders;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Projections;

/// <summary>Projects reminder events into the todo read model.</summary>
public sealed class TodoReminderProjection(
    DbContext context,
    IDomainEventSerializer serializer) : IProjection
{
    /// <inheritdoc />
    public string Name => "todo-reminders";

    /// <inheritdoc />
    public async Task<Result> ApplyAsync(
        StoredEvent storedEvent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IDomainEvent domainEvent = serializer.Deserialize(
                storedEvent.EventName,
                storedEvent.EventVersion,
                storedEvent.Payload);

            switch (domainEvent)
            {
                case ReminderScheduled scheduled:
                    TodoReminderModel? scheduledModel = await context.Set<TodoReminderModel>()
                        .FindAsync([scheduled.ReminderId], cancellationToken);
                    if (scheduledModel is null)
                    {
                        context.Set<TodoReminderModel>().Add(new TodoReminderModel
                        {
                            Id = scheduled.ReminderId,
                            TodoId = scheduled.TodoId,
                            TenantId = scheduled.TenantId,
                            RemindAtUtc = scheduled.RemindAtUtc,
                            Status = ReminderStatus.Scheduled,
                            SnoozeCount = 0,
                            Version = storedEvent.Version,
                        });
                    }
                    else if (scheduledModel.Version < storedEvent.Version)
                    {
                        scheduledModel.TodoId = scheduled.TodoId;
                        scheduledModel.TenantId = scheduled.TenantId;
                        scheduledModel.RemindAtUtc = scheduled.RemindAtUtc;
                        scheduledModel.Status = ReminderStatus.Scheduled;
                        scheduledModel.SnoozeCount = 0;
                        scheduledModel.DismissedAtUtc = null;
                        scheduledModel.Version = storedEvent.Version;
                    }
                    break;

                case ReminderSnoozed snoozed:
                    TodoReminderModel? snoozedModel = await context.Set<TodoReminderModel>()
                        .FindAsync([snoozed.ReminderId], cancellationToken);
                    if (snoozedModel is not null && snoozedModel.Version < storedEvent.Version)
                    {
                        snoozedModel.RemindAtUtc = snoozed.RemindAtUtc;
                        snoozedModel.SnoozeCount = snoozed.SnoozeCount;
                        snoozedModel.Status = ReminderStatus.Snoozed;
                        snoozedModel.Version = storedEvent.Version;
                    }
                    break;

                case ReminderDismissed dismissed:
                    TodoReminderModel? dismissedModel = await context.Set<TodoReminderModel>()
                        .FindAsync([dismissed.ReminderId], cancellationToken);
                    if (dismissedModel is not null && dismissedModel.Version < storedEvent.Version)
                    {
                        dismissedModel.Status = ReminderStatus.Dismissed;
                        dismissedModel.DismissedAtUtc = dismissed.DismissedAtUtc;
                        dismissedModel.Version = storedEvent.Version;
                    }
                    break;
            }

            await context.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result.Fail(
                EventSourcingOutcome.Failure.ProjectionApplyFailed(Name, exception.Message));
        }
    }
}
