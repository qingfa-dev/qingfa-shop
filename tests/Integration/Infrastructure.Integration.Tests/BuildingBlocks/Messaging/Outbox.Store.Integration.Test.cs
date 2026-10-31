using Application.BuildingBlocks.Messaging.Outbox;

using Core.BuildingBlocks.Functional.Results;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Integration.Tests.BuildingBlocks.Messaging;

/// <summary>PostgreSQL integration tests for the EF Core outbox store.</summary>
[Collection("messaging")]
public sealed class OutboxStoreIntegrationTest(MessagingFixture fixture) : IAsyncLifetime
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(1);

    private readonly MessagingFixture _fixture = fixture;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _fixture.CleanAsync();
        _fixture.CreateScope();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _fixture.CleanAsync();
    }

    [Fact(DisplayName = "Claim returns a pending row with a fresh lease")]
    public async Task ClaimAsync_WithPendingMessage_ShouldReturnRowAndAssignLease()
    {
        // Arrange
        OutboxMessage message = NewMessage();
        await EnqueueAsync(message);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Act
        Result<IReadOnlyList<OutboxMessage>> result = await _fixture.OutboxStore.ClaimAsync(
            10, now, LeaseDuration);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(claimed => claimed.Id == message.Id);
        OutboxMessage claimed = result.Value[0];
        claimed.Status.Should().Be(OutboxMessageStatus.Pending);
        claimed.LeaseId.Should().NotBeNull();
        claimed.LockedUntilUtc.Should().BeCloseTo(now.Add(LeaseDuration), TimeSpan.FromMilliseconds(1));

        // Check: a fresh context sees the lease, not just the claimed instance.
        OutboxMessage stored = await ReadAsync(message.Id);
        stored.LeaseId.Should().Be(claimed.LeaseId);
        stored.LockedUntilUtc.Should().BeCloseTo(now.Add(LeaseDuration), TimeSpan.FromMilliseconds(1));
    }

    [Fact(DisplayName = "Two stores claim disjoint batches with SKIP LOCKED")]
    public async Task ClaimAsync_WithTwoStores_ShouldReturnDisjointBatches()
    {
        // Arrange
        for (int index = 0; index < 4; index++)
            await EnqueueAsync(NewMessage());

        using IServiceScope secondScope = _fixture.Services.CreateScope();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var secondStore = new EfCoreOutboxStore(
            secondContext,
            secondScope.ServiceProvider.GetRequiredService<TimeProvider>());
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Act
        Task<Result<IReadOnlyList<OutboxMessage>>> first =
            _fixture.OutboxStore.ClaimAsync(2, now, LeaseDuration);
        Task<Result<IReadOnlyList<OutboxMessage>>> second =
            secondStore.ClaimAsync(2, now, LeaseDuration);
        await Task.WhenAll(first, second);

        // Assert
        Result<IReadOnlyList<OutboxMessage>> firstResult = await first;
        Result<IReadOnlyList<OutboxMessage>> secondResult = await second;
        firstResult.IsSuccess.Should().BeTrue();
        secondResult.IsSuccess.Should().BeTrue();
        firstResult.Value.Should().HaveCount(2);
        secondResult.Value.Should().HaveCount(2);

        List<Guid> ids = [.. firstResult.Value.Select(message => message.Id),
            .. secondResult.Value.Select(message => message.Id)];
        ids.Should().OnlyHaveUniqueItems();

        List<Guid?> leases = [.. firstResult.Value.Select(message => message.LeaseId),
            .. secondResult.Value.Select(message => message.LeaseId)];
        leases.Should().OnlyHaveUniqueItems();
    }

    [Fact(DisplayName = "A future availability is not claimable early")]
    public async Task ClaimAsync_WithFutureAvailability_ShouldSkipRow()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;
        OutboxMessage message = NewMessage(availableOnUtc: now.AddHours(1));
        await EnqueueAsync(message);

        // Act
        Result<IReadOnlyList<OutboxMessage>> early = await _fixture.OutboxStore.ClaimAsync(
            10, now, LeaseDuration);
        Result<IReadOnlyList<OutboxMessage>> later = await _fixture.OutboxStore.ClaimAsync(
            10, now.AddHours(2), LeaseDuration);

        // Assert
        early.IsSuccess.Should().BeTrue();
        early.Value.Should().BeEmpty();
        later.IsSuccess.Should().BeTrue();
        later.Value.Should().ContainSingle(claimed => claimed.Id == message.Id);
    }

    [Fact(DisplayName = "Mark processed persists the terminal state")]
    public async Task MarkProcessedAsync_WithLiveLease_ShouldPersistProcessed()
    {
        // Arrange
        OutboxMessage claimed = await EnqueueAndClaimAsync();
        DateTimeOffset processedOnUtc = DateTimeOffset.UtcNow;

        // Act
        Result result = await _fixture.OutboxStore.MarkProcessedAsync(
            claimed.Id, claimed.LeaseId!.Value, processedOnUtc);

        // Assert
        result.IsSuccess.Should().BeTrue();
        OutboxMessage stored = await ReadAsync(claimed.Id);
        stored.Status.Should().Be(OutboxMessageStatus.Processed);
        stored.ProcessedOnUtc.Should().BeCloseTo(processedOnUtc, TimeSpan.FromMilliseconds(1));
        stored.LeaseId.Should().BeNull();
        stored.LockedUntilUtc.Should().BeNull();
    }

    [Fact(DisplayName = "Mark failed increments attempts and reschedules")]
    public async Task MarkFailedAsync_WithLiveLease_ShouldPersistRetryState()
    {
        // Arrange
        OutboxMessage claimed = await EnqueueAndClaimAsync();
        DateTimeOffset nextAttemptUtc = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        Result result = await _fixture.OutboxStore.MarkFailedAsync(
            claimed.Id, claimed.LeaseId!.Value, "broker unavailable", nextAttemptUtc);

        // Assert
        result.IsSuccess.Should().BeTrue();
        OutboxMessage stored = await ReadAsync(claimed.Id);
        stored.Status.Should().Be(OutboxMessageStatus.Pending);
        stored.Attempts.Should().Be(1);
        stored.LastError.Should().Be("broker unavailable");
        stored.AvailableOnUtc.Should().BeCloseTo(nextAttemptUtc, TimeSpan.FromMilliseconds(1));
        stored.LeaseId.Should().BeNull();
    }

    [Fact(DisplayName = "Dead letter persists the terminal failure")]
    public async Task DeadLetterAsync_WithLiveLease_ShouldPersistDeadLettered()
    {
        // Arrange
        OutboxMessage claimed = await EnqueueAndClaimAsync();

        // Act
        Result result = await _fixture.OutboxStore.DeadLetterAsync(
            claimed.Id, claimed.LeaseId!.Value, "max attempts reached");

        // Assert
        result.IsSuccess.Should().BeTrue();
        OutboxMessage stored = await ReadAsync(claimed.Id);
        stored.Status.Should().Be(OutboxMessageStatus.DeadLettered);
        stored.Attempts.Should().Be(0);
        stored.LastError.Should().Be("max attempts reached");
        stored.LeaseId.Should().BeNull();
    }

    [Fact(DisplayName = "A stale lease cannot overwrite a claimed row")]
    public async Task MarkProcessedAsync_WithStaleLease_ShouldReturnLeaseLost()
    {
        // Arrange
        OutboxMessage claimed = await EnqueueAndClaimAsync();

        // Act
        Result result = await _fixture.OutboxStore.MarkProcessedAsync(
            claimed.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.LeaseLost);
        OutboxMessage stored = await ReadAsync(claimed.Id);
        stored.Status.Should().Be(OutboxMessageStatus.Pending);
        stored.LeaseId.Should().Be(claimed.LeaseId);
    }

    [Fact(DisplayName = "A lost lease does not poison the rest of the batch")]
    public async Task MarkProcessedAsync_AfterLostLease_ShouldNotAffectTheNextMark()
    {
        // Arrange
        await EnqueueAsync(NewMessage(), NewMessage());
        Result<IReadOnlyList<OutboxMessage>> claimed = await _fixture.OutboxStore.ClaimAsync(
            2, DateTimeOffset.UtcNow, LeaseDuration);
        claimed.IsSuccess.Should().BeTrue();
        claimed.Value.Should().HaveCount(2);
        OutboxMessage lost = claimed.Value[0];
        OutboxMessage live = claimed.Value[1];

        // Update: another worker re-claims the first row with a different lease.
        await _fixture.DbContext.Set<OutboxMessage>()
            .Where(message => message.Id == lost.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.LeaseId, Guid.NewGuid()));

        // Act
        Result lostResult = await _fixture.OutboxStore.MarkProcessedAsync(
            lost.Id, lost.LeaseId!.Value, DateTimeOffset.UtcNow);
        Result liveResult = await _fixture.OutboxStore.MarkProcessedAsync(
            live.Id, live.LeaseId!.Value, DateTimeOffset.UtcNow);

        // Assert
        lostResult.IsFailure.Should().BeTrue();
        lostResult.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.LeaseLost);
        liveResult.IsSuccess.Should().BeTrue();

        OutboxMessage stored = await ReadAsync(live.Id);
        stored.Status.Should().Be(OutboxMessageStatus.Processed);
    }

    [Fact(DisplayName = "Retention deletes only processed rows older than the cutoff")]
    public async Task DeleteProcessedBeforeAsync_ShouldDeleteOnlyProcessedRows()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;
        OutboxMessage oldProcessed = NewMessage(
            status: OutboxMessageStatus.Processed,
            processedOnUtc: now.AddDays(-10));
        OutboxMessage recentProcessed = NewMessage(
            status: OutboxMessageStatus.Processed,
            processedOnUtc: now);
        OutboxMessage pending = NewMessage();

        _fixture.DbContext.AddRange(oldProcessed, recentProcessed, pending);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        Result<int> result = await _fixture.OutboxStore.DeleteProcessedBeforeAsync(
            now.AddDays(-1));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        (await CountAsync()).Should().Be(2);
    }

    private static OutboxMessage NewMessage(
        DateTimeOffset? availableOnUtc = null,
        OutboxMessageStatus status = OutboxMessageStatus.Pending,
        DateTimeOffset? processedOnUtc = null) => new()
        {
            Type = "TodoCreatedEvent",
            Version = 1,
            Payload = "{\"title\":\"milk\"}",
            Metadata = "{}",
            Status = status,
            AvailableOnUtc = availableOnUtc ?? DateTimeOffset.UtcNow.AddMinutes(-1),
            ProcessedOnUtc = processedOnUtc,
        };

    private async Task EnqueueAsync(params OutboxMessage[] messages)
    {
        Result enqueued = await _fixture.OutboxStore.EnqueueAsync(messages);
        enqueued.IsSuccess.Should().BeTrue();

        Result saved = await _fixture.UnitOfWork.SaveChangesAsync();
        saved.IsSuccess.Should().BeTrue();
    }

    private async Task<OutboxMessage> EnqueueAndClaimAsync()
    {
        OutboxMessage message = NewMessage();
        await EnqueueAsync(message);

        Result<IReadOnlyList<OutboxMessage>> claimed = await _fixture.OutboxStore.ClaimAsync(
            1, DateTimeOffset.UtcNow, LeaseDuration);
        claimed.IsSuccess.Should().BeTrue();
        claimed.Value.Should().ContainSingle();

        return claimed.Value[0];
    }

    private Task<OutboxMessage> ReadAsync(Guid id) => QueryAsync(context =>
        context.Set<OutboxMessage>()
            .AsNoTracking()
            .SingleAsync(message => message.Id == id));

    private Task<int> CountAsync() => QueryAsync(context =>
        context.Set<OutboxMessage>().CountAsync());

    private async Task<T> QueryAsync<T>(Func<ApplicationDbContext, Task<T>> query)
    {
        using IServiceScope scope = _fixture.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        return await query(context);
    }
}