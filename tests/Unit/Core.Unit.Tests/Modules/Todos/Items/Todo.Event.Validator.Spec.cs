using Core.Modules.Todos.Items;

namespace Core.Unit.Tests.Modules.Todos.Items;

/// <summary>Tests for todo event payload validation.</summary>
[Trait("Category", "Unit")]
public sealed class TodoEventValidatorSpec
{
    private static readonly Guid TodoId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Created_WithValidPayload_ShouldPass()
        => TodoEventValidator.Validate(new TodoCreated(
            TodoId,
            TenantId,
            "title",
            null,
            TodoPriority.Medium,
            null,
            null,
            Now,
            "alice")).IsSuccess.Should().BeTrue();

    [Fact]
    public void Created_WithoutTenant_ShouldFail()
    {
        Result result = TodoEventValidator.Validate(new TodoCreated(
            TodoId,
            Guid.Empty,
            "title",
            null,
            TodoPriority.Medium,
            null,
            null,
            Now,
            "alice"));

        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoOutcome.Failure.TenantIdRequired.Code);
    }

    [Fact]
    public void StatusChanged_WithoutAChange_ShouldFail()
    {
        Result result = TodoEventValidator.Validate(new TodoStatusChanged(
            TodoId,
            TenantId,
            TodoStatus.Pending,
            TodoStatus.Pending,
            Now,
            "alice"));

        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Todo.Event.Invalid");
    }

    [Fact]
    public void Category_WithEmptyId_ShouldFail()
    {
        Result result = TodoEventValidator.Validate(
            new TodoCategoryAdded(TodoId, Guid.Empty, Now, "alice"));

        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoOutcome.Failure.CategoryRequired.Code);
    }

    [Fact]
    public void UnknownEvent_ShouldFailWithUnsupported()
    {
        Result result = TodoEventValidator.Validate(new UnsupportedEvent());

        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Todo.Event.Unsupported");
    }

    private sealed record UnsupportedEvent : Core.BuildingBlocks.Domain.Events.DomainEvent;
}
