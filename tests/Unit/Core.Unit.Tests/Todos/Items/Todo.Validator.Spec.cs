using System.Text.Json;

using Core.Todos.Items;

namespace Core.Unit.Tests.Todos.Items;

/// <summary>Specifications for the todo invariants.</summary>
public sealed class TodoValidatorSpec
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset CreatedAt = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Todo ValidTodo()
    {
        var result = Todo.Create(Guid.NewGuid(), TenantId, "Buy milk", CreatedAt, "alice");

        result.IsSuccess.Should().BeTrue();

        return result.Value;
    }

    private static Todo With(Func<TodoSnapshot, TodoSnapshot> mutate)
    {
        Todo valid = ValidTodo();
        TodoSnapshot snapshot = JsonSerializer.Deserialize<TodoSnapshot>(valid.CaptureSnapshot())!;
        Todo target = new TodoFactory().Create();
        target.RestoreSnapshot(JsonSerializer.Serialize(mutate(snapshot)));
        return target;
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
            TodoValidator.Validate<Todo>(null!),
            "Todo.Entity.Required");
    }

    [Fact]
    public void Validate_EmptyId_ShouldFailWithIdentifiableIdRequired()
    {
        ShouldFailWith(
            TodoValidator.Validate(With(snapshot => snapshot with { Id = Guid.Empty })),
            "Todo.Id.Required");
    }

    [Fact]
    public void Validate_EmptyTenantId_ShouldFailWithTenantableTenantIdRequired()
    {
        ShouldFailWith(
            TodoValidator.Validate(With(snapshot => snapshot with { TenantId = Guid.Empty })),
            "Todo.TenantId.Required");
    }

    [Fact]
    public void Validate_BlankTitle_ShouldFailWithTitleRequired()
    {
        ShouldFailWith(
            TodoValidator.Validate(With(snapshot => snapshot with { Title = "   " })),
            "Todo.Title.Required");
    }

    [Fact]
    public void Validate_LongTitle_ShouldFailWithTitleTooLong()
    {
        ShouldFailWith(
            TodoValidator.Validate(With(snapshot =>
                snapshot with { Title = new string('a', TodoConstant.Constraints.Title.MaxLength + 1) })),
            "Todo.Title.TooLong");
    }

    [Fact]
    public void Validate_LongDescription_ShouldFailWithDescriptionTooLong()
    {
        ShouldFailWith(
            TodoValidator.Validate(With(snapshot =>
                snapshot with { Description = new string('a', TodoConstant.Constraints.Description.MaxLength + 1) })),
            "Todo.Description.TooLong");
    }

    [Fact]
    public void Validate_UndefinedStatus_ShouldFailWithStatusInvalid()
    {
        ShouldFailWith(
            TodoValidator.Validate(With(snapshot => snapshot with { Status = (TodoStatus)99 })),
            "Todo.Status.Invalid");
    }

    [Fact]
    public void Validate_UndefinedPriority_ShouldFailWithPriorityInvalid()
    {
        ShouldFailWith(
            TodoValidator.Validate(With(snapshot => snapshot with { Priority = (TodoPriority)99 })),
            "Todo.Priority.Invalid");
    }

    [Fact]
    public void Validate_DueAtBeforeCreated_ShouldFailWithDueAtBeforeCreated()
    {
        ShouldFailWith(
            TodoValidator.Validate(With(snapshot => snapshot with { DueAtUtc = CreatedAt.AddDays(-1) })),
            "Todo.DueAt.BeforeCreated");
    }

    [Fact]
    public void Validate_EmptyCategoryId_ShouldFailWithCategoryRequired()
    {
        ShouldFailWith(
            TodoValidator.Validate(With(snapshot => snapshot with { CategoryIds = [Guid.Empty] })),
            "Todo.Category.Required");
    }

    [Fact]
    public void Validate_DuplicateCategories_ShouldFailWithCategoryDuplicate()
    {
        Guid categoryId = Guid.NewGuid();

        ShouldFailWith(
            TodoValidator.Validate(With(snapshot =>
                snapshot with { CategoryIds = [categoryId, categoryId] })),
            "Todo.Category.Duplicate");
    }

    [Fact]
    public void Validate_TooManyCategories_ShouldFailWithCategoriesTooMany()
    {
        Guid[] categories = [.. Enumerable
            .Range(0, TodoConstant.Constraints.Collection.MaxCategories + 1)
            .Select(_ => Guid.NewGuid())];

        ShouldFailWith(
            TodoValidator.Validate(With(snapshot => snapshot with { CategoryIds = categories })),
            "Todo.Categories.TooMany");
    }

    [Fact]
    public void Validate_EmptyListId_ShouldFailWithListIdRequired()
    {
        ShouldFailWith(
            TodoValidator.Validate(With(snapshot => snapshot with { ListId = Guid.Empty })),
            "Todo.ListId.Required");
    }

    [Fact]
    public void Validate_ValidTodo_ShouldSucceed()
    {
        var result = TodoValidator.Validate(ValidTodo());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateTransition_LegalMove_ShouldSucceed()
    {
        var result = TodoValidator.ValidateTransition(TodoStatus.Pending, TodoStatus.InProgress);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateTransition_IllegalMove_ShouldFailWithStatusTransitionInvalid()
    {
        ShouldFailWith(
            TodoValidator.ValidateTransition(TodoStatus.Pending, TodoStatus.Completed),
            "Todo.Status.TransitionInvalid");
    }

    [Fact]
    public void ValidateDeleted_DeletedTodo_ShouldSucceed()
    {
        Result result = TodoValidator.ValidateDeleted(isDeleted: true);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateDeleted_LiveTodo_ShouldFailWithDeletedNotMutable()
    {
        ShouldFailWith(
            TodoValidator.ValidateDeleted(isDeleted: false),
            "Todo.Deleted.NotMutable");
    }
}