using Core.Common.Domain.Events;

namespace Core.Todos.Categories;

/// <summary>Raised when a todo category is created.</summary>
public sealed record TodoCategoryCreated : DomainEvent
{
    /// <summary>Initializes the event.</summary>
    /// <param name="categoryId">The identifier of the category.</param>
    /// <param name="tenantId">The tenant the category belongs to.</param>
    /// <param name="name">The name of the category.</param>
    /// <param name="metadata">The optional metadata to merge with this event.</param>
    public TodoCategoryCreated(Guid categoryId, Guid tenantId, string name, DefaultMetadata? metadata = null)
        : base(metadata: metadata)
    {
        CategoryId = categoryId;
        TenantId = tenantId;
        Name = name;
    }

    /// <summary>Gets the identifier of the category.</summary>
    public Guid CategoryId { get; }

    /// <summary>Gets the tenant the category belongs to.</summary>
    public Guid TenantId { get; }

    /// <summary>Gets the name of the category.</summary>
    public string Name { get; }
}

/// <summary>Raised when a todo category is renamed.</summary>
public sealed record TodoCategoryRenamed : DomainEvent
{
    /// <summary>Initializes the event.</summary>
    /// <param name="categoryId">The identifier of the category.</param>
    /// <param name="tenantId">The tenant the category belongs to.</param>
    /// <param name="name">The new name of the category.</param>
    /// <param name="metadata">The optional metadata to merge with this event.</param>
    public TodoCategoryRenamed(Guid categoryId, Guid tenantId, string name, DefaultMetadata? metadata = null)
        : base(metadata: metadata)
    {
        CategoryId = categoryId;
        TenantId = tenantId;
        Name = name;
    }

    /// <summary>Gets the identifier of the category.</summary>
    public Guid CategoryId { get; }

    /// <summary>Gets the tenant the category belongs to.</summary>
    public Guid TenantId { get; }

    /// <summary>Gets the new name of the category.</summary>
    public string Name { get; }
}