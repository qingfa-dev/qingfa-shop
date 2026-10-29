using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Application.Common.Caching;
using Application.Common.Security.Authorization;
using Application.Common.Security.Authorization.Permissions.Services;
using Application.Common.Security.Authorization.Permissions.Store;
using Application.Common.Security.Authorization.Policies;
using Application.Common.Security.Authorization.Registry;

using Infrastructure.Security.Authorization;

namespace Infrastructure.Unit.Tests.Security.Authorization;

[Trait("Category", "Unit")]
[Trait("Module", "Authorization")]
[Trait("Feature", "AuthorizationPipeline")]
public sealed class AuthorizationPipelineTests
{
    [Fact(DisplayName = "Authorization pipeline: should grant a canonical permission resolved from the store")]
    public async Task AuthorizeAsync_ShouldGrant_WhenStoreHasDirectPermission()
    {
        Guid userId = Guid.NewGuid();
        string permission = PermissionContext.Todo.Items.Read.Identifier;
        var store = new FakePermissionStore();
        store.DirectPermissions[userId] = [permission];

        AuthorizationResult result = await AuthorizeAsync(
            store,
            CreatePrincipal(userId),
            permission);

        result.Succeeded.Should().BeTrue();
    }

    [Fact(DisplayName = "Authorization pipeline: should grant a permission inherited from a role")]
    public async Task AuthorizeAsync_ShouldGrant_WhenRoleHasPermission()
    {
        Guid userId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();
        string permission = PermissionContext.Identity.Users.Manage.Identifier;
        var store = new FakePermissionStore();
        store.RoleIds[userId] = [roleId];
        store.RolePermissions[roleId] = [permission];

        AuthorizationResult result = await AuthorizeAsync(
            store,
            CreatePrincipal(userId),
            permission);

        result.Succeeded.Should().BeTrue();
    }

    [Fact(DisplayName = "Authorization pipeline: should deny a permission the user does not hold")]
    public async Task AuthorizeAsync_ShouldDeny_WhenPermissionIsMissing()
    {
        Guid userId = Guid.NewGuid();
        var store = new FakePermissionStore();
        store.DirectPermissions[userId] = [PermissionContext.Todo.Items.Read.Identifier];

        AuthorizationResult result = await AuthorizeAsync(
            store,
            CreatePrincipal(userId),
            PermissionContext.Identity.Users.Manage.Identifier);

        result.Succeeded.Should().BeFalse();
    }

    [Fact(DisplayName = "Authorization pipeline: should deny anonymous callers")]
    public async Task AuthorizeAsync_ShouldDeny_WhenCallerIsAnonymous()
    {
        AuthorizationResult result = await AuthorizeAsync(
            new FakePermissionStore(),
            new ClaimsPrincipal(new ClaimsIdentity()),
            PermissionContext.Identity.Profile.Read.Identifier);

        result.Succeeded.Should().BeFalse();
    }

    [Fact(DisplayName = "Authorization pipeline: should grant every permission to the admin role")]
    public async Task AuthorizeAsync_ShouldGrant_WhenCallerIsAdmin()
    {
        AuthorizationResult result = await AuthorizeAsync(
            new FakePermissionStore(),
            CreatePrincipal(Guid.NewGuid(), isAdmin: true),
            "catalog.products.product.publish");

        result.Succeeded.Should().BeTrue();
    }

    [Fact(DisplayName = "Authorization pipeline: should observe invalidation after a permission change")]
    public async Task AuthorizeAsync_ShouldReflectInvalidation()
    {
        Guid userId = Guid.NewGuid();
        string permission = PermissionContext.Identity.Profile.Update.Identifier;
        var store = new FakePermissionStore();
        store.DirectPermissions[userId] = [permission];

        using WebApplication app = CreateHost(store);
        using IServiceScope scope = app.Services.CreateScope();

        AuthorizationResult granted = await AuthorizeAsync(scope.ServiceProvider, userId, permission);
        granted.Succeeded.Should().BeTrue();

        store.DirectPermissions[userId] = [];
        await scope.ServiceProvider
            .GetRequiredService<IPermissionService>()
            .InvalidateUserPermissionsAsync(userId, TestContext.Current.CancellationToken);

        AuthorizationResult revoked = await AuthorizeAsync(scope.ServiceProvider, userId, permission);
        revoked.Succeeded.Should().BeFalse();
    }

