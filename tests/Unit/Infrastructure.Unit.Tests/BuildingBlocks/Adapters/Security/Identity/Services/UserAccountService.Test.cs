using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Querying.Searching;
using Application.BuildingBlocks.Security.Authentication.Tokens.Abstractions.Refresh;
using Application.BuildingBlocks.Security.Authentication.Tokens.Domain;
using Application.BuildingBlocks.Security.Identity.Auth;
using Application.BuildingBlocks.Security.Identity.Users.Models;

using FluentEmail.Core;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Identity.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "IdentityUsers")]
public sealed class UserAccountServiceTest
{
    private const string Password = "StrongPass1!ab";
    private const string NewPassword = "AnotherPass1!ab";

    [Fact(DisplayName = "Search should filter, page, and clamp the page size")]
    public async Task Search_ShouldFilterPageAndClamp()
    {
        await using TestHost host = await TestHost.CreateAsync();
        await host.CreateAsync("alice", roles: [host.AdminRoleId]);
        await host.CreateAsync("bob");
        await host.CreateAsync("carol");

        Result<QueryPage<UserModel>> first = await host.Accounts.SearchAsync(
            new QueryParameters { Page = 1, PageSize = 2 });

        first.IsSuccess.Should().BeTrue();
        first.Value.TotalCount.Should().Be(3);
        first.Value.Page.Should().Be(1);
        first.Value.PageSize.Should().Be(2);
        first.Value.Items.Should().HaveCount(2);
        first.Value.Items[0].UserName.Should().Be("alice");
        first.Value.Items[0].Roles.Should().Equal("Admin");

        Result<QueryPage<UserModel>> second = await host.Accounts.SearchAsync(
            new QueryParameters { Page = 2, PageSize = 2 });

        second.Value.Items.Should().ContainSingle();
        second.Value.Items[0].UserName.Should().Be("carol");

        Result<QueryPage<UserModel>> filtered = await host.Accounts.SearchAsync(
            new QueryParameters { Search = "bob" });

        filtered.Value.TotalCount.Should().Be(1);
        filtered.Value.Items[0].Email.Should().Be("bob@example.com");

        Result<QueryPage<UserModel>> clamped = await host.Accounts.SearchAsync(
            new QueryParameters { Page = 0, PageSize = 500 });

        clamped.Value.Page.Should().Be(1);
        clamped.Value.PageSize.Should().Be(200);

        Result<QueryPage<UserModel>> empty = await host.Accounts.SearchAsync(
            new QueryParameters { Search = "nobody" });

        empty.Value.TotalCount.Should().Be(0);
        empty.Value.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "Search should reject every unsupported query parameter")]
    public async Task Search_ShouldRejectUnsupportedParameters()
    {
        await using TestHost host = await TestHost.CreateAsync();

        QueryParameters[] unsupported =
        [
            new() { Filter = "userName=alice" },
            new() { OrderBy = "userName" },
            new() { Search = "alice", SearchFields = "email" },
            new() { Search = "alice", SearchMode = SearchMode.Contains },
            new() { Search = "alice", SearchTermLogic = SearchTermLogic.Or },
            new() { Search = "alice", SearchCaseInsensitive = true },
            new() { Select = "userName" },
            new() { GroupBy = "isActive" },
            new() { Aggregate = "count(id)" },
        ];

        foreach (QueryParameters parameters in unsupported)
        {
            Result<QueryPage<UserModel>> result = await host.Accounts.SearchAsync(parameters);

            result.IsFailure.Should().BeTrue();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().ContainSingle(
                error => error.Code == QueryErrorCodes.Unsupported);
        }
    }

    [Fact(DisplayName = "Create should persist the user and assign roles")]
    public async Task Create_ShouldPersistAndAssignRoles()
    {
        await using TestHost host = await TestHost.CreateAsync();

        Result<UserModel> created = await host.Accounts.CreateAsync(
            TestHost.Spec("alice", roles: [host.AdminRoleId]));

        created.IsSuccess.Should().BeTrue();
        created.Value.UserName.Should().Be("alice");
        created.Value.FirstName.Should().Be("alice");
        created.Value.IsActive.Should().BeTrue();
        created.Value.Roles.Should().Equal("Admin");

        User user = (await host.Users.FindByNameAsync("alice"))!;
        user.CreatedBy.Should().Be("System");
        (await host.Users.IsInRoleAsync(user, "Admin")).Should().BeTrue();
    }

    [Fact(DisplayName = "Create should return 409 for duplicate usernames and emails")]
    public async Task Create_ShouldRejectDuplicatesWithConflict()
    {
        await using TestHost host = await TestHost.CreateAsync();
        await host.CreateAsync("alice");

        Result<UserModel> duplicateName = await host.Accounts.CreateAsync(
            TestHost.Spec("alice", "other@example.com"));

        duplicateName.StatusCode.Should().Be(409);
        duplicateName.Errors.Should().ContainSingle(
            error => error.Code == "Identity.UserName.Taken");

        Result<UserModel> duplicateEmail = await host.Accounts.CreateAsync(
            TestHost.Spec("other", "alice@example.com"));

        duplicateEmail.StatusCode.Should().Be(409);
        duplicateEmail.Errors.Should().ContainSingle(
            error => error.Code == "Identity.Email.Taken");
    }

    [Fact(DisplayName = "Create should validate the identity values and role ids")]
    public async Task Create_ShouldValidateValuesAndRoleIds()
    {
        await using TestHost host = await TestHost.CreateAsync();

        Result<UserModel> empty = await host.Accounts.CreateAsync(new CreateUserRequestModel());

        empty.IsFailure.Should().BeTrue();
        empty.Errors.Should().OnlyContain(
            error => error.Code.StartsWith("User.", StringComparison.Ordinal));

        Result<UserModel> weak = await host.Accounts.CreateAsync(new CreateUserRequestModel
        {
            UserName = "weak",
            Email = "weak@example.com",
            Password = "short",
        });

        weak.IsFailure.Should().BeTrue();
        weak.Errors.Should().ContainSingle(
            error => error.Code == "Identity.Password.TooShort");

        Result<UserModel> emptyRole = await host.Accounts.CreateAsync(
            TestHost.Spec("alice", roles: [Guid.Empty]));

        emptyRole.StatusCode.Should().Be(400);
        emptyRole.Errors.Should().ContainSingle(
            error => error.Code == "Identity.Role.Invalid");

        Result<UserModel> unknownRole = await host.Accounts.CreateAsync(
            TestHost.Spec("alice", roles: [Guid.NewGuid()]));

        unknownRole.StatusCode.Should().Be(404);
        unknownRole.Errors.Should().ContainSingle(
            error => error.Code == "Identity.Role.NotFound");
    }

    [Fact(DisplayName = "Update should replace the role set and profile fields")]
    public async Task Update_ShouldReplaceRolesAndProfile()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Result<UserModel> created = await host.CreateAsync("alice", roles: [host.AdminRoleId]);

        Result<UserModel> updated = await host.Accounts.UpdateAsync(
            created.Value.Id,
            new UpdateUserRequestModel()
            {
                Email = "alice.new@example.com",
                FirstName = "Alicia",
                LastName = "Smith",
                RoleIds = [host.EditorRoleId]
            });

        updated.IsSuccess.Should().BeTrue();
        updated.Value.Email.Should().Be("alice.new@example.com");
        updated.Value.FirstName.Should().Be("Alicia");
        updated.Value.LastName.Should().Be("Smith");
        updated.Value.EmailConfirmed.Should().BeFalse();
        updated.Value.Roles.Should().Equal("Editor");

        User user = (await host.Users.FindByIdAsync(created.Value.Id.ToString()))!;
        (await host.Users.IsInRoleAsync(user, "Admin")).Should().BeFalse();
        (await host.Users.IsInRoleAsync(user, "Editor")).Should().BeTrue();
    }

    [Fact(DisplayName = "Update should reject malformed values before persistence")]
    public async Task Update_ShouldRejectMalformedValues()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Result<UserModel> created = await host.CreateAsync("alice");

        (UpdateUserRequestModel Spec, string Code)[] cases =
        [
            (new UpdateUserRequestModel()
            {
                Email = "not-an-email"
            }, "Identity.Email.Invalid"),

            (new UpdateUserRequestModel()
            {
                FirstName = new string('a', 51)
            }, "Identity.User.FirstName.TooLong"),

            (new UpdateUserRequestModel()
            {
                LastName = new string('a', 51)
            }, "Identity.User.LastName.TooLong"),

            (new UpdateUserRequestModel()
            {
                RoleIds = [Guid.Empty]
            }, "Identity.Role.Invalid"),
        ];

        foreach ((UpdateUserRequestModel spec, string code) in cases)
        {
            Result<UserModel> result = await host.Accounts.UpdateAsync(created.Value.Id, spec);

            result.IsFailure.Should().BeTrue();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().ContainSingle(error => error.Code == code);
        }

        Result<UserModel> unknownRole = await host.Accounts.UpdateAsync(
            created.Value.Id,
            new UpdateUserRequestModel()
            {
                RoleIds = [Guid.NewGuid()]
            });

        unknownRole.StatusCode.Should().Be(404);
        unknownRole.Errors.Should().ContainSingle(
            error => error.Code == "Identity.Role.NotFound");
    }

    [Fact(DisplayName = "Update should return 409 when the email belongs to another user")]
    public async Task Update_ShouldRejectDuplicateEmailWithConflict()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Result<UserModel> alice = await host.CreateAsync("alice");
        await host.CreateAsync("bob");

        Result<UserModel> result = await host.Accounts.UpdateAsync(
            alice.Value.Id,
            new UpdateUserRequestModel()
            {
                Email = "bob@example.com"
            });

        result.StatusCode.Should().Be(409);
        result.Errors.Should().ContainSingle(
            error => error.Code == "Identity.Email.Taken");
    }

    [Fact(DisplayName = "Activate and deactivate should toggle the account flag")]
    public async Task SetActive_ShouldToggleTheFlag()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Result<UserModel> created = await host.CreateAsync("alice");

        Result<UserModel> deactivated = await host.Accounts.SetActiveAsync(
            created.Value.Id,
            active: false);

        deactivated.IsSuccess.Should().BeTrue();
        deactivated.Value.IsActive.Should().BeFalse();

        Result<UserModel> activated = await host.Accounts.SetActiveAsync(
            created.Value.Id,
            active: true);

        activated.Value.IsActive.Should().BeTrue();
    }

    [Fact(DisplayName = "Lock and unlock should change the lockout state")]
    public async Task SetLocked_ShouldChangeTheLockoutState()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Result<UserModel> created = await host.CreateAsync("alice");
        User existing = (await host.Users.FindByIdAsync(created.Value.Id.ToString()))!;
        await host.Users.SetLockoutEnabledAsync(existing, false);

        Result<UserModel> locked = await host.Accounts.SetLockedAsync(
            created.Value.Id,
            locked: true);

        locked.IsSuccess.Should().BeTrue();
        locked.Value.IsLockedOut.Should().BeTrue();
        locked.Value.LockoutEndUtc.Should().NotBeNull();

        Result<UserModel> unlocked = await host.Accounts.SetLockedAsync(
            created.Value.Id,
            locked: false);

        unlocked.Value.IsLockedOut.Should().BeFalse();
        unlocked.Value.LockoutEndUtc.Should().BeNull();
    }

    [Fact(DisplayName = "Confirm email should be idempotent")]
    public async Task ConfirmEmail_ShouldBeIdempotent()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Result<UserModel> created = await host.CreateAsync("alice");

        Result first = await host.Accounts.ConfirmEmailAsync(created.Value.Id);
        Result second = await host.Accounts.ConfirmEmailAsync(created.Value.Id);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        (await host.Users.FindByIdAsync(created.Value.Id.ToString()))!
            .EmailConfirmed.Should().BeTrue();
    }

    [Fact(DisplayName = "Reset password should replace the stored password")]
    public async Task ResetPassword_ShouldReplaceThePassword()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Result<UserModel> created = await host.CreateAsync("alice");

        Result result = await host.Accounts.ResetPasswordAsync(created.Value.Id, NewPassword);

        result.IsSuccess.Should().BeTrue();
        User user = (await host.Users.FindByIdAsync(created.Value.Id.ToString()))!;
        (await host.Users.CheckPasswordAsync(user, NewPassword)).Should().BeTrue();
        (await host.Users.CheckPasswordAsync(user, Password)).Should().BeFalse();

        Result weak = await host.Accounts.ResetPasswordAsync(created.Value.Id, "short");

        weak.IsFailure.Should().BeTrue();
        weak.Errors.Should().ContainSingle(
            error => error.Code == "Identity.Password.TooShort");
    }

    [Fact(DisplayName = "Revoke sessions should rotate the stamp and revoke refresh tokens")]
    public async Task RevokeSessions_ShouldRotateStampAndRevokeTokens()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Result<UserModel> created = await host.CreateAsync("alice");
        User before = (await host.Users.FindByIdAsync(created.Value.Id.ToString()))!;
        string stamp = before.SecurityStamp!;

        Result result = await host.Accounts.RevokeSessionsAsync(created.Value.Id);

        result.IsSuccess.Should().BeTrue();
        User after = (await host.Users.FindByIdAsync(created.Value.Id.ToString()))!;
        after.SecurityStamp.Should().NotBe(stamp);
        host.RefreshTokens.Verify(
            service => service.RevokeAllAsync(
                created.Value.Id.ToString(),
                It.IsAny<DateTimeOffset>(),
                RefreshTokenRevocationReason.UserLogoutAll,
                It.IsAny<CancellationToken>()),
            Times.Once);

        host.RefreshTokens
            .Setup(service => service.RevokeAllAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<RefreshTokenRevocationReason>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Fail(Error.Unexpected(
                "Refresh.Revoke.Failed",
                "The tokens could not be revoked.")));

        Result failed = await host.Accounts.RevokeSessionsAsync(created.Value.Id);

        failed.IsFailure.Should().BeTrue();
        failed.Errors.Should().ContainSingle(
            error => error.Code == "Refresh.Revoke.Failed");
    }

    [Fact(DisplayName = "Role assignment and removal should be idempotent")]
    public async Task RoleAssignment_ShouldBeIdempotent()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Result<UserModel> created = await host.CreateAsync("alice");

        Result<UserModel> assigned = await host.Accounts.AssignRoleAsync(
            created.Value.Id,
            host.AdminRoleId);
        Result<UserModel> assignedAgain = await host.Accounts.AssignRoleAsync(
            created.Value.Id,
            host.AdminRoleId);

        assigned.Value.Roles.Should().Equal("Admin");
        assignedAgain.Value.Roles.Should().Equal("Admin");

        Result<UserModel> removed = await host.Accounts.RemoveRoleAsync(
            created.Value.Id,
            host.AdminRoleId);
        Result<UserModel> removedAgain = await host.Accounts.RemoveRoleAsync(
            created.Value.Id,
            host.AdminRoleId);

        removed.Value.Roles.Should().BeEmpty();
        removedAgain.Value.Roles.Should().BeEmpty();

        Result<UserModel> missingAssign = await host.Accounts.AssignRoleAsync(
            created.Value.Id,
            Guid.NewGuid());

        missingAssign.StatusCode.Should().Be(404);
        missingAssign.Errors.Should().ContainSingle(
            error => error.Code == "Identity.Role.NotFound");

        Result<UserModel> missingRemove = await host.Accounts.RemoveRoleAsync(
            created.Value.Id,
            Guid.NewGuid());

        missingRemove.StatusCode.Should().Be(404);
        missingRemove.Errors.Should().ContainSingle(
            error => error.Code == "Identity.Role.NotFound");
    }

    [Fact(DisplayName = "Delete should remove the user account")]
    public async Task Delete_ShouldRemoveTheUser()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Result<UserModel> created = await host.CreateAsync("alice");

        Result deleted = await host.Accounts.DeleteAsync(created.Value.Id);

        deleted.IsSuccess.Should().BeTrue();
        (await host.Accounts.GetAsync(created.Value.Id)).StatusCode.Should().Be(404);
    }

    [Fact(DisplayName = "Every operation should return 404 for an unknown user")]
    public async Task Operations_WhenUserIsMissing_ShouldReturnNotFound()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Guid missing = Guid.NewGuid();

        Result[] results =
        [
            await host.Accounts.GetAsync(missing),
            await host.Accounts.UpdateAsync(missing, new UpdateUserRequestModel()),
            await host.Accounts.DeleteAsync(missing),
            await host.Accounts.SetActiveAsync(missing, true),
            await host.Accounts.AssignRoleAsync(missing, host.AdminRoleId),
            await host.Accounts.RemoveRoleAsync(missing, host.AdminRoleId),
            await host.Accounts.ResetPasswordAsync(missing, NewPassword),
            await host.Accounts.ConfirmEmailAsync(missing),
            await host.Accounts.SetLockedAsync(missing, true),
            await host.Accounts.RevokeSessionsAsync(missing),
        ];

        results.Should().OnlyContain(result =>
            result.IsFailure &&
            result.StatusCode == 404 &&
            result.Errors.All(error => error.Code == "Identity.User.NotFound"));
    }

    private sealed class TestHost : IAsyncDisposable
    {
        private TestHost(
            ServiceProvider provider,
            IServiceScope scope,
            UserAccountService accounts,
            UserManager<User> users,
            RoleManager<Role> roles,
            Mock<IRefreshTokenService> refreshTokens,
            Guid adminRoleId,
            Guid editorRoleId)
        {
            Provider = provider;
            Scope = scope;
            Accounts = accounts;
            Users = users;
            Roles = roles;
            RefreshTokens = refreshTokens;
            AdminRoleId = adminRoleId;
            EditorRoleId = editorRoleId;
        }

        public ServiceProvider Provider { get; }

        public IServiceScope Scope { get; }

        public UserAccountService Accounts { get; }

        public UserManager<User> Users { get; }

        public RoleManager<Role> Roles { get; }

        public Mock<IRefreshTokenService> RefreshTokens { get; }

        public Guid AdminRoleId { get; }

        public Guid EditorRoleId { get; }

        public static CreateUserRequestModel Spec(
            string userName,
            string? email = null,
            Guid[]? roles = null) => new()
            {
                UserName = userName,
                Email = email ?? $"{userName}@example.com",
                Password = Password,
                FirstName = userName,
                LastName = "Tester",
                EmailConfirmed = false,
                RoleIds = roles ?? [],
            };

        public static async Task<TestHost> CreateAsync()
        {
            var refreshTokens = new Mock<IRefreshTokenService>();
            refreshTokens
                .Setup(service => service.RevokeAllAsync(
                    It.IsAny<string>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<RefreshTokenRevocationReason>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<int>.Ok(1));

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase($"UserAccountService_{Guid.NewGuid():N}"));
            services.AddIdentityServices();
            services.AddSingleton(refreshTokens.Object);
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<UserAccountService>();

            ServiceProvider provider = services.BuildServiceProvider();
            IServiceScope scope = provider.CreateScope();
            RoleManager<Role> roles = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
            Role admin = Role.Create("Admin").Value;
            Role editor = Role.Create("Editor").Value;
            await roles.CreateAsync(admin);
            await roles.CreateAsync(editor);

            return new TestHost(
                provider,
                scope,
                scope.ServiceProvider.GetRequiredService<UserAccountService>(),
                scope.ServiceProvider.GetRequiredService<UserManager<User>>(),
                roles,
                refreshTokens,
                admin.Id,
                editor.Id);
        }

        public Task<Result<UserModel>> CreateAsync(
            string userName,
            string? email = null,
            Guid[]? roles = null) =>
            Accounts.CreateAsync(Spec(userName, email, roles));

        public ValueTask DisposeAsync()
        {
            Scope.Dispose();
            return Provider.DisposeAsync();
        }
    }
}
