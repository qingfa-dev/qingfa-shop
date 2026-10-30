using System.Text.Json.Serialization;

using Core.Common.Domain.Events;
using Core.Common.Monad.Metadata;

namespace Core.Todos.Items;

/// <summary>Raised when a todo is created.</summary>
public sealed record TodoCreated(
    Guid TodoId,
    Guid TenantId,
    string Title,
    string? Description,
    TodoPriority Priority,
    DateTimeOffset? DueAtUtc,
    Guid? ListId,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a todo's editable fields change.</summary>
public sealed record TodoUpdated(
    Guid TodoId,
    string Title,
    string? Description,
    TodoPriority Priority,
    DateTimeOffset? DueAtUtc,
    Guid? ListId,
    DateTimeOffset ModifiedAtUtc,
    string? ModifiedBy,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised for start, complete, cancel and reopen.</summary>
public sealed record TodoStatusChanged(
    Guid TodoId,
    Guid TenantId,
    TodoStatus From,
    TodoStatus To,
    DateTimeOffset OccurredAtUtc,
    string? Actor,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a category reference is added.</summary>
public sealed record TodoCategoryAdded(
    Guid TodoId,
    Guid CategoryId,
    DateTimeOffset OccurredAtUtc,
    string? Actor,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a category reference is removed.</summary>
public sealed record TodoCategoryRemoved(
    Guid TodoId,
    Guid CategoryId,
    DateTimeOffset OccurredAtUtc,
    string? Actor,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when the list reference changes; a null list clears it.</summary>
public sealed record TodoListChanged(
    Guid TodoId,
    Guid? ListId,
    DateTimeOffset OccurredAtUtc,
    string? Actor,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a todo is deleted; hard deletes remove the projection row.</summary>
public sealed record TodoDeleted(
    Guid TodoId,
    bool IsHard,
    DateTimeOffset OccurredAtUtc,
    string? Actor,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a soft-deleted todo is restored.</summary>
public sealed record TodoRestored(
    Guid TodoId,
    DateTimeOffset OccurredAtUtc,
    string? Actor,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);