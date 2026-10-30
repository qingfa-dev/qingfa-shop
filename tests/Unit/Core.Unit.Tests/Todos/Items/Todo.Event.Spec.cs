using Core.Common.Domain.Events;
using Core.Todos.Items;

namespace Core.Unit.Tests.Todos.Items;

/// <summary>Specifications for the todo domain events.</summary>
public sealed class TodoEventSpec
{
    private static readonly Guid TodoId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ListId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid CategoryId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTimeOffset OccurredOn = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TodoCreated_ShouldCarryTheFullState()
    {
        var @event = new TodoCreated(
            TodoId,
            TenantId,
            "Buy milk",
            "Two litres",
            TodoPriority.High,
            OccurredOn.AddDays(1),
            ListId,
            OccurredOn,
            "alice");

        @event.TodoId.Should().Be(TodoId);
        @event.TenantId.Should().Be(TenantId);
        @event.Title.Should().Be("Buy milk");
        @event.Description.Should().Be("Two litres");
        @event.Priority.Should().Be(TodoPriority.High);
        @event.DueAtUtc.Should().Be(OccurredOn.AddDays(1));
        @event.ListId.Should().Be(ListId);
        @event.CreatedAtUtc.Should().Be(OccurredOn);
        @event.CreatedBy.Should().Be("alice");
    }

    [Fact]
    public void TodoCreated_ShouldForwardTheSuppliedMetadata()
    {
        var metadata = DefaultMetadata.Create();
        metadata.SetValue(MetadataConstant.Keyword.TenantId, TenantId);

        var @event = new TodoCreated(
            TodoId,
            TenantId,
            "Buy milk",
            null,
            TodoPriority.Medium,
            null,
            null,
            OccurredOn,
            "alice",
            metadata);

        @event.Metadata.ContainsKey(MetadataConstant.Keyword.TenantId).Should().BeTrue();
        ((Guid)@event.Metadata[MetadataConstant.Keyword.TenantId]).Should().Be(TenantId);
    }

    [Fact]
    public void TodoCreated_ShouldCarryTheWellKnownMetadataEnvelope()
    {
        var @event = new TodoCreated(
            TodoId,
            TenantId,
            "Buy milk",
            null,
            TodoPriority.Medium,
            null,
            null,
            OccurredOn,
            "alice");

        // Assert: A domain event always stamps its own event id and timestamp.
        @event.GetEventId().Should().NotBeNull();
        @event.GetTimestamp().Should().NotBeNull();
    }

    [Fact]
    public void TodoUpdated_ShouldCarryTheEditableState()
    {
        var @event = new TodoUpdated(
            TodoId,
            "Renamed",
            "details",
            TodoPriority.Urgent,
            OccurredOn.AddDays(2),
            ListId,
            OccurredOn,
            "bob");

        @event.TodoId.Should().Be(TodoId);
        @event.Title.Should().Be("Renamed");
        @event.Description.Should().Be("details");
        @event.Priority.Should().Be(TodoPriority.Urgent);
        @event.DueAtUtc.Should().Be(OccurredOn.AddDays(2));
        @event.ListId.Should().Be(ListId);
        @event.ModifiedAtUtc.Should().Be(OccurredOn);
        @event.ModifiedBy.Should().Be("bob");
    }

    [Fact]
    public void TodoStatusChanged_ShouldCarryTheTransition()
    {
        var @event = new TodoStatusChanged(
            TodoId,
            TenantId,
            TodoStatus.InProgress,
            TodoStatus.Completed,
            OccurredOn,
            "alice");

        @event.TodoId.Should().Be(TodoId);
        @event.TenantId.Should().Be(TenantId);
        @event.From.Should().Be(TodoStatus.InProgress);
        @event.To.Should().Be(TodoStatus.Completed);
        @event.OccurredAtUtc.Should().Be(OccurredOn);
        @event.Actor.Should().Be("alice");
    }

    [Fact]
    public void TodoCategoryAdded_ShouldCarryTheReference()
    {
        var @event = new TodoCategoryAdded(TodoId, CategoryId, OccurredOn, "alice");

        @event.TodoId.Should().Be(TodoId);
        @event.CategoryId.Should().Be(CategoryId);
        @event.OccurredAtUtc.Should().Be(OccurredOn);
        @event.Actor.Should().Be("alice");
    }

    [Fact]
    public void TodoCategoryRemoved_ShouldCarryTheReference()
    {
        var @event = new TodoCategoryRemoved(TodoId, CategoryId, OccurredOn, "bob");

        @event.TodoId.Should().Be(TodoId);
        @event.CategoryId.Should().Be(CategoryId);
        @event.OccurredAtUtc.Should().Be(OccurredOn);
        @event.Actor.Should().Be("bob");
    }

    [Fact]
    public void TodoListChanged_ShouldCarryTheListReference()
    {
        var @event = new TodoListChanged(TodoId, ListId, OccurredOn, "alice");

        @event.TodoId.Should().Be(TodoId);
        @event.ListId.Should().Be(ListId);
        @event.OccurredAtUtc.Should().Be(OccurredOn);
        @event.Actor.Should().Be("alice");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TodoDeleted_ShouldCarryTheHardDeleteFlag(bool isHard)
    {
        var @event = new TodoDeleted(TodoId, isHard, OccurredOn, "alice");

        @event.TodoId.Should().Be(TodoId);
        @event.IsHard.Should().Be(isHard);
        @event.OccurredAtUtc.Should().Be(OccurredOn);
        @event.Actor.Should().Be("alice");
    }

    [Fact]
    public void TodoRestored_ShouldCarryTheInstant()
    {
        var @event = new TodoRestored(TodoId, OccurredOn, "bob");

        @event.TodoId.Should().Be(TodoId);
        @event.OccurredAtUtc.Should().Be(OccurredOn);
        @event.Actor.Should().Be("bob");
    }

    [Fact]
    public void EveryTodoEvent_ShouldBeADomainEvent()
    {
        // Assert: Domain events are Mediator notifications, so the base type is not optional.
        typeof(TodoCreated).Should().BeAssignableTo<IDomainEvent>();
        typeof(TodoUpdated).Should().BeAssignableTo<IDomainEvent>();
        typeof(TodoStatusChanged).Should().BeAssignableTo<IDomainEvent>();
        typeof(TodoCategoryAdded).Should().BeAssignableTo<IDomainEvent>();
        typeof(TodoCategoryRemoved).Should().BeAssignableTo<IDomainEvent>();
        typeof(TodoListChanged).Should().BeAssignableTo<IDomainEvent>();
        typeof(TodoDeleted).Should().BeAssignableTo<IDomainEvent>();
        typeof(TodoRestored).Should().BeAssignableTo<IDomainEvent>();
    }
}