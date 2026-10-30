using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Integration.Tests;

/// <summary>Exercises the admin user routes against the real Identity stores.</summary>
[Collection(ResultContractStackCollection.Name)]
public sealed class IdentityUsersApiTest : IdentityStackTestBase
{
    private const string Password = "Password@123";
    private const string NewPassword = "NewPassword@123";

    public IdentityUsersApiTest(ResultContractStackFixture fixture)
        : base(fixture)
    {
    }

    [Fact(DisplayName = "GET /api/identity/users should return the seeded users")]
    public async Task SearchUsers_ShouldReturnSeededUsers()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/identity/users?search=admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement value = await ReadValueAsync(response);
        value.GetProperty("items").EnumerateArray()
            .Should()
            .Contain(item => item.GetProperty("userName").GetString() == "admin");
    }

    [Fact(DisplayName = "GET /api/identity/users/{id} with an unknown id should return 404")]
    public async Task GetUser_WhenMissing_ShouldReturn404()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/identity/users/{Guid.NewGuid()}");

        await AssertFailureCodeAsync(response, HttpStatusCode.NotFound, "Identity.User.NotFound");
    }

    [Fact(DisplayName = "POST /api/identity/users should return 201, Location, and the user")]
    public async Task CreateUser_ShouldReturnCreatedUser()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        string userName = UniqueUserName();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/identity/users",
            new
            {
                userName,
                email = UniqueEmail(userName),
                firstName = "Created",
                lastName = "User",
                password = Password,
                emailConfirmed = false,
                roleIds = Array.Empty<Guid>()
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        JsonElement value = await ReadValueAsync(response);
        Guid id = value.GetProperty("id").GetGuid();

        value.GetProperty("userName").GetString().Should().Be(userName);
        value.GetProperty("firstName").GetString().Should().Be("Created");
        value.GetProperty("isActive").GetBoolean().Should().BeTrue();
        response.Headers.Location!.ToString().Should().Be($"/api/identity/users/{id}");

        using HttpResponseMessage fetched = await client.GetAsync($"/api/identity/users/{id}");

        fetched.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadValueAsync(fetched)).GetProperty("email").GetString()
            .Should().Be($"{userName}@example.com");
    }

    [Fact(DisplayName = "POST /api/identity/users with a duplicate email should return 409")]
    public async Task CreateUser_WithDuplicateEmail_ShouldReturn409()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        JsonElement created = await CreateUserAsync(client);
        string email = created.GetProperty("email").GetString()!;
        string otherUserName = UniqueUserName();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/identity/users",
            new
            {
                userName = otherUserName,
                email,
                firstName = "Duplicate",
                password = Password
            });

        await AssertFailureCodeAsync(response, HttpStatusCode.Conflict, "Identity.Email.Taken");
    }

    [Fact(DisplayName = "POST /api/identity/users with a weak password should return 400")]
    public async Task CreateUser_WithWeakPassword_ShouldReturn400()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        string userName = UniqueUserName();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/identity/users",
            new
            {
                userName,
                email = UniqueEmail(userName),
                firstName = "Weak",
                password = "short"
            });

        await AssertFailureAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /api/identity/users with an empty role id should return 400")]
    public async Task CreateUser_WithEmptyRoleId_ShouldReturn400()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        string userName = UniqueUserName();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/identity/users",
            new
            {
                userName,
                email = UniqueEmail(userName),
                firstName = "NoRole",
                password = Password,
                roleIds = new[] { Guid.Empty }
            });

        await AssertFailureCodeAsync(response, HttpStatusCode.BadRequest, "Identity.Role.Invalid");
    }

    [Fact(DisplayName = "POST /api/identity/users with an unknown role should return 404")]
    public async Task CreateUser_WithUnknownRole_ShouldReturn404()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        string userName = UniqueUserName();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/identity/users",
            new
            {
                userName,
                email = UniqueEmail(userName),
                firstName = "NoRole",
                password = Password,
                roleIds = new[] { Guid.NewGuid() }
            });

        await AssertFailureCodeAsync(response, HttpStatusCode.NotFound, "Identity.Role.NotFound");
    }

    [Fact(DisplayName = "PUT /api/identity/users/{id} should update the profile")]
    public async Task UpdateUser_ShouldReturnUpdatedProfile()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        JsonElement created = await CreateUserAsync(client);
        Guid id = created.GetProperty("id").GetGuid();

        using HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/identity/users/{id}",
            new
            {
                firstName = "Updated",
                lastName = "Name",
                email = $"updated-{Guid.NewGuid():N}@example.com"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement value = await ReadValueAsync(response);
        value.GetProperty("firstName").GetString().Should().Be("Updated");
        value.GetProperty("lastName").GetString().Should().Be("Name");
        value.GetProperty("emailConfirmed").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "PUT /api/identity/users/{id} should reject a malformed email")]
    public async Task UpdateUser_WithMalformedEmail_ShouldReturn400()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        JsonElement created = await CreateUserAsync(client);
        Guid id = created.GetProperty("id").GetGuid();

        using HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/identity/users/{id}",
            new { email = "not-an-email" });

        await AssertFailureCodeAsync(response, HttpStatusCode.BadRequest, "Identity.Email.Invalid");
    }

    [Fact(DisplayName = "PUT /api/identity/users/{id} with an unknown id should return 404")]
    public async Task UpdateUser_WhenMissing_ShouldReturn404()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();

        using HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/identity/users/{Guid.NewGuid()}",
            new { firstName = "Missing" });

        await AssertFailureCodeAsync(response, HttpStatusCode.NotFound, "Identity.User.NotFound");
    }

    [Fact(DisplayName = "PUT /api/identity/users/{id} should replace the role set")]
    public async Task UpdateUser_WithRoleIds_ShouldReplaceRoles()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        Guid userRoleId = await FindRoleIdAsync(client, "User");
        JsonElement created = await CreateUserAsync(client, roleIds: [userRoleId]);
        Guid id = created.GetProperty("id").GetGuid();

        using HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/identity/users/{id}",
            new { roleIds = Array.Empty<Guid>() });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadValueAsync(response)).GetProperty("roles").GetArrayLength().Should().Be(0);
    }

    [Fact(DisplayName = "POST /api/identity/users/{id}/activate and deactivate should toggle state")]
    public async Task ActivateAndDeactivate_ShouldToggleIsActive()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        JsonElement created = await CreateUserAsync(client);
        Guid id = created.GetProperty("id").GetGuid();

        using HttpResponseMessage deactivated = await client.PostAsync(
            $"/api/identity/users/{id}/deactivate",
            content: null);

        deactivated.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadValueAsync(deactivated)).GetProperty("isActive").GetBoolean().Should().BeFalse();

        using HttpResponseMessage activated = await client.PostAsync(
            $"/api/identity/users/{id}/activate",
            content: null);

        activated.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadValueAsync(activated)).GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "POST activate or deactivate with an unknown id should return 404")]
    public async Task ActivateOrDeactivate_WhenMissing_ShouldReturn404()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        Guid id = Guid.NewGuid();

        using HttpResponseMessage deactivate = await client.PostAsync(
            $"/api/identity/users/{id}/deactivate",
            content: null);

        await AssertFailureCodeAsync(deactivate, HttpStatusCode.NotFound, "Identity.User.NotFound");

        using HttpResponseMessage activate = await client.PostAsync(
            $"/api/identity/users/{id}/activate",
            content: null);

        await AssertFailureCodeAsync(activate, HttpStatusCode.NotFound, "Identity.User.NotFound");
    }

    [Fact(DisplayName = "POST /api/identity/users/{id}/lock and unlock should toggle lockout")]
    public async Task LockAndUnlock_ShouldToggleLockout()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        JsonElement created = await CreateUserAsync(client);
        Guid id = created.GetProperty("id").GetGuid();

        using HttpResponseMessage locked = await client.PostAsync(
            $"/api/identity/users/{id}/lock",
            content: null);

        locked.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadValueAsync(locked)).GetProperty("isLockedOut").GetBoolean().Should().BeTrue();

        using HttpResponseMessage unlocked = await client.PostAsync(
            $"/api/identity/users/{id}/unlock",
            content: null);

        unlocked.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadValueAsync(unlocked)).GetProperty("isLockedOut").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "POST lock or unlock with an unknown id should return 404")]
    public async Task LockOrUnlock_WhenMissing_ShouldReturn404()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        Guid id = Guid.NewGuid();

        using HttpResponseMessage lockResponse = await client.PostAsync(
            $"/api/identity/users/{id}/lock",
            content: null);

        await AssertFailureCodeAsync(lockResponse, HttpStatusCode.NotFound, "Identity.User.NotFound");

        using HttpResponseMessage unlockResponse = await client.PostAsync(
            $"/api/identity/users/{id}/unlock",
            content: null);

        await AssertFailureCodeAsync(unlockResponse, HttpStatusCode.NotFound, "Identity.User.NotFound");
    }

    [Fact(DisplayName = "POST /api/identity/users/{id}/reset-password should change the password")]
    public async Task ResetPassword_ShouldAllowLoginWithNewPassword()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        string userName = UniqueUserName();
        JsonElement created = await CreateUserAsync(client, userName: userName);
        Guid id = created.GetProperty("id").GetGuid();

        using HttpResponseMessage reset = await client.PostAsJsonAsync(
            $"/api/identity/users/{id}/reset-password",
            new { newPassword = NewPassword });

        await AssertNoContentAsync(reset);

        using HttpClient anonymous = CreateClient();
        JsonElement tokens = await LoginAsync(anonymous, userName, NewPassword);

        tokens.GetProperty("user").GetProperty("id").GetGuid().Should().Be(id);
    }

    [Fact(DisplayName = "POST /api/identity/users/{id}/reset-password with a weak password should fail")]
    public async Task ResetPassword_WithWeakPassword_ShouldReturn400()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        JsonElement created = await CreateUserAsync(client);
        Guid id = created.GetProperty("id").GetGuid();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/identity/users/{id}/reset-password",
            new { newPassword = "short" });

        await AssertFailureAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /api/identity/users/{id}/confirm-email should confirm the address")]
    public async Task ConfirmEmail_ShouldMarkTheAddressConfirmed()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        JsonElement created = await CreateUserAsync(client, emailConfirmed: false);
        Guid id = created.GetProperty("id").GetGuid();

        using HttpResponseMessage confirmed = await client.PostAsync(
            $"/api/identity/users/{id}/confirm-email",
            content: null);

        await AssertNoContentAsync(confirmed);

        using HttpResponseMessage fetched = await client.GetAsync($"/api/identity/users/{id}");

        (await ReadValueAsync(fetched)).GetProperty("emailConfirmed").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "POST /api/identity/users/{id}/revoke-sessions should revoke refresh tokens")]
    public async Task RevokeSessions_ShouldInvalidateRefreshTokens()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        string userName = UniqueUserName();
        JsonElement created = await CreateUserAsync(client, userName: userName);
        Guid id = created.GetProperty("id").GetGuid();

        using HttpClient anonymous = CreateClient();
        JsonElement tokens = await LoginAsync(anonymous, userName, Password);
        string refreshToken = tokens.GetProperty("refreshToken").GetString()!;

        using HttpResponseMessage revoked = await client.PostAsync(
            $"/api/identity/users/{id}/revoke-sessions",
            content: null);

        await AssertNoContentAsync(revoked);

        using HttpResponseMessage refresh = await anonymous.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken });

        await AssertFailureAsync(refresh, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST/DELETE /api/identity/users/{id}/roles should assign and remove a role")]
    public async Task AssignAndRemoveRole_ShouldUpdateTheRoleList()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        Guid roleId = await FindRoleIdAsync(client, "User");
        JsonElement created = await CreateUserAsync(client);
        Guid id = created.GetProperty("id").GetGuid();

        using HttpResponseMessage assigned = await client.PostAsync(
            $"/api/identity/users/{id}/roles/{roleId}",
            content: null);

        assigned.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadValueAsync(assigned)).GetProperty("roles").EnumerateArray()
            .Should()
            .Contain(role => role.GetString() == "User");

        using HttpResponseMessage roles = await client.GetAsync(
            $"/api/identity/users/{id}/roles");

        (await ReadValueAsync(roles)).EnumerateArray()
            .Should()
            .Contain(role => role.GetString() == "User");

        using HttpResponseMessage removed = await client.DeleteAsync(
            $"/api/identity/users/{id}/roles/{roleId}");

        removed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadValueAsync(removed)).GetProperty("roles").GetArrayLength().Should().Be(0);
    }

    [Fact(DisplayName = "POST /api/identity/users/{id}/roles/{roleId} with unknown ids should 404")]
    public async Task AssignRole_WithUnknownIds_ShouldReturn404()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        JsonElement created = await CreateUserAsync(client);
        Guid id = created.GetProperty("id").GetGuid();

        using HttpResponseMessage unknownUser = await client.PostAsync(
            $"/api/identity/users/{Guid.NewGuid()}/roles/{Guid.NewGuid()}",
            content: null);

        await AssertFailureCodeAsync(unknownUser, HttpStatusCode.NotFound, "Identity.User.NotFound");

        using HttpResponseMessage unknownRole = await client.PostAsync(
            $"/api/identity/users/{id}/roles/{Guid.NewGuid()}",
            content: null);

        await AssertFailureCodeAsync(unknownRole, HttpStatusCode.NotFound, "Identity.Role.NotFound");
    }

    [Fact(DisplayName = "GET /api/identity/users/{id}/roles with an unknown id should return 404")]
    public async Task GetUserRoles_WhenMissing_ShouldReturn404()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/identity/users/{Guid.NewGuid()}/roles");

        await AssertFailureCodeAsync(response, HttpStatusCode.NotFound, "Identity.User.NotFound");
    }

    [Fact(DisplayName = "DELETE /api/identity/users/{id} should delete the account")]
    public async Task DeleteUser_ShouldReturnNoContentAndHideTheUser()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        JsonElement created = await CreateUserAsync(client);
        Guid id = created.GetProperty("id").GetGuid();

        using HttpResponseMessage deleted = await client.DeleteAsync($"/api/identity/users/{id}");

        await AssertNoContentAsync(deleted);

        using HttpResponseMessage fetched = await client.GetAsync($"/api/identity/users/{id}");

        await AssertFailureCodeAsync(fetched, HttpStatusCode.NotFound, "Identity.User.NotFound");
    }

    [Fact(DisplayName = "DELETE /api/identity/users/{id} with an unknown id should return 404")]
    public async Task DeleteUser_WhenMissing_ShouldReturn404()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();

        using HttpResponseMessage response = await client.DeleteAsync(
            $"/api/identity/users/{Guid.NewGuid()}");

        await AssertFailureCodeAsync(response, HttpStatusCode.NotFound, "Identity.User.NotFound");
    }

    [Fact(DisplayName = "A token without an identity role should be forbidden from user admin")]
    public async Task UserRoutes_WithoutPermission_ShouldReturn403()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateClient();
        string userName = UniqueUserName();

        using HttpResponseMessage read = await client.GetAsync("/api/identity/users");

        await AssertFailureAsync(read, HttpStatusCode.Forbidden);

        using HttpResponseMessage create = await client.PostAsJsonAsync(
            "/api/identity/users",
            new
            {
                userName,
                email = UniqueEmail(userName),
                firstName = "Denied",
                password = Password
            });

        await AssertFailureAsync(create, HttpStatusCode.Forbidden);
    }

    private static async Task<JsonElement> CreateUserAsync(
        HttpClient client,
        string? userName = null,
        bool emailConfirmed = false,
        IReadOnlyList<Guid>? roleIds = null)
    {
        userName ??= UniqueUserName();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/identity/users",
            new
            {
                userName,
                email = UniqueEmail(userName),
                firstName = "Test",
                lastName = "User",
                password = Password,
                emailConfirmed,
                roleIds = roleIds ?? []
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return await ReadValueAsync(response);
    }
}
