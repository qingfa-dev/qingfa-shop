using Core.Todos.Items;

namespace Core.Unit.Tests.Todos.Items;

/// <summary>Specifications for the todo failure errors.</summary>
public sealed class TodoOutcomeSpec
{
    [Fact]
    public void TitleRequired_ShouldBeBadRequest()
    {
        TodoOutcome.Failure.TitleRequired.Status.Should().Be(400);
        TodoOutcome.Failure.TitleRequired.Code.Should().Be("Todo.Title.Required");
    }

    [Fact]
    public void TitleTooLong_ShouldBeBadRequest()
    {
        TodoOutcome.Failure.TitleTooLong.Status.Should().Be(400);
        TodoOutcome.Failure.TitleTooLong.Code.Should().Be("Todo.Title.TooLong");
    }

    [Fact]
    public void DescriptionTooLong_ShouldBeBadRequest()
    {
        TodoOutcome.Failure.DescriptionTooLong.Status.Should().Be(400);
        TodoOutcome.Failure.DescriptionTooLong.Code.Should().Be("Todo.Description.TooLong");
    }

    [Fact]
    public void StatusInvalid_ShouldBeBadRequest()
    {
        TodoOutcome.Failure.StatusInvalid.Status.Should().Be(400);
        TodoOutcome.Failure.StatusInvalid.Code.Should().Be("Todo.Status.Invalid");
    }

    [Fact]
    public void PriorityInvalid_ShouldBeBadRequest()
    {
        TodoOutcome.Failure.PriorityInvalid.Status.Should().Be(400);
        TodoOutcome.Failure.PriorityInvalid.Code.Should().Be("Todo.Priority.Invalid");
    }

    [Fact]
    public void DueAtBeforeCreated_ShouldBeBadRequest()
    {
        TodoOutcome.Failure.DueAtBeforeCreated.Status.Should().Be(400);
        TodoOutcome.Failure.DueAtBeforeCreated.Code.Should().Be("Todo.DueAt.BeforeCreated");
    }

    [Fact]
    public void CategoryRequired_ShouldBeBadRequest()
    {
        TodoOutcome.Failure.CategoryRequired.Status.Should().Be(400);
        TodoOutcome.Failure.CategoryRequired.Code.Should().Be("Todo.Category.Required");
    }

    [Fact]
    public void CategoryDuplicate_ShouldBeConflict()
    {
        TodoOutcome.Failure.CategoryDuplicate.Status.Should().Be(409);
        TodoOutcome.Failure.CategoryDuplicate.Code.Should().Be("Todo.Category.Duplicate");
    }

    [Fact]
    public void CategoryNotFound_ShouldBeUnprocessableEntity()
    {
        TodoOutcome.Failure.CategoryNotFound.Status.Should().Be(422);
        TodoOutcome.Failure.CategoryNotFound.Code.Should().Be("Todo.Category.NotFound");
    }

    [Fact]
    public void CategoriesTooMany_ShouldBeBadRequest()
    {
        TodoOutcome.Failure.CategoriesTooMany.Status.Should().Be(400);
        TodoOutcome.Failure.CategoriesTooMany.Code.Should().Be("Todo.Categories.TooMany");
    }

    [Fact]
    public void ListIdRequired_ShouldBeBadRequest()
    {
        TodoOutcome.Failure.ListIdRequired.Status.Should().Be(400);
        TodoOutcome.Failure.ListIdRequired.Code.Should().Be("Todo.ListId.Required");
    }

    [Fact]
    public void StatusTransitionInvalid_ShouldBeConflict()
    {
        TodoOutcome.Failure.StatusTransitionInvalid.Status.Should().Be(409);
        TodoOutcome.Failure.StatusTransitionInvalid.Code.Should().Be("Todo.Status.TransitionInvalid");
    }

    [Fact]
    public void DeletedNotMutable_ShouldBeConflict()
    {
        TodoOutcome.Failure.DeletedNotMutable.Status.Should().Be(409);
        TodoOutcome.Failure.DeletedNotMutable.Code.Should().Be("Todo.Deleted.NotMutable");
    }

    [Fact]
    public void EntityRequired_ShouldBeUnprocessableEntity()
    {
        TodoOutcome.Failure.EntityRequired.Status.Should().Be(422);
        TodoOutcome.Failure.EntityRequired.Code.Should().Be("Todo.Entity.Required");
    }

    [Fact]
    public void FailureCodes_ShouldBeUnique()
    {
        var codes = new[]
        {
            TodoOutcome.Failure.EntityRequired.Code,
            TodoOutcome.Failure.TitleRequired.Code,
            TodoOutcome.Failure.TitleTooLong.Code,
            TodoOutcome.Failure.DescriptionTooLong.Code,
            TodoOutcome.Failure.StatusInvalid.Code,
            TodoOutcome.Failure.PriorityInvalid.Code,
            TodoOutcome.Failure.DueAtBeforeCreated.Code,
            TodoOutcome.Failure.CategoryRequired.Code,
            TodoOutcome.Failure.CategoryDuplicate.Code,
            TodoOutcome.Failure.CategoryNotFound.Code,
            TodoOutcome.Failure.CategoriesTooMany.Code,
            TodoOutcome.Failure.ListIdRequired.Code,
            TodoOutcome.Failure.StatusTransitionInvalid.Code,
            TodoOutcome.Failure.DeletedNotMutable.Code
        };

        codes.Should().OnlyHaveUniqueItems();
    }
}