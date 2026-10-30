namespace Application.Features.Identity.Roles.Models;

/// <summary>The administrative projection of a role and its permissions.</summary>
/// <param name="Id">The role identifier.</param>
/// <param name="Name">The unique role name.</param>
/// <param name="Permissions">The permission identifiers assigned to the role.</param>
public sealed record RoleModel(
    Guid Id,
    string Name,
    IReadOnlyList<string> Permissions);

/// <summary>The validated values for creating a role.</summary>
/// <param name="Name">The unique role name.</param>
/// <param name="Permissions">The permission identifiers to assign initially.</param>
public sealed record CreateRoleSpec(string Name, IReadOnlyList<string> Permissions);

/// <summary>The validated values for updating a role.</summary>
/// <param name="Name">The replacement role name.</param>
public sealed record UpdateRoleSpec(string Name);
