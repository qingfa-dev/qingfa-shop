using Core.Todos.Lists;

namespace Core.Tests.Todos.Lists;

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
}