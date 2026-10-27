using Core.Todos.Categories;

namespace Core.Unit.Tests.Todos;

/// <summary>Specifications for the todo category constants.</summary>
public sealed class TodoCategoryConstantSpec
{
    [Fact]
    public void Name_Constraints_ShouldBoundTheName()
    {
        TodoCategoryConstant.Constraints.Name.MinLength.Should().Be(1);
        TodoCategoryConstant.Constraints.Name.MaxLength.Should().Be(128);
    }

    [Fact]
    public void Description_Constraints_ShouldBoundTheDescription()
    {
        TodoCategoryConstant.Constraints.Description.MaxLength.Should().Be(1000);
    }
}