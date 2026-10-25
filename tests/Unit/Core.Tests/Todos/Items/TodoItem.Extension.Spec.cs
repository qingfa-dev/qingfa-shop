using Core.Common.Concerns.Lifecycle.SoftDeletable;
using Core.Todos;
using Core.Todos.Items;

namespace Core.Tests.Todos.Items;

/// <summary>Specifications for the todo behaviours.</summary>
public sealed class TodoItemExtensionSpec
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset CreatedAt = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CompletedAt = new(2020, 1, 2, 0, 0, 0, TimeSpan.Zero);

    private static TodoItem ValidTodo()
    {
        var result = TodoItem.Create(Guid.NewGuid(), TenantId, "Buy milk", CreatedAt, "alice");

        result.IsSuccess.Should().BeTrue();

        return result.Value;
    }

    private static TodoItem DeletedTodo()
    {
        var todo = ValidTodo();

        Result<TodoItem>.Success(todo).MarkDeleted(CreatedAt, "alice");

        todo.IsDeleted.Should().BeTrue();

        return todo;
    }

    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    private static string Snapshot(TodoItem todo) =>
        string.Join(
            "|",
            todo.Title,
            todo.Description,
            todo.Status,
            todo.Priority,
            todo.DueAtUtc,
            todo.CompletedAtUtc,
            todo.ListId,
            string.Join(",", todo.CategoryIds),
            todo.Events.Count);

    // SetTitle

    [Fact]
    public void SetTitle_ValidTitle_ShouldSetTheTitle()
    {
        var todo = ValidTodo();

        var result = Result<TodoItem>.Success(todo).SetTitle("Buy bread");

        result.IsSuccess.Should().BeTrue();
        todo.Title.Should().Be("Buy bread");
    }

    [Fact]
    public void SetTitle_PaddedTitle_ShouldStoreTheTrimmedTitle()
    {
        var todo = ValidTodo();

        Result<TodoItem>.Success(todo).SetTitle("  Buy bread  ");

        todo.Title.Should().Be("Buy bread");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetTitle_MissingTitle_ShouldFailWithoutMutation(string? title)
    {
        var todo = ValidTodo();

        ShouldFailWith(
            Result<TodoItem>.Success(todo).SetTitle(title),
            "TodoItem.Title.Required");

        todo.Title.Should().Be("Buy milk");
    }

    [Fact]
    public void SetTitle_LongTitle_ShouldFailWithoutMutation()
    {
        var todo = ValidTodo();

        ShouldFailWith(
            Result<TodoItem>.Success(todo).SetTitle(new string('a', 201)),
            "TodoItem.Title.TooLong");

        todo.Title.Should().Be("Buy milk");
    }

    // SetDescription

    [Fact]
    public void SetDescription_Text_ShouldSetTheDescription()
    {
        var todo = ValidTodo();

        var result = Result<TodoItem>.Success(todo).SetDescription("Semi-skimmed milk");

        result.IsSuccess.Should().BeTrue();
        todo.Description.Should().Be("Semi-skimmed milk");
    }

    [Fact]
    public void SetDescription_Null_ShouldClearTheDescription()
    {
        var todo = ValidTodo();
        todo.Description = "Semi-skimmed milk";

        Result<TodoItem>.Success(todo).SetDescription(null);

        todo.Description.Should().BeNull();
    }

    [Fact]
    public void SetDescription_LongDescription_ShouldFailWithoutMutation()
    {
        var todo = ValidTodo();

        ShouldFailWith(
            Result<TodoItem>.Success(todo).SetDescription(new string('a', 4001)),
            "TodoItem.Description.TooLong");

        todo.Description.Should().BeNull();
    }

    // SetPriority

    [Fact]
    public void SetPriority_ValidPriority_ShouldSetThePriority()
    {
        var todo = ValidTodo();

        var result = Result<TodoItem>.Success(todo).SetPriority(TodoPriority.Urgent);

        result.IsSuccess.Should().BeTrue();
        todo.Priority.Should().Be(TodoPriority.Urgent);
    }

    [Fact]
    public void SetPriority_UndefinedPriority_ShouldFailWithoutMutation()
    {
        var todo = ValidTodo();

        ShouldFailWith(
            Result<TodoItem>.Success(todo).SetPriority((TodoPriority)99),
            "TodoItem.Priority.Invalid");

        todo.Priority.Should().Be(TodoPriority.Medium);
    }

    // SetDueDate / ClearDueDate

    [Fact]
    public void SetDueDate_FutureDate_ShouldSetTheDueDate()
    {
        var todo = ValidTodo();

        var result = Result<TodoItem>.Success(todo).SetDueDate(CreatedAt.AddDays(1));

        result.IsSuccess.Should().BeTrue();
        todo.DueAtUtc.Should().Be(CreatedAt.AddDays(1));
    }

    [Fact]
    public void SetDueDate_DateBeforeCreation_ShouldFailWithoutMutation()
    {
        var todo = ValidTodo();

        ShouldFailWith(
            Result<TodoItem>.Success(todo).SetDueDate(CreatedAt.AddDays(-1)),
            "TodoItem.DueAt.BeforeCreated");

        todo.DueAtUtc.Should().BeNull();
    }

    [Fact]
    public void SetDueDate_NonUtcOffset_ShouldStoreTheSameInstantInUtc()
    {
        var todo = ValidTodo();
        var due = new DateTimeOffset(2020, 1, 2, 5, 0, 0, TimeSpan.FromHours(5));

        Result<TodoItem>.Success(todo).SetDueDate(due);

        todo.DueAtUtc.Should().NotBeNull();
        todo.DueAtUtc!.Value.Offset.Should().Be(TimeSpan.Zero);
        todo.DueAtUtc!.Value.Should().Be(due);
    }

    [Fact]
    public void ClearDueDate_ShouldClearTheDueDate()
    {
        var todo = ValidTodo();
        todo.DueAtUtc = CreatedAt.AddDays(1);

        Result<TodoItem>.Success(todo).ClearDueDate();

        todo.DueAtUtc.Should().BeNull();
    }

    // Start

    [Fact]
    public void Start_PendingTodo_ShouldMoveToInProgress()
    {
        var todo = ValidTodo();

        var result = Result<TodoItem>.Success(todo).Start();

        result.IsSuccess.Should().BeTrue();
        todo.Status.Should().Be(TodoStatus.InProgress);
    }

    [Fact]
    public void Start_PendingTodo_ShouldRaiseTodoItemStarted()
    {
        var todo = ValidTodo();

        Result<TodoItem>.Success(todo).Start();

        todo.Events.OfType<TodoItemStarted>().Should().ContainSingle();
    }

    [Fact]
    public void Start_StartedTodo_ShouldFailWithoutMutation()
    {
        var todo = ValidTodo();
        Result<TodoItem>.Success(todo).Start();

        var before = Snapshot(todo);

        ShouldFailWith(
            Result<TodoItem>.Success(todo).Start(),
            "TodoItem.Status.TransitionInvalid");

        todo.Status.Should().Be(TodoStatus.InProgress);
        Snapshot(todo).Should().Be(before);
    }

    // Complete

    [Fact]
    public void Complete_StartedTodo_ShouldMoveToCompleted()
    {
        var todo = ValidTodo();
        Result<TodoItem> .Success(todo).Start();

        var result = Result<TodoItem> .Success(todo).Complete(CompletedAt);

        result.IsSuccess.Should().BeTrue();
        todo.Status.Should().Be(TodoStatus.Completed);
        todo.CompletedAtUtc.Should().Be(CompletedAt);
    }

    [Fact]
    public void Complete_StartedTodo_ShouldRaiseTodoItemCompleted()
    {
        var todo = ValidTodo();
        Result<TodoItem> .Success(todo).Start();

        Result<TodoItem> .Success(todo).Complete(CompletedAt);

        var raised = todo.Events.OfType<TodoItemCompleted>().Should().ContainSingle().Which;
        raised.CompletedAtUtc.Should().Be(CompletedAt);
    }

    [Fact]
    public void Complete_PendingTodo_ShouldFailWithoutMutation()
    {
        var todo = ValidTodo();

        var before = Snapshot(todo);

        ShouldFailWith(
            Result<TodoItem> .Success(todo).Complete(CompletedAt),
            "TodoItem.Status.TransitionInvalid");

        todo.Status.Should().Be(TodoStatus.Pending);
        todo.CompletedAtUtc.Should().BeNull();
        Snapshot(todo).Should().Be(before);
    }

    [Fact]
    public void Complete_NonUtcOffset_ShouldStoreTheSameInstantInUtc()
    {
        var todo = ValidTodo();
        Result<TodoItem> .Success(todo).Start();
        var completedAt = new DateTimeOffset(2020, 1, 2, 5, 0, 0, TimeSpan.FromHours(5));

        Result<TodoItem> .Success(todo).Complete(completedAt);

        todo.CompletedAtUtc.Should().NotBeNull();
        todo.CompletedAtUtc!.Value.Offset.Should().Be(TimeSpan.Zero);
        todo.CompletedAtUtc!.Value.Should().Be(completedAt);
    }

    // Cancel

    [Fact]
    public void Cancel_PendingTodo_ShouldMoveToCancelled()
    {
        var todo = ValidTodo();

        var result = Result<TodoItem> .Success(todo).Cancel();

        result.IsSuccess.Should().BeTrue();
        todo.Status.Should().Be(TodoStatus.Cancelled);
    }

    [Fact]
    public void Cancel_PendingTodo_ShouldRaiseTodoItemCancelled()
    {
        var todo = ValidTodo();

        Result<TodoItem> .Success(todo).Cancel();

        todo.Events.OfType<TodoItemCancelled>().Should().ContainSingle();
    }

    [Fact]
    public void Cancel_CompletedTodo_ShouldFailWithoutMutation()
    {
        var todo = ValidTodo();
        Result<TodoItem> .Success(todo).Start();
        Result<TodoItem> .Success(todo).Complete(CompletedAt);

        var before = Snapshot(todo);

        ShouldFailWith(
            Result<TodoItem> .Success(todo).Cancel(),
            "TodoItem.Status.TransitionInvalid");

        todo.Status.Should().Be(TodoStatus.Completed);
        Snapshot(todo).Should().Be(before);
    }

    // Reopen

    [Fact]
    public void Reopen_CompletedTodo_ShouldMoveToPendingAndClearTheCompletionDate()
    {
        var todo = ValidTodo();
        Result<TodoItem> .Success(todo).Start();
        Result<TodoItem> .Success(todo).Complete(CompletedAt);

        var result = Result<TodoItem> .Success(todo).Reopen();

        result.IsSuccess.Should().BeTrue();
        todo.Status.Should().Be(TodoStatus.Pending);
        todo.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Reopen_CompletedTodo_ShouldRaiseTodoItemReopened()
    {
        var todo = ValidTodo();
        Result<TodoItem> .Success(todo).Start();
        Result<TodoItem> .Success(todo).Complete(CompletedAt);

        Result<TodoItem> .Success(todo).Reopen();

        todo.Events.OfType<TodoItemReopened>().Should().ContainSingle();
    }

    [Fact]
    public void Reopen_PendingTodo_ShouldFailWithoutMutation()
    {
        var todo = ValidTodo();

        var before = Snapshot(todo);

        ShouldFailWith(
            Result<TodoItem> .Success(todo).Reopen(),
            "TodoItem.Status.TransitionInvalid");

        todo.Status.Should().Be(TodoStatus.Pending);
        Snapshot(todo).Should().Be(before);
    }

    [Fact]
    public void Reopen_CancelledTodo_ShouldMoveToPending()
    {
        var todo = ValidTodo();
        Result<TodoItem> .Success(todo).Cancel();

        var result = Result<TodoItem> .Success(todo).Reopen();

        result.IsSuccess.Should().BeTrue();
        todo.Status.Should().Be(TodoStatus.Pending);
    }

    // AssignToList / ClearList

    [Fact]
    public void AssignToList_ValidListId_ShouldSetTheList()
    {
        var todo = ValidTodo();
        var listId = Guid.NewGuid();

        var result = Result<TodoItem> .Success(todo).AssignToList(listId);

        result.IsSuccess.Should().BeTrue();
        todo.ListId.Should().Be(listId);
    }

    [Fact]
    public void AssignToList_EmptyListId_ShouldFailWithoutMutation()
    {
        var todo = ValidTodo();

        ShouldFailWith(
            Result<TodoItem> .Success(todo).AssignToList(Guid.Empty),
            "TodoItem.ListId.Required");

        todo.ListId.Should().BeNull();
    }

    [Fact]
    public void ClearList_ShouldClearTheList()
    {
        var todo = ValidTodo();
        todo.ListId = Guid.NewGuid();

        Result<TodoItem> .Success(todo).ClearList();

        todo.ListId.Should().BeNull();
    }

    // AddCategory / RemoveCategory

    [Fact]
    public void AddCategory_NewCategory_ShouldAddTheReference()
    {
        var todo = ValidTodo();
        var categoryId = Guid.NewGuid();

        var result = Result<TodoItem> .Success(todo).AddCategory(categoryId);

        result.IsSuccess.Should().BeTrue();
        todo.CategoryIds.Should().ContainSingle().Which.Should().Be(categoryId);
    }

    [Fact]
    public void AddCategory_EmptyCategoryId_ShouldFailWithoutMutation()
    {
        var todo = ValidTodo();

        ShouldFailWith(
            Result<TodoItem> .Success(todo).AddCategory(Guid.Empty),
            "TodoItem.Category.Required");

        todo.CategoryIds.Should().BeEmpty();
    }

    [Fact]
    public void AddCategory_ExistingCategory_ShouldFailWithoutMutation()
    {
        var todo = ValidTodo();
        var categoryId = Guid.NewGuid();
        Result<TodoItem> .Success(todo).AddCategory(categoryId);

        ShouldFailWith(
            Result<TodoItem> .Success(todo).AddCategory(categoryId),
            "TodoItem.Category.Duplicate");

        todo.CategoryIds.Should().ContainSingle();
    }

    [Fact]
    public void AddCategory_BeyondTheLimit_ShouldFailWithoutMutation()
    {
        var todo = ValidTodo();

        for (var i = 0; i < 20; i++)
            Result<TodoItem> .Success(todo).AddCategory(Guid.NewGuid());

        ShouldFailWith(
            Result<TodoItem> .Success(todo).AddCategory(Guid.NewGuid()),
            "TodoItem.Categories.TooMany");

        todo.CategoryIds.Should().HaveCount(20);
    }

    [Fact]
    public void RemoveCategory_ReferencedCategory_ShouldRemoveTheReference()
    {
        var todo = ValidTodo();
        var categoryId = Guid.NewGuid();
        Result<TodoItem> .Success(todo).AddCategory(categoryId);

        var result = Result<TodoItem> .Success(todo).RemoveCategory(categoryId);

        result.IsSuccess.Should().BeTrue();
        todo.CategoryIds.Should().BeEmpty();
    }

    [Fact]
    public void RemoveCategory_MissingCategory_ShouldFailWithoutMutation()
    {
        var todo = ValidTodo();
        var categoryId = Guid.NewGuid();
        Result<TodoItem> .Success(todo).AddCategory(categoryId);

        ShouldFailWith(
            Result<TodoItem> .Success(todo).RemoveCategory(Guid.NewGuid()),
            "TodoItem.Category.NotFound");

        todo.CategoryIds.Should().ContainSingle();
    }

    // Deleted guard

    public static TheoryData<string> DeletedMutators() =>
        new()
        {
            "SetTitle",
            "SetDescription",
            "SetPriority",
            "SetDueDate",
            "ClearDueDate",
            "Start",
            "Complete",
            "Cancel",
            "Reopen",
            "AssignToList",
            "ClearList",
            "AddCategory",
            "RemoveCategory"
        };

    [Theory]
    [MemberData(nameof(DeletedMutators))]
    public void Mutator_DeletedTodo_ShouldFailWithoutMutation(string mutator)
    {
        var todo = DeletedTodo();
        var before = Snapshot(todo);

        var result = ApplyMutator(mutator, todo);

        ShouldFailWith(result, "TodoItem.Deleted.NotMutable");
        Snapshot(todo).Should().Be(before);
    }

    private static Result<TodoItem>  ApplyMutator(string mutator, TodoItem todo) =>
        mutator switch
        {
            "SetTitle" => Result<TodoItem> .Success(todo).SetTitle("Buy bread"),
            "SetDescription" => Result<TodoItem> .Success(todo).SetDescription("Semi-skimmed milk"),
            "SetPriority" => Result<TodoItem> .Success(todo).SetPriority(TodoPriority.Urgent),
            "SetDueDate" => Result<TodoItem> .Success(todo).SetDueDate(CreatedAt.AddDays(1)),
            "ClearDueDate" => Result<TodoItem> .Success(todo).ClearDueDate(),
            "Start" => Result<TodoItem> .Success(todo).Start(),
            "Complete" => Result<TodoItem> .Success(todo).Complete(CompletedAt),
            "Cancel" => Result<TodoItem> .Success(todo).Cancel(),
            "Reopen" => Result<TodoItem> .Success(todo).Reopen(),
            "AssignToList" => Result<TodoItem> .Success(todo).AssignToList(Guid.NewGuid()),
            "ClearList" => Result<TodoItem> .Success(todo).ClearList(),
            "AddCategory" => Result<TodoItem> .Success(todo).AddCategory(Guid.NewGuid()),
            "RemoveCategory" => Result<TodoItem> .Success(todo).RemoveCategory(Guid.NewGuid()),
            _ => throw new ArgumentOutOfRangeException(nameof(mutator), mutator, "Unknown mutator.")
        };
}