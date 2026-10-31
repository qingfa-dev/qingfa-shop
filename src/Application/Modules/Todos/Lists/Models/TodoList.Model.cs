namespace Application.Modules.Todos.Lists.Models;

public abstract record TodoListParameterModel
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}

public record TodoListRequestModel : TodoListParameterModel;

public record TodoListResponseModel : TodoListParameterModel
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
