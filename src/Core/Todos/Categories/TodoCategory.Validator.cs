using Core.Common.Concerns.Foundation.Identifiable;
using Core.Common.Concerns.Foundation.Tenantable;

namespace Core.Todos;

/// <summary>Validates the invariants for <see cref="ITodoCategory"/> without mutating it.</summary>
public static class TodoCategoryValidator
{
    #region Public Methods

    /// <summary>Validates a todo category against every rule it must always satisfy.</summary>
    /// <param name="todoCategory">The category to validate.</param>
    /// <returns>Success when every rule holds; otherwise a failure with the first broken rule.</returns>
    public static Result<TodoCategory> Validate(TodoCategory todoCategory)
    {
        // Guard: Reject a missing category immediately.
        if (todoCategory is null)
        {
            return Result<TodoCategory>.Fail(
                TodoCategoryOutcome.Failure.EntityRequired);
        }

        return Result<TodoCategory>.Success(todoCategory)
            .Bind(entity => IdentifiableValidator.ValidateIdentification<TodoCategory, Guid>(entity))
            .Bind(entity => TenantableValidator.ValidateTenancy<TodoCategory, Guid>(entity))
            .Ensure(entity => ValidateName(entity.Name))
            .Ensure(entity => ValidateDescription(entity.Description));
    }

    /// <summary>Validates that a todo category is not soft-deleted, so it may be mutated.</summary>
    /// <param name="isDeleted">The deletion state of the category.</param>
    /// <returns>Success when the category is mutable; otherwise a failure.</returns>
    public static Result ValidateNotDeleted(bool isDeleted)
    {
        // Guard: A soft-deleted aggregate must not be mutated.
        if (isDeleted)
        {
            return Result.Fail(
                TodoCategoryOutcome.Failure.DeletedNotMutable);
        }

        return Result.Success();
    }

    /// <summary>Validates a name against the todo category's name rules.</summary>
    /// <param name="name">The name to validate.</param>
    /// <returns>Success when the name is usable; otherwise a failure.</returns>
    public static Result ValidateName(string? name)
    {
        // Guard: The name must be specified.
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail(
                TodoCategoryOutcome.Failure.NameRequired);
        }

        // Guard: The name must not exceed the maximum length.
        if (name.Length > TodoCategoryConstant.Constraints.Name.MaxLength)
        {
            return Result.Fail(
                TodoCategoryOutcome.Failure.NameTooLong);
        }

        return Result.Success();
    }

    /// <summary>Validates a description against the todo category's description rules.</summary>
    /// <param name="description">The description to validate.</param>
    /// <returns>Success when the description is usable; otherwise a failure.</returns>
    public static Result ValidateDescription(string? description)
    {
        // Guard: The description must not exceed the maximum length.
        if (description is not null &&
            description.Length > TodoCategoryConstant.Constraints.Description.MaxLength)
        {
            return Result.Fail(
                TodoCategoryOutcome.Failure.DescriptionTooLong);
        }

        return Result.Success();
    }

    #endregion
}