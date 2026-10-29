using Microsoft.AspNetCore.Http;

using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

using Application.Common.Security.Authorization.Domain;
using Application.Common.Security.Authorization.Permissions.Services;
using Application.Common.Security.Authorization.Registry;

using Web.Api.Endpoints.Authorization;

namespace Api.Integration.Tests.Authorization;

[Trait("Category", "Integration")]
[Trait("Module", "Authorization")]
[Trait("Feature", "PermissionRegistryEndpoint")]
public sealed class PermissionRegistryEndpointTest
{
    private static readonly DateTimeOffset s_now = DateTimeOffset.UtcNow;

    [Fact(DisplayName = "GET permissions should project the current snapshot")]
    public async Task ListPermissions_ShouldReturnSnapshot()
    {
        var registry = new FakeRegistry();

        int status = await StatusAsync(PermissionRegistryEndpoint.ListPermissionsAsync(
            registry,
            domain: null,
            category: null,
            search: null,
            TestContext.Current.CancellationToken));

        status.Should().Be(StatusCodes.Status200OK);
        registry.RefreshCalls.Should().Be(1);
    }

    [Fact(DisplayName = "GET permissions should apply domain, category, and search filters")]
    public async Task ListPermissions_WithFilters_ShouldReturnFilteredSet()
    {
        var registry = new FakeRegistry();

        int status = await StatusAsync(PermissionRegistryEndpoint.ListPermissionsAsync(
            registry,
            domain: "identity",
            category: "users",
            search: "manage",
            TestContext.Current.CancellationToken));

        status.Should().Be(StatusCodes.Status200OK);
    }

    [Fact(DisplayName = "GET permissions should surface a refresh failure")]
    public async Task ListPermissions_WhenRefreshFails_ShouldReturnProblem()
    {
        var registry = new FakeRegistry
        {
            RefreshResult = Result<PermissionMetadataSnapshot>.Fail(
                PermissionRegistryOutcome.Failure.RefreshFailed()),
        };

        int status = await StatusAsync(PermissionRegistryEndpoint.ListPermissionsAsync(
            registry,
            domain: null,
            category: null,
            search: null,
            TestContext.Current.CancellationToken));

        status.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact(DisplayName = "GET permission by identifier should return 200 or 404")]
    public async Task GetPermission_ShouldReturnFoundOrNotFound()
    {
        var registry = new FakeRegistry();

        int found = await StatusAsync(PermissionRegistryEndpoint.GetPermissionAsync(
            PermissionContext.Identity.Users.Manage.Identifier,
            registry,
            TestContext.Current.CancellationToken));
        int missing = await StatusAsync(PermissionRegistryEndpoint.GetPermissionAsync(
            "unknown.permission.read",
            registry,
            TestContext.Current.CancellationToken));

        found.Should().Be(StatusCodes.Status200OK);
        missing.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact(DisplayName = "GET permission should surface a refresh failure")]
    public async Task GetPermission_WhenRefreshFails_ShouldReturnProblem()
    {
        var registry = new FakeRegistry
        {
            RefreshResult = Result<PermissionMetadataSnapshot>.Fail(
                PermissionRegistryOutcome.Failure.RefreshFailed()),
        };

        int status = await StatusAsync(PermissionRegistryEndpoint.GetPermissionAsync(
            PermissionContext.Identity.Users.Manage.Identifier,
            registry,
            TestContext.Current.CancellationToken));

        status.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact(DisplayName = "GET domains and categories should project counts")]
    public async Task ListDomainsAndCategories_ShouldReturnCounts()
    {
        var registry = new FakeRegistry();

        int domains = await StatusAsync(PermissionRegistryEndpoint.ListDomainsAsync(
            registry,
            TestContext.Current.CancellationToken));
        int categories = await StatusAsync(PermissionRegistryEndpoint.ListCategoriesAsync(
            registry,
            TestContext.Current.CancellationToken));

        domains.Should().Be(StatusCodes.Status200OK);
        categories.Should().Be(StatusCodes.Status200OK);
    }

