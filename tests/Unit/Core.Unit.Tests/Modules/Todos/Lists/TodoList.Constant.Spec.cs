using Core.Modules.Todos.Lists;

namespace Core.Unit.Tests.Modules.Todos.Lists;

/// <summary>Specifications for the todo list constants.</summary>
public sealed class TodoListConstantSpec
{
    [Fact]
    public void Name_Constraints_ShouldBoundTheName()
    {
        TodoListConstant.Constraints.Name.MinLength.Should().Be(1);
        TodoListConstant.Constraints.Name.MaxLength.Should().Be(128);
    }

    [Fact]
    public void Description_Constraints_ShouldBoundTheDescription()
    {
        TodoListConstant.Constraints.Description.MaxLength.Should().Be(1000);
    }
}