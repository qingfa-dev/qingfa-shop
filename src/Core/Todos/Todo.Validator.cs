using Core.Common.Concerns.Foundation.Identifiable;
using Core.Common.Concerns.Foundation.Tenantable;

namespace Core.Todos;

/// <summary>Validates the invariants for <see cref="ITodo"/> without mutating it.</summary>
public static class TodoValidator
{
    #region Public Methods

    /// <summary>Validates a todo against every rule it must always satisfy.</summary>
    /// <param name="todo">The todo to validate.</param>
    /// <returns>Success when every rule holds; otherwise a failure with the first broken rule.</returns>
    public static Result<Todo> Validate(Todo todo)
    {
        // Guard: Reject a missing todo immediately.
        if (todo is null)
        {
            return Result<Todo>.Fail(
                TodoOutcome.Failure.EntityRequired);
        }

        return Result<Todo>.Success(todo)
            .Bind(entity => IdentifiableValidator.ValidateIdentification<Todo, Guid>(entity))
            .Bind(entity => TenantableValidator.ValidateTenancy<Todo, Guid>(entity))
            .Ensure(entity => ValidateTitle(entity.Title))
            .Ensure(entity => ValidateDescription(entity.Description))
            .Ensure(entity => ValidateStatus(entity.Status))
            .Ensure(entity => ValidatePriority(entity.Priority))
            .Ensure(entity => ValidateDueDate(entity.DueAtUtc, entity.CreatedAtUtc))
            .Ensure(entity => ValidateCategoryIds(entity.CategoryIds))
            .Ensure(entity => ValidateListId(entity.ListId));
    }

    /// <summary>Validates that a todo may move from one status to another.</summary>
    /// <param name="current">The status the todo is in now.</param>
    /// <param name="next">The status the todo would move to.</param>
    /// <returns>Success when the move is legal; otherwise a failure.</returns>
    public static Result ValidateTransition(TodoStatus current, TodoStatus next)
    {
        // Compute: The transition table is the single source of truth for legal moves.
        if (!current.CanTransitionTo(next))
        {
            return Result.Fail(
                TodoOutcome.Failure.StatusTransitionInvalid);
        }

        return Result.Success();
    }

    /// <summary>Validates that a todo is not soft-deleted, so it may be mutated.</summary>
    /// <param name="isDeleted">The deletion state of the todo.</param>
    /// <returns>Success when the todo is mutable; otherwise a failure.</returns>
    public static Result ValidateNotDeleted(bool isDeleted)
    {
        // Guard: A soft-deleted aggregate must not be mutated.
        if (isDeleted)
        {
            return Result.Fail(
                TodoOutcome.Failure.DeletedNotMutable);
        }

        return Result.Success();
    }

    /// <summary>Validates a title against the todo's title rules.</summary>
    /// <param name="title">The title to validate.</param>
    /// <returns>Success when the title is usable; otherwise a failure.</returns>
    public static Result ValidateTitle(string? title)
    {
        // Guard: The title must be specified.
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Fail(
                TodoOutcome.Failure.TitleRequired);
        }

        // Guard: The title must not exceed the maximum length.
        if (title.Length > TodoConstant.Constraints.Title.MaxLength)
        {
            return Result.Fail(
                TodoOutcome.Failure.TitleTooLong);
        }

        return Result.Success();
    }

    /// <summary>Validates a description against the todo's description rules.</summary>
    /// <param name="description">The description to validate.</param>
    /// <returns>Success when the description is usable; otherwise a failure.</returns>
    public static Result ValidateDescription(string? description)
    {
        // Guard: The description must not exceed the maximum length.
        if (description is not null &&
            description.Length > TodoConstant.Constraints.Description.MaxLength)
        {
            return Result.Fail(
                TodoOutcome.Failure.DescriptionTooLong);
        }

        return Result.Success();
    }

    /// <summary>Validates a status against the todo's status rules.</summary>
    /// <param name="status">The status to validate.</param>
    /// <returns>Success when the status is usable; otherwise a failure.</returns>
    public static Result ValidateStatus(TodoStatus status)
    {
        // Guard: The status must be a defined value.
        if (!Enum.IsDefined(status))
        {
            return Result.Fail(
                TodoOutcome.Failure.StatusInvalid);
        }

        return Result.Success();
    }

    /// <summary>Validates a priority against the todo's priority rules.</summary>
    /// <param name="priority">The priority to validate.</param>
    /// <returns>Success when the priority is usable; otherwise a failure.</returns>
    public static Result ValidatePriority(TodoPriority priority)
    {
        // Guard: The priority must be a defined value.
        if (!Enum.IsDefined(priority))
        {
            return Result.Fail(
                TodoOutcome.Failure.PriorityInvalid);
        }

        return Result.Success();
    }

    /// <summary>Validates a due date against the todo's creation date.</summary>
    /// <param name="dueAtUtc">The due date to validate.</param>
    /// <param name="createdAtUtc">The date the todo was created.</param>
    /// <returns>Success when the due date is usable; otherwise a failure.</returns>
    public static Result ValidateDueDate(DateTimeOffset? dueAtUtc, DateTimeOffset createdAtUtc)
    {
        // Guard: The due date must not precede the creation date.
        if (dueAtUtc is not null &&
            createdAtUtc != default &&
            dueAtUtc < createdAtUtc)
        {
            return Result.Fail(
                TodoOutcome.Failure.DueAtBeforeCreated);
        }

        return Result.Success();
    }

    /// <summary>Validates a collection of category references against the todo's category rules.</summary>
    /// <param name="categoryIds">The category references to validate.</param>
    /// <returns>Success when the collection is usable; otherwise a failure.</returns>
    public static Result ValidateCategoryIds(ICollection<Guid> categoryIds)
    {
        // Guard: Every category reference must be specified.
        if (categoryIds.Any(id => id == Guid.Empty))
        {
            return Result.Fail(
                TodoOutcome.Failure.CategoryRequired);
        }

        // Guard: The same category must not be referenced twice.
        if (categoryIds.Distinct().Count() != categoryIds.Count)
        {
            return Result.Fail(
                TodoOutcome.Failure.CategoryDuplicate);
        }

        // Guard: The number of categories must not exceed the limit.
        if (categoryIds.Count > TodoConstant.Constraints.Collection.MaxCategories)
        {
            return Result.Fail(
                TodoOutcome.Failure.CategoriesTooMany);
        }

        return Result.Success();
    }

    /// <summary>Validates a list reference against the todo's list rules.</summary>
    /// <param name="listId">The list reference to validate.</param>
    /// <returns>Success when the reference is usable; otherwise a failure.</returns>
    public static Result ValidateListId(Guid? listId)
    {
        // Guard: A list reference must not be the empty identifier.
        if (listId is not null && listId == Guid.Empty)
        {
            return Result.Fail(
                TodoOutcome.Failure.ListIdRequired);
        }

        return Result.Success();
    }

    #endregion
}