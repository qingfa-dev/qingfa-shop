using Core.Modules.Todos.Items;

namespace Core.Unit.Tests.Modules.Todos.Items;

/// <summary>Specifications for the todo constants.</summary>
public sealed class TodoConstantSpec
{
    [Fact]
    public void Title_Constraints_ShouldBoundTheTitle()
    {
        TodoConstant.Constraints.Title.MinLength.Should().Be(1);
        TodoConstant.Constraints.Title.MaxLength.Should().Be(200);
    }

    [Fact]
    public void Description_Constraints_ShouldBoundTheDescription()
    {
        TodoConstant.Constraints.Description.MaxLength.Should().Be(4000);
    }

    [Fact]
    public void Collection_Constraints_ShouldBoundTheCategoryReferences()
    {
        TodoConstant.Constraints.Collection.MaxCategories.Should().Be(20);
    }

    [Fact]
    public void Defaults_ShouldStartATodoPendingAtMediumPriority()
    {
        TodoConstant.Defaults.Status.Should().Be(TodoStatus.Pending);
        TodoConstant.Defaults.Priority.Should().Be(TodoPriority.Medium);
    }
}