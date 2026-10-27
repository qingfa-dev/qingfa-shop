using Core.Common.Concerns.Foundation.Tenantable;
using Core.Common.Concerns.Lifecycle.Creatable;
using Core.Common.Domain.Aggregates;
using Core.Common.Domain.Events;

namespace Core.Todos.Items;

/// <summary>A single unit of work with a lifecycle, a priority, and optional list and category references.</summary>
public sealed class Todo : AggregateRoot<Guid>, ITodo
{
    // Persistence: EF assigns this backing field when materializing the JSON collection.
#pragma warning disable IDE0044
    private List<Guid> _categoryIds = [];
#pragma warning restore IDE0044

    /// <summary>
    /// Initializes an empty todo so a persistence layer can materialize one.
    /// </summary>
    private Todo()
    {
        Title = string.Empty;
        Status = TodoConstant.Defaults.Status;
        Priority = TodoConstant.Defaults.Priority;
    }

    /// <summary>
    /// Initializes a todo with its domain state.
    /// </summary>
    /// <param name="id">The identifier of the todo.</param>
    /// <param name="title">The trimmed title of the todo.</param>
    /// <param name="description">The trimmed description of the todo.</param>
    /// <param name="priority">The priority of the todo.</param>
    /// <param name="dueAtUtc">The optional UTC due date of the todo.</param>
    /// <param name="listId">The optional identifier of the list the todo belongs to.</param>
    private Todo(
        Guid id,
        string title,
        string? description,
        TodoPriority priority,
        DateTimeOffset? dueAtUtc,
        Guid? listId)
        : base(id)
    {
        Title = title;
        Description = description;
        // Create: A todo starts pending so progress is opt-in and never implicit.
        Status = TodoConstant.Defaults.Status;
        Priority = priority;
        DueAtUtc = dueAtUtc?.ToUniversalTime();
        ListId = listId;
    }

    /// <inheritdoc />
    public string Title { get; set; }

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public TodoStatus Status { get; set; }

    /// <inheritdoc />
    public TodoPriority Priority { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DueAtUtc { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? CompletedAtUtc { get; set; }

    /// <inheritdoc />
    public Guid? ListId { get; set; }

    /// <inheritdoc />
    public ICollection<Guid> CategoryIds => _categoryIds;

    /// <inheritdoc />
    public bool IsCompleted => Status == TodoStatus.Completed;

    /// <inheritdoc />
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <inheritdoc />
    public string? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedAtUtc { get; set; }

    /// <inheritdoc />
    public string? ModifiedBy { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAtUtc { get; set; }

    /// <inheritdoc />
    public string? DeletedBy { get; set; }

    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>
    /// Creates a todo in the pending state and records its creation audit metadata.
    /// </summary>
    /// <param name="id">The identifier of the todo.</param>
    /// <param name="tenantId">The tenant the todo belongs to.</param>
    /// <param name="title">The title of the todo; surrounding whitespace is removed.</param>
    /// <param name="nowUtc">The UTC moment the todo was created.</param>
    /// <param name="actor">The optional actor who created the todo.</param>
    /// <param name="description">The optional description of the todo; surrounding whitespace is removed.</param>
    /// <param name="priority">The priority of the todo.</param>
    /// <param name="dueAtUtc">The optional UTC due date of the todo.</param>
    /// <param name="listId">The optional identifier of the list the todo belongs to.</param>
    /// <param name="metadata">The optional metadata to merge with the todo.</param>
    /// <returns>The created todo, or the first broken rule.</returns>
    public static Result<Todo> Create(
        Guid id,
        Guid tenantId,
        string? title,
        DateTimeOffset nowUtc,
        string? actor = null,
        string? description = null,
        TodoPriority priority = TodoConstant.Defaults.Priority,
        DateTimeOffset? dueAtUtc = null,
        Guid? listId = null,
        IMetadata? metadata = null)
    {
        // Normalize: Trim the free text so edge whitespace cannot create phantom differences.
        var candidate = new Todo(
            id,
            title?.Trim() ?? string.Empty,
            description?.Trim(),
            priority,
            dueAtUtc,
            listId);

        if (metadata is not null)
            candidate.MergeMetadata(metadata);

        return Result<Todo>.Success(candidate)
            .InitializeAudit(nowUtc, actor)
            .SetTenantId(tenantId)
            .Bind(todo => TodoValidator.Validate(todo))
            .Tap(todo => todo.RaiseEvent(
                new TodoCreated(todo.Id, todo.TenantId, todo.Title, todo.Metadata)));
    }

    /// <summary>
    /// Records a domain event on this todo.
    /// </summary>
    /// <param name="domainEvent">The event to record.</param>
    internal void RaiseEvent(IDomainEvent domainEvent) => AddEvent(domainEvent);
}