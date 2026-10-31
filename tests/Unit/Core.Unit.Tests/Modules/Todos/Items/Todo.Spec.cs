using Core.BuildingBlocks.Modeling.Metadata;
using Core.Modules.Todos.Items;

namespace Core.Unit.Tests.Modules.Todos.Items;

/// <summary>Specifications for todo creation, mutation and aggregate state.</summary>
public sealed class TodoSpec
{
    private static readonly Guid TodoId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset CreatedAt = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = new(2020, 1, 2, 0, 0, 0, TimeSpan.Zero);

    private static Todo NewTodo(string title = "Buy milk")
        => Todo.Create(Guid.NewGuid(), TenantId, title, CreatedAt, "alice").Value;

    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    private static void ShouldFailWithoutMutation(Todo todo, Func<Result> action, string expectedCode)
    {
        long version = todo.Version;
        int eventCount = todo.UncommittedEvents.Count;

        ShouldFailWith(action(), expectedCode);

        todo.Version.Should().Be(version);
        todo.UncommittedEvents.Should().HaveCount(eventCount);
    }

    // Create

    [Fact]
    public void Create_ValidInput_ShouldSucceed()
    {
        var result = Todo.Create(TodoId, TenantId, "Buy milk", CreatedAt, "alice");

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(TodoId);
    }