    [Fact(DisplayName = "Authorization pipeline: should grant an ANY multi-permission policy when one is held")]
    public async Task AuthorizeAsync_ShouldGrantAnyPolicy_WhenOnePermissionIsHeld()
    {
        Guid userId = Guid.NewGuid();
        var store = new FakePermissionStore();
        store.DirectPermissions[userId] = [PermissionContext.Todo.Items.Read.Identifier];

        string policyName = PermissionPolicyParser.Compose(
            [
                PermissionContext.Todo.Items.Update.Identifier,
                PermissionContext.Todo.Items.Read.Identifier,
            ],
            PermissionMatchMode.Any);

        AuthorizationResult result = await AuthorizeAsync(
            store,
            CreatePrincipal(userId),
            policyName);

        result.Succeeded.Should().BeTrue();
    }

    [Fact(DisplayName = "Authorization pipeline: should deny an ALL multi-permission policy when one is missing")]
    public async Task AuthorizeAsync_ShouldDenyAllPolicy_WhenOnePermissionIsMissing()
    {
        Guid userId = Guid.NewGuid();
        var store = new FakePermissionStore();
        store.DirectPermissions[userId] = [PermissionContext.Todo.Items.Read.Identifier];

        string policyName = PermissionPolicyParser.Compose(
            [
                PermissionContext.Todo.Items.Update.Identifier,
                PermissionContext.Todo.Items.Read.Identifier,
            ],
            PermissionMatchMode.All);

        AuthorizationResult result = await AuthorizeAsync(
            store,
            CreatePrincipal(userId),
            policyName);

        result.Succeeded.Should().BeFalse();
    }

    [Fact(DisplayName = "Authorization pipeline: should deny an unregistered canonical permission to a normal user")]
    public async Task AuthorizeAsync_ShouldDeny_ForUnregisteredCanonicalPermission()
    {
        AuthorizationResult result = await AuthorizeAsync(
            new FakePermissionStore(),
            CreatePrincipal(Guid.NewGuid()),
            "catalog.products.product.publish");

        result.Succeeded.Should().BeFalse();
    }

    private static async Task<AuthorizationResult> AuthorizeAsync(
        FakePermissionStore store,
        ClaimsPrincipal principal,
        string permission)
    {
        using WebApplication app = CreateHost(store);
        using IServiceScope scope = app.Services.CreateScope();

        return await AuthorizeAsync(scope.ServiceProvider, principal, permission);
    }

    private static async Task<AuthorizationResult> AuthorizeAsync(
        IServiceProvider provider,
        Guid userId,
        string permission) =>
        await AuthorizeAsync(provider, CreatePrincipal(userId), permission);

    private static async Task<AuthorizationResult> AuthorizeAsync(
        IServiceProvider provider,
        ClaimsPrincipal principal,
        string permission)
    {
        IAuthorizationPolicyProvider policyProvider =
            provider.GetRequiredService<IAuthorizationPolicyProvider>();
        IAuthorizationService authorizationService =
            provider.GetRequiredService<IAuthorizationService>();

        AuthorizationPolicy? policy = await policyProvider.GetPolicyAsync(permission);
        policy.Should().NotBeNull();

        return await authorizationService.AuthorizeAsync(principal, resource: null, policy: policy!);
    }

    private static WebApplication CreateHost(FakePermissionStore store)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
        builder.Services.AddSingleton<IPermissionStore>(store);
        builder.AddAuthorizationPipeline();

