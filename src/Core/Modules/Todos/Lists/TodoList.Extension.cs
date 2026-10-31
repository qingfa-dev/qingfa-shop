namespace Core.Modules.Todos.Lists;

/// <summary>Extension methods for <see cref="ITodoList"/>.</summary>
public static class TodoListExtensions
{
    #region Public Methods

    /// <summary>Sets the name after validating it.</summary>
    /// <param name="result">The result carrying the list.</param>
    /// <param name="name">The name to set; surrounding whitespace is removed.</param>
    /// <returns>The result with the list mutated on success; otherwise a failure.</returns>
    public static Result<TodoList> SetName(this Result<TodoList> result, string? name)
    {
        // Normalize: Trim the name so edge whitespace cannot create phantom differences.
        var normalized = name?.Trim();

        return result
            .Ensure(list => TodoListValidator.ValidateNotDeleted(list.IsDeleted))
            .Ensure(list => TodoListValidator.ValidateName(normalized))
            .Tap(list =>
            {
                // Update: The rename event fires only when the name actually changed.
                var changed = !string.Equals(list.Name, normalized, StringComparison.Ordinal);

                list.Name = normalized!;

                if (changed)
                {
                    list.RaiseEvent(
                        new TodoListRenamed(list.Id, list.TenantId, list.Name, list.Metadata));
                }
            });
    }

    /// <summary>Sets the description after validating it.</summary>
    /// <param name="result">The result carrying the list.</param>
    /// <param name="description">The description to set; surrounding whitespace is removed.</param>
    /// <returns>The result with the list mutated on success; otherwise a failure.</returns>
    public static Result<TodoList> SetDescription(this Result<TodoList> result, string? description)
    {
        var normalized = description?.Trim();

        return result
            .Ensure(list => TodoListValidator.ValidateNotDeleted(list.IsDeleted))
            .Ensure(list => TodoListValidator.ValidateDescription(normalized))
            .Tap(list => list.Description = normalized);
    }

    #endregion
}