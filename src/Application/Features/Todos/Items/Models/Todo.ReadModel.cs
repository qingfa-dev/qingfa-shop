using Core.Common.Concerns.Foundation.Tenantable;
using Core.Common.Concerns.Lifecycle.SoftDeletable;
using Core.Common.Domain.Entities;
using Core.Todos.Items;

namespace Application.Features.Todos.Items.Models;

/// <summary>Relational projection of the Todo event stream.</summary>
public sealed record TodoReadModel : IEntity<Guid>, ITenantable<Guid>, ISoftDeletable, ITodo
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TodoStatus Status { get; set; }
    public TodoPriority Priority { get; set; }
    public DateTimeOffset? DueAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public Guid? ListId { get; set; }
    public ICollection<Guid> CategoryIds { get; set; } = [];
    public bool IsCompleted => Status == TodoStatus.Completed;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsHardDeleted => false;
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public long Version { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = [];
}