using Core.Common.Domain.Events;

using Core.Todos;

namespace Core.Unit.Tests.Todos;

/// <summary>Specifications for the todo list domain events.</summary>
public sealed class TodoListEventSpec
{
    private static readonly Guid ListId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void TodoListCreated_ShouldCarryTheListIdentity()
    {
        var @event = new TodoListCreated(ListId, TenantId, "Groceries");

        @event.ListId.Should().Be(ListId);
        @event.TenantId.Should().Be(TenantId);
        @event.Name.Should().Be("Groceries");
    }

    [Fact]
    public void TodoListRenamed_ShouldCarryTheNewName()
    {
        var @event = new TodoListRenamed(ListId, TenantId, "Hardware");

        @event.ListId.Should().Be(ListId);
        @event.TenantId.Should().Be(TenantId);
        @event.Name.Should().Be("Hardware");
    }

    [Fact]
    public void EveryTodoListEvent_ShouldBeADomainEvent()
    {
        // Assert: Domain events are Mediator notifications, so the base type is not optional.
        typeof(TodoListCreated).Should().BeAssignableTo<IDomainEvent>();
        typeof(TodoListRenamed).Should().BeAssignableTo<IDomainEvent>();
    }
}