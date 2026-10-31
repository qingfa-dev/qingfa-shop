using Core.BuildingBlocks.Modeling.Concerns.Lifecycle.SoftDeletable;
using Core.Modules.Todos.Lists;

namespace Core.Unit.Tests.Modules.Todos.Lists;

/// <summary>Specifications for the todo list behaviours.</summary>
public sealed class TodoListExtensionSpec
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset CreatedAt = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static TodoList ValidList()
    {
        var result = TodoList.Create(Guid.NewGuid(), TenantId, "Groceries", CreatedAt, "alice");

        result.IsSuccess.Should().BeTrue();

        return result.Value;
    }

    private static TodoList DeletedList()
    {
        var list = ValidList();

        Result<TodoList>.Success(list).MarkDeleted(CreatedAt, "alice");

        list.IsDeleted.Should().BeTrue();

        return list;
    }

    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact]
    public void SetName_NewName_ShouldSetTheNameAndRaiseTodoListRenamed()
    {
        var list = ValidList();

        var result = Result<TodoList>.Success(list).SetName("Hardware");

        result.IsSuccess.Should().BeTrue();
        list.Name.Should().Be("Hardware");
        list.Events.OfType<TodoListRenamed>().Should().ContainSingle();
    }

    [Fact]
    public void SetName_SameName_ShouldNotRaiseTodoListRenamed()
    {
        var list = ValidList();

        Result<TodoList>.Success(list).SetName("Groceries");

        list.Events.OfType<TodoListRenamed>().Should().BeEmpty();
    }

    [Fact]
    public void SetName_PaddedName_ShouldStoreTheTrimmedName()
    {
        var list = ValidList();

        Result<TodoList>.Success(list).SetName("  Hardware  ");

        list.Name.Should().Be("Hardware");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetName_MissingName_ShouldFailWithoutMutation(string? name)
    {
        var list = ValidList();

        ShouldFailWith(
            Result<TodoList>.Success(list).SetName(name),
            "TodoList.Name.Required");

        list.Name.Should().Be("Groceries");
    }

    [Fact]
    public void SetName_LongName_ShouldFailWithoutMutation()
    {
        var list = ValidList();

        ShouldFailWith(
            Result<TodoList>.Success(list).SetName(new string('a', 129)),
            "TodoList.Name.TooLong");

        list.Name.Should().Be("Groceries");
    }

    [Fact]
    public void SetDescription_Text_ShouldSetTheDescription()
    {
        var list = ValidList();

        var result = Result<TodoList>.Success(list).SetDescription("Weekly shop");

        result.IsSuccess.Should().BeTrue();
        list.Description.Should().Be("Weekly shop");
    }

    [Fact]
    public void SetDescription_Null_ShouldClearTheDescription()
    {
        var list = ValidList();
        list.Description = "Weekly shop";

        Result<TodoList>.Success(list).SetDescription(null);

        list.Description.Should().BeNull();
    }

    [Fact]
    public void SetDescription_LongDescription_ShouldFailWithoutMutation()
    {
        var list = ValidList();

        ShouldFailWith(
            Result<TodoList>.Success(list).SetDescription(new string('a', 1001)),
            "TodoList.Description.TooLong");

        list.Description.Should().BeNull();
    }

    [Fact]
    public void SetName_DeletedList_ShouldFailWithoutMutation()
    {
        var list = DeletedList();

        ShouldFailWith(
            Result<TodoList>.Success(list).SetName("Hardware"),
            "TodoList.Deleted.NotMutable");

        list.Name.Should().Be("Groceries");
    }
}