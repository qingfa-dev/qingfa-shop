using Core.BuildingBlocks.Modeling.Concerns.Lifecycle.Modifiable;
using Core.BuildingBlocks.Modeling.Concerns.Lifecycle.SoftDeletable;
using Core.Modules.Todos.Categories;

namespace Core.Unit.Tests.Modules.Todos.Categories;

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
        var result = TodoCategory.Create(TenantId, "Errands", CreatedAt, "alice", id: CategoryId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(CategoryId);
    }

    [Fact]
    public void Create_ValidInput_ShouldStampTheAuditMetadataAndTenant()
    {
        var result = TodoCategory.Create(TenantId, "Errands", CreatedAt, "alice", id: CategoryId);

        result.Value.CreatedAtUtc.Should().Be(CreatedAt);
        result.Value.CreatedBy.Should().Be("alice");
        result.Value.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void Create_PaddedName_ShouldStoreTheTrimmedName()
    {
        var result = TodoCategory.Create(TenantId, "  Errands  ", CreatedAt, "alice", id: CategoryId);

        result.Value.Name.Should().Be("Errands");
    }

    [Fact]
    public void Create_ValidInput_ShouldRaiseTodoCategoryCreated()
    {
        var result = TodoCategory.Create(TenantId, "Errands", CreatedAt, "alice", id: CategoryId);

        var raised = result.Value.Events.Should().ContainSingle().Which;
        raised.Should().BeOfType<TodoCategoryCreated>();
        ((TodoCategoryCreated)raised).CategoryId.Should().Be(CategoryId);
    }

    [Fact]
    public void Create_EmptyId_ShouldFailWithIdentifiableIdRequired()
    {
        ShouldFailWith(
            TodoCategory.Create(TenantId, "Errands", CreatedAt, "alice", id: Guid.Empty),
            "Identifiable.Id.Required");
    }

    [Fact]
    public void Create_EmptyTenantId_ShouldFailWithTenantableTenantIdRequired()
    {
        ShouldFailWith(
            TodoCategory.Create(Guid.Empty, "Errands", CreatedAt, "alice",
            id: CategoryId),
            "Tenantable.TenantId.Required");
    }

    [Fact]
    public void Create_BlankName_ShouldFailWithNameRequired()
    {
        ShouldFailWith(
            TodoCategory.Create(
                TenantId, "   ",
                CreatedAt, "alice",
                id: CategoryId),
            "TodoCategory.Name.Required");
    }

    [Fact]
    public void Create_NoOptionalInput_ShouldLeaveOptionalStateEmpty()
    {
        var result = TodoCategory.Create(
            TenantId, "Errands",
            CreatedAt, "alice",
            id: CategoryId);

        result.Value.Description.Should().BeNull();
    }

    [Fact]
    public void Create_NoModificationOrDeletion_ShouldLeaveTheAuditFlagsClear()
    {
        var result = TodoCategory.Create(
            TenantId, "Errands",
            CreatedAt, "alice",
            id: CategoryId);

        result.Value.ModifiedAtUtc.Should().BeNull();
        result.Value.ModifiedBy.Should().BeNull();
        result.Value.IsDeleted.Should().BeFalse();
        result.Value.DeletedAtUtc.Should().BeNull();
        result.Value.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void Create_PaddedDescription_ShouldStoreTheTrimmedDescription()
    {
        var result = TodoCategory.Create(
            TenantId,
            "Errands",
            CreatedAt,
            "alice",
            description: "  Out of the house  ",
            id: CategoryId);

        result.Value.Description.Should().Be("Out of the house");
    }

    [Fact]
    public void Create_DescriptionExceedingMaxLength_ShouldFailWithDescriptionTooLong()
    {
        string tooLong = new('a', TodoCategoryConstant.Constraints.Description.MaxLength + 1);

        ShouldFailWith(
            TodoCategory.Create(TenantId, "Errands", CreatedAt, "alice", description: tooLong,
            id: CategoryId),
            "TodoCategory.Description.TooLong");
    }

    [Fact]
    public void Create_NameExceedingMaxLength_ShouldFailWithNameTooLong()
    {
        string tooLong = new('a', TodoCategoryConstant.Constraints.Name.MaxLength + 1);

        ShouldFailWith(
            TodoCategory.Create(
                TenantId, tooLong, CreatedAt, "alice",
                id: CategoryId),
            "TodoCategory.Name.TooLong");
    }

    [Fact]
    public void MarkDeleted_ThenRestore_ShouldRoundTripTheDeletionAudit()
    {
        TodoCategory category = TodoCategory.Create(
            TenantId,
            "Errands",
            CreatedAt,
            "alice",
            id: CategoryId).Value;
        DateTimeOffset deletedAt = CreatedAt.AddDays(1);

        Result<TodoCategory> deleted = Result<TodoCategory>.Ok(category).MarkDeleted(deletedAt, "bob");

        deleted.IsSuccess.Should().BeTrue();
        category.IsDeleted.Should().BeTrue();
        category.DeletedAtUtc.Should().Be(deletedAt);
        category.DeletedBy.Should().Be("bob");

        Result<TodoCategory> restored = Result<TodoCategory>.Ok(category).Restore("alice");

        restored.IsSuccess.Should().BeTrue();
        category.IsDeleted.Should().BeFalse();
        category.DeletedAtUtc.Should().BeNull();
        category.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void MarkModified_ShouldStampTheTrimmedActor()
    {
        TodoCategory category = TodoCategory.Create(
            TenantId,
            "Errands",
            CreatedAt,
            "alice",
            id: CategoryId).Value;
        DateTimeOffset modifiedAt = CreatedAt.AddHours(1);

        Result<TodoCategory> modified = Result<TodoCategory>.Ok(category)
            .MarkModified(modifiedAt, "bob");

        modified.IsSuccess.Should().BeTrue();
        category.ModifiedAtUtc.Should().Be(modifiedAt);
        category.ModifiedBy.Should().Be("bob");
    }
}