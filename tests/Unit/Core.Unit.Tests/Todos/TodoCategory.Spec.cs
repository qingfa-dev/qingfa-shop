using Core.Todos;

namespace Core.Unit.Tests.Todos;

/// <summary>Specifications for todo category creation and aggregate state.</summary>
public sealed class TodoCategorySpec
{
    private static readonly Guid CategoryId = Guid.Parse("44444444-4444-4444-4444-444444444444");
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
        var result = TodoCategory.Create(CategoryId, TenantId, "Errands", CreatedAt, "alice");

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(CategoryId);
    }

    [Fact]
    public void Create_ValidInput_ShouldStampTheAuditMetadataAndTenant()
    {
        var result = TodoCategory.Create(CategoryId, TenantId, "Errands", CreatedAt, "alice");

        result.Value.CreatedAtUtc.Should().Be(CreatedAt);
        result.Value.CreatedBy.Should().Be("alice");
        result.Value.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void Create_PaddedName_ShouldStoreTheTrimmedName()
    {
        var result = TodoCategory.Create(CategoryId, TenantId, "  Errands  ", CreatedAt, "alice");

        result.Value.Name.Should().Be("Errands");
    }

    [Fact]
    public void Create_ValidInput_ShouldRaiseTodoCategoryCreated()
    {
        var result = TodoCategory.Create(CategoryId, TenantId, "Errands", CreatedAt, "alice");

        var raised = result.Value.Events.Should().ContainSingle().Which;
        raised.Should().BeOfType<TodoCategoryCreated>();
        ((TodoCategoryCreated)raised).CategoryId.Should().Be(CategoryId);
    }

    [Fact]
    public void Create_EmptyId_ShouldFailWithIdentifiableIdRequired()
    {
        ShouldFailWith(
            TodoCategory.Create(Guid.Empty, TenantId, "Errands", CreatedAt, "alice"),
            "Identifiable.Id.Required");
    }

    [Fact]
    public void Create_EmptyTenantId_ShouldFailWithTenantableTenantIdRequired()
    {
        ShouldFailWith(
            TodoCategory.Create(CategoryId, Guid.Empty, "Errands", CreatedAt, "alice"),
            "Tenantable.TenantId.Required");
    }

    [Fact]
    public void Create_BlankName_ShouldFailWithNameRequired()
    {
        ShouldFailWith(
            TodoCategory.Create(CategoryId, TenantId, "   ", CreatedAt, "alice"),
            "TodoCategory.Name.Required");
    }
}