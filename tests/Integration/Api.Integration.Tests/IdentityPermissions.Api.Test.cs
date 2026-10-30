using System.Net;
using System.Text.Json;

namespace Api.Integration.Tests;

/// <summary>Exercises the permission catalog and the user/role permission projections.</summary>
[Collection(ResultContractStackCollection.Name)]
public sealed class IdentityPermissionsApiTest : IdentityStackTestBase
{
    private const string UsersRead = "identity.users.all.read";
    private const string RolesRead = "identity.roles.all.read";

    public IdentityPermissionsApiTest(ResultContractStackFixture fixture)
        : base(fixture)
    {
    }

    [Fact(DisplayName = "GET /api/identity/permissions should search the catalog")]
    public async Task SearchPermissions_ShouldReturnCatalogPage()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/identity/permissions?search=identity.users&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement value = await ReadValueAsync(response);

        value.GetProperty("items").EnumerateArray()
            .Should()
            .Contain(item => item.GetProperty("identifier").GetString() == UsersRead);
        value.GetProperty("totalCount").GetInt64().Should().BePositive();
    }

    [Fact(DisplayName = "GET /api/identity/permissions/domains should list distinct domains")]
    public async Task ListDomains_ShouldReturnDistinctDomains()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/identity/permissions/domains");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await ReadValueAsync(response)).EnumerateArray()
            .Select(domain => domain.GetString())
            .Should()
            .Contain("identity");
    }

    [Fact(DisplayName = "GET /api/identity/permissions/categories should list categories")]
    public async Task ListCategories_ShouldReturnDistinctCategories()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/identity/permissions/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await ReadValueAsync(response)).EnumerateArray()
            .Select(category => category.GetString())
            .Should()
            .Contain("users");
    }

    [Fact(DisplayName = "GET /api/identity/permissions/{identifier} should return one entry")]
    public async Task GetPermission_ShouldReturnMetadata()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/identity/permissions/{UsersRead}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement value = await ReadValueAsync(response);

        value.GetProperty("identifier").GetString().Should().Be(UsersRead);
        value.GetProperty("domain").GetString().Should().Be("identity");
        value.GetProperty("category").GetString().Should().Be("users");
        value.GetProperty("action").GetString().Should().Be("read");
    }

    [Fact(DisplayName = "GET /api/identity/permissions/{identifier} with an unknown id should 404")]
    public async Task GetPermission_WhenMissing_ShouldReturn404()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/identity/permissions/identity.unknown.all.read");

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.NotFound,
            "PermissionCatalog.NotFound");
    }

    [Fact(DisplayName = "GET /api/identity/permissions/users/{id} should list effective permissions")]
    public async Task GetUserPermissions_ShouldListEffectivePermissions()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        Guid userId = await FindUserIdAsync(client, "admin");

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/identity/permissions/users/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await ReadValueAsync(response)).EnumerateArray()
            .Select(permission => permission.GetString())
            .Should()
            .Contain(UsersRead);
    }

    [Fact(DisplayName = "GET .../users/{id}/check should grant a held permission")]
    public async Task CheckUserPermissions_WithHeldPermission_ShouldGrant()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        Guid userId = await FindUserIdAsync(client, "admin");

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/identity/permissions/users/{userId}/check" +
            $"?permissions={UsersRead}&mode=All");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement value = await ReadValueAsync(response);

        value.GetProperty("granted").GetBoolean().Should().BeTrue();
        value.GetProperty("userId").GetGuid().Should().Be(userId);
        value.GetProperty("permissions").EnumerateArray()
            .Select(permission => permission.GetString())
            .Should()
            .Contain(UsersRead);
    }

    [Fact(DisplayName = "GET .../users/{id}/check should deny an unheld permission")]
    public async Task CheckUserPermissions_WithUnheldPermission_ShouldDeny()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        Guid userId = await FindUserIdAsync(client, "admin");

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/identity/permissions/users/{userId}/check" +
            "?permissions=identity.unknown.all.read&mode=Any");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadValueAsync(response)).GetProperty("granted").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "GET .../users/{id}/check without permissions should return 400")]
    public async Task CheckUserPermissions_WithoutPermissions_ShouldReturn400()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        Guid userId = await FindUserIdAsync(client, "admin");

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/identity/permissions/users/{userId}/check");

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.BadRequest,
            "PermissionCatalog.Permissions.Required");
    }

    [Fact(DisplayName = "GET /api/identity/permissions/roles/{id} should list role permissions")]
    public async Task GetRolePermissions_ShouldListAssignedPermissions()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        Guid roleId = await FindRoleIdAsync(client, "Admin");

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/identity/permissions/roles/{roleId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await ReadValueAsync(response)).EnumerateArray()
            .Select(permission => permission.GetString())
            .Should()
            .Contain(RolesRead);
    }

    [Fact(DisplayName = "POST /api/identity/permissions/refresh should rebuild the snapshot")]
    public async Task RefreshCatalog_ShouldReturnSnapshot()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();

        using HttpResponseMessage response = await client.PostAsync(
            "/api/identity/permissions/refresh",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement value = await ReadValueAsync(response);

        value.GetProperty("permissions").EnumerateArray()
            .Should()
            .Contain(item => item.GetProperty("identifier").GetString() == UsersRead);
        value.GetProperty("refreshedOnUtc").GetDateTimeOffset().Should()
            .BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));
    }

    [Fact(DisplayName = "The removed /api/authorization routes should return 404")]
    public async Task RemovedAuthorizationRoutes_ShouldReturn404()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/authorization/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "A token without an identity role should be forbidden from the catalog")]
    public async Task PermissionRoutes_WithoutPermission_ShouldReturn403()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateClient();

        using HttpResponseMessage read = await client.GetAsync("/api/identity/permissions");

        await AssertFailureAsync(read, HttpStatusCode.Forbidden);

        using HttpResponseMessage refresh = await client.PostAsync(
            "/api/identity/permissions/refresh",
            content: null);

        await AssertFailureAsync(refresh, HttpStatusCode.Forbidden);
    }
}
