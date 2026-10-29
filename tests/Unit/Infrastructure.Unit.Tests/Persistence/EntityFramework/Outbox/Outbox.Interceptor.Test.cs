using Application.Common.Messaging.Outbox;
using Application.Common.Messaging.Serialization;

using Core.Common.Domain.Events;

using Infrastructure.Integrations.Messaging.Serialization;
using Infrastructure.Unit.Tests.Persistence.EntityFramework.Interceptors;
using Infrastructure.Persistence.EntityFramework.Outbox;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Outbox;

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

    [Fact(DisplayName = "Outbox interceptor should not stage the same event twice")]
    public async Task SavingChanges_WhenSavedAgain_ShouldNotDuplicateRows()
    {
        // Arrange
        await using OutboxTestContext context = CreateContext();
        var aggregate = new EventAggregate(Guid.NewGuid());
        aggregate.Raise(new TestDomainEvent("once"));
        context.Add(aggregate);
        await context.SaveChangesAsync();

        // Act
        await context.SaveChangesAsync();

        // Assert
        (await context.Messages.AsNoTracking().CountAsync()).Should().Be(1);
    }

    [Fact(DisplayName = "Outbox interceptor should leave events on the aggregate when the save fails")]
    public async Task SavingChanges_WhenSaveFails_ShouldLeaveEventsAndNoRows()
    {
        // Arrange
        await using OutboxTestContext context = CreateContext(new ThrowingSaveInterceptor());
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
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
            => ValueTask.FromException<InterceptionResult<int>>(
                new InvalidOperationException("The save was rejected."));
    }
}
