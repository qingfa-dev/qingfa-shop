using Application.Common.Querying;
using Application.Common.Querying.Pagination;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Users.Models;

namespace Application.Unit.Tests.Features.Identity.Users;

/// <summary>Shared sample data for the admin user slice unit tests.</summary>
public sealed class UserFixture
{
    /// <summary>The sample user identifier.</summary>
    public static readonly Guid UserId = new("22222222-2222-2222-2222-222222222222");

    /// <summary>The sample role identifier.</summary>
    public static readonly Guid RoleId = new("33333333-3333-3333-3333-333333333333");

    /// <summary>Gets the user account service mock.</summary>
    public Mock<IUserAccountService> Users { get; } = new();

    /// <summary>Gets a representative user projection.</summary>
    public static UserModel User => new(
        UserId,
        "admin",
        "admin@example.com",
        "Admin",
        "User",
        true,
        false,
        true,
        false,
        null,
        ["Admin"]);

    /// <summary>Gets a representative search page.</summary>
    public static QueryPage<UserModel> Page => new([User], 1, 20, 1);

    /// <summary>Gets a representative query-parameter set.</summary>
    public static QueryParameters Parameters => new() { Search = "adm", Page = 1, PageSize = 20 };

    /// <summary>Gets a representative service failure.</summary>
    public static Error Failure => Error.NotFound(
        "Identity.Test.NotFound",
        "The test user was not found.");
}
