using Application.Common.Querying;

namespace Application.Features.Identity.Users.Models;

#region Parameters

/// <summary>Fields that describe a user profile (login + name parts).</summary>
public abstract record UserProfileParameter
{
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}

/// <summary>Patch semantics: every field is optional — <c>null</c> means “leave unchanged”.</summary>
public abstract record UserPatchParameter
{
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}

#endregion

#region Request

public record CreateUserRequestModel : UserProfileParameter
{
    public string Password { get; init; } = string.Empty;
    public bool EmailConfirmed { get; init; }
    public IReadOnlyList<Guid> RoleIds { get; init; } = [];
}

public record UpdateUserRequestModel : UserPatchParameter
{
    public Guid Id { get; init; }
    public IReadOnlyList<Guid>? RoleIds { get; init; }
}

public record ResetUserPasswordModel
{
    public Guid Id { get; init; }
    public string NewPassword { get; init; } = string.Empty;
}

/// <summary>A request addressed to one user.</summary>
public abstract record UserTargetModel
{
    public Guid Id { get; init; }
}

/// <summary>A user search request.</summary>
public abstract record SearchUsersModel
{
    public QueryParameters Parameters { get; init; } = new();
}

#endregion

#region Response

public sealed record UserModel : UserProfileParameter
{
    public Guid Id { get; init; }
    public bool EmailConfirmed { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public bool IsActive { get; init; }
    public bool IsLockedOut { get; init; }
    public DateTimeOffset? LockoutEndUtc { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
}

public sealed record UserSummaryModel : UserProfileParameter
{
    public bool IsActive { get; init; }
    public bool IsLockedOut { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
}

#endregion