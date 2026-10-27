using Contract.Todos.Models;

using Core.Todos.Categories;

namespace Application.Features.Todos.Mapping;

/// <summary>Hand-written mapping from the TodoCategory aggregate to its read model.</summary>
public static class TodoCategoryMapping
{
    /// <summary>Maps a todo category to its read model.</summary>
    /// <param name="category">The category to map.</param>
    /// <returns>The mapped model.</returns>
    public static TodoCategoryModel ToModel(this TodoCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new TodoCategoryModel(
            category.Id,
            category.TenantId,
            category.Name,
            category.Description,
            category.CreatedAtUtc,
            category.CreatedBy,
            category.ModifiedAtUtc,
            category.ModifiedBy,
            category.IsDeleted,
            category.DeletedAtUtc,
            category.DeletedBy);
    }
}
