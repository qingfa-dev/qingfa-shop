using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Security.Authorization;
using Application.BuildingBlocks.Security.Identity.Abstractions;
using Application.BuildingBlocks.Security.Identity.Permissions.Models;

namespace Application.Unit.Tests.BuildingBlocks.Security.Identity.Permissions;

/// <summary>Shared sample data for the permissions slice unit tests.</summary>
public sealed class PermissionFixture
{
    /// <summary>The sample user identifier.</summary>
    public static readonly Guid UserId = new("55555555-5555-5555-5555-555555555555");

    /// <summary>The sample role identifier.</summary>
    public static readonly Guid RoleId = new("66666666-6666-6666-6666-666666666666");

    /// <summary>The sample catalog refresh instant.</summary>
    public static readonly DateTimeOffset RefreshedOnUtc =
        new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Gets the permission catalog service mock.</summary>
    public Mock<IPermissionCatalogService> Catalog { get; } = new();

    /// <summary>Gets a representative permission projection.</summary>
    public static PermissionItemModel Item => new(
        "identity.users.all.read",
        "identity",
        "users",
        "all",
        "read",
        "Read every user.");

    /// <summary>Gets a second representative permission projection.</summary>
    public static PermissionItemModel Item2 => new(
        "identity.roles.all.manage",
        "identity",
        "roles",
        "all",
        "manage",
        "Manage every role.");

    /// <summary>Gets a third representative permission projection.</summary>
    public static PermissionItemModel Item3 => new(
        "todo.todos.item.read",
        "todo",
        "todos",
        "item",
        "read",
        "Read todo items.");

    /// <summary>Gets a representative catalog snapshot.</summary>
    public static PermissionSnapshotModel Snapshot => new([Item, Item2, Item3], RefreshedOnUtc);

    /// <summary>Gets representative permission identifiers.</summary>
    public static IReadOnlyList<string> Permissions => ["identity.roles.all.manage", "identity.users.all.read"];

    /// <summary>Gets a representative query-parameter set.</summary>
    public static QueryParameters Parameters => new() { Page = 2, PageSize = 1 };

    /// <summary>Gets a representative service failure.</summary>
    public static Error Failure => Error.ServiceUnavailable(
        "Permission.Test.Unavailable",
        "The test catalog is unavailable.");
}
