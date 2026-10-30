namespace Application.Features.Identity.Users.Models;

#region Parameters
/// <summary>The profile fields of a user (login name, email, name parts).</summary>
public abstract record UserProfileParameter
{
    /// <summary>The unique login name.</summary>
    public string UserName { get; init; } = string.Empty;
    /// <summary>The email address.</summary>
    public string Email { get; init; } = string.Empty;
    /// <summary>The optional first name.</summary>
    public string? FirstName { get; init; }
    /// <summary>The optional last name.</summary>
    public string? LastName { get; init; }
}
#endregion

#region Request
/// <summary>The validated values for creating a user account.</summary>
public sealed record CreateUserRequestModel : UserProfileParameter
{
    /// <summary>The plain-text password, hashed before storage.</summary>
    public string Password { get; init; } = string.Empty;
    /// <summary>Whether the email starts out confirmed.</summary>
    public bool EmailConfirmed { get; init; }
    /// <summary>The roles to assign after creation.</summary>
    public IReadOnlyList<Guid> RoleIds { get; init; } = [];
}
#endregion

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


/// <summary>Create-user request values shared by the command and the HTTP model.</summary>
public abstract record CreateUserModel
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool EmailConfirmed { get; set; }
    public IReadOnlyList<Guid> RoleIds { get; set; } = [];
}

/// <summary>Update-user request values shared by the command and the HTTP model.</summary>
public abstract record UpdateUserModel
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public IReadOnlyList<Guid>? RoleIds { get; set; }
}

/// <summary>A request addressed to one user.</summary>
public abstract record UserTargetModel
{
    public Guid Id { get; set; }
}

/// <summary>A request linking one user to one role.</summary>
public abstract record UserRoleModel
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

/// <summary>An admin password reset request.</summary>
public abstract record ResetUserPasswordModel
{
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>A user search request.</summary>
public abstract record SearchUsersModel
{
    public Application.Common.Querying.QueryParameters Parameters { get; set; } = new();
}
