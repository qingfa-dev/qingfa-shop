using Application.BuildingBlocks.Messaging.Outbox;
using Application.BuildingBlocks.Messaging.Serialization;

using Core.BuildingBlocks.Domain.Events;

using Infrastructure.BuildingBlocks.Adapters.Messaging.Serialization;
using Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.Interceptors;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;

/// <summary>Tests for staging aggregate events into the outbox.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class OutboxInterceptorTest
{
    [Fact(DisplayName = "Outbox interceptor should stage one row per aggregate event")]
    public async Task SavingChanges_WithAggregateEvents_ShouldStageOneRowPerEvent()
    {
        // Arrange
        await using OutboxTestContext context = CreateContext();
        var aggregate = new EventAggregate(Guid.NewGuid());
        var first = new TestDomainEvent("first");
        var second = new TestDomainEvent("second");
        Guid tenantId = Guid.NewGuid();
        first.SetTenantId(tenantId);
        aggregate.Raise(first);
        aggregate.Raise(second);
        context.Add(aggregate);

        // Act
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Assert
        List<OutboxMessage> messages = await context.Messages.AsNoTracking().ToListAsync();
        messages.Should().HaveCount(2);
        messages.Should().OnlyContain(message => message.Kind == OutboxMessageKind.Domain);
        messages.Select(message => message.Id)
            .Should().BeEquivalentTo(new[] { first.GetEventId()!.Value, second.GetEventId()!.Value });
        messages.Should().OnlyContain(message => message.Payload.Contains("\"name\""));
        messages.Should().Contain(message => message.TenantId == tenantId);
        aggregate.Events.Should().BeEmpty();
    }

    [Fact(DisplayName = "Outbox interceptor should stage each new event exactly once")]
    public async Task SavingChanges_WhenANewEventIsRaised_ShouldNotDuplicateRows()
    {
        // Arrange
        await using OutboxTestContext context = CreateContext();
        var aggregate = new EventAggregate(Guid.NewGuid());
        var first = new TestDomainEvent("first");
        aggregate.Raise(first);
        context.Add(aggregate);
        await context.SaveChangesAsync();

        var second = new TestDomainEvent("second");
        aggregate.Raise(second);

        // Act
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Assert
        List<OutboxMessage> messages = await context.Messages.AsNoTracking().ToListAsync();
        messages.Should().HaveCount(2);
        messages.Select(message => message.Id)
            .Should().BeEquivalentTo(new[] { first.GetEventId()!.Value, second.GetEventId()!.Value });
        messages.Count(message => message.Id == first.GetEventId()!.Value).Should().Be(1);
    }

    [Fact(DisplayName = "Outbox interceptor should leave events and no rows when the save fails")]
    public async Task SavingChanges_WhenSaveFails_ShouldLeaveEventsAndNoRows()
    {
        // Arrange
        var throwing = new ThrowingSaveInterceptor();
        await using OutboxTestContext context = CreateContext(throwing);
        var aggregate = new EventAggregate(Guid.NewGuid());
        aggregate.Raise(new TestDomainEvent("rejected"));
        context.Add(aggregate);

        // Act
        Func<Task> act = () => context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        (await context.Messages.AsNoTracking().CountAsync()).Should().Be(0);
        aggregate.Events.Should().ContainSingle();
    }

    [Fact(DisplayName = "Outbox interceptor should recover a failed save without orphaning rows")]
    public async Task SavingChanges_WhenRetriedAfterFailure_ShouldStageOnceAndNotOrphan()
    {
        // Arrange
        var throwing = new ThrowingSaveInterceptor();
        await using OutboxTestContext context = CreateContext(throwing);
        var aggregate = new EventAggregate(Guid.NewGuid());
        aggregate.Raise(new TestDomainEvent("retry"));
        context.Add(aggregate);

        Func<Task> failed = () => context.SaveChangesAsync();
        await failed.Should().ThrowAsync<InvalidOperationException>();

        // Act: a caller retry after clearing the failed tracking state.
        context.ChangeTracker.Clear();
        context.Add(aggregate);
        throwing.ShouldThrow = false;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Assert
        (await context.Messages.AsNoTracking().CountAsync()).Should().Be(1);
        aggregate.Events.Should().BeEmpty();
    }

    [Fact(DisplayName = "Outbox interceptor should do nothing when there are no aggregate events")]
    public async Task SavingChanges_WithNoEvents_ShouldNotStage()
    {
        // Arrange
        await using OutboxTestContext context = CreateContext();
        context.Add(new EventAggregate(Guid.NewGuid()));

        // Act
        await context.SaveChangesAsync();

        // Assert
        (await context.Messages.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact(DisplayName = "Outbox interceptor should stage on the synchronous save path")]
    public void SavingChanges_WhenSaveChangesIsSynchronous_ShouldStage()
    {
        // Arrange
        using OutboxTestContext context = CreateContext();
        var aggregate = new EventAggregate(Guid.NewGuid());
        aggregate.Raise(new TestDomainEvent("sync"));
        context.Add(aggregate);

        // Act
        context.SaveChanges();

        // Assert
        context.Messages.AsNoTracking().Should().ContainSingle();
        aggregate.Events.Should().BeEmpty();
    }

    private static OutboxTestContext CreateContext(params IInterceptor[] additionalInterceptors)
    {
        var builder = new DbContextOptionsBuilder<OutboxTestContext>()
            .UseInMemoryDatabase($"outbox_{Guid.NewGuid():N}");

        builder.AddInterceptors(
            new OutboxInterceptor(CreateSerializer(), TimeProvider.System));

        if (additionalInterceptors.Length > 0)
            builder.AddInterceptors(additionalInterceptors);

        return new OutboxTestContext(builder.Options);
    }

    private static IDomainEventSerializer CreateSerializer()
        => new JsonMessageSerializer(new EventTypeRegistry(typeof(DomainEvent).Assembly));

    private sealed class OutboxTestContext(DbContextOptions<OutboxTestContext> options)
        : Microsoft.EntityFrameworkCore.DbContext(options)
    {
        public DbSet<OutboxMessage> Messages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EventAggregate>(builder =>
            {
                builder.HasKey(aggregate => aggregate.Id);
                builder.Ignore(aggregate => aggregate.Events);
                builder.Ignore(aggregate => aggregate.Metadata);
                builder.Ignore(aggregate => aggregate.Version);
            });

            modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        }
    }

    /// <summary>Fails the save after the outbox interceptor has staged rows.</summary>
    private sealed class ThrowingSaveInterceptor : SaveChangesInterceptor
    {
        public bool ShouldThrow { get; set; } = true;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
            => ShouldThrow
                ? ValueTask.FromException<InterceptionResult<int>>(
                    new InvalidOperationException("The save was rejected."))
                : base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
