using Core.Modules.Todos.Lists;

namespace Core.Unit.Tests.Modules.Todos.Lists;

/// <summary>Specifications for the todo list invariants.</summary>
public sealed class TodoListValidatorSpec
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset CreatedAt = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static TodoList ValidList()
    {
        var result = TodoList.Create(Guid.NewGuid(), TenantId, "Groceries", CreatedAt, "alice");

        result.IsSuccess.Should().BeTrue();

        return result.Value;
    }

    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact]
    public void Validate_NullList_ShouldFailWithEntityRequired()
    {
        ShouldFailWith(
            TodoListValidator.Validate(null!),
            "TodoList.Entity.Required");
    }

    [Fact]
    public void Validate_EmptyTenantId_ShouldFailWithTenantableTenantIdRequired()
    {
        var list = ValidList();
        list.TenantId = Guid.Empty;

        ShouldFailWith(
            TodoListValidator.Validate(list),
            "Tenantable.TenantId.Required");
    }

    [Fact]
    public void Validate_BlankName_ShouldFailWithNameRequired()
    {
        var list = ValidList();
        list.Name = "   ";

        ShouldFailWith(
            TodoListValidator.Validate(list),
            "TodoList.Name.Required");
    }

    [Fact]
    public void Validate_LongName_ShouldFailWithNameTooLong()
    {
        var list = ValidList();
        list.Name = new string('a', TodoListConstant.Constraints.Name.MaxLength + 1);

        ShouldFailWith(
            TodoListValidator.Validate(list),
            "TodoList.Name.TooLong");
    }

    [Fact]
    public void Validate_LongDescription_ShouldFailWithDescriptionTooLong()
    {
        var list = ValidList();
        list.Description = new string('a', TodoListConstant.Constraints.Description.MaxLength + 1);

        ShouldFailWith(
            TodoListValidator.Validate(list),
            "TodoList.Description.TooLong");
    }

    [Fact]
    public void Validate_ValidList_ShouldSucceed()
    {
        var result = TodoListValidator.Validate(ValidList());

        result.IsSuccess.Should().BeTrue();
    }
}