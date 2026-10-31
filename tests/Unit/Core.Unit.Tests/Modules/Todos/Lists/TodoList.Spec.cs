using Core.BuildingBlocks.Modeling.Concerns.Lifecycle.Modifiable;
using Core.BuildingBlocks.Modeling.Concerns.Lifecycle.SoftDeletable;
using Core.Modules.Todos.Lists;

namespace Core.Unit.Tests.Modules.Todos.Lists;

/// <summary>Specifications for todo list creation and aggregate state.</summary>
public sealed class TodoListSpec
{
    private static readonly Guid ListId = Guid.Parse("33333333-3333-3333-3333-333333333333");
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
        var result = TodoList.Create(ListId, TenantId, "Groceries", CreatedAt, "alice");

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(ListId);
    }

    [Fact]
    public void Create_ValidInput_ShouldStampTheAuditMetadataAndTenant()
    {
        var result = TodoList.Create(ListId, TenantId, "Groceries", CreatedAt, "alice");

        result.Value.CreatedAtUtc.Should().Be(CreatedAt);
        result.Value.CreatedBy.Should().Be("alice");
        result.Value.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void Create_PaddedName_ShouldStoreTheTrimmedName()
    {
        var result = TodoList.Create(ListId, TenantId, "  Groceries  ", CreatedAt, "alice");

        result.Value.Name.Should().Be("Groceries");
    }

    [Fact]
    public void Create_ValidInput_ShouldRaiseTodoListCreated()
    {
        var result = TodoList.Create(ListId, TenantId, "Groceries", CreatedAt, "alice");

        var raised = result.Value.Events.Should().ContainSingle().Which;
        raised.Should().BeOfType<TodoListCreated>();
        ((TodoListCreated)raised).ListId.Should().Be(ListId);
    }

    [Fact]
    public void Create_EmptyId_ShouldFailWithIdentifiableIdRequired()
    {
        ShouldFailWith(
            TodoList.Create(Guid.Empty, TenantId, "Groceries", CreatedAt, "alice"),
            "Identifiable.Id.Required");
    }

    [Fact]
    public void Create_EmptyTenantId_ShouldFailWithTenantableTenantIdRequired()
    {
        ShouldFailWith(
            TodoList.Create(ListId, Guid.Empty, "Groceries", CreatedAt, "alice"),
            "Tenantable.TenantId.Required");
    }

    [Fact]
    public void Create_BlankName_ShouldFailWithNameRequired()
    {
        ShouldFailWith(
            TodoList.Create(ListId, TenantId, "   ", CreatedAt, "alice"),
            "TodoList.Name.Required");
    }

    [Fact]
    public void Create_NoOptionalInput_ShouldLeaveOptionalStateEmpty()
    {
        var result = TodoList.Create(ListId, TenantId, "Groceries", CreatedAt, "alice");

        result.Value.Description.Should().BeNull();
    }

    [Fact]
    public void Create_NoModificationOrDeletion_ShouldLeaveTheAuditFlagsClear()
    {
        var result = TodoList.Create(ListId, TenantId, "Groceries", CreatedAt, "alice");

        result.Value.ModifiedAtUtc.Should().BeNull();
        result.Value.ModifiedBy.Should().BeNull();
        result.Value.IsDeleted.Should().BeFalse();
        result.Value.DeletedAtUtc.Should().BeNull();
        result.Value.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void Create_PaddedDescription_ShouldStoreTheTrimmedDescription()
    {
        var result = TodoList.Create(
            ListId,
            TenantId,
            "Groceries",
            CreatedAt,
            "alice",
            description: "  Weekly shop  ");

        result.Value.Description.Should().Be("Weekly shop");
    }

    [Fact]
    public void Create_DescriptionExceedingMaxLength_ShouldFailWithDescriptionTooLong()
    {
        string tooLong = new('a', TodoListConstant.Constraints.Description.MaxLength + 1);

        ShouldFailWith(
            TodoList.Create(ListId, TenantId, "Groceries", CreatedAt, "alice", description: tooLong),
            "TodoList.Description.TooLong");
    }

    [Fact]
    public void Create_NameExceedingMaxLength_ShouldFailWithNameTooLong()
    {
        string tooLong = new('a', TodoListConstant.Constraints.Name.MaxLength + 1);

        ShouldFailWith(
            TodoList.Create(ListId, TenantId, tooLong, CreatedAt, "alice"),
            "TodoList.Name.TooLong");
    }

    [Fact]
    public void MarkDeleted_ThenRestore_ShouldRoundTripTheDeletionAudit()
    {
        TodoList list = TodoList.Create(ListId, TenantId, "Groceries", CreatedAt, "alice").Value;
        DateTimeOffset deletedAt = CreatedAt.AddDays(1);

        Result<TodoList> deleted = Result<TodoList>.Ok(list).MarkDeleted(deletedAt, "bob");

        deleted.IsSuccess.Should().BeTrue();
        list.IsDeleted.Should().BeTrue();
        list.DeletedAtUtc.Should().Be(deletedAt);
        list.DeletedBy.Should().Be("bob");

        Result<TodoList> restored = Result<TodoList>.Ok(list).Restore("alice");

        restored.IsSuccess.Should().BeTrue();
        list.IsDeleted.Should().BeFalse();
        list.DeletedAtUtc.Should().BeNull();
        list.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void MarkModified_ShouldStampTheTrimmedActor()
    {
        TodoList list = TodoList.Create(ListId, TenantId, "Groceries", CreatedAt, "alice").Value;
        DateTimeOffset modifiedAt = CreatedAt.AddHours(1);

        Result<TodoList> modified = Result<TodoList>.Ok(list).MarkModified(modifiedAt, "bob");

        modified.IsSuccess.Should().BeTrue();
        list.ModifiedAtUtc.Should().Be(modifiedAt);
        list.ModifiedBy.Should().Be("bob");
    }
}