using System.Text.Json.Serialization;

using Core.Common.Domain.Events;
using Core.Common.Monad.Metadata;

namespace Infrastructure.Security.Identity.Domain.Users;

/// <summary>Raised when a user is created.</summary>
/// <param name="UserId">The created user identifier.</param>
/// <param name="UserName">The created username.</param>
/// <param name="Email">The created email address.</param>
/// <param name="metadata">Optional event metadata.</param>
public sealed record UserCreated(
    Guid UserId,
    string UserName,
    string Email,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a user profile changes.</summary>
/// <param name="UserId">The updated user identifier.</param>
/// <param name="metadata">Optional event metadata.</param>
public sealed record UserUpdated(Guid UserId, [property: JsonIgnore] DefaultMetadata? metadata = null)
    : DomainEvent(metadata: metadata);

/// <summary>Raised when a user is activated.</summary>
/// <param name="UserId">The activated user identifier.</param>
/// <param name="metadata">Optional event metadata.</param>
public sealed record UserActivated(Guid UserId, [property: JsonIgnore] DefaultMetadata? metadata = null)
    : DomainEvent(metadata: metadata);

/// <summary>Raised when a user is deactivated.</summary>
/// <param name="UserId">The deactivated user identifier.</param>
/// <param name="metadata">Optional event metadata.</param>
public sealed record UserDeactivated(Guid UserId, [property: JsonIgnore] DefaultMetadata? metadata = null)
    : DomainEvent(metadata: metadata);

/// <summary>Raised when a user is locked out.</summary>
/// <param name="UserId">The locked user identifier.</param>
/// <param name="LockoutEnd">The optional lockout end.</param>
/// <param name="metadata">Optional event metadata.</param>
public sealed record UserLocked(
    Guid UserId,
    DateTimeOffset? LockoutEnd,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a user is unlocked.</summary>
/// <param name="UserId">The unlocked user identifier.</param>
/// <param name="metadata">Optional event metadata.</param>
public sealed record UserUnlocked(Guid UserId, [property: JsonIgnore] DefaultMetadata? metadata = null)
    : DomainEvent(metadata: metadata);

/// <summary>Raised when a user signs in.</summary>
/// <param name="UserId">The signed-in user identifier.</param>
/// <param name="IpAddress">The optional sign-in IP address.</param>
/// <param name="metadata">Optional event metadata.</param>
public sealed record UserSignedIn(
    Guid UserId,
    string? IpAddress,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);