    [Fact(DisplayName = "GET domains should surface a refresh failure")]
    public async Task ListDomains_WhenRefreshFails_ShouldReturnProblem()
    {
        var registry = new FakeRegistry
        {
            RefreshResult = Result<PermissionMetadataSnapshot>.Fail(
                PermissionRegistryOutcome.Failure.RefreshFailed()),
        };

        int status = await StatusAsync(PermissionRegistryEndpoint.ListDomainsAsync(
            registry,
            TestContext.Current.CancellationToken));

        status.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact(DisplayName = "GET categories should surface a refresh failure")]
    public async Task ListCategories_WhenRefreshFails_ShouldReturnProblem()
    {
        var registry = new FakeRegistry
        {
            RefreshResult = Result<PermissionMetadataSnapshot>.Fail(
                PermissionRegistryOutcome.Failure.RefreshFailed()),
        };

        int status = await StatusAsync(PermissionRegistryEndpoint.ListCategoriesAsync(
            registry,
            TestContext.Current.CancellationToken));

        status.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact(DisplayName = "GET roles should return the built-in role permission sets")]
    public void ListRolePermissions_ShouldReturnBuiltIns()
    {
        IResult result = PermissionRegistryEndpoint.ListRolePermissionsAsync();

        Status(result).Should().Be(StatusCodes.Status200OK);
    }

    [Fact(DisplayName = "POST refresh should return the new snapshot summary")]
    public async Task Refresh_ShouldReturnSummary()
    {
        var registry = new FakeRegistry();

        int status = await StatusAsync(PermissionRegistryEndpoint.RefreshAsync(
            registry,
            TestContext.Current.CancellationToken));

        status.Should().Be(StatusCodes.Status200OK);
    }

    [Fact(DisplayName = "POST refresh should surface a refresh failure")]
    public async Task Refresh_WhenFails_ShouldReturnProblem()
    {
        var registry = new FakeRegistry
        {
            RefreshResult = Result<PermissionMetadataSnapshot>.Fail(
                PermissionRegistryOutcome.Failure.RefreshFailed()),
        };

        int status = await StatusAsync(PermissionRegistryEndpoint.RefreshAsync(
            registry,
            TestContext.Current.CancellationToken));

        status.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact(DisplayName = "GET user permissions should return the effective set")]
    public async Task GetUserPermissions_ShouldReturnEffectiveSet()
    {
        var service = new FakePermissionService();

        int status = await StatusAsync(PermissionRegistryEndpoint.GetUserPermissionsAsync(
            Guid.NewGuid(),
            service,
            TestContext.Current.CancellationToken));

        status.Should().Be(StatusCodes.Status200OK);
    }

    [Fact(DisplayName = "GET user permissions should surface a resolution failure")]
    public async Task GetUserPermissions_WhenFails_ShouldReturnProblem()
    {
        var service = new FakePermissionService
        {
            Failure = Error.ServiceUnavailable("Permission.Fail", "Unavailable."),
        };

        int status = await StatusAsync(PermissionRegistryEndpoint.GetUserPermissionsAsync(
            Guid.NewGuid(),
            service,
            TestContext.Current.CancellationToken));

        status.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact(DisplayName = "GET permission check should validate the request")]
    public async Task CheckUserPermissions_ShouldValidateRequest()
    {
        var service = new FakePermissionService();

        int empty = await StatusAsync(PermissionRegistryEndpoint.CheckUserPermissionsAsync(
            Guid.NewGuid(),
            permissions: [],
            mode: null,
            service,
            TestContext.Current.CancellationToken));
        int invalidMode = await StatusAsync(PermissionRegistryEndpoint.CheckUserPermissionsAsync(
            Guid.NewGuid(),
            permissions: ["identity.users.all.read"],
            mode: "some",
            service,
            TestContext.Current.CancellationToken));

        empty.Should().Be(StatusCodes.Status400BadRequest);
        invalidMode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact(DisplayName = "GET permission check should evaluate ALL and ANY modes")]
    public async Task CheckUserPermissions_ShouldEvaluateModes()
    {
        var service = new FakePermissionService();

        int all = await StatusAsync(PermissionRegistryEndpoint.CheckUserPermissionsAsync(
            Guid.NewGuid(),
            permissions: ["identity.users.all.read"],
            mode: "all",
            service,
            TestContext.Current.CancellationToken));
        int any = await StatusAsync(PermissionRegistryEndpoint.CheckUserPermissionsAsync(
            Guid.NewGuid(),
            permissions: ["identity.users.all.read", "missing.permission.read"],
            mode: "any",
            service,
            TestContext.Current.CancellationToken));

        all.Should().Be(StatusCodes.Status200OK);
        any.Should().Be(StatusCodes.Status200OK);
    }

    [Fact(DisplayName = "GET permission check should surface a resolution failure")]
    public async Task CheckUserPermissions_WhenFails_ShouldReturnProblem()
    {
        var service = new FakePermissionService
        {
            Failure = Error.ServiceUnavailable("Permission.Fail", "Unavailable."),
        };

        int status = await StatusAsync(PermissionRegistryEndpoint.CheckUserPermissionsAsync(
            Guid.NewGuid(),
            permissions: ["identity.users.all.read"],
            mode: "all",
            service,
            TestContext.Current.CancellationToken));

        status.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact(DisplayName = "GET role permissions should return the effective set")]
    public async Task GetRolePermissions_ShouldReturnEffectiveSet()
    {
        var service = new FakePermissionService();

        int status = await StatusAsync(PermissionRegistryEndpoint.GetRolePermissionsAsync(
            Guid.NewGuid(),
            service,
            TestContext.Current.CancellationToken));

        status.Should().Be(StatusCodes.Status200OK);
    }

