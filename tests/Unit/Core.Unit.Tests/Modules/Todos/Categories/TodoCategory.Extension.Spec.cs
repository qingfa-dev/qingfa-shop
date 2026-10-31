using Core.BuildingBlocks.Modeling.Concerns.Lifecycle.SoftDeletable;
using Core.Modules.Todos.Categories;

namespace Core.Unit.Tests.Modules.Todos.Categories;

/// <summary>Specifications for the todo category behaviours.</summary>
public sealed class TodoCategoryExtensionSpec
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset CreatedAt = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static TodoCategory ValidCategory()
    {
        var result = TodoCategory.Create(tenantId: TenantId, "Errands", CreatedAt, "alice", id: Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();

        return result.Value;
    }

    private static TodoCategory DeletedCategory()
    {
        var category = ValidCategory();

        Result<TodoCategory>.Success(category).MarkDeleted(CreatedAt, "alice");

        category.IsDeleted.Should().BeTrue();

        return category;
    }

    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact]
    public void SetName_NewName_ShouldSetTheNameAndRaiseTodoCategoryRenamed()
    {
        var category = ValidCategory();

        var result = Result<TodoCategory>.Success(category).SetName("Chores");

        result.IsSuccess.Should().BeTrue();
        category.Name.Should().Be("Chores");
        category.Events.OfType<TodoCategoryRenamed>().Should().ContainSingle();
    }

    [Fact]
    public void SetName_SameName_ShouldNotRaiseTodoCategoryRenamed()
    {
        var category = ValidCategory();

        Result<TodoCategory>.Success(category).SetName("Errands");

        category.Events.OfType<TodoCategoryRenamed>().Should().BeEmpty();
    }

    [Fact]
    public void SetName_PaddedName_ShouldStoreTheTrimmedName()
    {
        var category = ValidCategory();

        Result<TodoCategory>.Success(category).SetName("  Chores  ");

        category.Name.Should().Be("Chores");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetName_MissingName_ShouldFailWithoutMutation(string? name)
    {
        var category = ValidCategory();

        ShouldFailWith(
            Result<TodoCategory>.Success(category).SetName(name),
            "TodoCategory.Name.Required");

        category.Name.Should().Be("Errands");
    }

    [Fact]
    public void SetName_LongName_ShouldFailWithoutMutation()
    {
        var category = ValidCategory();

        ShouldFailWith(
            Result<TodoCategory>.Success(category).SetName(new string('a', 129)),
            "TodoCategory.Name.TooLong");

        category.Name.Should().Be("Errands");
    }

    [Fact]
    public void SetDescription_Text_ShouldSetTheDescription()
    {
        var category = ValidCategory();

        var result = Result<TodoCategory>.Success(category).SetDescription("Anything outside");

        result.IsSuccess.Should().BeTrue();
        category.Description.Should().Be("Anything outside");
    }

    [Fact]
    public void SetDescription_Null_ShouldClearTheDescription()
    {
        var category = ValidCategory();
        category.Description = "Anything outside";

        Result<TodoCategory>.Success(category).SetDescription(null);

        category.Description.Should().BeNull();
    }

    [Fact]
    public void SetDescription_LongDescription_ShouldFailWithoutMutation()
    {
        var category = ValidCategory();

        ShouldFailWith(
            Result<TodoCategory>.Success(category).SetDescription(new string('a', 1001)),
            "TodoCategory.Description.TooLong");

        category.Description.Should().BeNull();
    }

    [Fact]
    public void SetName_DeletedCategory_ShouldFailWithoutMutation()
    {
        var category = DeletedCategory();

        ShouldFailWith(
            Result<TodoCategory>.Success(category).SetName("Chores"),
            "TodoCategory.Deleted.NotMutable");

        category.Name.Should().Be("Errands");
    }
}