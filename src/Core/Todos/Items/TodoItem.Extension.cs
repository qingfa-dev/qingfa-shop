namespace Core.Todos.Items;

/// <summary>Extension methods for <see cref="ITodoItem"/>.</summary>
public static class TodoItemExtensions
{
    #region Public Methods

    /// <summary>Sets the title after validating it.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <param name="title">The title to set; surrounding whitespace is removed.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<TodoItem> SetTitle(this Result<TodoItem> result, string? title)
    {
        // Normalize: Trim the title so edge whitespace cannot create phantom differences.
        var normalized = title?.Trim();

        return result
            .Ensure(todo => TodoItemValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoItemValidator.ValidateTitle(normalized))
            .Tap(todo => todo.Title = normalized!);
    }

    /// <summary>Sets the description after validating it.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <param name="description">The description to set; surrounding whitespace is removed.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<TodoItem> SetDescription(this Result<TodoItem> result, string? description)
    {
        var normalized = description?.Trim();

        return result
            .Ensure(todo => TodoItemValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoItemValidator.ValidateDescription(normalized))
            .Tap(todo => todo.Description = normalized);
    }

    /// <summary>Sets the priority after validating it.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <param name="priority">The priority to set.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<TodoItem> SetPriority(this Result<TodoItem> result, TodoPriority priority)
    {
        return result
            .Ensure(todo => TodoItemValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoItemValidator.ValidatePriority(priority))
            .Tap(todo => todo.Priority = priority);
    }

    /// <summary>Sets the due date after validating it against the creation date.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <param name="dueAtUtc">The UTC due date to set, or <c>null</c> to clear it.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<TodoItem> SetDueDate(this Result<TodoItem> result, DateTimeOffset? dueAtUtc)
    {
        return result
            .Ensure(todo => TodoItemValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoItemValidator.ValidateDueDate(dueAtUtc, todo.CreatedAtUtc))
            .Tap(todo => todo.DueAtUtc = dueAtUtc?.ToUniversalTime());
    }

    /// <summary>Clears the due date.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<TodoItem> ClearDueDate(this Result<TodoItem> result)
    {
        return result
            .Ensure(todo => TodoItemValidator.ValidateNotDeleted(todo.IsDeleted))
            .Tap(todo => todo.DueAtUtc = null);
    }

    /// <summary>Moves a pending todo into progress.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<TodoItem> Start(this Result<TodoItem> result)
    {
        return result
            .Ensure(todo => TodoItemValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoItemValidator.ValidateTransition(todo.Status, TodoStatus.InProgress))
            .Tap(todo =>
            {
                todo.Status = TodoStatus.InProgress;

                // Raise: The event lets subscribers react without polling the aggregate.
                todo.RaiseEvent(new TodoItemStarted(todo.Id, todo.TenantId, todo.Metadata));
            });
    }

    /// <summary>Completes a started todo and stamps the completion date.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <param name="nowUtc">The UTC moment the work finished.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<TodoItem> Complete(this Result<TodoItem> result, DateTimeOffset nowUtc)
    {
        return result
            .Ensure(todo => TodoItemValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoItemValidator.ValidateTransition(todo.Status, TodoStatus.Completed))
            .Tap(todo =>
            {
                // Update: The completion stamp records when the work actually finished.
                todo.Status = TodoStatus.Completed;
                todo.CompletedAtUtc = nowUtc.ToUniversalTime();

                todo.RaiseEvent(
                    new TodoItemCompleted(todo.Id, todo.TenantId, todo.CompletedAtUtc.Value, todo.Metadata));
            });
    }

    /// <summary>Cancels a todo that has not been completed.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<TodoItem> Cancel(this Result<TodoItem> result)
    {
        return result
            .Ensure(todo => TodoItemValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoItemValidator.ValidateTransition(todo.Status, TodoStatus.Cancelled))
            .Tap(todo =>
            {
                todo.Status = TodoStatus.Cancelled;

                todo.RaiseEvent(new TodoItemCancelled(todo.Id, todo.TenantId, todo.Metadata));
            });
    }

    /// <summary>Reopens a finished todo and clears its completion date.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<TodoItem> Reopen(this Result<TodoItem> result)
    {
        return result
            .Ensure(todo => TodoItemValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoItemValidator.ValidateTransition(todo.Status, TodoStatus.Pending))
            .Tap(todo =>
            {
                todo.Status = TodoStatus.Pending;
                todo.CompletedAtUtc = null;

                todo.RaiseEvent(new TodoItemReopened(todo.Id, todo.TenantId, todo.Metadata));
            });
    }

    /// <summary>Assigns the todo to a list.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <param name="listId">The identifier of the list.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<TodoItem> AssignToList(this Result<TodoItem> result, Guid listId)
    {
        return result
            .Ensure(todo => TodoItemValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoItemValidator.ValidateListId(listId))
            .Tap(todo => todo.ListId = listId);
    }

    /// <summary>Removes the todo from its list.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<TodoItem> ClearList(this Result<TodoItem> result)
    {
        return result
            .Ensure(todo => TodoItemValidator.ValidateNotDeleted(todo.IsDeleted))
            .Tap(todo => todo.ListId = null);
    }

    /// <summary>Adds a category reference to the todo.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <param name="categoryId">The identifier of the category.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<TodoItem> AddCategory(this Result<TodoItem> result, Guid categoryId)
    {
        return result
            .Ensure(todo => TodoItemValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoItemValidator.ValidateCategoryIds([.. todo.CategoryIds, categoryId]))
            .Tap(todo => todo.CategoryIds.Add(categoryId));
    }

    /// <summary>Removes a category reference from the todo.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <param name="categoryId">The identifier of the category.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<TodoItem> RemoveCategory(this Result<TodoItem> result, Guid categoryId)
    {
        return result
            .Ensure(todo => TodoItemValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => todo.CategoryIds.Contains(categoryId), TodoItemOutcome.Failure.CategoryNotFound)
            .Tap(todo => todo.CategoryIds.Remove(categoryId));
    }

    #endregion
}