    [Fact]
    public void Create_ValidInput_ShouldStartPendingAtMediumPriority()
    {
        var result = Todo.Create(TodoId, TenantId, "Buy milk", CreatedAt, "alice");

        result.Value.Status.Should().Be(TodoStatus.Pending);
        result.Value.Priority.Should().Be(TodoPriority.Medium);
        result.Value.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void Create_ValidInput_ShouldStampTheAuditMetadata()
    {
        var result = Todo.Create(TodoId, TenantId, "Buy milk", CreatedAt, "alice");

        result.Value.CreatedAtUtc.Should().Be(CreatedAt);
        result.Value.CreatedBy.Should().Be("alice");
    }

    [Fact]
    public void Create_ValidInput_ShouldSetTheTenant()
    {
        var result = Todo.Create(TodoId, TenantId, "Buy milk", CreatedAt, "alice");

        result.Value.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void Create_PaddedTitle_ShouldStoreTheTrimmedTitle()
    {
        var result = Todo.Create(TodoId, TenantId, "  Buy milk  ", CreatedAt, "alice");

        result.Value.Title.Should().Be("Buy milk");
    }

    [Fact]
    public void Create_PaddedActor_ShouldStoreTheTrimmedActor()
    {
        var result = Todo.Create(TodoId, TenantId, "Buy milk", CreatedAt, "  alice  ");

        result.Value.CreatedBy.Should().Be("alice");
    }

    [Fact]
    public void Create_ValidInput_ShouldRaiseTodoCreated()
    {
        var result = Todo.Create(TodoId, TenantId, "Buy milk", CreatedAt, "alice");

        var raised = result.Value.UncommittedEvents.Should().ContainSingle().Which;
        raised.Should().BeOfType<TodoCreated>();
        ((TodoCreated)raised).TodoId.Should().Be(TodoId);
    }

    [Fact]
    public void Create_WithMetadata_ShouldForwardItToTheEvent()
    {
        var metadata = DefaultMetadata.Create();
        metadata.SetValue(MetadataConstant.Keyword.TenantId, TenantId);

        var result = Todo.Create(
            TodoId,
            TenantId,
            "Buy milk",
            CreatedAt,
            "alice",
            metadata: metadata);

        var created = result.Value.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoCreated>().Subject;
        created.Metadata[MetadataConstant.Keyword.TenantId].Should().Be(TenantId);
    }

    [Fact]
    public void Create_InvalidInput_ShouldNotReturnATodo()
    {
        var result = Todo.Create(TodoId, TenantId, "   ", CreatedAt, "alice");

        // Validate: A rejected creation never reaches the event, so nothing is announced.
        result.IsFailure.Should().BeTrue();
        result.TryGetValue(out _).Should().BeFalse();
    }

    [Fact]
    public void Create_EmptyId_ShouldFailWithIdentifiableIdRequired()
    {
        ShouldFailWith(
            Todo.Create(Guid.Empty, TenantId, "Buy milk", CreatedAt, "alice"),
            "Todo.Id.Required");
    }

    [Fact]
    public void Create_EmptyTenantId_ShouldFailWithTenantableTenantIdRequired()
    {
        ShouldFailWith(
            Todo.Create(TodoId, Guid.Empty, "Buy milk", CreatedAt, "alice"),
            "Todo.TenantId.Required");
    }

    [Fact]
    public void Create_MissingCreationInstant_ShouldFailWithCreatedAtRequired()
    {
        ShouldFailWith(
            Todo.Create(TodoId, TenantId, "Buy milk", nowUtc: default, actor: "alice"),
            "Todo.CreatedAt.Required");
    }

    [Fact]
    public void Create_BlankActor_ShouldStoreNull()
    {
        var result = Todo.Create(TodoId, TenantId, "Buy milk", CreatedAt, "   ");

        result.Value.CreatedBy.Should().BeNull();
    }

    [Fact]
    public void Create_BlankTitle_ShouldFailWithTitleRequired()
    {
        ShouldFailWith(
            Todo.Create(TodoId, TenantId, "   ", CreatedAt, "alice"),
            "Todo.Title.Required");
    }

    [Fact]
    public void Create_DueDateBeforeCreation_ShouldFailWithDueAtBeforeCreated()
    {
        ShouldFailWith(
            Todo.Create(TodoId, TenantId, "Buy milk", CreatedAt, "alice", dueAtUtc: CreatedAt.AddDays(-1)),
            "Todo.DueAt.BeforeCreated");
    }

    [Fact]
    public void Create_NonUtcNow_ShouldStoreTheSameInstantInUtc()
    {
        var now = new DateTimeOffset(2020, 1, 1, 5, 0, 0, TimeSpan.FromHours(5));

        var result = Todo.Create(TodoId, TenantId, "Buy milk", now, "alice");

        result.Value.CreatedAtUtc.Offset.Should().Be(TimeSpan.Zero);
        result.Value.CreatedAtUtc.Should().Be(now);
    }

    [Fact]
    public void IsCompleted_ShouldFollowTheStatus()
    {
        Todo todo = NewTodo();

        todo.IsCompleted.Should().BeFalse();

        todo.Start(Later, "alice").IsSuccess.Should().BeTrue();
        todo.Complete(Later, "alice").IsSuccess.Should().BeTrue();

        todo.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void Create_NoOptionalInput_ShouldLeaveOptionalStateEmpty()
    {
        var result = Todo.Create(TodoId, TenantId, "Buy milk", CreatedAt, "alice");

        result.Value.Description.Should().BeNull();
        result.Value.DueAtUtc.Should().BeNull();
        result.Value.CompletedAtUtc.Should().BeNull();
        result.Value.ListId.Should().BeNull();
        result.Value.CategoryIds.Should().BeEmpty();
    }

    [Fact]
    public void Create_NoModificationOrDeletion_ShouldLeaveTheAuditFlagsClear()
    {
        var result = Todo.Create(TodoId, TenantId, "Buy milk", CreatedAt, "alice");

        result.Value.ModifiedAtUtc.Should().BeNull();
        result.Value.ModifiedBy.Should().BeNull();
        result.Value.IsDeleted.Should().BeFalse();
        result.Value.DeletedAtUtc.Should().BeNull();
        result.Value.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void CategoryIds_ShouldBeAReadOnlyView()
    {
        Todo todo = NewTodo();

        Action add = () => todo.CategoryIds.Add(Guid.NewGuid());

        add.Should().Throw<NotSupportedException>();
        todo.CategoryIds.Should().BeEmpty();
        todo.Version.Should().Be(1);
    }

    [Fact]
    public void Update_BlankActor_ShouldStoreNull()
    {
        Todo todo = NewTodo();

        todo.Update("Buy milk", null, TodoPriority.Medium, null, null, Later, "   ")
            .IsSuccess.Should().BeTrue();

        todo.ModifiedBy.Should().BeNull();
    }

    [Fact]
    public void Create_PaddedDescription_ShouldStoreTheTrimmedDescription()
    {
        var result = Todo.Create(
            TodoId,
            TenantId,
            "Buy milk",
            CreatedAt,
            "alice",
            description: "  Two litres  ");

        result.Value.Description.Should().Be("Two litres");
    }

    [Fact]
    public void Create_DescriptionExceedingMaxLength_ShouldFailWithDescriptionTooLong()
    {
        string tooLong = new('a', TodoConstant.Constraints.Description.MaxLength + 1);

        ShouldFailWith(
            Todo.Create(TodoId, TenantId, "Buy milk", CreatedAt, "alice", description: tooLong),
            "Todo.Description.TooLong");
    }

    [Fact]
    public void Create_DueDateWithOffset_ShouldNormalizeToUtc()
    {
        var dueAt = new DateTimeOffset(2020, 1, 2, 12, 0, 0, TimeSpan.FromHours(7));

        var result = Todo.Create(
            TodoId,
            TenantId,
            "Buy milk",
            CreatedAt,
            "alice",
            dueAtUtc: dueAt);

        result.Value.DueAtUtc.Should().Be(dueAt.ToUniversalTime());
        result.Value.DueAtUtc!.Value.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Create_DueDateEqualToCreation_ShouldSucceed()
    {
        var result = Todo.Create(
            TodoId,
            TenantId,
            "Buy milk",
            CreatedAt,
            "alice",
            dueAtUtc: CreatedAt);

        result.IsSuccess.Should().BeTrue();
        result.Value.DueAtUtc.Should().Be(CreatedAt);
    }

    // Update

    [Fact]
    public void Update_ShouldReplaceTheEditableFields()
    {
        Todo todo = NewTodo();
        Guid listId = Guid.NewGuid();

        Result result = todo.Update(
            "  Buy bread  ",
            "  Wholemeal  ",
            TodoPriority.High,
            Later,
            listId,
            Later,
            "  bob  ");

        result.IsSuccess.Should().BeTrue();
        todo.Title.Should().Be("Buy bread");
        todo.Description.Should().Be("Wholemeal");
        todo.Priority.Should().Be(TodoPriority.High);
        todo.DueAtUtc.Should().Be(Later);
        todo.ListId.Should().Be(listId);
        todo.ModifiedAtUtc.Should().Be(Later);
        todo.ModifiedBy.Should().Be("bob");
    }

    [Fact]
    public void Update_NoOptionalInput_ShouldClearTheOptionalFields()
    {
        Todo todo = NewTodo();
        Result first = todo.Update("Buy milk", "Two litres", TodoPriority.High, Later, Guid.NewGuid(), Later, "alice");
        first.IsSuccess.Should().BeTrue();

        Result result = todo.Update("Buy milk", null, TodoPriority.Medium, null, null, Later, "bob");

        result.IsSuccess.Should().BeTrue();
        todo.Description.Should().BeNull();
        todo.DueAtUtc.Should().BeNull();
        todo.ListId.Should().BeNull();
    }

    [Fact]
    public void Update_NonUtcDueDate_ShouldStoreTheSameInstantInUtc()
    {
        Todo todo = NewTodo();
        var due = new DateTimeOffset(2020, 1, 2, 5, 0, 0, TimeSpan.FromHours(5));

        Result result = todo.Update("Buy milk", null, TodoPriority.Medium, due, null, Later, "alice");

        result.IsSuccess.Should().BeTrue();
        todo.DueAtUtc.Should().Be(due);
        todo.DueAtUtc!.Value.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Update_BlankTitle_ShouldFailWithoutMutation()
    {
        Todo todo = NewTodo();

        ShouldFailWithoutMutation(
            todo,
            () => todo.Update("   ", null, TodoPriority.Medium, null, null, Later, "alice"),
            "Todo.Title.Required");

        todo.Title.Should().Be("Buy milk");
    }

    [Fact]
    public void Update_LongTitle_ShouldFailWithoutMutation()
    {
        Todo todo = NewTodo();

        ShouldFailWithoutMutation(
            todo,
            () => todo.Update(new string('a', 201), null, TodoPriority.Medium, null, null, Later, "alice"),
            "Todo.Title.TooLong");

        todo.Title.Should().Be("Buy milk");
    }

    [Fact]
    public void Update_LongDescription_ShouldFailWithoutMutation()
    {
        Todo todo = NewTodo();

        ShouldFailWithoutMutation(
            todo,
            () => todo.Update("Buy milk", new string('a', 4001), TodoPriority.Medium, null, null, Later, "alice"),
            "Todo.Description.TooLong");

        todo.Description.Should().BeNull();
    }

    [Fact]
    public void Update_UndefinedPriority_ShouldFailWithoutMutation()
    {
        Todo todo = NewTodo();

        ShouldFailWithoutMutation(
            todo,
            () => todo.Update("Buy milk", null, (TodoPriority)99, null, null, Later, "alice"),
            "Todo.Priority.Invalid");

        todo.Priority.Should().Be(TodoPriority.Medium);
    }

    [Fact]
    public void Update_DueDateBeforeCreation_ShouldFailWithoutMutation()
    {
        Todo todo = NewTodo();

        ShouldFailWithoutMutation(
            todo,
            () => todo.Update("Buy milk", null, TodoPriority.Medium, CreatedAt.AddDays(-1), null, Later, "alice"),
            "Todo.DueAt.BeforeCreated");

        todo.DueAtUtc.Should().BeNull();
    }

    [Fact]
    public void Update_EmptyListId_ShouldFailWithoutMutation()
    {
        Todo todo = NewTodo();

        ShouldFailWithoutMutation(
            todo,
            () => todo.Update("Buy milk", null, TodoPriority.Medium, null, Guid.Empty, Later, "alice"),
            "Todo.ListId.Required");

        todo.ListId.Should().BeNull();
    }

    // Start / Complete / Cancel / Reopen

    [Fact]
    public void Start_PendingTodo_ShouldMoveToInProgress()
    {
        Todo todo = NewTodo();

        Result result = todo.Start(Later, "alice");

        result.IsSuccess.Should().BeTrue();
        todo.Status.Should().Be(TodoStatus.InProgress);
        todo.ModifiedAtUtc.Should().Be(Later);
        todo.ModifiedBy.Should().Be("alice");
    }

    [Fact]
    public void Start_StartedTodo_ShouldFailWithoutMutation()
    {
        Todo todo = NewTodo();
        todo.Start(Later, "alice");

        ShouldFailWithoutMutation(
            todo,
            () => todo.Start(Later, "alice"),
            "Todo.Status.TransitionInvalid");

        todo.Status.Should().Be(TodoStatus.InProgress);
    }

    [Fact]
    public void Complete_StartedTodo_ShouldMoveToCompleted()
    {
        Todo todo = NewTodo();
        todo.Start(Later, "alice");

        Result result = todo.Complete(Later, "alice");

        result.IsSuccess.Should().BeTrue();
        todo.Status.Should().Be(TodoStatus.Completed);
        todo.CompletedAtUtc.Should().Be(Later);
    }

    [Fact]
    public void Complete_NonUtcInstant_ShouldStoreTheSameInstantInUtc()
    {
        Todo todo = NewTodo();
        todo.Start(Later, "alice");
        var completedAt = new DateTimeOffset(2020, 1, 2, 5, 0, 0, TimeSpan.FromHours(5));

        Result result = todo.Complete(completedAt, "alice");

        result.IsSuccess.Should().BeTrue();
        todo.CompletedAtUtc.Should().Be(completedAt);
        todo.CompletedAtUtc!.Value.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Complete_PendingTodo_ShouldFailWithoutMutation()
    {
        Todo todo = NewTodo();

        ShouldFailWithoutMutation(
            todo,
            () => todo.Complete(Later, "alice"),
            "Todo.Status.TransitionInvalid");

        todo.Status.Should().Be(TodoStatus.Pending);
        todo.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Cancel_PendingTodo_ShouldMoveToCancelled()
    {
        Todo todo = NewTodo();

        Result result = todo.Cancel(Later, "alice");

        result.IsSuccess.Should().BeTrue();
        todo.Status.Should().Be(TodoStatus.Cancelled);
    }

    [Fact]
    public void Cancel_CompletedTodo_ShouldFailWithoutMutation()
    {
        Todo todo = NewTodo();
        todo.Start(Later, "alice");
        todo.Complete(Later, "alice");

        ShouldFailWithoutMutation(
            todo,
            () => todo.Cancel(Later, "alice"),
            "Todo.Status.TransitionInvalid");

        todo.Status.Should().Be(TodoStatus.Completed);
    }

    [Fact]
    public void Reopen_CompletedTodo_ShouldMoveToPendingAndClearTheCompletionDate()
    {
        Todo todo = NewTodo();
        todo.Start(Later, "alice");
        todo.Complete(Later, "alice");

        Result result = todo.Reopen(Later, "alice");

        result.IsSuccess.Should().BeTrue();
        todo.Status.Should().Be(TodoStatus.Pending);
        todo.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Reopen_PendingTodo_ShouldFailWithoutMutation()
    {
        Todo todo = NewTodo();

        ShouldFailWithoutMutation(
            todo,
            () => todo.Reopen(Later, "alice"),
            "Todo.Status.TransitionInvalid");

        todo.Status.Should().Be(TodoStatus.Pending);
    }

    [Fact]
    public void Reopen_CancelledTodo_ShouldMoveToPending()
    {
        Todo todo = NewTodo();
        todo.Cancel(Later, "alice");

        Result result = todo.Reopen(Later, "alice");

        result.IsSuccess.Should().BeTrue();
        todo.Status.Should().Be(TodoStatus.Pending);
    }

    // AssignToList / ClearList

    [Fact]
    public void AssignToList_ValidListId_ShouldSetTheList()
    {
        Todo todo = NewTodo();
        Guid listId = Guid.NewGuid();

        Result result = todo.AssignToList(listId, Later, "alice");

        result.IsSuccess.Should().BeTrue();
        todo.ListId.Should().Be(listId);
    }

    [Fact]
    public void AssignToList_EmptyListId_ShouldFailWithoutMutation()
    {
        Todo todo = NewTodo();

        ShouldFailWithoutMutation(
            todo,
            () => todo.AssignToList(Guid.Empty, Later, "alice"),
            "Todo.ListId.Required");

        todo.ListId.Should().BeNull();
    }

    [Fact]
    public void ClearList_ShouldClearTheList()
    {
        Todo todo = NewTodo();
        todo.AssignToList(Guid.NewGuid(), Later, "alice");

        Result result = todo.ClearList(Later, "alice");

        result.IsSuccess.Should().BeTrue();
        todo.ListId.Should().BeNull();
    }

    // AddCategory / RemoveCategory

    [Fact]
    public void AddCategory_NewCategory_ShouldAddTheReference()
    {
        Todo todo = NewTodo();
        Guid categoryId = Guid.NewGuid();

        Result result = todo.AddCategory(categoryId, Later, "alice");

        result.IsSuccess.Should().BeTrue();
        todo.CategoryIds.Should().ContainSingle().Which.Should().Be(categoryId);
    }

    [Fact]
    public void AddCategory_EmptyCategoryId_ShouldFailWithoutMutation()
    {
        Todo todo = NewTodo();

        ShouldFailWithoutMutation(
            todo,
            () => todo.AddCategory(Guid.Empty, Later, "alice"),
            "Todo.Category.Required");

        todo.CategoryIds.Should().BeEmpty();
    }

    [Fact]
    public void AddCategory_ExistingCategory_ShouldFailWithoutMutation()
    {
        Todo todo = NewTodo();
        Guid categoryId = Guid.NewGuid();
        todo.AddCategory(categoryId, Later, "alice");

        ShouldFailWithoutMutation(
            todo,
            () => todo.AddCategory(categoryId, Later, "alice"),
            "Todo.Category.Duplicate");

        todo.CategoryIds.Should().ContainSingle();
    }

    [Fact]
    public void AddCategory_BeyondTheLimit_ShouldFailWithoutMutation()
    {
        Todo todo = NewTodo();

        for (var i = 0; i < TodoConstant.Constraints.Collection.MaxCategories; i++)
            todo.AddCategory(Guid.NewGuid(), Later, "alice").IsSuccess.Should().BeTrue();

        ShouldFailWithoutMutation(
            todo,
            () => todo.AddCategory(Guid.NewGuid(), Later, "alice"),
            "Todo.Categories.TooMany");

        todo.CategoryIds.Should().HaveCount(TodoConstant.Constraints.Collection.MaxCategories);
    }

    [Fact]
    public void RemoveCategory_ReferencedCategory_ShouldRemoveTheReference()
    {
        Todo todo = NewTodo();
        Guid categoryId = Guid.NewGuid();
        todo.AddCategory(categoryId, Later, "alice");

        Result result = todo.RemoveCategory(categoryId, Later, "alice");

        result.IsSuccess.Should().BeTrue();
        todo.CategoryIds.Should().BeEmpty();
    }

    [Fact]
    public void RemoveCategory_MissingCategory_ShouldFailWithoutMutation()
    {
        Todo todo = NewTodo();
        todo.AddCategory(Guid.NewGuid(), Later, "alice");

        ShouldFailWithoutMutation(
            todo,
            () => todo.RemoveCategory(Guid.NewGuid(), Later, "alice"),
            "Todo.Category.NotFound");

        todo.CategoryIds.Should().ContainSingle();
    }

    // Delete / Restore

    [Fact]
    public void Delete_Soft_ShouldStampTheDeletionAudit()
    {
        Todo todo = NewTodo();

        Result result = todo.Delete(isHard: false, Later, "bob");

        result.IsSuccess.Should().BeTrue();
        todo.IsDeleted.Should().BeTrue();
        todo.DeletedAtUtc.Should().Be(Later);
        todo.DeletedBy.Should().Be("bob");
    }

    [Fact]
    public void Delete_SoftTwice_ShouldNotRaiseAnotherEvent()
    {
        Todo todo = NewTodo();
        todo.Delete(isHard: false, Later, "bob");
        int eventCount = todo.UncommittedEvents.Count;

        Result result = todo.Delete(isHard: false, Later.AddMinutes(1), "bob");

        result.IsSuccess.Should().BeTrue();
        todo.UncommittedEvents.Should().HaveCount(eventCount);
    }

    [Fact]
    public void Delete_Hard_ShouldRaiseHardDeleteEvent()
    {
        Todo todo = NewTodo();

        todo.Delete(isHard: true, Later, "bob").IsSuccess.Should().BeTrue();

        var deleted = todo.UncommittedEvents.OfType<TodoDeleted>().Should().ContainSingle().Subject;
        deleted.IsHard.Should().BeTrue();
        todo.IsHardDeleted.Should().BeTrue();
    }

    [Fact]
    public void Delete_BlankActor_ShouldStoreNull()
    {
        Todo todo = NewTodo();

        todo.Delete(isHard: false, Later, "   ").IsSuccess.Should().BeTrue();

        todo.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void Restore_DeletedTodo_ShouldClearTheDeletionAudit()
    {
        Todo todo = NewTodo();
        todo.Delete(isHard: false, Later, "bob");

        Result result = todo.Restore(Later.AddMinutes(1), "alice");

        result.IsSuccess.Should().BeTrue();
        todo.IsDeleted.Should().BeFalse();
        todo.DeletedAtUtc.Should().BeNull();
        todo.DeletedBy.Should().BeNull();
        todo.ModifiedBy.Should().Be("alice");
    }

    [Fact]
    public void Restore_LiveTodo_ShouldNotRaiseAnotherEvent()
    {
        Todo todo = NewTodo();
        int eventCount = todo.UncommittedEvents.Count;

        Result result = todo.Restore(Later, "alice");

        result.IsSuccess.Should().BeTrue();
        todo.UncommittedEvents.Should().HaveCount(eventCount);
    }

    [Fact]
    public void Restore_HardDeletedTodo_ShouldFailWithHardDeletedNotFound()
    {
        Todo todo = NewTodo();
        todo.Delete(isHard: true, Later, "bob").IsSuccess.Should().BeTrue();

        ShouldFailWithoutMutation(
            todo,
            () => todo.Restore(Later.AddMinutes(1), "alice"),
            "Todo.Deleted.Hard");
    }

    [Fact]
    public void ClearList_SoftDeletedTodoWithAllowDeleted_ShouldRaiseTheEvent()
    {
        Todo todo = NewTodo();
        todo.Delete(isHard: false, Later, "bob").IsSuccess.Should().BeTrue();
        todo.ClearUncommittedEvents();

        Result result = todo.ClearList(Later, "cleaner", allowDeleted: true);

        result.IsSuccess.Should().BeTrue();
        todo.IsDeleted.Should().BeTrue();
        todo.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoListChanged>();
    }

    [Fact]
    public void RemoveCategory_SoftDeletedTodoWithAllowDeleted_ShouldRaiseTheEvent()
    {
        Guid categoryId = Guid.NewGuid();
        Todo todo = NewTodo();
        todo.AddCategory(categoryId, CreatedAt, "alice").IsSuccess.Should().BeTrue();
        todo.Delete(isHard: false, Later, "bob").IsSuccess.Should().BeTrue();
        todo.ClearUncommittedEvents();

        Result result = todo.RemoveCategory(categoryId, Later, "cleaner", allowDeleted: true);

        result.IsSuccess.Should().BeTrue();
        todo.IsDeleted.Should().BeTrue();
        todo.CategoryIds.Should().BeEmpty();
        todo.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoCategoryRemoved>();
    }

    [Fact]
    public void ClearList_HardDeletedTodoWithAllowDeleted_ShouldStillFail()
    {
        Todo todo = NewTodo();
        todo.Delete(isHard: true, Later, "bob").IsSuccess.Should().BeTrue();
        todo.ClearUncommittedEvents();

        ShouldFailWithoutMutation(
            todo,
            () => todo.ClearList(Later, "cleaner", allowDeleted: true),
            "Todo.Deleted.Hard");
    }

    // Deleted guard

    public static TheoryData<string> DeletedMutators() =>
        new()
        {
            "Update",
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
        Todo todo = NewTodo();
        todo.Delete(isHard: false, Later, "bob").IsSuccess.Should().BeTrue();

        ShouldFailWithoutMutation(
            todo,
            () => ApplyMutator(mutator, todo),
            "Todo.Deleted.NotMutable");
    }

    private static Result ApplyMutator(string mutator, Todo todo) =>
        mutator switch
        {
            "Update" => todo.Update("Buy bread", "Wholemeal", TodoPriority.Urgent, Later, Guid.NewGuid(), Later, "alice"),
            "Start" => todo.Start(Later, "alice"),
            "Complete" => todo.Complete(Later, "alice"),
            "Cancel" => todo.Cancel(Later, "alice"),
            "Reopen" => todo.Reopen(Later, "alice"),
            "AssignToList" => todo.AssignToList(Guid.NewGuid(), Later, "alice"),
            "ClearList" => todo.ClearList(Later, "alice"),
            "AddCategory" => todo.AddCategory(Guid.NewGuid(), Later, "alice"),
            "RemoveCategory" => todo.RemoveCategory(Guid.NewGuid(), Later, "alice"),
            _ => throw new ArgumentOutOfRangeException(nameof(mutator), mutator, "Unknown mutator.")
        };
}