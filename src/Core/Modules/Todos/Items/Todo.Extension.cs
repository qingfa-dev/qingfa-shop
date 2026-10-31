namespace Core.Modules.Todos.Items;

/// <summary>Monadic field and transition guards shared by every todo state.</summary>
/// <remarks>
/// The extensions only read <see cref="ITodo"/> state, so the aggregate and the
/// read model compose the same rules. Mutators on the aggregate remain the only
/// place that raises events.
/// </remarks>
public static class TodoExtensions
{
    /// <summary>Fails when the todo is soft-deleted, unless cleanup explicitly allows it.</summary>
    public static Result<TTodo> EnsureMutable<TTodo>(
        this Result<TTodo> result,
        bool allowDeleted = false)
        where TTodo : class, ITodo
        => result
            .Ensure(todo => !todo.IsHardDeleted, TodoOutcome.Failure.HardDeletedNotFound)
            .Ensure(todo => allowDeleted || !todo.IsDeleted, TodoOutcome.Failure.DeletedNotMutable);

    /// <summary>Validates the title carried by the todo or a new request.</summary>
    public static Result<TTodo> EnsureTitle<TTodo>(
        this Result<TTodo> result,
        string? title)
        where TTodo : class, ITodo
        => result.Ensure(_ => TodoValidator.ValidateTitle(title));

    /// <summary>Validates the description carried by the todo or a new request.</summary>
    public static Result<TTodo> EnsureDescription<TTodo>(
        this Result<TTodo> result,
        string? description)
        where TTodo : class, ITodo
        => result.Ensure(_ => TodoValidator.ValidateDescription(description));

    /// <summary>Validates the priority carried by the todo or a new request.</summary>
    public static Result<TTodo> EnsurePriority<TTodo>(
        this Result<TTodo> result,
        TodoPriority priority)
        where TTodo : class, ITodo
        => result.Ensure(_ => TodoValidator.ValidatePriority(priority));

    /// <summary>Validates the due date against the todo's creation instant.</summary>
    public static Result<TTodo> EnsureDueDate<TTodo>(
        this Result<TTodo> result,
        DateTimeOffset? dueAtUtc)
        where TTodo : class, ITodo
        => result.Ensure(todo => TodoValidator.ValidateDueDate(dueAtUtc, todo.CreatedAtUtc));

    /// <summary>Validates the list reference.</summary>
    public static Result<TTodo> EnsureListId<TTodo>(
        this Result<TTodo> result,
        Guid? listId)
        where TTodo : class, ITodo
        => result.Ensure(_ => TodoValidator.ValidateListId(listId));

    /// <summary>Validates that the todo may move to <paramref name="next"/>.</summary>
    public static Result<TTodo> EnsureTransition<TTodo>(
        this Result<TTodo> result,
        TodoStatus next)
        where TTodo : class, ITodo
        => result.Ensure(todo => TodoValidator.ValidateTransition(todo.Status, next));

    /// <summary>Validates the creation instant.</summary>
    public static Result<TTodo> EnsureCreatedAt<TTodo>(this Result<TTodo> result)
        where TTodo : class, ITodo
        => result.Ensure(todo => TodoValidator.ValidateCreatedAt(todo.CreatedAtUtc));

    /// <summary>Validates a category reference set.</summary>
    public static Result<TTodo> EnsureCategories<TTodo>(
        this Result<TTodo> result,
        ICollection<Guid> categoryIds)
        where TTodo : class, ITodo
        => result.Ensure(_ => TodoValidator.ValidateCategoryIds(categoryIds));
}
