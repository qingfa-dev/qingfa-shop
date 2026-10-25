using Core.Common.Domain.Events;
using Core.Todos.Items;

namespace Core.Tests.Todos.Items;

/// <summary>Specifications for the todo domain events.</summary>
public sealed class TodoItemEventSpec
{
    private static readonly Guid TodoId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset OccurredOn = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TodoItemCreated_ShouldCarryTheTodoIdentity()
    {
        var @event = new TodoItemCreated(TodoId, TenantId, "Buy milk");

        @event.TodoId.Should().Be(TodoId);
        @event.TenantId.Should().Be(TenantId);
        @event.Title.Should().Be("Buy milk");
    }

    [Fact]
    public void TodoItemCreated_ShouldForwardTheSuppliedMetadata()
    {
        var metadata = DefaultMetadata.Create();
        metadata.SetValue(MetadataConstant.Keyword.TenantId, TenantId);

        var @event = new TodoItemCreated(TodoId, TenantId, "Buy milk", metadata);

        @event.Metadata.ContainsKey(MetadataConstant.Keyword.TenantId).Should().BeTrue();
        ((Guid)@event.Metadata[MetadataConstant.Keyword.TenantId]).Should().Be(TenantId);
    }

    [Fact]
    public void TodoItemCreated_ShouldCarryTheWellKnownMetadataEnvelope()
    {
        var @event = new TodoItemCreated(TodoId, TenantId, "Buy milk");

        // Assert: A domain event always stamps its own event id and timestamp.
        @event.GetEventId().Should().NotBeNull();
        @event.GetTimestamp().Should().NotBeNull();
    }

    [Fact]
    public void TodoItemStarted_ShouldCarryTheTodoIdentity()
    {
        var @event = new TodoItemStarted(TodoId, TenantId);

        @event.TodoId.Should().Be(TodoId);
        @event.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void TodoItemCompleted_ShouldCarryTheCompletionTimestamp()
    {
        var @event = new TodoItemCompleted(TodoId, TenantId, OccurredOn);

        @event.TodoId.Should().Be(TodoId);
        @event.TenantId.Should().Be(TenantId);
        @event.CompletedAtUtc.Should().Be(OccurredOn);
    }

    [Fact]
    public void TodoItemCancelled_ShouldCarryTheTodoIdentity()
    {
        var @event = new TodoItemCancelled(TodoId, TenantId);

        @event.TodoId.Should().Be(TodoId);
        @event.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void TodoItemReopened_ShouldCarryTheTodoIdentity()
    {
        var @event = new TodoItemReopened(TodoId, TenantId);

        @event.TodoId.Should().Be(TodoId);
        @event.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void EveryTodoEvent_ShouldBeADomainEvent()
    {
        // Assert: Domain events are Mediator notifications, so the base type is not optional.
        typeof(TodoItemCreated).Should().BeAssignableTo<IDomainEvent>();
        typeof(TodoItemStarted).Should().BeAssignableTo<IDomainEvent>();
        typeof(TodoItemCompleted).Should().BeAssignableTo<IDomainEvent>();
        typeof(TodoItemCancelled).Should().BeAssignableTo<IDomainEvent>();
        typeof(TodoItemReopened).Should().BeAssignableTo<IDomainEvent>();
    }
}