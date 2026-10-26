using Core.Common.Concerns.Organization.Categorizable;

namespace Core.Unit.Tests.Common.Concerns.Organization.Categorizable;

/// <summary>Specification for <see cref="CategorizableExtensions"/>.</summary>
public class CategorizableExtensionSpec
{
    #region Helper Methods

    private sealed class TestCategorizable : ICategorizable<string>
    {
        public ICollection<string> Categories { get; } = new List<string>();
    }

    private sealed class TestCategorizableGuid : ICategorizable<Guid>
    {
        public ICollection<Guid> Categories { get; } = new List<Guid>();
    }

    /// <summary>Creates a successful result for the test entity.</summary>
    private static Result<TestCategorizable> Success(
        TestCategorizable entity)
    {
        return Result<TestCategorizable>.Success(entity);
    }

    #endregion

    #region Test Cases

    [Fact]
    public void AddCategory_NewCategory_ShouldAddCategory()
    {
        // Arrange:
        var entity = new TestCategorizable();

        // Act:
        var result = Success(entity).AddCategory("shoes");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Categories.Should().ContainSingle().Which.Should().Be("shoes");
    }

    [Fact]
    public void AddCategory_DuplicateCategory_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestCategorizable();
        Success(entity).AddCategory("shoes");

        // Act:
        var result = Success(entity).AddCategory("shoes");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(
            "Categorizable.Category.Duplicate");
        entity.Categories.Should().ContainSingle();
    }

    [Fact]
    public void RemoveCategory_AssignedCategory_ShouldRemoveCategory()
    {
        // Arrange:
        var entity = new TestCategorizable();
        Success(entity).AddCategory("shoes");

        // Act:
        var result = Success(entity).RemoveCategory("shoes");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Categories.Should().BeEmpty();
    }

    [Fact]
    public void RemoveCategory_MissingCategory_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestCategorizable();
        Success(entity).AddCategory("shoes");

        // Act:
        var result = Success(entity).RemoveCategory("boots");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(
            "Categorizable.Category.NotFound");
        entity.Categories.Should().ContainSingle();
    }

    [Fact]
    public void ValidateAddCategory_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = CategorizableValidator.ValidateAddCategory(
            (TestCategorizable)null!,
            "shoes");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Categorizable.Entity.Required");
    }

    #endregion

    #region Validator Specs

    [Fact]
    public void ValidateAddCategory_NullCategory_ShouldFailCategoryRequired()
    {
        // Arrange:
        var entity = new TestCategorizable();

        // Act:
        var result = CategorizableValidator.ValidateAddCategory(
            entity,
            (string)null!);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Categorizable.Category.Required");
    }

    [Fact]
    public void ValidateAddCategory_EmptyGuid_ShouldFailCategoryEmpty()
    {
        // Arrange:
        var entity = new TestCategorizableGuid();

        // Act:
        var result = CategorizableValidator.ValidateAddCategory(
            entity,
            Guid.Empty);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Categorizable.Category.Empty");
    }

    [Fact]
    public void ValidateAddCategory_BlankName_ShouldFailCategoryRequired()
    {
        // Arrange:
        var entity = new TestCategorizable();

        // Act:
        var result = CategorizableValidator.ValidateAddCategory(
            entity,
            "   ");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Categorizable.Category.Required");
    }

    [Fact]
    public void ValidateAddCategory_TooLongName_ShouldFailCategoryTooLong()
    {
        // Arrange:
        var entity = new TestCategorizable();
        var longName = new string(
            'x',
            CategorizableConstant.Constraints.Category.MaxNameLength + 1);

        // Act:
        var result = CategorizableValidator.ValidateAddCategory(
            entity,
            longName);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Categorizable.Category.TooLong");
    }

    [Fact]
    public void ValidateAddCategory_Duplicate_ShouldFailCategoryDuplicate()
    {
        // Arrange:
        var entity = new TestCategorizable();
        entity.Categories.Add("shoes");

        // Act:
        var result = CategorizableValidator.ValidateAddCategory(
            entity,
            "shoes");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Categorizable.Category.Duplicate");
    }

    [Fact]
    public void ValidateAddCategory_LimitExceeded_ShouldFailCategoryLimitExceeded()
    {
        // Arrange:
        var entity = new TestCategorizable();
        for (var i = 0; i < CategorizableConstant.Constraints.Category.MaxCount; i++)
        {
            entity.Categories.Add($"cat-{i}");
        }

        // Act:
        var result = CategorizableValidator.ValidateAddCategory(
            entity,
            "one-more");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Categorizable.Category.LimitExceeded");
    }

    [Fact]
    public void ValidateAddCategory_Valid_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestCategorizable();

        // Act:
        var result = CategorizableValidator.ValidateAddCategory(
            entity,
            "shoes");

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateRemoveCategory_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = CategorizableValidator.ValidateRemoveCategory(
            (TestCategorizable)null!,
            "shoes");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Categorizable.Entity.Required");
    }

    [Fact]
    public void ValidateRemoveCategory_NullCategory_ShouldFailCategoryRequired()
    {
        // Arrange:
        var entity = new TestCategorizable();

        // Act:
        var result = CategorizableValidator.ValidateRemoveCategory(
            entity,
            (string)null!);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Categorizable.Category.Required");
    }

    [Fact]
    public void ValidateRemoveCategory_Missing_ShouldFailCategoryNotFound()
    {
        // Arrange:
        var entity = new TestCategorizable();
        entity.Categories.Add("shoes");

        // Act:
        var result = CategorizableValidator.ValidateRemoveCategory(
            entity,
            "boots");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Categorizable.Category.NotFound");
    }

    [Fact]
    public void ValidateRemoveCategory_Assigned_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestCategorizable();
        entity.Categories.Add("shoes");

        // Act:
        var result = CategorizableValidator.ValidateRemoveCategory(
            entity,
            "shoes");

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ClearCategories_AssignedCategories_ShouldRemoveAll()
    {
        // Arrange:
        var entity = new TestCategorizable();
        Success(entity).AddCategory("shoes");

        // Act:
        var result = Success(entity).ClearCategories<TestCategorizable, string>();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Categories.Should().BeEmpty();
    }

    #endregion

    #region Batch and Replace Specs

    [Fact]
    public void AddCategory_TrimmedInput_ShouldStoreTrimmed()
    {
        // Arrange:
        var entity = new TestCategorizable();

        // Act:
        var result = Success(entity).AddCategory("  shoes  ");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Categories.Should().ContainSingle().Which.Should().Be("shoes");
    }

    [Fact]
    public void AddCategory_NullCategory_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestCategorizable();

        // Act:
        var result = Success(entity).AddCategory((string)null!);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Categorizable.Category.Required");
        entity.Categories.Should().BeEmpty();
    }

    [Fact]
    public void RemoveCategory_TrimmedInput_ShouldRemove()
    {
        // Arrange:
        var entity = new TestCategorizable();
        Success(entity).AddCategory("shoes");

        // Act:
        var result = Success(entity).RemoveCategory("  shoes  ");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Categories.Should().BeEmpty();
    }

    [Fact]
    public void AddCategories_AllValid_ShouldAddAll()
    {
        // Arrange:
        var entity = new TestCategorizable();

        // Act:
        var result = Success(entity).AddCategories<TestCategorizable, string>(new[] { "shoes", "boots" });

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Categories.Count.Should().Be(2);
        entity.Categories.Should().Contain("shoes");
        entity.Categories.Should().Contain("boots");
    }

    [Fact]
    public void AddCategories_DuplicateOfExisting_ShouldFailWithoutPartialMutation()
    {
        // Arrange:
        var entity = new TestCategorizable();
        Success(entity).AddCategory("shoes");

        // Act:
        var result = Success(entity).AddCategories<TestCategorizable, string>(new[] { "boots", "shoes" });

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Categorizable.Category.Duplicate");
        entity.Categories.Count.Should().Be(1);
        entity.Categories.Should().Contain("shoes");
        entity.Categories.Should().NotContain("boots");
    }

    [Fact]
    public void AddCategories_NullItem_ShouldFailWithoutPartialMutation()
    {
        // Arrange:
        var entity = new TestCategorizable();

        // Act:
        var result = Success(entity).AddCategories<TestCategorizable, string>(new[] { "boots", null! });

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Categorizable.Category.Required");
        entity.Categories.Should().BeEmpty();
    }

    [Fact]
    public void AddCategories_SecondItemTooLong_ShouldFailWithoutPartialMutation()
    {
        // Arrange:
        var entity = new TestCategorizable();
        var longName = new string('x', CategorizableConstant.Constraints.Category.MaxNameLength + 1);

        // Act:
        var result = Success(entity).AddCategories<TestCategorizable, string>(new[] { "ok", longName });

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Categorizable.Category.TooLong");
        entity.Categories.Should().BeEmpty();
    }

    [Fact]
    public void AddCategories_ExceedingLimit_ShouldFailWithoutPartialMutation()
    {
        // Arrange:
        var entity = new TestCategorizable();
        for (var i = 0; i < CategorizableConstant.Constraints.Category.MaxCount - 1; i++)
        {
            entity.Categories.Add($"cat-{i}");
        }

        // Act:
        var result = Success(entity).AddCategories<TestCategorizable, string>(new[] { "extra-a", "extra-b" });

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Categorizable.Category.LimitExceeded");
        entity.Categories.Count.Should().Be(CategorizableConstant.Constraints.Category.MaxCount - 1);
        entity.Categories.Should().NotContain("extra-a");
        entity.Categories.Should().NotContain("extra-b");
    }

    [Fact]
    public void SetCategories_Replace_ShouldReplaceAll()
    {
        // Arrange:
        var entity = new TestCategorizable();
        Success(entity).AddCategory("old");

        // Act:
        var result = Success(entity).SetCategories<TestCategorizable, string>(new[] { "a", "b" });

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Categories.Count.Should().Be(2);
        entity.Categories.Should().Contain("a");
        entity.Categories.Should().Contain("b");
        entity.Categories.Should().NotContain("old");
    }

    [Fact]
    public void SetCategories_Duplicates_ShouldDedup()
    {
        // Arrange:
        var entity = new TestCategorizable();

        // Act:
        var result = Success(entity).SetCategories<TestCategorizable, string>(new[] { "  a  ", "a", "b" });

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Categories.Count.Should().Be(2);
        entity.Categories.Should().Contain("a");
        entity.Categories.Should().Contain("b");
    }

    [Fact]
    public void SetCategories_ExceedingLimit_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestCategorizable();
        Success(entity).AddCategory("keep");
        var oversized = Enumerable.Range(0, CategorizableConstant.Constraints.Category.MaxCount + 1)
            .Select(i => $"cat-{i}")
            .ToList();

        // Act:
        var result = Success(entity).SetCategories<TestCategorizable, string>(oversized);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Categorizable.Category.LimitExceeded");
        entity.Categories.Should().ContainSingle().Which.Should().Be("keep");
    }

    [Fact]
    public void ClearCategories_Empty_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestCategorizable();

        // Act:
        var result = Success(entity).ClearCategories<TestCategorizable, string>();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Categories.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAddCategory_GuidValid_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestCategorizableGuid();

        // Act:
        var result = CategorizableValidator.ValidateAddCategory(entity, Guid.NewGuid());

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void AddCategory_GuidValid_ShouldAdd()
    {
        // Arrange:
        var entity = new TestCategorizableGuid();
        var id = Guid.NewGuid();

        // Act:
        var result = Result<TestCategorizableGuid>.Success(entity).AddCategory(id);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Categories.Should().ContainSingle().Which.Should().Be(id);
    }

    [Fact]
    public void FailureCodes_ShouldHaveExpectedCodes()
    {
        // Arrange:
        // Act:
        // Assert:
        CategorizableResult.Failure.EntityRequired.Code.Should().Be("Categorizable.Entity.Required");
        CategorizableResult.Failure.CategoryRequired.Code.Should().Be("Categorizable.Category.Required");
        CategorizableResult.Failure.CategoryEmpty.Code.Should().Be("Categorizable.Category.Empty");
        CategorizableResult.Failure.CategoryTooLong.Code.Should().Be("Categorizable.Category.TooLong");
        CategorizableResult.Failure.CategoryLimitExceeded.Code.Should().Be("Categorizable.Category.LimitExceeded");
        CategorizableResult.Failure.CategoryDuplicate.Code.Should().Be("Categorizable.Category.Duplicate");
        CategorizableResult.Failure.CategoryNotFound.Code.Should().Be("Categorizable.Category.NotFound");
    }

    #endregion

    #region Constant Specs

    [Fact]
    public void Constants_ShouldMatchNestedConstraints()
    {
        // Arrange:
        // Act:
        // Assert:
        CategorizableConstant.Constraints.Category.MinNameLength.Should().Be(1);
        CategorizableConstant.Constraints.Category.MaxNameLength.Should().Be(128);
        CategorizableConstant.Constraints.Category.MaxCount.Should().Be(32);
        CategorizableConstant.Defaults.TrimValues.Should().BeTrue();
    }

    #endregion
}