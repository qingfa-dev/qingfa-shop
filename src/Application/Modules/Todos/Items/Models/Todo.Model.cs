using Core.Modules.Todos.Items;

namespace Application.Modules.Todos.Items.Models;

#region Parameters
public abstract record TodoParameterModel
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public TodoPriority? Priority { get; set; }
    public DateTimeOffset? DueAtUtc { get; set; }
    public Guid? ListId { get; set; }
    public IReadOnlyList<Guid> CategoryIds { get; set; } = [];
}
#endregion

#region Request
public record TodoRequestModel : TodoParameterModel;
#endregion


#region Response
public record TodoResponseModel : TodoParameterModel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public TodoStatus Status { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
}

public record TodoSummaryResponseModel
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public TodoStatus Status { get; set; }

    public TodoPriority Priority { get; set; }

    public DateTimeOffset? DueAtUtc { get; set; }

    public Guid? ListId { get; set; }

    public IReadOnlyList<Guid> CategoryIds { get; set; } = [];

    public bool IsCompleted { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
#endregion