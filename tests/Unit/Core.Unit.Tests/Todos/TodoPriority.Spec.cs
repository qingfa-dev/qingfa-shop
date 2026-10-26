using Core.Todos;

namespace Core.Unit.Tests.Todos;

/// <summary>Specifications for the todo priority enum.</summary>
public sealed class TodoPrioritySpec
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
}