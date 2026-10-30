using Core.Todos.Items;

namespace Core.Unit.Tests.Todos.Items;

/// <summary>Tests for the shared monadic todo guards.</summary>
[Trait("Category", "Unit")]
public sealed class TodoExtensionSpec
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void EnsureMutable_OnLiveTodo_ShouldKeepSuccess()
    {
        Result<Todo> result = Result<Todo>.Success(Create()).EnsureMutable();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void EnsureMutable_OnDeletedTodo_ShouldFail()
    {
        Todo todo = Create();
        todo.Delete(isHard: false, Now.AddMinutes(1), "alice");

        Result<Todo> result = Result<Todo>.Success(todo).EnsureMutable();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoOutcome.Failure.DeletedNotMutable.Code);
    }

    [Fact]
    public void EnsureMutable_OnHardDeletedTodo_ShouldFailEvenForCleanup()
    {
        Todo todo = Create();
        todo.Delete(isHard: true, Now.AddMinutes(1), "alice");

        Result<Todo> result = Result<Todo>.Success(todo).EnsureMutable(allowDeleted: true);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoOutcome.Failure.HardDeletedNotFound.Code);
    }

    [Fact]
    public void FieldGuards_ShouldPreserveTheValidatorCode()
    {
        Result<Todo> title = Result<Todo>.Success(Create()).EnsureTitle(" ");

        title.IsFailure.Should().BeTrue();
        title.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoOutcome.Failure.TitleRequired.Code);
    }

    [Fact]
    public void EnsureTransition_ShouldRejectAnIllegalMove()
    {
        Todo todo = Create();

        Result<Todo> result = Result<Todo>.Success(todo)
            .EnsureTransition(TodoStatus.Completed);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoOutcome.Failure.StatusTransitionInvalid.Code);
    }

    [Fact]
    public void EnsureCategories_ShouldRejectDuplicates()
    {
        Guid categoryId = Guid.NewGuid();

        Result<Todo> result = Result<Todo>.Success(Create())
            .EnsureCategories([categoryId, categoryId]);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoOutcome.Failure.CategoryDuplicate.Code);
    }

    private static Todo Create() =>
        Todo.Create(Guid.NewGuid(), TenantId, "Write tests", Now, "alice").Value;
}
