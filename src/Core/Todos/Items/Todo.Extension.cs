namespace Core.Todos.Items;

/// <summary>Extension methods for <see cref="ITodo"/>.</summary>
public static class TodoExtensions
{
    #region Public Methods

    /// <summary>Sets the title after validating it.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <param name="title">The title to set; surrounding whitespace is removed.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<Todo> SetTitle(this Result<Todo> result, string? title)
    {
        // Normalize: Trim the title so edge whitespace cannot create phantom differences.
        var normalized = title?.Trim();

        return result
            .Ensure(todo => TodoValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoValidator.ValidateTitle(normalized))
            .Tap(todo => todo.Title = normalized!);
    }

    /// <summary>Sets the description after validating it.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <param name="description">The description to set; surrounding whitespace is removed.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<Todo> SetDescription(this Result<Todo> result, string? description)
    {
        var normalized = description?.Trim();

        return result
            .Ensure(todo => TodoValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoValidator.ValidateDescription(normalized))
            .Tap(todo => todo.Description = normalized);
    }

    /// <summary>Sets the priority after validating it.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <param name="priority">The priority to set.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<Todo> SetPriority(this Result<Todo> result, TodoPriority priority)
    {
        return result
            .Ensure(todo => TodoValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoValidator.ValidatePriority(priority))
            .Tap(todo => todo.Priority = priority);
    }

    /// <summary>Sets the due date after validating it against the creation date.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <param name="dueAtUtc">The UTC due date to set, or <c>null</c> to clear it.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<Todo> SetDueDate(this Result<Todo> result, DateTimeOffset? dueAtUtc)
    {
        return result
            .Ensure(todo => TodoValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoValidator.ValidateDueDate(dueAtUtc, todo.CreatedAtUtc))
            .Tap(todo => todo.DueAtUtc = dueAtUtc?.ToUniversalTime());
    }

    /// <summary>Clears the due date.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<Todo> ClearDueDate(this Result<Todo> result)
    {
        return result
            .Ensure(todo => TodoValidator.ValidateNotDeleted(todo.IsDeleted))
            .Tap(todo => todo.DueAtUtc = null);
    }

    /// <summary>Moves a pending todo into progress.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<Todo> Start(this Result<Todo> result)
    {
        return result
            .Ensure(todo => TodoValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoValidator.ValidateTransition(todo.Status, TodoStatus.InProgress))
            .Tap(todo =>
            {
                todo.Status = TodoStatus.InProgress;

                // Raise: The event lets subscribers react without polling the aggregate.
                todo.RaiseEvent(new TodoStarted(todo.Id, todo.TenantId, todo.Metadata));
            });
    }

    /// <summary>Completes a started todo and stamps the completion date.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <param name="nowUtc">The UTC moment the work finished.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<Todo> Complete(this Result<Todo> result, DateTimeOffset nowUtc)
    {
        return result
            .Ensure(todo => TodoValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoValidator.ValidateTransition(todo.Status, TodoStatus.Completed))
            .Tap(todo =>
            {
                // Update: The completion stamp records when the work actually finished.
                todo.Status = TodoStatus.Completed;
                todo.CompletedAtUtc = nowUtc.ToUniversalTime();

                todo.RaiseEvent(
                    new TodoCompleted(todo.Id, todo.TenantId, todo.CompletedAtUtc.Value, todo.Metadata));
            });
    }

    /// <summary>Cancels a todo that has not been completed.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<Todo> Cancel(this Result<Todo> result)
    {
        return result
            .Ensure(todo => TodoValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoValidator.ValidateTransition(todo.Status, TodoStatus.Cancelled))
            .Tap(todo =>
            {
                todo.Status = TodoStatus.Cancelled;

                todo.RaiseEvent(new TodoCancelled(todo.Id, todo.TenantId, todo.Metadata));
            });
    }

    /// <summary>Reopens a finished todo and clears its completion date.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<Todo> Reopen(this Result<Todo> result)
    {
        return result
            .Ensure(todo => TodoValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoValidator.ValidateTransition(todo.Status, TodoStatus.Pending))
            .Tap(todo =>
            {
                todo.Status = TodoStatus.Pending;
                todo.CompletedAtUtc = null;

                todo.RaiseEvent(new TodoReopened(todo.Id, todo.TenantId, todo.Metadata));
            });
    }

    /// <summary>Assigns the todo to a list.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <param name="listId">The identifier of the list.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<Todo> AssignToList(this Result<Todo> result, Guid listId)
    {
        return result
            .Ensure(todo => TodoValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoValidator.ValidateListId(listId))
            .Tap(todo => todo.ListId = listId);
    }

    /// <summary>Removes the todo from its list.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<Todo> ClearList(this Result<Todo> result)
    {
        return result
            .Ensure(todo => TodoValidator.ValidateNotDeleted(todo.IsDeleted))
            .Tap(todo => todo.ListId = null);
    }

    /// <summary>Adds a category reference to the todo.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <param name="categoryId">The identifier of the category.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<Todo> AddCategory(this Result<Todo> result, Guid categoryId)
    {
        return result
            .Ensure(todo => TodoValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => TodoValidator.ValidateCategoryIds([.. todo.CategoryIds, categoryId]))
            .Tap(todo => todo.CategoryIds.Add(categoryId));
    }

    /// <summary>Removes a category reference from the todo.</summary>
    /// <param name="result">The result carrying the todo.</param>
    /// <param name="categoryId">The identifier of the category.</param>
    /// <returns>The result with the todo mutated on success; otherwise a failure.</returns>
    public static Result<Todo> RemoveCategory(this Result<Todo> result, Guid categoryId)
    {
        return result
            .Ensure(todo => TodoValidator.ValidateNotDeleted(todo.IsDeleted))
            .Ensure(todo => todo.CategoryIds.Contains(categoryId), TodoOutcome.Failure.CategoryNotFound)
            .Tap(todo => todo.CategoryIds.Remove(categoryId));
    }

    #endregion
}