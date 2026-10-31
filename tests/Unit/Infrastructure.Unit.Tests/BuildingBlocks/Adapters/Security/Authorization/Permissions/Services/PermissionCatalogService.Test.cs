using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Security.Authorization;
using Application.BuildingBlocks.Security.Authorization.Domain;
using Application.BuildingBlocks.Security.Authorization.Permissions.Services;
using Application.BuildingBlocks.Security.Authorization.Registry;
using Application.Modules.Todos.Permissions;
using Application.BuildingBlocks.Security.Identity.Permissions.Models;

using Infrastructure.BuildingBlocks.Adapters.Security.Authorization.Permissions.Services;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Authorization.Permissions.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Authorization")]
[Trait("Feature", "PermissionCatalog")]
public sealed class PermissionCatalogServiceTest
{
    private static readonly DateTimeOffset s_refreshedOn =
        new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid s_userId = new("77777777-7777-7777-7777-777777777777");
    private static readonly Guid s_roleId = new("88888888-8888-8888-8888-888888888888");

    private static PermissionMetadataSnapshot Snapshot => PermissionMetadataSnapshot.Create(
        [
            PermissionContext.Identity.Users.Manage,
            PermissionContext.Identity.Roles.Read,
            TodoPermissionCatalog.Items.Read,
        ],
        s_refreshedOn);

    private static Error Unavailable => Error.ServiceUnavailable(
        "PermissionCatalog.Test.Unavailable",
        "The test registry is unavailable.");

    [Fact(DisplayName = "Search should filter identifiers, domains, and actions")]
    public async Task Search_ShouldFilterCatalog()
    {
        PermissionCatalogService service = BuildService(Result<PermissionMetadataSnapshot>.Ok(Snapshot));

        Result<PermissionSnapshotModel> byText = await service.SearchAsync(
            new QueryParameters { Search = "identity" });
        Result<PermissionSnapshotModel> byAction = await service.SearchAsync(
            new QueryParameters { Search = "manage" });
        Result<PermissionSnapshotModel> byDomain = await service.SearchAsync(
            new QueryParameters { Search = "todo" });

        byText.IsSuccess.Should().BeTrue();
        byText.Value.Permissions.Should().HaveCount(2);
        byText.Value.Permissions.Should().OnlyContain(permission => permission.Domain == "identity");
        byText.Value.RefreshedOnUtc.Should().Be(s_refreshedOn);

        byAction.Value.Permissions.Should().ContainSingle()
            .Which.Identifier.Should().Be(PermissionContext.Identity.Users.Manage.Identifier);
        byAction.Value.Permissions[0].Description.Should()
            .Be(PermissionContext.Identity.Users.Manage.Description);

        byDomain.Value.Permissions.Should().ContainSingle()
            .Which.Identifier.Should().Be(TodoPermissionCatalog.Items.Read.Identifier);
    }

    [Fact(DisplayName = "Search should return the whole catalog without search text")]
    public async Task Search_WithoutText_ShouldReturnEverything()
    {
        PermissionCatalogService service = BuildService(Result<PermissionMetadataSnapshot>.Ok(Snapshot));

        Result<PermissionSnapshotModel> result = await service.SearchAsync(new QueryParameters());

        result.IsSuccess.Should().BeTrue();
        result.Value.Permissions.Should().HaveCount(3);
    }

