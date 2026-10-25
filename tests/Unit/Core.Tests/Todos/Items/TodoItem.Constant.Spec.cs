using Core.Todos.Items;

namespace Core.Tests.Todos.Items;

/// <summary>Specifications for the todo constants.</summary>
public sealed class TodoItemConstantSpec
{
    [Fact]
    public void Title_Constraints_ShouldBoundTheTitle()
    {
        TodoItemConstant.Constraints.Title.MinLength.Should().Be(1);
        TodoItemConstant.Constraints.Title.MaxLength.Should().Be(200);
    }

    [Fact]
    public void Description_Constraints_ShouldBoundTheDescription()
    {
        TodoItemConstant.Constraints.Description.MaxLength.Should().Be(4000);
    }

    [Fact]
    public void Collection_Constraints_ShouldBoundTheCategoryReferences()
    {
        TodoItemConstant.Constraints.Collection.MaxCategories.Should().Be(20);
    }

    [Fact]
    public void Defaults_ShouldStartATodoPendingAtMediumPriority()
    {
        TodoItemConstant.Defaults.Status.Should().Be(TodoStatus.Pending);
        TodoItemConstant.Defaults.Priority.Should().Be(TodoPriority.Medium);
    }
}