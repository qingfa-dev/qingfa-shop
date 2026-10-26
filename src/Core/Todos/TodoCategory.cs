using Core.Common.Concerns.Foundation.Tenantable;
using Core.Common.Concerns.Lifecycle.Creatable;
using Core.Common.Domain.Aggregates;
using Core.Common.Domain.Events;

namespace Core.Todos;

/// <summary>A label that todos can reference to group related work.</summary>
public sealed class TodoCategory : AggregateRoot<Guid>, ITodoCategory
{
    /// <summary>
    /// Initializes an empty category so a persistence layer can materialize one.
    /// </summary>
    private TodoCategory()
    {
        Name = string.Empty;
    }

    /// <summary>
    /// Initializes a category with its domain state.
    /// </summary>
    /// <param name="id">The identifier of the category.</param>
    /// <param name="name">The trimmed name of the category.</param>
    /// <param name="description">The trimmed description of the category.</param>
    private TodoCategory(Guid id, string name, string? description)
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
    /// Creates a todo category and records its creation audit metadata.
    /// </summary>
    /// <param name="id">The identifier of the category.</param>
    /// <param name="tenantId">The tenant the category belongs to.</param>
    /// <param name="name">The name of the category; surrounding whitespace is removed.</param>
    /// <param name="nowUtc">The UTC moment the category was created.</param>
    /// <param name="actor">The optional actor who created the category.</param>
    /// <param name="description">The optional description of the category; surrounding whitespace is removed.</param>
    /// <param name="metadata">The optional metadata to merge with the category.</param>
    /// <returns>The created category, or the first broken rule.</returns>
    public static Result<TodoCategory> Create(
        Guid id,
        Guid tenantId,
        string? name,
        DateTimeOffset nowUtc,
        string? actor = null,
        string? description = null,
        IMetadata? metadata = null)
    {
        // Normalize: Trim the free text so edge whitespace cannot create phantom differences.
        var candidate = new TodoCategory(
            id,
            name?.Trim() ?? string.Empty,
            description?.Trim());

        if (metadata is not null)
            candidate.MergeMetadata(metadata);

        return Result<TodoCategory>.Success(candidate)
            .InitializeAudit(nowUtc, actor)
            .SetTenantId(tenantId)
            .Bind(category => TodoCategoryValidator.Validate(category))
            .Tap(category => category.RaiseEvent(
                new TodoCategoryCreated(category.Id, category.TenantId, category.Name, category.Metadata)));
    }

    /// <summary>
    /// Records a domain event on this category.
    /// </summary>
    /// <param name="domainEvent">The event to record.</param>
    internal void RaiseEvent(IDomainEvent domainEvent) => AddEvent(domainEvent);
}