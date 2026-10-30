using Application.Common.Querying;
using Application.Common.Querying.Pagination;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Roles.Models;

namespace Application.Unit.Tests.Features.Identity.Roles;

/// <summary>Shared sample data for the roles slice unit tests.</summary>
public sealed class RoleFixture
{
    /// <summary>The sample role identifier.</summary>
    public static readonly Guid RoleId = new("44444444-4444-4444-4444-444444444444");

    /// <summary>Gets the role management service mock.</summary>
    public Mock<IRoleManagementService> Roles { get; } = new();

    /// <summary>Gets a representative role projection.</summary>
    public static RoleModel Role => new()
    {
        Id = RoleId,
        Name = "Admin",
        Description = "System administrator",
        Permissions = ["identity.roles.all.read"],
    };

    /// <summary>Gets a representative role summary.</summary>
    public static RoleSummaryModel Summary => new()
    {
        Id = RoleId,
        Name = "Admin",
        Description = "System administrator",
        PermissionCount = 1,
    };

    /// <summary>Gets a representative search page.</summary>
    public static QueryPage<RoleModel> Page => new([Role], 1, 20, 1);

    /// <summary>Gets a representative query-parameter set.</summary>
    public static QueryParameters Parameters => new() { Search = "adm", Page = 1, PageSize = 20 };

    /// <summary>Gets a representative service failure.</summary>
    public static Error Failure => Error.NotFound(
        "Role.Test.NotFound",
        "The test role was not found.");
}
