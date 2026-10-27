namespace Contract.Todos.Models;

/// <summary>Read model for a todo category, including audit state.</summary>
/// <param name="Id">The category identifier.</param>
/// <param name="TenantId">The tenant that owns the category.</param>
/// <param name="Name">The category name.</param>
/// <param name="Description">The optional description.</param>
/// <param name="CreatedAtUtc">The UTC creation date.</param>
/// <param name="CreatedBy">The optional creator.</param>
/// <param name="ModifiedAtUtc">The optional UTC last-modification date.</param>
/// <param name="ModifiedBy">The optional last modifier.</param>
/// <param name="IsDeleted">Whether the category is soft-deleted.</param>
/// <param name="DeletedAtUtc">The optional UTC deletion date.</param>
/// <param name="DeletedBy">The optional deleter.</param>
public sealed record TodoCategoryModel(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? ModifiedAtUtc,
    string? ModifiedBy,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc,
    string? DeletedBy);
