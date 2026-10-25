using Core.Todos.Items;

namespace Core.Tests.Todos.Items;

/// <summary>Specifications for the todo priority enum.</summary>
public sealed class TodoItemEnumerateSpec
{
    [Fact]
    public void TodoPriority_Values_ShouldBeStable()
    {
        // Compute: The persisted integers must not shift when a member is inserted later.
        ((int)TodoPriority.Low).Should().Be(0);
        ((int)TodoPriority.Medium).Should().Be(1);
        ((int)TodoPriority.High).Should().Be(2);
        ((int)TodoPriority.Urgent).Should().Be(3);
    }

    [Fact]
    public void TodoPriority_ShouldBeOrderedByUrgency()
    {
        // Compute: Higher urgency must compare greater so the enum can be sorted.
        ((int)TodoPriority.Low).Should().BeLessThan((int)TodoPriority.Medium);
        ((int)TodoPriority.Medium).Should().BeLessThan((int)TodoPriority.High);
        ((int)TodoPriority.High).Should().BeLessThan((int)TodoPriority.Urgent);
    }

    [Fact]
    public void TodoStatus_Values_ShouldBeStable()
    {
        // Compute: The persisted integers must not shift when a member is inserted later.
        ((int)TodoStatus.Pending).Should().Be(0);
        ((int)TodoStatus.InProgress).Should().Be(1);
        ((int)TodoStatus.Completed).Should().Be(2);
        ((int)TodoStatus.Cancelled).Should().Be(3);
    }

    [Theory]
    [InlineData(TodoStatus.Pending, TodoStatus.Pending, false)]
    [InlineData(TodoStatus.Pending, TodoStatus.InProgress, true)]
    [InlineData(TodoStatus.Pending, TodoStatus.Completed, false)]
    [InlineData(TodoStatus.Pending, TodoStatus.Cancelled, true)]
    [InlineData(TodoStatus.InProgress, TodoStatus.Pending, false)]
    [InlineData(TodoStatus.InProgress, TodoStatus.InProgress, false)]
    [InlineData(TodoStatus.InProgress, TodoStatus.Completed, true)]
    [InlineData(TodoStatus.InProgress, TodoStatus.Cancelled, true)]
    [InlineData(TodoStatus.Completed, TodoStatus.Pending, true)]
    [InlineData(TodoStatus.Completed, TodoStatus.InProgress, false)]
    [InlineData(TodoStatus.Completed, TodoStatus.Completed, false)]
    [InlineData(TodoStatus.Completed, TodoStatus.Cancelled, false)]
    [InlineData(TodoStatus.Cancelled, TodoStatus.Pending, true)]
    [InlineData(TodoStatus.Cancelled, TodoStatus.InProgress, false)]
    [InlineData(TodoStatus.Cancelled, TodoStatus.Completed, false)]
    [InlineData(TodoStatus.Cancelled, TodoStatus.Cancelled, false)]
    public void CanTransitionTo_FromTo_ShouldDecide(
        TodoStatus current,
        TodoStatus next,
        bool expected)
    {
        current.CanTransitionTo(next).Should().Be(expected);
    }

    [Theory]
    [InlineData(TodoStatus.Pending, false)]
    [InlineData(TodoStatus.InProgress, false)]
    [InlineData(TodoStatus.Completed, true)]
    [InlineData(TodoStatus.Cancelled, true)]
    public void IsTerminal_Status_ShouldReportEndStates(TodoStatus status, bool expected)
    {
        status.IsTerminal().Should().Be(expected);
    }
}