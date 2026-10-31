using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Security.Authorization;
using Application.BuildingBlocks.Security.Identity.Permissions.Models;
using Application.BuildingBlocks.Security.Identity.Permissions.Queries;

namespace Application.Unit.Tests.BuildingBlocks.Security.Identity.Permissions.Queries;

/// <summary>Unit tests for the permission query handlers.</summary>
public sealed class PermissionQueriesTest
{
    [Fact(DisplayName = "Search should pass parameters and page the snapshot")]
    public async Task Search_ShouldPassParametersAndPageSnapshot()
    {
        var fixture = new PermissionFixture();
        fixture.Catalog
            .Setup(service => service.SearchAsync(
                PermissionFixture.Parameters,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionSnapshotModel>.Ok(PermissionFixture.Snapshot));

        Result<QueryPage<PermissionItemModel>> result =
            await new SearchPermissions.UseCaseHandler(fixture.Catalog.Object)
                .Handle(
                    new SearchPermissions.UseCase(PermissionFixture.Parameters),
                    CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(1);
        result.Value.TotalCount.Should().Be(3);
        result.Value.Items.Should().ContainSingle()
            .Which.Identifier.Should().Be("identity.roles.all.manage");
    }

    [Fact(DisplayName = "Search should preserve a service failure")]
    public async Task Search_WhenServiceFails_ShouldPreserveFailure()
    {
        var fixture = new PermissionFixture();
        fixture.Catalog
            .Setup(service => service.SearchAsync(
                It.IsAny<Application.BuildingBlocks.Querying.QueryParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionSnapshotModel>.Fail(PermissionFixture.Failure));

        Result<QueryPage<PermissionItemModel>> result =
            await new SearchPermissions.UseCaseHandler(fixture.Catalog.Object)
                .Handle(
                    new SearchPermissions.UseCase(PermissionFixture.Parameters),
                    CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(503);
    }

    [Fact(DisplayName = "Get should return the permission")]
    public async Task Get_ShouldReturnPermission()
    {
        var fixture = new PermissionFixture();
        fixture.Catalog
            .Setup(service => service.GetAsync(
                PermissionFixture.Item.Identifier,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionItemModel>.Ok(PermissionFixture.Item));

        Result<PermissionItemModel> result =
            await new GetPermission.UseCaseHandler(fixture.Catalog.Object)
                .Handle(
                    new GetPermission.UseCase(PermissionFixture.Item.Identifier),
                    CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Identifier.Should().Be(PermissionFixture.Item.Identifier);
        result.Value.Domain.Should().Be("identity");
    }

    [Fact(DisplayName = "Get should preserve a missing permission")]
    public async Task Get_WhenMissing_ShouldPreserveFailure()
    {
        var fixture = new PermissionFixture();
        fixture.Catalog
            .Setup(service => service.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionItemModel>.Fail(PermissionFixture.Failure));

        Result<PermissionItemModel> result =
            await new GetPermission.UseCaseHandler(fixture.Catalog.Object)
                .Handle(
                    new GetPermission.UseCase("unknown.permission.read"),
                    CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(503);
    }

    [Fact(DisplayName = "List domains should return the service values")]
    public async Task ListDomains_ShouldReturnValues()
    {
        var fixture = new PermissionFixture();
        fixture.Catalog
            .Setup(service => service.ListDomainsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<string>>.Ok(["identity", "todo"]));

        Result<IReadOnlyList<string>> result =
            await new ListPermissionDomains.UseCaseHandler(fixture.Catalog.Object)
                .Handle(new ListPermissionDomains.UseCase(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal("identity", "todo");
    }

    [Fact(DisplayName = "List domains should preserve a service failure")]
    public async Task ListDomains_WhenServiceFails_ShouldPreserveFailure()
    {
        var fixture = new PermissionFixture();
        fixture.Catalog
            .Setup(service => service.ListDomainsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<string>>.Fail(PermissionFixture.Failure));

        Result<IReadOnlyList<string>> result =
            await new ListPermissionDomains.UseCaseHandler(fixture.Catalog.Object)
                .Handle(new ListPermissionDomains.UseCase(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(503);
    }

    [Fact(DisplayName = "List categories should return the service values")]
    public async Task ListCategories_ShouldReturnValues()
    {
        var fixture = new PermissionFixture();
        fixture.Catalog
            .Setup(service => service.ListCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<string>>.Ok(["roles", "users"]));

        Result<IReadOnlyList<string>> result =
            await new ListPermissionCategories.UseCaseHandler(fixture.Catalog.Object)
                .Handle(new ListPermissionCategories.UseCase(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal("roles", "users");
    }

    [Fact(DisplayName = "List categories should preserve a service failure")]
    public async Task ListCategories_WhenServiceFails_ShouldPreserveFailure()
    {
        var fixture = new PermissionFixture();
        fixture.Catalog
            .Setup(service => service.ListCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<string>>.Fail(PermissionFixture.Failure));

        Result<IReadOnlyList<string>> result =
            await new ListPermissionCategories.UseCaseHandler(fixture.Catalog.Object)
                .Handle(new ListPermissionCategories.UseCase(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(503);
    }

    [Fact(DisplayName = "Get user permissions should pass the user id through")]
    public async Task GetUserPermissions_ShouldPassUserId()
    {
        var fixture = new PermissionFixture();
        fixture.Catalog
            .Setup(service => service.GetUserPermissionsAsync(
                PermissionFixture.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<string>>.Ok(PermissionFixture.Permissions));

        Result<IReadOnlyList<string>> result =
            await new GetUserPermissions.UseCaseHandler(fixture.Catalog.Object)
                .Handle(
                    new GetUserPermissions.UseCase(PermissionFixture.UserId),
                    CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal(PermissionFixture.Permissions);
    }

    [Fact(DisplayName = "Get user permissions should preserve a service failure")]
    public async Task GetUserPermissions_WhenServiceFails_ShouldPreserveFailure()
    {
        var fixture = new PermissionFixture();
        fixture.Catalog
            .Setup(service => service.GetUserPermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<string>>.Fail(PermissionFixture.Failure));

        Result<IReadOnlyList<string>> result =
            await new GetUserPermissions.UseCaseHandler(fixture.Catalog.Object)
                .Handle(
                    new GetUserPermissions.UseCase(PermissionFixture.UserId),
                    CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(503);
    }

    [Fact(DisplayName = "Get role permissions should pass the role id through")]
    public async Task GetRolePermissions_ShouldPassRoleId()
    {
        var fixture = new PermissionFixture();
        fixture.Catalog
            .Setup(service => service.GetRolePermissionsAsync(
                PermissionFixture.RoleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<string>>.Ok(PermissionFixture.Permissions));

        Result<IReadOnlyList<string>> result =
            await new GetRolePermissions.UseCaseHandler(fixture.Catalog.Object)
                .Handle(
                    new GetRolePermissions.UseCase(PermissionFixture.RoleId),
                    CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal(PermissionFixture.Permissions);
    }

    [Fact(DisplayName = "Get role permissions should preserve a service failure")]
    public async Task GetRolePermissions_WhenServiceFails_ShouldPreserveFailure()
    {
        var fixture = new PermissionFixture();
        fixture.Catalog
            .Setup(service => service.GetRolePermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<string>>.Fail(PermissionFixture.Failure));

        Result<IReadOnlyList<string>> result =
            await new GetRolePermissions.UseCaseHandler(fixture.Catalog.Object)
                .Handle(
                    new GetRolePermissions.UseCase(PermissionFixture.RoleId),
                    CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(503);
    }

    [Fact(DisplayName = "Check should map the request and the granted flag")]
    public async Task Check_ShouldMapRequestAndGrantedFlag()
    {
        var fixture = new PermissionFixture();
        fixture.Catalog
            .Setup(service => service.CheckUserPermissionsAsync(
                PermissionFixture.UserId,
                PermissionFixture.Permissions,
                PermissionMatchMode.Any,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        Result<PermissionCheckModel> result =
            await new CheckUserPermissions.UseCaseHandler(fixture.Catalog.Object)
                .Handle(
                    new CheckUserPermissions.UseCase(
                        PermissionFixture.UserId,
                        new CheckUserPermissions.Request
                        {
                            Permissions = PermissionFixture.Permissions,
                            Mode = PermissionMatchMode.Any,
                        }),
                    CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(PermissionFixture.UserId);
        result.Value.Permissions.Should().Equal(PermissionFixture.Permissions);
        result.Value.Mode.Should().Be(PermissionMatchMode.Any);
        result.Value.Granted.Should().BeTrue();
    }

    [Fact(DisplayName = "Check should preserve a service failure")]
    public async Task Check_WhenServiceFails_ShouldPreserveFailure()
    {
        var fixture = new PermissionFixture();
        fixture.Catalog
            .Setup(service => service.CheckUserPermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<PermissionMatchMode>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Fail(PermissionFixture.Failure));

        Result<PermissionCheckModel> result =
            await new CheckUserPermissions.UseCaseHandler(fixture.Catalog.Object)
                .Handle(
                    new CheckUserPermissions.UseCase(
                        PermissionFixture.UserId,
                        new CheckUserPermissions.Request
                        {
                            Permissions = PermissionFixture.Permissions,
                        }),
                    CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(503);
    }

    [Fact(DisplayName = "Check should forward an empty request without pre-validating")]
    public async Task Check_WhenRequestIsEmpty_ShouldReachTheService()
    {
        var fixture = new PermissionFixture();
        fixture.Catalog
            .Setup(service => service.CheckUserPermissionsAsync(
                PermissionFixture.UserId,
                It.IsAny<IReadOnlyList<string>>(),
                PermissionMatchMode.All,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(false));

        Result<PermissionCheckModel> result =
            await new CheckUserPermissions.UseCaseHandler(fixture.Catalog.Object)
                .Handle(
                    new CheckUserPermissions.UseCase(
                        PermissionFixture.UserId,
                        new CheckUserPermissions.Request()),
                    CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Granted.Should().BeFalse();
        fixture.Catalog.Verify(
            service => service.CheckUserPermissionsAsync(
                PermissionFixture.UserId,
                It.Is<IReadOnlyList<string>>(identifiers => identifiers.Count == 0),
                PermissionMatchMode.All,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
