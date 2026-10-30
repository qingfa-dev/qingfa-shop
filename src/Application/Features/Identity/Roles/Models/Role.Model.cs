namespace Application.Features.Identity.Roles.Models;

#region Parameters

/// <summary>Fields that describe a role profile.</summary>
public abstract record RoleProfileParameter
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

/// <summary>A request linking one role to one permission.</summary>
public abstract record RolePermissionModel
{
    public Guid RoleId { get; init; }
    public string Permission { get; init; } = string.Empty;
}

#endregion

#region Request

public record CreateRoleRequestModel : RoleProfileParameter
{
    public IReadOnlyList<string> Permissions { get; init; } = [];
}

public record UpdateRoleRequestModel : RoleProfileParameter;

#endregion

#region Response

public sealed record RoleModel : RoleProfileParameter
{
    public Guid Id { get; init; }
    public IReadOnlyList<string> Permissions { get; init; } = [];
}

public sealed record RoleSummaryModel : RoleProfileParameter
{
    public Guid Id { get; init; }
    public int PermissionCount { get; init; }
}

#endregion
