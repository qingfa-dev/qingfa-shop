using Core.Common.Concerns.Lifecycle.Modifiable;
using Core.Common.Concerns.Lifecycle.SoftDeletable;
using Core.Todos.Items;

namespace Core.Unit.Tests.Todos.Items;

/// <summary>Specifications for todo creation and aggregate state.</summary>
public sealed class TodoSpec
{
    private static readonly Guid TodoId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset CreatedAt = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

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
    public void Create_ValidInput_ShouldRaiseTodoCreated()
    {
        var result = Todo.Create(TodoId, TenantId, "Buy milk", CreatedAt, "alice");

        var raised = result.Value.Events.Should().ContainSingle().Which;
        raised.Should().BeOfType<TodoCreated>();
        ((TodoCreated)raised).TodoId.Should().Be(TodoId);
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
            "Identifiable.Id.Required");
    }

    [Fact]
    public void Create_EmptyTenantId_ShouldFailWithTenantableTenantIdRequired()
    {
        ShouldFailWith(
            Todo.Create(TodoId, Guid.Empty, "Buy milk", CreatedAt, "alice"),
            "Tenantable.TenantId.Required");
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
    public void IsCompleted_ShouldFollowTheStatus()
    {
        var todo = Todo.Create(TodoId, TenantId, "Buy milk", CreatedAt, "alice").Value;

        todo.IsCompleted.Should().BeFalse();

        todo.Status = TodoStatus.Completed;

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

    [Fact]
    public void MarkDeleted_ThenRestore_ShouldRoundTripTheDeletionAudit()
    {
        Todo todo = Todo.Create(TodoId, TenantId, "Buy milk", CreatedAt, "alice").Value;
        DateTimeOffset deletedAt = CreatedAt.AddDays(1);

        Result<Todo> deleted = Result<Todo>.Ok(todo).MarkDeleted(deletedAt, "bob");

        deleted.IsSuccess.Should().BeTrue();
        todo.IsDeleted.Should().BeTrue();
        todo.DeletedAtUtc.Should().Be(deletedAt);
        todo.DeletedBy.Should().Be("bob");

        Result<Todo> restored = Result<Todo>.Ok(todo).Restore("alice");

        restored.IsSuccess.Should().BeTrue();
        todo.IsDeleted.Should().BeFalse();
        todo.DeletedAtUtc.Should().BeNull();
        todo.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void MarkModified_ShouldStampTheTrimmedActor()
    {
        Todo todo = Todo.Create(TodoId, TenantId, "Buy milk", CreatedAt, "alice").Value;
        DateTimeOffset modifiedAt = CreatedAt.AddHours(1);

        Result<Todo> modified = Result<Todo>.Ok(todo).MarkModified(modifiedAt, "bob");

        modified.IsSuccess.Should().BeTrue();
        todo.ModifiedAtUtc.Should().Be(modifiedAt);
        todo.ModifiedBy.Should().Be("bob");
    }
}