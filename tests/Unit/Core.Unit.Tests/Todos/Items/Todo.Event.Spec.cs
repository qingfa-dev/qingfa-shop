using Core.Common.Domain.Events;
using Core.Todos.Items;

namespace Core.Unit.Tests.Todos;

/// <summary>Specifications for the todo domain events.</summary>
public sealed class TodoEventSpec
{
    private static readonly Guid TodoId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset OccurredOn = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TodoCreated_ShouldCarryTheTodoIdentity()
    {
        var @event = new TodoCreated(TodoId, TenantId, "Buy milk");

        @event.TodoId.Should().Be(TodoId);
        @event.TenantId.Should().Be(TenantId);
        @event.Title.Should().Be("Buy milk");
    }

    [Fact]
    public void TodoCreated_ShouldForwardTheSuppliedMetadata()
    {
        var metadata = DefaultMetadata.Create();
        metadata.SetValue(MetadataConstant.Keyword.TenantId, TenantId);

        var @event = new TodoCreated(TodoId, TenantId, "Buy milk", metadata);

        @event.Metadata.ContainsKey(MetadataConstant.Keyword.TenantId).Should().BeTrue();
        ((Guid)@event.Metadata[MetadataConstant.Keyword.TenantId]).Should().Be(TenantId);
    }

    [Fact]
    public void TodoCreated_ShouldCarryTheWellKnownMetadataEnvelope()
    {
        var @event = new TodoCreated(TodoId, TenantId, "Buy milk");

        // Assert: A domain event always stamps its own event id and timestamp.
        @event.GetEventId().Should().NotBeNull();
        @event.GetTimestamp().Should().NotBeNull();
    }

    [Fact]
    public void TodoStarted_ShouldCarryTheTodoIdentity()
    {
        var @event = new TodoStarted(TodoId, TenantId);

        @event.TodoId.Should().Be(TodoId);
        @event.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void TodoCompleted_ShouldCarryTheCompletionTimestamp()
    {
        var @event = new TodoCompleted(TodoId, TenantId, OccurredOn);

        @event.TodoId.Should().Be(TodoId);
        @event.TenantId.Should().Be(TenantId);
        @event.CompletedAtUtc.Should().Be(OccurredOn);
    }

    [Fact]
    public void TodoCancelled_ShouldCarryTheTodoIdentity()
    {
        var @event = new TodoCancelled(TodoId, TenantId);

        @event.TodoId.Should().Be(TodoId);
        @event.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void TodoReopened_ShouldCarryTheTodoIdentity()
    {
        var @event = new TodoReopened(TodoId, TenantId);

        @event.TodoId.Should().Be(TodoId);
        @event.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void EveryTodoEvent_ShouldBeADomainEvent()
    {
        // Assert: Domain events are Mediator notifications, so the base type is not optional.
        typeof(TodoCreated).Should().BeAssignableTo<IDomainEvent>();
        typeof(TodoStarted).Should().BeAssignableTo<IDomainEvent>();
        typeof(TodoCompleted).Should().BeAssignableTo<IDomainEvent>();
        typeof(TodoCancelled).Should().BeAssignableTo<IDomainEvent>();
        typeof(TodoReopened).Should().BeAssignableTo<IDomainEvent>();
    }
}