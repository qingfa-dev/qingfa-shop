using Core.BuildingBlocks.Domain.Events;
using Core.Modules.Todos.Categories;

namespace Core.Unit.Tests.Modules.Todos.Categories;

/// <summary>Specifications for the todo category domain events.</summary>
public sealed class TodoCategoryEventSpec
{
    private static readonly Guid CategoryId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void TodoCategoryCreated_ShouldCarryTheCategoryIdentity()
    {
        var @event = new TodoCategoryCreated(CategoryId, TenantId, "Errands");

        @event.CategoryId.Should().Be(CategoryId);
        @event.TenantId.Should().Be(TenantId);
        @event.Name.Should().Be("Errands");
    }

    [Fact]
    public void TodoCategoryRenamed_ShouldCarryTheNewName()
    {
        var @event = new TodoCategoryRenamed(CategoryId, TenantId, "Chores");

        @event.CategoryId.Should().Be(CategoryId);
        @event.TenantId.Should().Be(TenantId);
        @event.Name.Should().Be("Chores");
    }

    [Fact]
    public void EveryTodoCategoryEvent_ShouldBeADomainEvent()
    {
        // Assert: Domain events are Mediator notifications, so the base type is not optional.
        typeof(TodoCategoryCreated).Should().BeAssignableTo<IDomainEvent>();
        typeof(TodoCategoryRenamed).Should().BeAssignableTo<IDomainEvent>();
    }
}