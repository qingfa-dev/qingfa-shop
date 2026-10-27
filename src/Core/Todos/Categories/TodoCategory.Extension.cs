namespace Core.Todos.Categories;

/// <summary>Extension methods for <see cref="ITodoCategory"/>.</summary>
public static class TodoCategoryExtensions
{
    #region Public Methods

    /// <summary>Sets the name after validating it.</summary>
    /// <param name="result">The result carrying the category.</param>
    /// <param name="name">The name to set; surrounding whitespace is removed.</param>
    /// <returns>The result with the category mutated on success; otherwise a failure.</returns>
    public static Result<TodoCategory> SetName(this Result<TodoCategory> result, string? name)
    {
        // Normalize: Trim the name so edge whitespace cannot create phantom differences.
        var normalized = name?.Trim();

        return result
            .Ensure(category => TodoCategoryValidator.ValidateNotDeleted(category.IsDeleted))
            .Ensure(category => TodoCategoryValidator.ValidateName(normalized))
            .Tap(category =>
            {
                // Update: The rename event fires only when the name actually changed.
                var changed = !string.Equals(category.Name, normalized, StringComparison.Ordinal);

                category.Name = normalized!;

                if (changed)
                {
                    category.RaiseEvent(
                        new TodoCategoryRenamed(category.Id, category.TenantId, category.Name, category.Metadata));
                }
            });
    }

    /// <summary>Sets the description after validating it.</summary>
    /// <param name="result">The result carrying the category.</param>
    /// <param name="description">The description to set; surrounding whitespace is removed.</param>
    /// <returns>The result with the category mutated on success; otherwise a failure.</returns>
    public static Result<TodoCategory> SetDescription(this Result<TodoCategory> result, string? description)
    {
        var normalized = description?.Trim();

        return result
            .Ensure(category => TodoCategoryValidator.ValidateNotDeleted(category.IsDeleted))
            .Ensure(category => TodoCategoryValidator.ValidateDescription(normalized))
            .Tap(category => category.Description = normalized);
    }

    #endregion
}