using Core.Todos.Items;

namespace Core.Unit.Tests.Todos.Items;

/// <summary>Rehydration, snapshot and transition tests for the event-sourced todo.</summary>
[Trait("Category", "Unit")]
public sealed class TodoEventSourcedSpec
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_ShouldRaiseTodoCreatedWithFullState()
    {
        Result<Todo> result = Todo.Create(
            Guid.NewGuid(), TenantId, "  Write tests  ", Now, "alice",
            description: "  details  ", priority: TodoPriority.High,
            dueAtUtc: Now.AddDays(1), listId: Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        Todo todo = result.Value;
        todo.Title.Should().Be("Write tests");
        todo.Description.Should().Be("details");
        todo.Status.Should().Be(TodoStatus.Pending);
        todo.Version.Should().Be(1);

        TodoCreated created = todo.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoCreated>().Subject;
        created.TodoId.Should().Be(todo.Id);
        created.TenantId.Should().Be(TenantId);
        created.CreatedAtUtc.Should().Be(Now);
        created.CreatedBy.Should().Be("alice");
    }

    [Fact]
    public void Update_ShouldRaiseOneEventWithFinalState()
    {
        Todo todo = Create();
        todo.ClearUncommittedEvents();
        Guid listId = Guid.NewGuid();

        Result result = todo.Update(
            "  Renamed  ", "desc", TodoPriority.Urgent,
            Now.AddDays(2), listId, Now.AddMinutes(5), "bob");

        result.IsSuccess.Should().BeTrue();
        todo.Title.Should().Be("Renamed");
        todo.Description.Should().Be("desc");
        todo.Priority.Should().Be(TodoPriority.Urgent);
        todo.DueAtUtc.Should().Be(Now.AddDays(2));
        todo.ListId.Should().Be(listId);
        todo.ModifiedBy.Should().Be("bob");
        todo.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoUpdated>();
    }

    [Fact]
    public void StatusTransitions_ShouldRaiseStatusChanged()
    {
        Todo todo = Create();

        todo.Start(Now.AddMinutes(1), "alice").IsSuccess.Should().BeTrue();
        todo.Complete(Now.AddMinutes(2), "alice").IsSuccess.Should().BeTrue();

        todo.Status.Should().Be(TodoStatus.Completed);
        todo.CompletedAtUtc.Should().Be(Now.AddMinutes(2));
        todo.UncommittedEvents.OfType<TodoStatusChanged>().Should().HaveCount(2);
        todo.UncommittedEvents.OfType<TodoStatusChanged>().Last()
            .Should().Match<TodoStatusChanged>(changed =>
                changed.From == TodoStatus.InProgress &&
                changed.To == TodoStatus.Completed);

        todo.Reopen(Now.AddMinutes(3), "alice").IsSuccess.Should().BeTrue();
        todo.Status.Should().Be(TodoStatus.Pending);
        todo.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public void IllegalTransition_ShouldNotMutateOrRaise()
    {
        Todo todo = Create();

        Result result = todo.Complete(Now.AddMinutes(1), "alice");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoOutcome.Failure.StatusTransitionInvalid.Code);
        todo.Version.Should().Be(1);
        todo.UncommittedEvents.Should().ContainSingle();
    }

    [Fact]
    public void CategoriesAndList_ShouldRaiseDedicatedEvents()
    {
        Todo todo = Create();
        Guid categoryId = Guid.NewGuid();
        Guid listId = Guid.NewGuid();

        todo.AddCategory(categoryId, Now.AddMinutes(1), "alice").IsSuccess.Should().BeTrue();
        todo.AssignToList(listId, Now.AddMinutes(2), "alice").IsSuccess.Should().BeTrue();
        todo.ClearList(Now.AddMinutes(3), "alice").IsSuccess.Should().BeTrue();
        todo.RemoveCategory(categoryId, Now.AddMinutes(4), "alice").IsSuccess.Should().BeTrue();

        todo.CategoryIds.Should().BeEmpty();
        todo.ListId.Should().BeNull();
        todo.UncommittedEvents.Select(domainEvent => domainEvent.GetType().Name)
            .Should().ContainInOrder(
                nameof(TodoCreated), nameof(TodoCategoryAdded), nameof(TodoListChanged),
                nameof(TodoListChanged), nameof(TodoCategoryRemoved));
    }

    [Fact]
    public void DeleteAndRestore_ShouldRaiseLifecycleEvents()
    {
        Todo todo = Create();

        todo.Delete(isHard: false, Now.AddMinutes(1), "alice").IsSuccess.Should().BeTrue();
        todo.IsDeleted.Should().BeTrue();
        todo.DeletedBy.Should().Be("alice");

        todo.Restore(Now.AddMinutes(2), "alice").IsSuccess.Should().BeTrue();
        todo.IsDeleted.Should().BeFalse();
        todo.DeletedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Snapshot_ShouldRoundTrip()
    {
        Todo todo = Create();
        todo.AddCategory(Guid.NewGuid(), Now.AddMinutes(1), "alice");
        todo.Start(Now.AddMinutes(2), "alice");

        string state = todo.CaptureSnapshot();
        Todo restored = new TodoFactory().Create();
        restored.RestoreSnapshot(state);

        restored.Id.Should().Be(todo.Id);
        restored.TenantId.Should().Be(todo.TenantId);
        restored.Title.Should().Be(todo.Title);
        restored.Status.Should().Be(TodoStatus.InProgress);
        restored.CategoryIds.Should().BeEquivalentTo(todo.CategoryIds);
        restored.Version.Should().Be(todo.Version);
    }

    [Fact]
    public void Rehydration_ShouldMatchLiveState()
    {
        Todo live = Create();
        live.AddCategory(Guid.NewGuid(), Now.AddMinutes(1), "alice");
        live.Start(Now.AddMinutes(2), "alice");
        live.Complete(Now.AddMinutes(3), "alice");
        live.Update("Final", "final", TodoPriority.Low, null, null, Now.AddMinutes(4), "bob");

        var rehydrated = new TodoFactory().Create();
        Result loaded = rehydrated.TryLoadFromHistory(
            live.Version,
            live.UncommittedEvents);

        loaded.IsSuccess.Should().BeTrue();
        rehydrated.Id.Should().Be(live.Id);
        rehydrated.Title.Should().Be("Final");
        rehydrated.Status.Should().Be(TodoStatus.Completed);
        rehydrated.CompletedAtUtc.Should().Be(Now.AddMinutes(3));
        rehydrated.ModifiedBy.Should().Be("bob");
        rehydrated.Version.Should().Be(live.Version);
        rehydrated.UncommittedEvents.Should().BeEmpty();
    }

    private static Todo Create() =>
        Todo.Create(Guid.NewGuid(), TenantId, "Original", Now, "alice").Value;
}