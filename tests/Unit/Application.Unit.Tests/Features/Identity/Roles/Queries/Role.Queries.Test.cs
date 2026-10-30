using Application.Common.Querying;
using Application.Common.Querying.Pagination;
using Application.Features.Identity.Roles.Models;
using Application.Features.Identity.Roles.Queries;

namespace Application.Unit.Tests.Features.Identity.Roles.Queries;

/// <summary>Unit tests for the role query handlers.</summary>
public sealed class RoleQueriesTest
{
    [Fact(DisplayName = "Search should pass the query parameters through")]
    public async Task Search_ShouldPassParameters()
    {
        var fixture = new RoleFixture();
        fixture.Roles
            .Setup(service => service.SearchAsync(
                RoleFixture.Parameters,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QueryPage<RoleModel>>.Ok(RoleFixture.Page));

        Result<QueryPage<RoleModel>> result = await new SearchRoles.UseCaseHandler(fixture.Roles.Object)
            .Handle(
                new SearchRoles.UseCase(RoleFixture.Parameters),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(
            role => role.Id == RoleFixture.RoleId);
    }

    [Fact(DisplayName = "Search should preserve a service failure")]
    public async Task Search_WhenServiceFails_ShouldPreserveFailure()
    {
        var fixture = new RoleFixture();
        fixture.Roles
            .Setup(service => service.SearchAsync(
                It.IsAny<QueryParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QueryPage<RoleModel>>.Fail(RoleFixture.Failure));

        Result<QueryPage<RoleModel>> result = await new SearchRoles.UseCaseHandler(fixture.Roles.Object)
            .Handle(
                new SearchRoles.UseCase(RoleFixture.Parameters),
                CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }

    [Fact(DisplayName = "Get should return the role")]
    public async Task Get_ShouldReturnRole()
    {
        var fixture = new RoleFixture();
        fixture.Roles
            .Setup(service => service.GetAsync(
                RoleFixture.RoleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RoleModel>.Ok(RoleFixture.Role));

        Result<RoleModel> result = await new GetRole.UseCaseHandler(fixture.Roles.Object)
            .Handle(
                new GetRole.UseCase(RoleFixture.RoleId),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Admin");
        result.Value.Description.Should().Be("System administrator");
    }

    [Fact(DisplayName = "Get should preserve a missing role")]
    public async Task Get_WhenRoleMissing_ShouldPreserveFailure()
    {
        var fixture = new RoleFixture();
        fixture.Roles
            .Setup(service => service.GetAsync(
                RoleFixture.RoleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RoleModel>.Fail(RoleFixture.Failure));

        Result<RoleModel> result = await new GetRole.UseCaseHandler(fixture.Roles.Object)
            .Handle(
                new GetRole.UseCase(RoleFixture.RoleId),
                CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }

    [Fact(DisplayName = "Get permissions should project the permission identifiers")]
    public async Task GetPermissions_ShouldProjectIdentifiers()
    {
        var fixture = new RoleFixture();
        fixture.Roles
            .Setup(service => service.GetAsync(
                RoleFixture.RoleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RoleModel>.Ok(RoleFixture.Role));

        Result<IReadOnlyList<string>> result =
            await new GetRolePermissions.UseCaseHandler(fixture.Roles.Object)
                .Handle(
                    new GetRolePermissions.UseCase(RoleFixture.RoleId),
                    CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal("identity.roles.all.read");
    }

    [Fact(DisplayName = "Get permissions should preserve a missing role")]
    public async Task GetPermissions_WhenRoleMissing_ShouldPreserveFailure()
    {
        var fixture = new RoleFixture();
        fixture.Roles
            .Setup(service => service.GetAsync(
                RoleFixture.RoleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RoleModel>.Fail(RoleFixture.Failure));

        Result<IReadOnlyList<string>> result =
            await new GetRolePermissions.UseCaseHandler(fixture.Roles.Object)
                .Handle(
                    new GetRolePermissions.UseCase(RoleFixture.RoleId),
                    CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }
}
