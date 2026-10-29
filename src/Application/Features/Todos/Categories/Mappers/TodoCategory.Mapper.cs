using Application.Features.Todos.Categories.Models;

using Core.Todos.Categories;

namespace Application.Features.Todos.Categories.Mappers;

public static class TodoCategoryMapper
{
    public static T ToModel<T>(this TodoCategory category) where T : TodoCategoryResponseModel, new()
    {
        return new T
        {
            Id = category.Id,
            TenantId = category.TenantId,
            Name = category.Name,
            Description = category.Description,
            CreatedAtUtc = category.CreatedAtUtc,
            CreatedBy = category.CreatedBy,
            ModifiedAtUtc = category.ModifiedAtUtc,
            ModifiedBy = category.ModifiedBy,
            IsDeleted = category.IsDeleted,
            DeletedAtUtc = category.DeletedAtUtc,
            DeletedBy = category.DeletedBy,
        };
    }

}