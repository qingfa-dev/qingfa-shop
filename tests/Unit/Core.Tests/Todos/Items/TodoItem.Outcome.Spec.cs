using Core.Todos.Items;

namespace Core.Tests.Todos.Items;

/// <summary>Specifications for the todo failure errors.</summary>
public sealed class TodoItemOutcomeSpec
{
    [Fact]
    public void TitleRequired_ShouldBeBadRequest()
    {
        TodoItemOutcome.Failure.TitleRequired.Status.Should().Be(400);
        TodoItemOutcome.Failure.TitleRequired.Code.Should().Be("TodoItem.Title.Required");
    }

    [Fact]
    public void TitleTooLong_ShouldBeBadRequest()
    {
        TodoItemOutcome.Failure.TitleTooLong.Status.Should().Be(400);
        TodoItemOutcome.Failure.TitleTooLong.Code.Should().Be("TodoItem.Title.TooLong");
    }

    [Fact]
    public void DescriptionTooLong_ShouldBeBadRequest()
    {
        TodoItemOutcome.Failure.DescriptionTooLong.Status.Should().Be(400);
        TodoItemOutcome.Failure.DescriptionTooLong.Code.Should().Be("TodoItem.Description.TooLong");
    }

    [Fact]
    public void StatusInvalid_ShouldBeBadRequest()
    {
        TodoItemOutcome.Failure.StatusInvalid.Status.Should().Be(400);
        TodoItemOutcome.Failure.StatusInvalid.Code.Should().Be("TodoItem.Status.Invalid");
    }

    [Fact]
    public void PriorityInvalid_ShouldBeBadRequest()
    {
        TodoItemOutcome.Failure.PriorityInvalid.Status.Should().Be(400);
        TodoItemOutcome.Failure.PriorityInvalid.Code.Should().Be("TodoItem.Priority.Invalid");
    }

    [Fact]
    public void DueAtBeforeCreated_ShouldBeBadRequest()
    {
        TodoItemOutcome.Failure.DueAtBeforeCreated.Status.Should().Be(400);
        TodoItemOutcome.Failure.DueAtBeforeCreated.Code.Should().Be("TodoItem.DueAt.BeforeCreated");
    }

    [Fact]
    public void CategoryRequired_ShouldBeBadRequest()
    {
        TodoItemOutcome.Failure.CategoryRequired.Status.Should().Be(400);
        TodoItemOutcome.Failure.CategoryRequired.Code.Should().Be("TodoItem.Category.Required");
    }

    [Fact]
    public void CategoryDuplicate_ShouldBeConflict()
    {
        TodoItemOutcome.Failure.CategoryDuplicate.Status.Should().Be(409);
        TodoItemOutcome.Failure.CategoryDuplicate.Code.Should().Be("TodoItem.Category.Duplicate");
    }

    [Fact]
    public void CategoryNotFound_ShouldBeUnprocessableEntity()
    {
        TodoItemOutcome.Failure.CategoryNotFound.Status.Should().Be(422);
        TodoItemOutcome.Failure.CategoryNotFound.Code.Should().Be("TodoItem.Category.NotFound");
    }

    [Fact]
    public void CategoriesTooMany_ShouldBeBadRequest()
    {
        TodoItemOutcome.Failure.CategoriesTooMany.Status.Should().Be(400);
        TodoItemOutcome.Failure.CategoriesTooMany.Code.Should().Be("TodoItem.Categories.TooMany");
    }

    [Fact]
    public void ListIdRequired_ShouldBeBadRequest()
    {
        TodoItemOutcome.Failure.ListIdRequired.Status.Should().Be(400);
        TodoItemOutcome.Failure.ListIdRequired.Code.Should().Be("TodoItem.ListId.Required");
    }

    [Fact]
    public void StatusTransitionInvalid_ShouldBeConflict()
    {
        TodoItemOutcome.Failure.StatusTransitionInvalid.Status.Should().Be(409);
        TodoItemOutcome.Failure.StatusTransitionInvalid.Code.Should().Be("TodoItem.Status.TransitionInvalid");
    }

    [Fact]
    public void DeletedNotMutable_ShouldBeConflict()
    {
        TodoItemOutcome.Failure.DeletedNotMutable.Status.Should().Be(409);
        TodoItemOutcome.Failure.DeletedNotMutable.Code.Should().Be("TodoItem.Deleted.NotMutable");
    }

    [Fact]
    public void EntityRequired_ShouldBeUnprocessableEntity()
    {
        TodoItemOutcome.Failure.EntityRequired.Status.Should().Be(422);
        TodoItemOutcome.Failure.EntityRequired.Code.Should().Be("TodoItem.Entity.Required");
    }

    [Fact]
    public void FailureCodes_ShouldBeUnique()
    {
        var codes = new[]
        {
            TodoItemOutcome.Failure.EntityRequired.Code,
            TodoItemOutcome.Failure.TitleRequired.Code,
            TodoItemOutcome.Failure.TitleTooLong.Code,
            TodoItemOutcome.Failure.DescriptionTooLong.Code,
            TodoItemOutcome.Failure.StatusInvalid.Code,
            TodoItemOutcome.Failure.PriorityInvalid.Code,
            TodoItemOutcome.Failure.DueAtBeforeCreated.Code,
            TodoItemOutcome.Failure.CategoryRequired.Code,
            TodoItemOutcome.Failure.CategoryDuplicate.Code,
            TodoItemOutcome.Failure.CategoryNotFound.Code,
            TodoItemOutcome.Failure.CategoriesTooMany.Code,
            TodoItemOutcome.Failure.ListIdRequired.Code,
            TodoItemOutcome.Failure.StatusTransitionInvalid.Code,
            TodoItemOutcome.Failure.DeletedNotMutable.Code
        };

        codes.Should().OnlyHaveUniqueItems();
    }
}