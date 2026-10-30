using Application.Common.Querying;
using Application.Common.Querying.Pagination;
using Application.Features.Identity.Users.Models;
using Application.Features.Identity.Users.Queries;

namespace Application.Unit.Tests.Features.Identity.Users.Queries;

/// <summary>Unit tests for the admin user query handlers.</summary>
public sealed class UserQueriesTest
{
    [Fact(DisplayName = "Search should pass the query parameters through")]
    public async Task Search_ShouldPassParameters()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.SearchAsync(
                UserFixture.Parameters,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QueryPage<UserModel>>.Ok(UserFixture.Page));

        Result<QueryPage<UserModel>> result = await new SearchUsers.UseCaseHandler(fixture.Users.Object)
            .Handle(
                new SearchUsers.UseCase(new SearchUsers.Request { Parameters = UserFixture.Parameters }),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(
            user => user.Id == UserFixture.UserId);
    }

    [Fact(DisplayName = "Search should preserve a service failure")]
    public async Task Search_WhenServiceFails_ShouldPreserveFailure()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.SearchAsync(
                It.IsAny<QueryParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QueryPage<UserModel>>.Fail(UserFixture.Failure));

        Result<QueryPage<UserModel>> result = await new SearchUsers.UseCaseHandler(fixture.Users.Object)
            .Handle(
                new SearchUsers.UseCase(new SearchUsers.Request { Parameters = UserFixture.Parameters }),
                CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }

    [Fact(DisplayName = "Get should return the user")]
    public async Task Get_ShouldReturnUser()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.GetAsync(
                UserFixture.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserModel>.Ok(UserFixture.User));

        Result<UserModel> result = await new GetUser.UseCaseHandler(fixture.Users.Object)
            .Handle(
                new GetUser.UseCase(new GetUser.Request { Id = UserFixture.UserId }),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("admin@example.com");
    }

    [Fact(DisplayName = "Get should preserve a missing user")]
    public async Task Get_WhenUserMissing_ShouldPreserveFailure()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.GetAsync(
                UserFixture.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserModel>.Fail(UserFixture.Failure));

        Result<UserModel> result = await new GetUser.UseCaseHandler(fixture.Users.Object)
            .Handle(
                new GetUser.UseCase(new GetUser.Request { Id = UserFixture.UserId }),
                CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }

    [Fact(DisplayName = "Get roles should project the role names")]
    public async Task GetRoles_ShouldProjectRoleNames()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.GetAsync(
                UserFixture.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserModel>.Ok(UserFixture.User));

        Result<IReadOnlyList<string>> result = await new GetUserRoles.UseCaseHandler(fixture.Users.Object)
            .Handle(
                new GetUserRoles.UseCase(new GetUserRoles.Request { Id = UserFixture.UserId }),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal("Admin");
    }

    [Fact(DisplayName = "Get roles should preserve a missing user")]
    public async Task GetRoles_WhenUserMissing_ShouldPreserveFailure()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.GetAsync(
                UserFixture.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserModel>.Fail(UserFixture.Failure));

        Result<IReadOnlyList<string>> result = await new GetUserRoles.UseCaseHandler(fixture.Users.Object)
            .Handle(
                new GetUserRoles.UseCase(new GetUserRoles.Request { Id = UserFixture.UserId }),
                CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }
}
