namespace Application.Features.Identity.Users.Models;

/// <summary>The full administrative projection of a user account.</summary>
/// <param name="Id">The user identifier.</param>
/// <param name="UserName">The unique login name.</param>
/// <param name="Email">The email address.</param>
/// <param name="FirstName">The optional first name.</param>
/// <param name="LastName">The optional last name.</param>
/// <param name="EmailConfirmed">Whether the email address has been confirmed.</param>
/// <param name="TwoFactorEnabled">Whether two-factor authentication is enabled.</param>
/// <param name="IsActive">Whether the account may sign in.</param>
/// <param name="IsLockedOut">Whether the account is currently locked out.</param>
/// <param name="LockoutEndUtc">When the lockout ends; null when it is indefinite or not locked.</param>
/// <param name="Roles">The names of the roles assigned to the user.</param>
public sealed record UserModel(
    Guid Id,
    string UserName,
    string Email,
    string? FirstName,
    string? LastName,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    bool IsActive,
    bool IsLockedOut,
    DateTimeOffset? LockoutEndUtc,
    IReadOnlyList<string> Roles);

/// <summary>The compact user projection used by list and search results.</summary>
/// <param name="Id">The user identifier.</param>
/// <param name="UserName">The unique login name.</param>
/// <param name="Email">The email address.</param>
/// <param name="IsActive">Whether the account may sign in.</param>
/// <param name="IsLockedOut">Whether the account is currently locked out.</param>
/// <param name="Roles">The names of the roles assigned to the user.</param>
public sealed record UserSummaryModel(
    Guid Id,
    string UserName,
    string Email,
    bool IsActive,
    bool IsLockedOut,
    IReadOnlyList<string> Roles);

/// <summary>The validated values for creating a user account.</summary>
/// <param name="UserName">The unique login name.</param>
/// <param name="Email">The email address.</param>
/// <param name="Password">The plain-text password, hashed before storage.</param>
/// <param name="FirstName">The optional first name.</param>
/// <param name="LastName">The optional last name.</param>
/// <param name="EmailConfirmed">Whether the email starts out confirmed.</param>
/// <param name="RoleIds">The roles to assign after creation.</param>
public sealed record CreateUserSpec(
    string UserName,
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    bool EmailConfirmed,
    IReadOnlyList<Guid> RoleIds);

/// <summary>The partial update values for a user account.</summary>
/// <param name="Email">The replacement email, or null to leave it unchanged.</param>
/// <param name="FirstName">The replacement first name, or null to leave it unchanged.</param>
/// <param name="LastName">The replacement last name, or null to leave it unchanged.</param>
/// <param name="RoleIds">The replacement role set, or null to leave assignments unchanged.</param>
public sealed record UpdateUserSpec(
    string? Email,
    string? FirstName,
    string? LastName,
    IReadOnlyList<Guid>? RoleIds);
