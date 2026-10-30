using Core.Todos.Items;

using Application.Features.Todos.Items.Models;

namespace Application.Features.Todos.Items.Validators;

/// <summary>Reuses the todo invariants for projection rows.</summary>
public static class TodoReadModelValidation
{
    /// <summary>Validates a read model with the same monadic rules as the aggregate.</summary>
    /// <param name="model">The projection row to validate.</param>
    /// <returns>Success when the row satisfies every todo invariant.</returns>
    public static Result<TodoReadModel> Validate(TodoReadModel model)
        => TodoValidator.Validate(model);
}
