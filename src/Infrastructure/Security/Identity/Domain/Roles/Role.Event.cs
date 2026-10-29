using Core.Common.Domain.Events;
using Core.Common.Monad.Metadata;

namespace Infrastructure.Security.Identity.Domain.Roles;

/// <summary>Raised when a role is created.</summary>
/// <param name="RoleId">The created role identifier.</param>
/// <param name="Name">The created role name.</param>
/// <param name="metadata">Optional event metadata.</param>
public sealed record RoleCreated(
    Guid RoleId,
    string Name,
    DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a role changes.</summary>
/// <param name="RoleId">The updated role identifier.</param>
/// <param name="Name">The updated role name.</param>
/// <param name="metadata">Optional event metadata.</param>
public sealed record RoleUpdated(
    Guid RoleId,
    string Name,
    DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a role is deleted.</summary>
/// <param name="RoleId">The deleted role identifier.</param>
/// <param name="metadata">Optional event metadata.</param>
public sealed record RoleDeleted(Guid RoleId, DefaultMetadata? metadata = null)
    : DomainEvent(metadata: metadata);
