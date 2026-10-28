using Core.Todos.Categories;

namespace Core.Unit.Tests.Todos.Categories;

/// <summary>Specifications for the todo category failure errors.</summary>
public sealed class TodoCategoryOutcomeSpec
{
    [Fact]
    public void EntityRequired_ShouldBeUnprocessableEntity()
    {
        TodoCategoryOutcome.Failure.EntityRequired.Status.Should().Be(422);
        TodoCategoryOutcome.Failure.EntityRequired.Code.Should().Be("TodoCategory.Entity.Required");
    }

    [Fact]
    public void NameRequired_ShouldBeBadRequest()
    {
        TodoCategoryOutcome.Failure.NameRequired.Status.Should().Be(400);
        TodoCategoryOutcome.Failure.NameRequired.Code.Should().Be("TodoCategory.Name.Required");
    }

    [Fact]
    public void NameTooLong_ShouldBeBadRequest()
    {
        TodoCategoryOutcome.Failure.NameTooLong.Status.Should().Be(400);
        TodoCategoryOutcome.Failure.NameTooLong.Code.Should().Be("TodoCategory.Name.TooLong");
    }

    [Fact]
    public void DescriptionTooLong_ShouldBeBadRequest()
    {
        TodoCategoryOutcome.Failure.DescriptionTooLong.Status.Should().Be(400);
        TodoCategoryOutcome.Failure.DescriptionTooLong.Code.Should().Be("TodoCategory.Description.TooLong");
    }

    [Fact]
    public void DeletedNotMutable_ShouldBeConflict()
    {
        TodoCategoryOutcome.Failure.DeletedNotMutable.Status.Should().Be(409);
        TodoCategoryOutcome.Failure.DeletedNotMutable.Code.Should().Be("TodoCategory.Deleted.NotMutable");
    }

    [Fact]
    public void FailureCodes_ShouldBeUnique()
    {
        var codes = new[]
        {
            TodoCategoryOutcome.Failure.EntityRequired.Code,
            TodoCategoryOutcome.Failure.NameRequired.Code,
            TodoCategoryOutcome.Failure.NameTooLong.Code,
            TodoCategoryOutcome.Failure.DescriptionTooLong.Code,
            TodoCategoryOutcome.Failure.DeletedNotMutable.Code
        };

        codes.Should().OnlyHaveUniqueItems();
    }
}