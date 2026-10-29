using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Application.Common.Security.Authorization.Domain;
using Application.Common.Security.Authorization.Permissions.Store;
using Application.Common.Security.Authorization.Registry;

using Infrastructure.Security.Authorization.Permissions.Registry;

namespace Infrastructure.Unit.Tests.Security.Authorization.Permissions.Registry;

[Trait("Category", "Unit")]
[Trait("Module", "Authorization")]
[Trait("Feature", "PermissionMetadataRegistry")]
public sealed class PermissionMetadataRegistryTests
{
    [Fact(DisplayName = "PermissionMetadataSnapshot: should build case-insensitive lookups")]
    public void Snapshot_ShouldBuildLookups()
    {
        PermissionMetadataSnapshot snapshot = PermissionMetadataSnapshot.Create(
            [PermissionContext.Identity.Users.Manage, PermissionContext.Todo.Items.Read],
            DateTimeOffset.UtcNow);

        snapshot.Count.Should().Be(2);
        snapshot.Permissions.Select(permission => permission.Identifier)
            .Should().BeInAscendingOrder(StringComparer.Ordinal);
        snapshot.Find("IDENTITY.USERS.ALL.MANAGE")!.Identifier
            .Should().Be(PermissionContext.Identity.Users.Manage.Identifier);
        snapshot.Find(null).Should().BeNull();
        snapshot.ByDomain("identity").Should().ContainSingle();
        snapshot.ByDomain("IDENTITY").Should().ContainSingle();
        snapshot.ByDomain("unknown").Should().BeEmpty();
        snapshot.ByCategory("todos").Should().ContainSingle();
        snapshot.ByCategory(null).Should().BeEmpty();
    }

    [Fact(DisplayName = "PermissionMetadataSnapshot: should de-duplicate by identifier")]
    public void Snapshot_ShouldDeDuplicate()
    {
        PermissionMetadata first = PermissionContext.Identity.Profile.Read;
        PermissionMetadata duplicate = PermissionMetadata.Create(
            first.Domain,
            first.Category,
            first.Resource,
            first.Action,
            name: "Duplicate");

        PermissionMetadataSnapshot snapshot = PermissionMetadataSnapshot.Create(
            [first, duplicate],
            DateTimeOffset.UtcNow);

        snapshot.Count.Should().Be(1);
        snapshot.Permissions[0].Name.Should().Be(first.Name);
    }

    [Fact(DisplayName = "PermissionMetadataSnapshot: Empty should be usable")]
    public void Snapshot_Empty_ShouldBeUsable()
    {
        PermissionMetadataSnapshot.Empty.Count.Should().Be(0);
        PermissionMetadataSnapshot.Empty.RefreshedAtUtc.Should().Be(DateTimeOffset.MinValue);
        PermissionMetadataSnapshot.Empty.Find("identity.users.all.manage").Should().BeNull();
    }

    [Fact(DisplayName = "PermissionMetadataRegistry: should start with the built-in catalog")]
    public async Task Registry_ShouldStartWithBuiltIns()
    {
        await using ServiceProvider provider = BuildProvider(new FakePermissionStore());
        var registry = new PermissionMetadataRegistry(
            provider.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            Mock.Of<ILogger<PermissionMetadataRegistry>>());

        registry.Snapshot.Count.Should().Be(PermissionContext.All.Count);
        registry.Find(PermissionContext.Identity.Users.Manage.Identifier).Should().NotBeNull();
        registry.ByDomain("identity").Should().NotBeEmpty();
    }