    [Fact(DisplayName = "Search should reject unsupported query parameters")]
    public async Task Search_WithUnsupportedParameter_ShouldFail()
    {
        Mock<IPermissionMetadataRegistry> registry = Registry(Result<PermissionMetadataSnapshot>.Ok(Snapshot));
        PermissionCatalogService service = BuildService(registry);

        Result<PermissionSnapshotModel> result = await service.SearchAsync(
            new QueryParameters { OrderBy = "identifier" });

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(400);
        result.Errors.Should().ContainSingle(error => error.Code == QueryOutcome.Codes.Unsupported);
        registry.Verify(
            candidate => candidate.RefreshAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Search should preserve a registry failure")]
    public async Task Search_WhenRegistryFails_ShouldPreserveFailure()
    {
        PermissionCatalogService service = BuildService(Result<PermissionMetadataSnapshot>.Fail(Unavailable));

        Result<PermissionSnapshotModel> result = await service.SearchAsync(new QueryParameters());

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(503);
    }

    [Fact(DisplayName = "Get should return a known permission and fail on an unknown one")]
    public async Task Get_ShouldReturnKnownAndFailUnknown()
    {
        PermissionCatalogService service = BuildService(Result<PermissionMetadataSnapshot>.Ok(Snapshot));

        Result<PermissionItemModel> found = await service.GetAsync(
            PermissionContext.Identity.Users.Manage.Identifier);
        Result<PermissionItemModel> missing = await service.GetAsync("unknown.permission.read");

        found.IsSuccess.Should().BeTrue();
        found.Value.Identifier.Should().Be(PermissionContext.Identity.Users.Manage.Identifier);
        found.Value.Domain.Should().Be("identity");
        found.Value.Category.Should().Be("users");

        missing.IsFailure.Should().BeTrue();
        missing.StatusCode.Should().Be(404);
        missing.Errors.Should().ContainSingle(
            error => error.Code == "PermissionCatalog.NotFound");
    }

    [Fact(DisplayName = "Get should preserve a registry failure")]
    public async Task Get_WhenRegistryFails_ShouldPreserveFailure()
    {
        PermissionCatalogService service = BuildService(Result<PermissionMetadataSnapshot>.Fail(Unavailable));

        Result<PermissionItemModel> result = await service.GetAsync(
            PermissionContext.Identity.Users.Manage.Identifier);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(503);
    }

    [Fact(DisplayName = "Domains and categories should be distinct and sorted")]
    public async Task Lists_ShouldBeDistinctAndSorted()
    {
        PermissionCatalogService service = BuildService(Result<PermissionMetadataSnapshot>.Ok(Snapshot));

        Result<IReadOnlyList<string>> domains = await service.ListDomainsAsync();
        Result<IReadOnlyList<string>> categories = await service.ListCategoriesAsync();

        domains.IsSuccess.Should().BeTrue();
        domains.Value.Should().Equal("identity", "todo");
        categories.IsSuccess.Should().BeTrue();
        categories.Value.Should().Equal("roles", "todos", "users");
    }

    [Fact(DisplayName = "Domains and categories should preserve a registry failure")]
    public async Task Lists_WhenRegistryFails_ShouldPreserveFailure()
    {
        PermissionCatalogService service = BuildService(Result<PermissionMetadataSnapshot>.Fail(Unavailable));

        Result<IReadOnlyList<string>> domains = await service.ListDomainsAsync();
        Result<IReadOnlyList<string>> categories = await service.ListCategoriesAsync();

        domains.IsFailure.Should().BeTrue();
        domains.StatusCode.Should().Be(503);
        categories.IsFailure.Should().BeTrue();
        categories.StatusCode.Should().Be(503);
    }

    [Fact(DisplayName = "User permissions should be sorted in place")]
    public async Task GetUserPermissions_ShouldSort()
    {
        Mock<IPermissionService> permissions = Permissions();
        permissions
            .Setup(service => service.GetEffectiveUserPermissionsAsync(
                s_userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Ok(
                new HashSet<string>(
                    ["todo.todos.item.read", "identity.users.all.read"],
                    StringComparer.OrdinalIgnoreCase)));

        PermissionCatalogService service = BuildService(
            Result<PermissionMetadataSnapshot>.Ok(Snapshot),
            permissions);

        Result<IReadOnlyList<string>> result = await service.GetUserPermissionsAsync(s_userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal("identity.users.all.read", "todo.todos.item.read");
    }

    [Fact(DisplayName = "User permissions should preserve a service failure")]
    public async Task GetUserPermissions_WhenServiceFails_ShouldPreserveFailure()
    {
        Mock<IPermissionService> permissions = Permissions();
        permissions
            .Setup(service => service.GetEffectiveUserPermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Fail(Unavailable));

        PermissionCatalogService service = BuildService(
            Result<PermissionMetadataSnapshot>.Ok(Snapshot),
            permissions);

        Result<IReadOnlyList<string>> result = await service.GetUserPermissionsAsync(s_userId);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(503);
    }

    [Fact(DisplayName = "Role permissions should be sorted in place")]
    public async Task GetRolePermissions_ShouldSort()
    {
        Mock<IPermissionService> permissions = Permissions();
        permissions
            .Setup(service => service.GetRolePermissionsAsync(
                s_roleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Ok(
                new HashSet<string>(
                    ["todo.todos.item.read", "identity.roles.all.read"],
                    StringComparer.OrdinalIgnoreCase)));

        PermissionCatalogService service = BuildService(
            Result<PermissionMetadataSnapshot>.Ok(Snapshot),
            permissions);

        Result<IReadOnlyList<string>> result = await service.GetRolePermissionsAsync(s_roleId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal("identity.roles.all.read", "todo.todos.item.read");
    }

    [Fact(DisplayName = "Role permissions should preserve a service failure")]
    public async Task GetRolePermissions_WhenServiceFails_ShouldPreserveFailure()
    {
        Mock<IPermissionService> permissions = Permissions();
        permissions
            .Setup(service => service.GetRolePermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Fail(Unavailable));

        PermissionCatalogService service = BuildService(
            Result<PermissionMetadataSnapshot>.Ok(Snapshot),
            permissions);

        Result<IReadOnlyList<string>> result = await service.GetRolePermissionsAsync(s_roleId);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(503);
    }

    [Fact(DisplayName = "Check should delegate ALL and ANY modes")]
    public async Task Check_ShouldDelegateModes()
    {
        Mock<IPermissionService> permissions = Permissions();
        permissions
            .Setup(service => service.HasAllPermissionsAsync(
                s_userId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));
        permissions
            .Setup(service => service.HasAnyPermissionsAsync(
                s_userId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(false));

        PermissionCatalogService service = BuildService(
            Result<PermissionMetadataSnapshot>.Ok(Snapshot),
            permissions);

        string[] identifiers = [PermissionContext.Identity.Users.Manage.Identifier];

        Result<bool> all = await service.CheckUserPermissionsAsync(
            s_userId,
            identifiers,
            PermissionMatchMode.All);
        Result<bool> any = await service.CheckUserPermissionsAsync(
            s_userId,
            identifiers,
            PermissionMatchMode.Any);

        all.Value.Should().BeTrue();
        any.Value.Should().BeFalse();
        permissions.Verify(
            candidate => candidate.HasAllPermissionsAsync(
                s_userId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        permissions.Verify(
            candidate => candidate.HasAnyPermissionsAsync(
                s_userId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Check should reject an empty requirement")]
    public async Task Check_WhenEmpty_ShouldFailWithoutCallingTheService()
    {
        Mock<IPermissionService> permissions = Permissions();
        PermissionCatalogService service = BuildService(
            Result<PermissionMetadataSnapshot>.Ok(Snapshot),
            permissions);

        Result<bool> empty = await service.CheckUserPermissionsAsync(
            s_userId,
            [],
            PermissionMatchMode.All);
        Result<bool> missing = await service.CheckUserPermissionsAsync(
            s_userId,
            null!,
            PermissionMatchMode.All);

        empty.IsFailure.Should().BeTrue();
        empty.StatusCode.Should().Be(400);
        empty.Errors.Should().ContainSingle(
            error => error.Code == "PermissionCatalog.Permissions.Required");
        missing.IsFailure.Should().BeTrue();

        permissions.Verify(
            candidate => candidate.HasAllPermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Check should preserve a service failure")]
    public async Task Check_WhenServiceFails_ShouldPreserveFailure()
    {
        Mock<IPermissionService> permissions = Permissions();
        permissions
            .Setup(service => service.HasAllPermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Fail(Unavailable));

        PermissionCatalogService service = BuildService(
            Result<PermissionMetadataSnapshot>.Ok(Snapshot),
            permissions);

        Result<bool> result = await service.CheckUserPermissionsAsync(
            s_userId,
            [PermissionContext.Identity.Users.Manage.Identifier],
            PermissionMatchMode.All);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(503);
    }

    [Fact(DisplayName = "Refresh should map the rebuilt snapshot")]
    public async Task Refresh_ShouldMapSnapshot()
    {
        PermissionCatalogService service = BuildService(Result<PermissionMetadataSnapshot>.Ok(Snapshot));

        Result<PermissionSnapshotModel> result = await service.RefreshAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Permissions.Should().HaveCount(3);
        result.Value.RefreshedOnUtc.Should().Be(s_refreshedOn);
    }

    [Fact(DisplayName = "Refresh should preserve a registry failure")]
    public async Task Refresh_WhenRegistryFails_ShouldPreserveFailure()
    {
        PermissionCatalogService service = BuildService(Result<PermissionMetadataSnapshot>.Fail(Unavailable));

        Result<PermissionSnapshotModel> result = await service.RefreshAsync();

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(503);
    }

    [Fact(DisplayName = "Search should reject a null parameter set")]
    public async Task Search_WhenParametersAreNull_ShouldThrow()
    {
        PermissionCatalogService service = BuildService(Result<PermissionMetadataSnapshot>.Ok(Snapshot));

        Func<Task> search = () => service.SearchAsync(null!);

        await search.Should().ThrowAsync<ArgumentNullException>();
    }

    private static PermissionCatalogService BuildService(
        Result<PermissionMetadataSnapshot> refreshResult,
        Mock<IPermissionService>? permissions = null) =>
        new(Registry(refreshResult).Object, (permissions ?? Permissions()).Object);

    private static PermissionCatalogService BuildService(
        Mock<IPermissionMetadataRegistry> registry) =>
        new(registry.Object, Permissions().Object);

    private static Mock<IPermissionMetadataRegistry> Registry(
        Result<PermissionMetadataSnapshot> refreshResult)
    {
        var registry = new Mock<IPermissionMetadataRegistry>();
        registry
            .Setup(candidate => candidate.RefreshAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshResult);
        return registry;
    }

    private static Mock<IPermissionService> Permissions() => new();
}
