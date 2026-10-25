using Core.Todos.Items;

namespace Core.Tests.Todos.Items;

/// <summary>Specifications for the todo invariants.</summary>
public sealed class TodoItemValidatorSpec
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset CreatedAt = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static TodoItem ValidTodo()
    {
        var result = TodoItem.Create(Guid.NewGuid(), TenantId, "Buy milk", CreatedAt, "alice");

        result.IsSuccess.Should().BeTrue();

        return result.Value;
    }

    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact]
    public void Validate_NullTodo_ShouldFailWithEntityRequired()
    {
        ShouldFailWith(
            TodoItemValidator.Validate(null!),
            "TodoItem.Entity.Required");
    }

    [Fact]
    public void Validate_EmptyTenantId_ShouldFailWithTenantableTenantIdRequired()
    {
        var todo = ValidTodo();
        todo.TenantId = Guid.Empty;

        ShouldFailWith(
            TodoItemValidator.Validate(todo),
            "Tenantable.TenantId.Required");
    }

    [Fact]
    public void Validate_BlankTitle_ShouldFailWithTitleRequired()
    {
        var todo = ValidTodo();
        todo.Title = "   ";

        ShouldFailWith(
            TodoItemValidator.Validate(todo),
            "TodoItem.Title.Required");
    }

    [Fact]
    public void Validate_LongTitle_ShouldFailWithTitleTooLong()
    {
        var todo = ValidTodo();
        todo.Title = new string('a', TodoItemConstant.Constraints.Title.MaxLength + 1);

        ShouldFailWith(
            TodoItemValidator.Validate(todo),
            "TodoItem.Title.TooLong");
    }

    [Fact]
    public void Validate_LongDescription_ShouldFailWithDescriptionTooLong()
    {
        var todo = ValidTodo();
        todo.Description = new string('a', TodoItemConstant.Constraints.Description.MaxLength + 1);

        ShouldFailWith(
            TodoItemValidator.Validate(todo),
            "TodoItem.Description.TooLong");
    }

    [Fact]
    public void Validate_UndefinedStatus_ShouldFailWithStatusInvalid()
    {
        var todo = ValidTodo();
        todo.Status = (TodoStatus)99;

        ShouldFailWith(
            TodoItemValidator.Validate(todo),
            "TodoItem.Status.Invalid");
    }

    [Fact]
    public void Validate_UndefinedPriority_ShouldFailWithPriorityInvalid()
    {
        var todo = ValidTodo();
        todo.Priority = (TodoPriority)99;

        ShouldFailWith(
            TodoItemValidator.Validate(todo),
            "TodoItem.Priority.Invalid");
    }

    [Fact]
    public void Validate_DueAtBeforeCreated_ShouldFailWithDueAtBeforeCreated()
    {
        var todo = ValidTodo();
        todo.DueAtUtc = CreatedAt.AddDays(-1);

        ShouldFailWith(
            TodoItemValidator.Validate(todo),
            "TodoItem.DueAt.BeforeCreated");
    }

    [Fact]
    public void Validate_EmptyCategoryId_ShouldFailWithCategoryRequired()
    {
        var todo = ValidTodo();
        todo.CategoryIds.Add(Guid.Empty);

        ShouldFailWith(
            TodoItemValidator.Validate(todo),
            "TodoItem.Category.Required");
    }

    [Fact]
    public void Validate_DuplicateCategories_ShouldFailWithCategoryDuplicate()
    {
        var todo = ValidTodo();
        var categoryId = Guid.NewGuid();
        todo.CategoryIds.Add(categoryId);
        todo.CategoryIds.Add(categoryId);

        ShouldFailWith(
            TodoItemValidator.Validate(todo),
            "TodoItem.Category.Duplicate");
    }

    [Fact]
    public void Validate_TooManyCategories_ShouldFailWithCategoriesTooMany()
    {
        var todo = ValidTodo();

        for (var i = 0; i <= TodoItemConstant.Constraints.Collection.MaxCategories; i++)
            todo.CategoryIds.Add(Guid.NewGuid());

        ShouldFailWith(
            TodoItemValidator.Validate(todo),
            "TodoItem.Categories.TooMany");
    }

    [Fact]
    public void Validate_EmptyListId_ShouldFailWithListIdRequired()
    {
        var todo = ValidTodo();
        todo.ListId = Guid.Empty;

        ShouldFailWith(
            TodoItemValidator.Validate(todo),
            "TodoItem.ListId.Required");
    }

    [Fact]
    public void Validate_ValidTodo_ShouldSucceed()
    {
        var result = TodoItemValidator.Validate(ValidTodo());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateTransition_LegalMove_ShouldSucceed()
    {
        var result = TodoItemValidator.ValidateTransition(TodoStatus.Pending, TodoStatus.InProgress);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateTransition_IllegalMove_ShouldFailWithStatusTransitionInvalid()
    {
        ShouldFailWith(
            TodoItemValidator.ValidateTransition(TodoStatus.Pending, TodoStatus.Completed),
            "TodoItem.Status.TransitionInvalid");
    }
}