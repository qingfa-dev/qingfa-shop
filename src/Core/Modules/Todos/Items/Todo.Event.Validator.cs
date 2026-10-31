using Core.BuildingBlocks.Domain.Events;

namespace Core.Modules.Todos.Items;

/// <summary>Validates event payloads before they enter a todo stream.</summary>
public static class TodoEventValidator
{
    /// <summary>Validates one todo event against its payload invariants.</summary>
    /// <param name="domainEvent">The event to validate.</param>
    /// <returns>Success when the payload is usable; otherwise a stable failure.</returns>
    public static Result Validate(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return domainEvent switch
        {
            TodoCreated created => ValidateCreated(created),
            TodoUpdated updated => ValidateUpdated(updated),
            TodoStatusChanged changed => ValidateStatus(changed),
            TodoCategoryAdded added => ValidateCategory(added.CategoryId),
            TodoCategoryRemoved removed => ValidateCategory(removed.CategoryId),
            TodoListChanged changed => TodoValidator.ValidateListId(changed.ListId),
            TodoDeleted deleted => ValidateInstant(deleted.OccurredAtUtc),
            TodoRestored restored => ValidateInstant(restored.OccurredAtUtc),
            _ => Result.Fail(TodoOutcome.Failure.EventUnsupported(domainEvent.GetType().Name)),
        };
    }

    private static Result ValidateCreated(TodoCreated created)
        => TodoValidator.ValidateCreatedAt(created.CreatedAtUtc)
            .Ensure(() => created.TodoId != Guid.Empty, TodoOutcome.Failure.IdRequired)
            .Ensure(() => created.TenantId != Guid.Empty, TodoOutcome.Failure.TenantIdRequired)
            .Ensure(() => TodoValidator.ValidateTitle(created.Title).IsSuccess, TodoOutcome.Failure.TitleRequired)
            .Ensure(() => TodoValidator.ValidatePriority(created.Priority).IsSuccess, TodoOutcome.Failure.PriorityInvalid)
            .Ensure(() => TodoValidator.ValidateListId(created.ListId).IsSuccess, TodoOutcome.Failure.ListIdRequired);

    private static Result ValidateUpdated(TodoUpdated updated)
        => TodoValidator.ValidateTitle(updated.Title)
            .Ensure(() => TodoValidator.ValidateDescription(updated.Description).IsSuccess, TodoOutcome.Failure.DescriptionTooLong)
            .Ensure(() => TodoValidator.ValidatePriority(updated.Priority).IsSuccess, TodoOutcome.Failure.PriorityInvalid)
            .Ensure(() => TodoValidator.ValidateListId(updated.ListId).IsSuccess, TodoOutcome.Failure.ListIdRequired)
            .Ensure(() => ValidateInstant(updated.ModifiedAtUtc).IsSuccess, TodoOutcome.Failure.EventInvalid("modified instant is missing"));

    private static Result ValidateStatus(TodoStatusChanged changed)
        => TodoValidator.ValidateStatus(changed.From)
            .Ensure(() => TodoValidator.ValidateStatus(changed.To).IsSuccess, TodoOutcome.Failure.StatusInvalid)
            .Ensure(() => changed.From != changed.To, TodoOutcome.Failure.EventInvalid("status did not change"))
            .Ensure(() => changed.TenantId != Guid.Empty, TodoOutcome.Failure.TenantIdRequired)
            .Ensure(() => ValidateInstant(changed.OccurredAtUtc).IsSuccess, TodoOutcome.Failure.EventInvalid("occurred instant is missing"));

    private static Result ValidateCategory(Guid categoryId)
        => categoryId != Guid.Empty
            ? Result.Ok()
            : Result.Fail(TodoOutcome.Failure.CategoryRequired);

    private static Result ValidateInstant(DateTimeOffset instant)
        => instant == default
            ? Result.Fail(TodoOutcome.Failure.EventInvalid("instant is missing"))
            : Result.Ok();
}
