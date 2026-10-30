using Application.Common.Messaging.Outbox;

using Infrastructure.Persistence.EntityFramework.Outbox;
using Infrastructure.Persistence.EntityFramework.Outbox.Options;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Outbox;

/// <summary>Tests for one outbox worker tick.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Messaging")]
public sealed class OutboxWorkerTest
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "Worker should dispatch and prune processed rows on one tick")]
    public async Task RunOnce_WhenEnabled_ShouldDispatchAndPrune()
    {
        // Arrange
        var dispatcher = new FakeDispatcher();
        var store = new FakeStore();
        var services = new ServiceCollection();
        services.AddSingleton<IOutboxDispatcher>(dispatcher);
        services.AddSingleton<IOutboxStore>(store);
        using ServiceProvider provider = services.BuildServiceProvider();
        var setting = new OutboxSetting { RetentionDays = 7 };
        var worker = new OutboxWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            setting,
            new FixedTimeProvider(Now),
            NullLogger<OutboxWorker>.Instance);

        // Act
        await worker.RunOnceAsync();

        // Assert
        dispatcher.Calls.Should().Be(1);
        store.Cutoffs.Should().ContainSingle()
            .Which.Should().Be(Now.AddDays(-7));
    }

    [Fact(DisplayName = "Worker should do nothing when disabled")]
    public async Task RunOnce_WhenDisabled_ShouldNotResolveServices()
    {
        // Arrange
        using var provider = new ServiceCollection().BuildServiceProvider();
        var worker = new OutboxWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new OutboxSetting { Enabled = false },
            new FixedTimeProvider(Now),
            NullLogger<OutboxWorker>.Instance);

        // Act
        Func<Task> act = () => worker.RunOnceAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeDispatcher : IOutboxDispatcher
    {
        public int Calls { get; private set; }

        public Task<Result<int>> DispatchBatchAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Result<int>.Success(0));
        }
    }

    private sealed class FakeStore : IOutboxStore
    {
        public List<DateTimeOffset> Cutoffs { get; } = [];

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

        public Task<Result<int>> CountPendingAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Result<int>.Success(0));

        public Task<Result<DateTimeOffset?>> OldestPendingOnUtcAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Result<DateTimeOffset?>.Success(null));

        public Task<Result<int>> DeleteProcessedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken = default)
        {
            Cutoffs.Add(cutoffUtc);
            return Task.FromResult(Result<int>.Success(0));
        }
    }
}
