namespace Application.Modules.Todos.Categories.Models;

public abstract record TodoCategoryParameterModel
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}

public record TodoCategoryRequestModel : TodoCategoryParameterModel;

public record TodoCategoryResponseModel : TodoCategoryParameterModel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public string? ModifiedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public string? DeletedBy { get; set; }
}
