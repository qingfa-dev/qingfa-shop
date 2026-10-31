using Core.BuildingBlocks.Modeling.Concerns.Foundation.Tenantable;
using Core.BuildingBlocks.Modeling.Concerns.Lifecycle.Creatable;
using Core.BuildingBlocks.Domain.Aggregates;
using Core.BuildingBlocks.Domain.Events;

namespace Core.Modules.Todos.Lists;

/// <summary>A named collection that todos can be assigned to.</summary>
public sealed class TodoList : AggregateRoot<Guid>, ITodoList
{
    /// <summary>
    /// Initializes an empty list so a persistence layer can materialize one.
    /// </summary>
    private TodoList()
    {
        Name = string.Empty;
    }

    /// <summary>
    /// Initializes a list with its domain state.
    /// </summary>
    /// <param name="id">The identifier of the list.</param>
    /// <param name="name">The trimmed name of the list.</param>
    /// <param name="description">The trimmed description of the list.</param>
    private TodoList(Guid id, string name, string? description)
        : base(id)
    {
        Name = name;
        Description = description;
    }

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string? Description { get; set; }

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
    /// Creates a todo list and records its creation audit metadata.
    /// </summary>
    /// <param name="id">The identifier of the list.</param>
    /// <param name="tenantId">The tenant the list belongs to.</param>
    /// <param name="name">The name of the list; surrounding whitespace is removed.</param>
    /// <param name="nowUtc">The UTC moment the list was created.</param>
    /// <param name="actor">The optional actor who created the list.</param>
    /// <param name="description">The optional description of the list; surrounding whitespace is removed.</param>
    /// <param name="metadata">The optional metadata to merge with the list.</param>
    /// <returns>The created list, or the first broken rule.</returns>
    public static Result<TodoList> Create(
        Guid id,
        Guid tenantId,
        string? name,
        DateTimeOffset nowUtc,
        string? actor = null,
        string? description = null,
        IMetadata? metadata = null)
    {
        // Normalize: Trim the free text so edge whitespace cannot create phantom differences.
        var candidate = new TodoList(
            id,
            name?.Trim() ?? string.Empty,
            description?.Trim());

        if (metadata is not null)
            candidate.MergeMetadata(metadata);

        return Result<TodoList>.Success(candidate)
            .InitializeAudit(nowUtc, actor)
            .SetTenantId(tenantId)
            .Bind(list => TodoListValidator.Validate(list))
            .Tap(list => list.RaiseEvent(
                new TodoListCreated(list.Id, list.TenantId, list.Name, list.Metadata)));
    }

    /// <summary>
    /// Records a domain event on this list.
    /// </summary>
    /// <param name="domainEvent">The event to record.</param>
    internal void RaiseEvent(IDomainEvent domainEvent) => AddEvent(domainEvent);
}