        return builder.Build();
    }

    private static ClaimsPrincipal CreatePrincipal(Guid userId, bool isAdmin = false)
    {
        List<Claim> claims = [new(ClaimTypes.NameIdentifier, userId.ToString())];
        if (isAdmin)
            claims.Add(new Claim(ClaimTypes.Role, AuthorizationConstant.Defaults.AdminRole));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }

    private sealed class InMemoryCacheService : ICacheService
    {
        private readonly Dictionary<string, object> _entries = [];

        public ValueTask<Result<T>> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, ValueTask<T>> factory,
            CachingEntryOption? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
            where T : notnull
        {
            if (_entries.TryGetValue(key, out object? existing))
                return ValueTask.FromResult(Result<T>.Ok((T)existing));

            T created = factory(cancellationToken).GetAwaiter().GetResult();
            _entries[key] = created;
            return ValueTask.FromResult(Result<T>.Ok(created));
        }

        public ValueTask<Result> SetAsync<T>(
            string key,
            T value,
            CachingEntryOption? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
            where T : notnull
        {
            _entries[key] = value;
            return ValueTask.FromResult(Result.Ok());
        }

        public ValueTask<Result> RemoveAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            _entries.Remove(key);
            return ValueTask.FromResult(Result.Ok());
        }

        public ValueTask<Result> RemoveByTagAsync(
            IEnumerable<string> tags,
            CancellationToken cancellationToken = default)
        {
            foreach (string tag in tags)
                RemoveByTag(tag);

            return ValueTask.FromResult(Result.Ok());
        }

        public ValueTask<Result> RemoveByTagAsync(
            string tag,
            CancellationToken cancellationToken = default)
        {
            RemoveByTag(tag);
            return ValueTask.FromResult(Result.Ok());
        }

        private void RemoveByTag(string tag)
        {
            if (tag == "perm:global")
            {
                _entries.Clear();
                return;
            }

            _entries.Remove(tag);
        }
    }

    private sealed class FakePermissionStore : IPermissionStore
    {
        public Dictionary<Guid, HashSet<string>> DirectPermissions { get; } = [];

        public Dictionary<Guid, HashSet<Guid>> RoleIds { get; } = [];

        public Dictionary<Guid, HashSet<string>> RolePermissions { get; } = [];

        public Task<Result<HashSet<string>>> GetUserPermissionsAsync(
            Guid userId,
            CancellationToken ct = default)
        {
            var permissions = new HashSet<string>(
                DirectPermissions.GetValueOrDefault(userId) ?? [],
                StringComparer.OrdinalIgnoreCase);

            foreach (Guid roleId in RoleIds.GetValueOrDefault(userId) ?? [])
                permissions.UnionWith(RolePermissions.GetValueOrDefault(roleId) ?? []);

            return Task.FromResult(Result<HashSet<string>>.Ok(permissions));
        }

        public Task<Result<HashSet<string>>> GetRolePermissionsAsync(
            Guid roleId,
            CancellationToken ct = default) =>
            Task.FromResult(Result<HashSet<string>>.Ok(
                new HashSet<string>(
                    RolePermissions.GetValueOrDefault(roleId) ?? [],
                    StringComparer.OrdinalIgnoreCase)));

        public Task<Result<HashSet<Guid>>> GetUserRoleIdsAsync(
            Guid userId,
            CancellationToken ct = default) =>
            Task.FromResult(Result<HashSet<Guid>>.Ok(
                new HashSet<Guid>(RoleIds.GetValueOrDefault(userId) ?? [])));

        public Task<Result<HashSet<string>>> GetUserDirectPermissionsAsync(
            Guid userId,
            CancellationToken ct = default) =>
            Task.FromResult(Result<HashSet<string>>.Ok(
                new HashSet<string>(
                    DirectPermissions.GetValueOrDefault(userId) ?? [],
                    StringComparer.OrdinalIgnoreCase)));

        public Task<Result<HashSet<string>>> GetAllPermissionIdentifiersAsync(
            CancellationToken ct = default) =>
            Task.FromResult(Result<HashSet<string>>.Ok([]));

        public Task<Result> AddRolePermissionsAsync(
            Guid roleId,
            IEnumerable<string> permissions,
            CancellationToken ct = default) =>
            Task.FromResult(Result.Ok());

        public Task<Result> RemoveRolePermissionsAsync(
            Guid roleId,
            IEnumerable<string> permissions,
            CancellationToken ct = default) =>
            Task.FromResult(Result.Ok());

        public Task<Result> AddUserDirectPermissionsAsync(
            Guid userId,
            IEnumerable<string> permissions,
            CancellationToken ct = default) =>
            Task.FromResult(Result.Ok());

        public Task<Result> RemoveUserDirectPermissionsAsync(
            Guid userId,
            IEnumerable<string> permissions,
            CancellationToken ct = default) =>
            Task.FromResult(Result.Ok());
    }
}
