using Core.Modules.Todos.Lists;

namespace Core.Unit.Tests.Modules.Todos.Lists;

/// <summary>Specifications for the todo list failure errors.</summary>
public sealed class TodoListOutcomeSpec
{
    [Fact]
    public void EntityRequired_ShouldBeUnprocessableEntity()
    {
        TodoListOutcome.Failure.EntityRequired.Status.Should().Be(422);
        TodoListOutcome.Failure.EntityRequired.Code.Should().Be("TodoList.Entity.Required");
    }

    [Fact]
    public void NameRequired_ShouldBeBadRequest()
    {
        TodoListOutcome.Failure.NameRequired.Status.Should().Be(400);
        TodoListOutcome.Failure.NameRequired.Code.Should().Be("TodoList.Name.Required");
    }

    [Fact]
    public void NameTooLong_ShouldBeBadRequest()
    {
        TodoListOutcome.Failure.NameTooLong.Status.Should().Be(400);
        TodoListOutcome.Failure.NameTooLong.Code.Should().Be("TodoList.Name.TooLong");
    }

    [Fact]
    public void DescriptionTooLong_ShouldBeBadRequest()
    {
        TodoListOutcome.Failure.DescriptionTooLong.Status.Should().Be(400);
        TodoListOutcome.Failure.DescriptionTooLong.Code.Should().Be("TodoList.Description.TooLong");
    }

    [Fact]
    public void DeletedNotMutable_ShouldBeConflict()
    {
        TodoListOutcome.Failure.DeletedNotMutable.Status.Should().Be(409);
        TodoListOutcome.Failure.DeletedNotMutable.Code.Should().Be("TodoList.Deleted.NotMutable");
    }

    [Fact]
    public void FailureCodes_ShouldBeUnique()
    {
        var codes = new[]
        {
            TodoListOutcome.Failure.EntityRequired.Code,
            TodoListOutcome.Failure.NameRequired.Code,
            TodoListOutcome.Failure.NameTooLong.Code,
            TodoListOutcome.Failure.DescriptionTooLong.Code,
            TodoListOutcome.Failure.DeletedNotMutable.Code
        };

        codes.Should().OnlyHaveUniqueItems();
    }
}