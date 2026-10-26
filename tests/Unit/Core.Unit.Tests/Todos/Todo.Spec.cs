using Core.Todos;

namespace Core.Unit.Tests.Todos;

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
}