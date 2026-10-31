using Core.Modules.Todos.Categories;

namespace Core.Unit.Tests.Modules.Todos.Categories;

/// <summary>Specifications for the todo category invariants.</summary>
public sealed class TodoCategoryValidatorSpec
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset CreatedAt = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static TodoCategory ValidCategory()
    {
        var result = TodoCategory.Create(TenantId, "Errands", CreatedAt, "alice", id: Guid.NewGuid());

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
    public void Validate_NullCategory_ShouldFailWithEntityRequired()
    {
        ShouldFailWith(
            TodoCategoryValidator.Validate(null!),
            "TodoCategory.Entity.Required");
    }

    [Fact]
    public void Validate_EmptyTenantId_ShouldFailWithTenantableTenantIdRequired()
    {
        var category = ValidCategory();
        category.TenantId = Guid.Empty;

        ShouldFailWith(
            TodoCategoryValidator.Validate(category),
            "Tenantable.TenantId.Required");
    }

    [Fact]
    public void Validate_BlankName_ShouldFailWithNameRequired()
    {
        var category = ValidCategory();
        category.Name = "   ";

        ShouldFailWith(
            TodoCategoryValidator.Validate(category),
            "TodoCategory.Name.Required");
    }

    [Fact]
    public void Validate_LongName_ShouldFailWithNameTooLong()
    {
        var category = ValidCategory();
        category.Name = new string('a', TodoCategoryConstant.Constraints.Name.MaxLength + 1);

        ShouldFailWith(
            TodoCategoryValidator.Validate(category),
            "TodoCategory.Name.TooLong");
    }

    [Fact]
    public void Validate_LongDescription_ShouldFailWithDescriptionTooLong()
    {
        var category = ValidCategory();
        category.Description = new string('a', TodoCategoryConstant.Constraints.Description.MaxLength + 1);

        ShouldFailWith(
            TodoCategoryValidator.Validate(category),
            "TodoCategory.Description.TooLong");
    }

    [Fact]
    public void Validate_ValidCategory_ShouldSucceed()
    {
        var result = TodoCategoryValidator.Validate(ValidCategory());

        result.IsSuccess.Should().BeTrue();
    }
}