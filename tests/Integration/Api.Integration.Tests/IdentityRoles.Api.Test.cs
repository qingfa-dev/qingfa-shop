using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Integration.Tests;

/// <summary>Exercises the admin role routes against the real Identity stores.</summary>
[Collection(ResultContractStackCollection.Name)]
public sealed class IdentityRolesApiTest : IdentityStackTestBase
{
    private const string Permission = "identity.users.all.read";
    private const string SecondPermission = "identity.roles.all.read";

    public IdentityRolesApiTest(ResultContractStackFixture fixture)
        : base(fixture)
    {
    }

    [Fact(DisplayName = "GET /api/identity/roles should return the seeded roles")]
    public async Task SearchRoles_ShouldReturnSeededRoles()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();

        using HttpResponseMessage response = await client.GetAsync("/api/identity/roles?search=admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement value = await ReadValueAsync(response);
        value.GetProperty("items").EnumerateArray()
            .Should()
            .Contain(item => item.GetProperty("name").GetString() == "Admin");
    }

    [Fact(DisplayName = "GET /api/identity/roles with an unsupported filter should return 400")]
    public async Task SearchRoles_WithUnsupportedFilter_ShouldReturn400()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/identity/roles?filter=name%20eq%20Admin");

        await AssertFailureCodeAsync(response, HttpStatusCode.BadRequest, "Query.Unsupported");
    }

    [Fact(DisplayName = "GET /api/identity/roles/{id} with an unknown id should return 404")]
    public async Task GetRole_WhenMissing_ShouldReturn404()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/identity/roles/{Guid.NewGuid()}");

        await AssertFailureCodeAsync(response, HttpStatusCode.NotFound, "Role.NotFound");
    }

    [Fact(DisplayName = "POST /api/identity/roles should return 201, Location, and permissions")]
    public async Task CreateRole_ShouldReturnCreatedRole()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        string name = UniqueRoleName();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/identity/roles",
            new
            {
                name,
                description = "Created by the API test",
                permissions = new[] { Permission }
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        JsonElement value = await ReadValueAsync(response);
        Guid id = value.GetProperty("id").GetGuid();

        value.GetProperty("name").GetString().Should().Be(name);
        value.GetProperty("permissions").EnumerateArray()
            .Select(permission => permission.GetString())
            .Should()
            .Contain(Permission);

        response.Headers.Location!.ToString().Should().Be($"/api/identity/roles/{id}");

        using HttpResponseMessage fetched = await client.GetAsync($"/api/identity/roles/{id}");

        fetched.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadValueAsync(fetched)).GetProperty("name").GetString().Should().Be(name);
    }

    [Fact(DisplayName = "POST /api/identity/roles with a duplicate name should return 409")]
    public async Task CreateRole_WithDuplicateName_ShouldReturn409()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        string name = UniqueRoleName();
        await CreateRoleAsync(client, name);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/identity/roles",
            new { name, description = "Duplicate" });

        await AssertFailureCodeAsync(response, HttpStatusCode.Conflict, "Role.AlreadyExists");
    }

    [Fact(DisplayName = "POST /api/identity/roles with a malformed permission should return 422")]
    public async Task CreateRole_WithInvalidPermission_ShouldReturn422()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/identity/roles",
            new
            {
                name = UniqueRoleName(),
                permissions = new[] { "not-a-canonical-permission" }
            });

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.UnprocessableEntity,
            "PermissionStore.Identifier.Invalid");
    }

    [Fact(DisplayName = "PUT /api/identity/roles/{id} should update name and description")]
    public async Task UpdateRole_ShouldReturnUpdatedRole()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        JsonElement created = await CreateRoleAsync(client);
        Guid id = created.GetProperty("id").GetGuid();

        using HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/identity/roles/{id}",
            new { name = $"{created.GetProperty("name").GetString()}-updated", description = "Updated" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement value = await ReadValueAsync(response);

        value.GetProperty("name").GetString().Should().EndWith("-updated");
        value.GetProperty("description").GetString().Should().Be("Updated");
    }

    [Fact(DisplayName = "PUT /api/identity/roles/{id} with an unknown id should return 404")]
    public async Task UpdateRole_WhenMissing_ShouldReturn404()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();

        using HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/identity/roles/{Guid.NewGuid()}",
            new { name = UniqueRoleName() });

        await AssertFailureCodeAsync(response, HttpStatusCode.NotFound, "Role.NotFound");
    }

    [Fact(DisplayName = "GET /api/identity/roles/{id}/permissions should list assigned permissions")]
    public async Task GetRolePermissions_ShouldReturnAssignedPermissions()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        JsonElement created = await CreateRoleAsync(client, permissions: [Permission]);
        Guid id = created.GetProperty("id").GetGuid();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/identity/roles/{id}/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await ReadValueAsync(response)).EnumerateArray()
            .Select(permission => permission.GetString())
            .Should()
            .Contain(Permission);
    }

    [Fact(DisplayName = "POST/DELETE /api/identity/roles/{id}/permissions should mutate the set")]
    public async Task AssignAndRemovePermission_ShouldUpdateTheSet()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        JsonElement created = await CreateRoleAsync(client);
        Guid id = created.GetProperty("id").GetGuid();

        using HttpResponseMessage assigned = await client.PostAsJsonAsync(
            $"/api/identity/roles/{id}/permissions",
            new { permission = SecondPermission });

        assigned.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadValueAsync(assigned)).GetProperty("permissions").EnumerateArray()
            .Select(permission => permission.GetString())
            .Should()
            .Contain(SecondPermission);

        using HttpResponseMessage removed = await client.DeleteAsync(
            $"/api/identity/roles/{id}/permissions/{Uri.EscapeDataString(SecondPermission)}");

        removed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadValueAsync(removed)).GetProperty("permissions").EnumerateArray()
            .Select(permission => permission.GetString())
            .Should()
            .NotContain(SecondPermission);
    }

    [Fact(DisplayName = "POST permissions with a malformed identifier should return 422")]
    public async Task AssignPermission_WithInvalidIdentifier_ShouldReturn422()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        JsonElement created = await CreateRoleAsync(client);
        Guid id = created.GetProperty("id").GetGuid();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/identity/roles/{id}/permissions",
            new { permission = "invalid" });

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.UnprocessableEntity,
            "PermissionStore.Identifier.Invalid");
    }

    [Fact(DisplayName = "DELETE /api/identity/roles/{id} should delete a custom role")]
    public async Task DeleteRole_ShouldReturnNoContent()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        JsonElement created = await CreateRoleAsync(client);
        Guid id = created.GetProperty("id").GetGuid();

        using HttpResponseMessage deleted = await client.DeleteAsync($"/api/identity/roles/{id}");

        await AssertNoContentAsync(deleted);

        using HttpResponseMessage fetched = await client.GetAsync($"/api/identity/roles/{id}");

        await AssertFailureCodeAsync(fetched, HttpStatusCode.NotFound, "Role.NotFound");
    }

    [Fact(DisplayName = "DELETE /api/identity/roles/{id} should protect a seeded system role")]
    public async Task DeleteRole_WhenSystemRole_ShouldReturn403()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        Guid roleId = await FindRoleIdAsync(client, "Admin");

        using HttpResponseMessage response = await client.DeleteAsync($"/api/identity/roles/{roleId}");

        await AssertFailureCodeAsync(response, HttpStatusCode.Forbidden, "Role.System.Protected");
    }

    [Fact(DisplayName = "DELETE /api/identity/roles/{id} should reject an assigned role")]
    public async Task DeleteRole_WhenAssigned_ShouldReturn422()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        JsonElement role = await CreateRoleAsync(client);
        Guid roleId = role.GetProperty("id").GetGuid();
        string userName = UniqueUserName();

        using HttpResponseMessage user = await client.PostAsJsonAsync(
            "/api/identity/users",
            new
            {
                userName,
                email = UniqueEmail(userName),
                firstName = "Assigned",
                password = "Password@123",
                roleIds = new[] { roleId }
            });

        user.StatusCode.Should().Be(HttpStatusCode.Created);

        using HttpResponseMessage response = await client.DeleteAsync($"/api/identity/roles/{roleId}");

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.UnprocessableEntity,
            "Role.CannotDeleteRoleWithUsers");
    }

    [Fact(DisplayName = "DELETE /api/identity/roles/{id} with an unknown id should return 404")]
    public async Task DeleteRole_WhenMissing_ShouldReturn404()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();

        using HttpResponseMessage response = await client.DeleteAsync(
            $"/api/identity/roles/{Guid.NewGuid()}");

        await AssertFailureCodeAsync(response, HttpStatusCode.NotFound, "Role.NotFound");
    }

    [Fact(DisplayName = "A token without an identity role should be forbidden from role admin")]
    public async Task RoleRoutes_WithoutPermission_ShouldReturn403()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateClient();

        using HttpResponseMessage read = await client.GetAsync("/api/identity/roles");

        await AssertFailureAsync(read, HttpStatusCode.Forbidden);

        using HttpResponseMessage create = await client.PostAsJsonAsync(
            "/api/identity/roles",
            new { name = UniqueRoleName() });

        await AssertFailureAsync(create, HttpStatusCode.Forbidden);
    }

    private static async Task<JsonElement> CreateRoleAsync(
        HttpClient client,
        string? name = null,
        IReadOnlyList<string>? permissions = null)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/identity/roles",
            new
            {
                name = name ?? UniqueRoleName(),
                description = "Test role",
                permissions = permissions ?? []
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return await ReadValueAsync(response);
    }

    private static string UniqueRoleName() => $"role-{Guid.NewGuid():N}";
}
