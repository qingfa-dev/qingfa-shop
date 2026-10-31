using Application.BuildingBlocks.Messaging.Outbox;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox.Options;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;

/// <summary>Tests for outbox lag reporting.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Messaging")]
public sealed class OutboxHealthCheckTest
{
    [Fact(DisplayName = "Health check should be healthy when the outbox is disabled")]
    public async Task CheckHealth_WhenDisabled_ShouldBeHealthy()
    {
        // Arrange
        var check = new OutboxHealthCheck(
            new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            new OutboxSetting { Enabled = false },
            TimeProvider.System);

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact(DisplayName = "Health check should be healthy when no rows are pending")]
    public async Task CheckHealth_WhenEmpty_ShouldBeHealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IOutboxStore>(_ => new EmptyOutboxStore());
        using ServiceProvider provider = services.BuildServiceProvider();
        var check = new OutboxHealthCheck(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new OutboxSetting(),
            TimeProvider.System);

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact(DisplayName = "Health check should degrade when the oldest pending row is too old")]
    public async Task CheckHealth_WhenOldRowExists_ShouldDegrade()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IOutboxStore>(_ => new PendingOutboxStore(DateTimeOffset.UtcNow.AddHours(-1)));
        using ServiceProvider provider = services.BuildServiceProvider();
        var check = new OutboxHealthCheck(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new OutboxSetting { MaxAgeSeconds = 30 },
            TimeProvider.System);

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
    }

    private class EmptyOutboxStore : IOutboxStore
    {
        public Task<Result> EnqueueAsync(IReadOnlyCollection<OutboxMessage> messages, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());

        public Task<Result<IReadOnlyList<OutboxMessage>>> ClaimAsync(int batchSize, DateTimeOffset nowUtc, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<IReadOnlyList<OutboxMessage>>.Success([]));

        public Task<Result> MarkProcessedAsync(Guid id, Guid leaseId, DateTimeOffset processedOnUtc, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());

        public Task<Result> MarkFailedAsync(Guid id, Guid leaseId, string error, DateTimeOffset nextAttemptUtc, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());

        public Task<Result> DeadLetterAsync(Guid id, Guid leaseId, string error, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());

        public virtual Task<Result<int>> CountPendingAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Result<int>.Success(0));

        public virtual Task<Result<DateTimeOffset?>> OldestPendingOnUtcAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Result<DateTimeOffset?>.Success(null));

        public Task<Result<int>> DeleteProcessedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<int>.Success(0));
    }

    private sealed class PendingOutboxStore(DateTimeOffset oldest) : EmptyOutboxStore
    {
        public override Task<Result<int>> CountPendingAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Result<int>.Success(1));

        public override Task<Result<DateTimeOffset?>> OldestPendingOnUtcAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Result<DateTimeOffset?>.Success(oldest));
    }
}