    [Fact(DisplayName = "PermissionMetadataRegistry: refresh should merge persisted identifiers")]
    public async Task Refresh_ShouldMergePersistedIdentifiers()
    {
        var store = new FakePermissionStore
        {
            Identifiers = ["catalog.products.product.publish", "catalog.products.product.retire"],
        };

        await using ServiceProvider provider = BuildProvider(store);
        var registry = new PermissionMetadataRegistry(
            provider.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            Mock.Of<ILogger<PermissionMetadataRegistry>>());

        Result<PermissionMetadataSnapshot> result = await registry.RefreshAsync(
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(PermissionContext.All.Count + 2);
        result.Value.Find("catalog.products.product.publish").Should().NotBeNull();
        registry.Snapshot.Count.Should().Be(result.Value.Count);
        registry.ByDomain("catalog").Should().HaveCount(2);
    }

    [Fact(DisplayName = "PermissionMetadataRegistry: refresh should skip malformed claim values")]
    public async Task Refresh_ShouldSkipMalformedIdentifiers()
    {
        var store = new FakePermissionStore
        {
            Identifiers = ["not-a-permission", "catalog.products.product.publish"],
        };

        await using ServiceProvider provider = BuildProvider(store);
        var registry = new PermissionMetadataRegistry(
            provider.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            Mock.Of<ILogger<PermissionMetadataRegistry>>());

        Result<PermissionMetadataSnapshot> result = await registry.RefreshAsync(
            TestContext.Current.CancellationToken);

        result.Value.Count.Should().Be(PermissionContext.All.Count + 1);
        result.Value.Find("catalog.products.product.publish").Should().NotBeNull();
        result.Value.Find("not-a-permission").Should().BeNull();
    }

    [Fact(DisplayName = "PermissionMetadataRegistry: refresh should propagate store failures")]
    public async Task Refresh_ShouldPropagateStoreFailures()
    {
        var store = new FakePermissionStore
        {
            Failure = PermissionStoreOutcome.Failure.AllIdentifiersReadFailed(),
        };

        await using ServiceProvider provider = BuildProvider(store);
        var registry = new PermissionMetadataRegistry(
            provider.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            Mock.Of<ILogger<PermissionMetadataRegistry>>());

        Result<PermissionMetadataSnapshot> result = await registry.RefreshAsync(
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("PermissionStore.AllIdentifiers.ReadFailed");
        registry.Snapshot.Count.Should().Be(PermissionContext.All.Count);
    }

    [Fact(DisplayName = "PermissionMetadataRegistry: concurrent refreshes should stay consistent")]
    public async Task Refresh_ConcurrentCalls_ShouldStayConsistent()
    {
        var store = new FakePermissionStore
        {
            Identifiers = ["catalog.products.product.publish"],
            Delay = TimeSpan.FromMilliseconds(5),
        };

        await using ServiceProvider provider = BuildProvider(store);
        var registry = new PermissionMetadataRegistry(
            provider.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            Mock.Of<ILogger<PermissionMetadataRegistry>>());

        Result<PermissionMetadataSnapshot>[] results = await Task.WhenAll(
            Enumerable.Range(0, 16)
                .Select(_ => registry.RefreshAsync(TestContext.Current.CancellationToken)));

        results.Should().OnlyContain(result => result.IsSuccess);
        store.Calls.Should().Be(16);
        registry.Snapshot.Count.Should().Be(PermissionContext.All.Count + 1);
        registry.Snapshot.Should().BeSameAs(results[^1].Value);
    }

    private static ServiceProvider BuildProvider(IPermissionStore store)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => store);
        return services.BuildServiceProvider();
    }

    private sealed class FakePermissionStore : IPermissionStore
    {
        private int _calls;

        public IReadOnlyList<string> Identifiers { get; init; } = [];

        public Error? Failure { get; init; }

        public TimeSpan Delay { get; init; }

        public int Calls => _calls;

        public async Task<Result<HashSet<string>>> GetAllPermissionIdentifiersAsync(
            CancellationToken ct = default)
        {
            Interlocked.Increment(ref _calls);

            if (Delay > TimeSpan.Zero)
                await Task.Delay(Delay, ct);

            return Failure is null
                ? Result<HashSet<string>>.Ok([.. Identifiers])
                : Result<HashSet<string>>.Fail(Failure);
        }

        public Task<Result<HashSet<string>>> GetUserPermissionsAsync(
            Guid userId,
            CancellationToken ct = default) =>
            Task.FromResult(Result<HashSet<string>>.Ok([]));

        public Task<Result<HashSet<string>>> GetRolePermissionsAsync(
            Guid roleId,
            CancellationToken ct = default) =>
            Task.FromResult(Result<HashSet<string>>.Ok([]));

        public Task<Result<HashSet<Guid>>> GetUserRoleIdsAsync(
            Guid userId,
            CancellationToken ct = default) =>
            Task.FromResult(Result<HashSet<Guid>>.Ok([]));

        public Task<Result<HashSet<string>>> GetUserDirectPermissionsAsync(
            Guid userId,
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
