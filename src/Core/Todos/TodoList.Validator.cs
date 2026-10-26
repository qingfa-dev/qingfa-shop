using Core.Common.Concerns.Foundation.Identifiable;
using Core.Common.Concerns.Foundation.Tenantable;

namespace Core.Todos;

/// <summary>Validates the invariants for <see cref="ITodoList"/> without mutating it.</summary>
public static class TodoListValidator
{
    #region Public Methods

    /// <summary>Validates a todo list against every rule it must always satisfy.</summary>
    /// <param name="todoList">The list to validate.</param>
    /// <returns>Success when every rule holds; otherwise a failure with the first broken rule.</returns>
    public static Result<TodoList> Validate(TodoList todoList)
    {
        // Guard: Reject a missing list immediately.
        if (todoList is null)
        {
            return Result<TodoList>.Fail(
                TodoListOutcome.Failure.EntityRequired);
        }

        return Result<TodoList>.Success(todoList)
            .Bind(entity => IdentifiableValidator.ValidateIdentification<TodoList, Guid>(entity))
            .Bind(entity => TenantableValidator.ValidateTenancy<TodoList, Guid>(entity))
            .Ensure(entity => ValidateName(entity.Name))
            .Ensure(entity => ValidateDescription(entity.Description));
    }

    /// <summary>Validates that a todo list is not soft-deleted, so it may be mutated.</summary>
    /// <param name="isDeleted">The deletion state of the list.</param>
    /// <returns>Success when the list is mutable; otherwise a failure.</returns>
    public static Result ValidateNotDeleted(bool isDeleted)
    {
        // Guard: A soft-deleted aggregate must not be mutated.
        if (isDeleted)
        {
            return Result.Fail(
                TodoListOutcome.Failure.DeletedNotMutable);
        }

        return Result.Success();
    }

    /// <summary>Validates a name against the todo list's name rules.</summary>
    /// <param name="name">The name to validate.</param>
    /// <returns>Success when the name is usable; otherwise a failure.</returns>
    public static Result ValidateName(string? name)
    {
        // Guard: The name must be specified.
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail(
                TodoListOutcome.Failure.NameRequired);
        }

        // Guard: The name must not exceed the maximum length.
        if (name.Length > TodoListConstant.Constraints.Name.MaxLength)
        {
            return Result.Fail(
                TodoListOutcome.Failure.NameTooLong);
        }

        return Result.Success();
    }

    /// <summary>Validates a description against the todo list's description rules.</summary>
    /// <param name="description">The description to validate.</param>
    /// <returns>Success when the description is usable; otherwise a failure.</returns>
    public static Result ValidateDescription(string? description)
    {
        // Guard: The description must not exceed the maximum length.
        if (description is not null &&
            description.Length > TodoListConstant.Constraints.Description.MaxLength)
        {
            return Result.Fail(
                TodoListOutcome.Failure.DescriptionTooLong);
        }

        return Result.Success();
    }

    #endregion
}