    [Fact(DisplayName = "GET role permissions should surface a resolution failure")]
    public async Task GetRolePermissions_WhenFails_ShouldReturnProblem()
    {
        var service = new FakePermissionService
        {
            Failure = Error.ServiceUnavailable("Permission.Fail", "Unavailable."),
        };

        int status = await StatusAsync(PermissionRegistryEndpoint.GetRolePermissionsAsync(
            Guid.NewGuid(),
            service,
            TestContext.Current.CancellationToken));

        status.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<int> StatusAsync(Task<IResult> result) =>
        Status(await result);

    private static int Status(IResult result) =>
        result as IStatusCodeHttpResult is { StatusCode: { } status }
            ? status
            : throw new InvalidOperationException($"Unexpected result type: {result.GetType().Name}");

    private sealed class FakeRegistry : IPermissionMetadataRegistry
    {
        public PermissionMetadataSnapshot Snapshot { get; } = PermissionMetadataSnapshot.Create(
            PermissionContext.All,
            s_now);

        public Result<PermissionMetadataSnapshot> RefreshResult { get; init; }

        public int RefreshCalls { get; private set; }

        public FakeRegistry()
        {
            RefreshResult = Result<PermissionMetadataSnapshot>.Ok(Snapshot);
        }

        public PermissionMetadata? Find(string? identifier) => Snapshot.Find(identifier);

        public IReadOnlyList<PermissionMetadata> ByDomain(string? domain) => Snapshot.ByDomain(domain);

        public IReadOnlyList<PermissionMetadata> ByCategory(string? category) => Snapshot.ByCategory(category);

        public Task<Result<PermissionMetadataSnapshot>> RefreshAsync(
            CancellationToken ct = default)
        {
            RefreshCalls++;
            return Task.FromResult(RefreshResult);
        }
    }

    private sealed class FakePermissionService : IPermissionService
    {
        private static readonly HashSet<string> s_permissions =
            [PermissionContext.Identity.Users.Manage.Identifier];

        public Error? Failure { get; init; }

        public Task<Result<HashSet<string>>> GetEffectiveUserPermissionsAsync(
            Guid userId,
            CancellationToken ct = default) =>
            Task.FromResult(Failure is null
                ? Result<HashSet<string>>.Ok([.. s_permissions])
                : Result<HashSet<string>>.Fail(Failure));

        public Task<Result<HashSet<string>>> GetRolePermissionsAsync(
            Guid roleId,
            CancellationToken ct = default) =>
            Task.FromResult(Failure is null
                ? Result<HashSet<string>>.Ok([.. s_permissions])
                : Result<HashSet<string>>.Fail(Failure));

        public Task<Result<bool>> HasAllPermissionsAsync(
            Guid userId,
            IEnumerable<string> permissions,
            CancellationToken ct = default) =>
            Evaluate(permissions, all: true);

        public Task<Result<bool>> HasAnyPermissionsAsync(
            Guid userId,
            IEnumerable<string> permissions,
            CancellationToken ct = default) =>
            Evaluate(permissions, all: false);

        public Task<Result<bool>> RoleHasAllPermissionsAsync(
            Guid roleId,
            IEnumerable<string> permissions,
            CancellationToken ct = default) =>
            Evaluate(permissions, all: true);

        public Task<Result<bool>> RoleHasAnyPermissionsAsync(
            Guid roleId,
            IEnumerable<string> permissions,
            CancellationToken ct = default) =>
            Evaluate(permissions, all: false);

        public Task<Result> InvalidateRolePermissionsAsync(
            Guid roleId,
            CancellationToken ct = default) =>
            Task.FromResult(Result.Ok());

        public Task<Result> InvalidateUserPermissionsAsync(
            Guid userId,
            CancellationToken ct = default) =>
            Task.FromResult(Result.Ok());

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

        private Task<Result<bool>> Evaluate(IEnumerable<string> permissions, bool all)
        {
            if (Failure is not null)
                return Task.FromResult(Result<bool>.Fail(Failure));

            bool granted = all
                ? permissions.All(s_permissions.Contains)
                : permissions.Any(s_permissions.Contains);

            return Task.FromResult(Result<bool>.Ok(granted));
        }
    }
}
