using Core.Common.Concerns.Organization.Collectionable;

namespace Core.UnitTests.Common.Concerns.Organization.Collectionable;

/// <summary>Specification for <see cref="CollectionableExtensions"/>.</summary>
public class CollectionableExtensionSpec
{
    #region Helper Methods

    private sealed class TestCollectionable : ICollectionable<string>
    {
        public ICollection<string> Collections { get; } = new List<string>();
    }

    private sealed class TestCollectionableGuid : ICollectionable<Guid>
    {
        public ICollection<Guid> Collections { get; } = new List<Guid>();
    }

    /// <summary>Creates a successful result for the test entity.</summary>
    private static Result<TestCollectionable> Success(
        TestCollectionable entity)
    {
        return Result<TestCollectionable>.Success(entity);
    }

    #endregion

    #region Test Cases

    [Fact]
    public void AddCollection_NewCollection_ShouldAddCollection()
    {
        // Arrange:
        var entity = new TestCollectionable();

        // Act:
        var result = Success(entity).AddCollection("summer-2026");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Collections.Should().ContainSingle().Which.Should().Be("summer-2026");
    }

    [Fact]
    public void AddCollection_DuplicateCollection_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestCollectionable();
        Success(entity).AddCollection("summer-2026");

        // Act:
        var result = Success(entity).AddCollection("summer-2026");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(
            "Collectionable.Collection.Duplicate");
        entity.Collections.Should().ContainSingle();
    }

    [Fact]
    public void RemoveCollection_AssignedCollection_ShouldRemoveCollection()
    {
        // Arrange:
        var entity = new TestCollectionable();
        Success(entity).AddCollection("summer-2026");

        // Act:
        var result = Success(entity).RemoveCollection("summer-2026");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Collections.Should().BeEmpty();
    }

    [Fact]
    public void RemoveCollection_MissingCollection_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestCollectionable();
        Success(entity).AddCollection("summer-2026");

        // Act:
        var result = Success(entity).RemoveCollection("winter-2026");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(
            "Collectionable.Collection.NotFound");
        entity.Collections.Should().ContainSingle();
    }

    [Fact]
    public void ValidateAddCollection_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = CollectionableValidator.ValidateAddCollection(
            (TestCollectionable)null!,
            "summer-2026");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(
            "Collectionable.Entity.Required");
    }

    #endregion

    #region Validator Specs

    [Fact]
    public void ValidateAddCollection_NullCollection_ShouldFailCollectionRequired()
    {
        // Arrange:
        var entity = new TestCollectionable();

        // Act:
        var result = CollectionableValidator.ValidateAddCollection(
            entity,
            (string)null!);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Collectionable.Collection.Required");
    }

    [Fact]
    public void ValidateAddCollection_EmptyGuid_ShouldFailCollectionEmpty()
    {
        // Arrange:
        var entity = new TestCollectionableGuid();

        // Act:
        var result = CollectionableValidator.ValidateAddCollection(
            entity,
            Guid.Empty);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Collectionable.Collection.Empty");
    }

    [Fact]
    public void ValidateAddCollection_BlankName_ShouldFailCollectionRequired()
    {
        // Arrange:
        var entity = new TestCollectionable();

        // Act:
        var result = CollectionableValidator.ValidateAddCollection(
            entity,
            "   ");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Collectionable.Collection.Required");
    }

    [Fact]
    public void ValidateAddCollection_TooLongName_ShouldFailCollectionTooLong()
    {
        // Arrange:
        var entity = new TestCollectionable();
        var longName = new string(
            'x',
            CollectionableConstant.Constraints.Collection.MaxNameLength + 1);

        // Act:
        var result = CollectionableValidator.ValidateAddCollection(
            entity,
            longName);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Collectionable.Collection.TooLong");
    }

    [Fact]
    public void ValidateAddCollection_Duplicate_ShouldFailCollectionDuplicate()
    {
        // Arrange:
        var entity = new TestCollectionable();
        entity.Collections.Add("summer-2026");

        // Act:
        var result = CollectionableValidator.ValidateAddCollection(
            entity,
            "summer-2026");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Collectionable.Collection.Duplicate");
    }

    [Fact]
    public void ValidateAddCollection_LimitExceeded_ShouldFailCollectionLimitExceeded()
    {
        // Arrange:
        var entity = new TestCollectionable();
        for (var i = 0; i < CollectionableConstant.Constraints.Collection.MaxCount; i++)
        {
            entity.Collections.Add($"collection-{i}");
        }

        // Act:
        var result = CollectionableValidator.ValidateAddCollection(
            entity,
            "one-more");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Collectionable.Collection.LimitExceeded");
    }

    [Fact]
    public void ValidateAddCollection_Valid_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestCollectionable();

        // Act:
        var result = CollectionableValidator.ValidateAddCollection(
            entity,
            "summer-2026");

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateRemoveCollection_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = CollectionableValidator.ValidateRemoveCollection(
            (TestCollectionable)null!,
            "summer-2026");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Collectionable.Entity.Required");
    }

    [Fact]
    public void ValidateRemoveCollection_NullCollection_ShouldFailCollectionRequired()
    {
        // Arrange:
        var entity = new TestCollectionable();

        // Act:
        var result = CollectionableValidator.ValidateRemoveCollection(
            entity,
            (string)null!);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Collectionable.Collection.Required");
    }

    [Fact]
    public void ValidateRemoveCollection_Missing_ShouldFailCollectionNotFound()
    {
        // Arrange:
        var entity = new TestCollectionable();
        entity.Collections.Add("summer-2026");

        // Act:
        var result = CollectionableValidator.ValidateRemoveCollection(
            entity,
            "winter-2026");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Collectionable.Collection.NotFound");
    }

    [Fact]
    public void ValidateRemoveCollection_Assigned_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestCollectionable();
        entity.Collections.Add("summer-2026");

        // Act:
        var result = CollectionableValidator.ValidateRemoveCollection(
            entity,
            "summer-2026");

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ClearCollections_AssignedCollections_ShouldRemoveAll()
    {
        // Arrange:
        var entity = new TestCollectionable();
        Success(entity).AddCollection("summer-2026");

        // Act:
        var result = Success(entity).ClearCollections<TestCollectionable, string>();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Collections.Should().BeEmpty();
    }

    #endregion

    #region Batch and Replace Specs

    [Fact]
    public void AddCollection_TrimmedInput_ShouldStoreTrimmed()
    {
        // Arrange:
        var entity = new TestCollectionable();

        // Act:
        var result = Success(entity).AddCollection("  summer-2026  ");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Collections.Should().ContainSingle().Which.Should().Be("summer-2026");
    }

    [Fact]
    public void AddCollection_NullCollection_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestCollectionable();

        // Act:
        var result = Success(entity).AddCollection((string)null!);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Collectionable.Collection.Required");
        entity.Collections.Should().BeEmpty();
    }

    [Fact]
    public void RemoveCollection_TrimmedInput_ShouldRemove()
    {
        // Arrange:
        var entity = new TestCollectionable();
        Success(entity).AddCollection("summer-2026");

        // Act:
        var result = Success(entity).RemoveCollection("  summer-2026  ");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Collections.Should().BeEmpty();
    }

    [Fact]
    public void AddCollections_AllValid_ShouldAddAll()
    {
        // Arrange:
        var entity = new TestCollectionable();

        // Act:
        var result = Success(entity).AddCollections<TestCollectionable, string>(new[] { "summer-2026", "winter-2026" });

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Collections.Count.Should().Be(2);
        entity.Collections.Should().Contain("summer-2026");
        entity.Collections.Should().Contain("winter-2026");
    }

    [Fact]
    public void AddCollections_DuplicateOfExisting_ShouldFailWithoutPartialMutation()
    {
        // Arrange:
        var entity = new TestCollectionable();
        Success(entity).AddCollection("summer-2026");

        // Act:
        var result = Success(entity).AddCollections<TestCollectionable, string>(new[] { "winter-2026", "summer-2026" });

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Collectionable.Collection.Duplicate");
        entity.Collections.Count.Should().Be(1);
        entity.Collections.Should().Contain("summer-2026");
        entity.Collections.Should().NotContain("winter-2026");
    }

    [Fact]
    public void AddCollections_NullItem_ShouldFailWithoutPartialMutation()
    {
        // Arrange:
        var entity = new TestCollectionable();

        // Act:
        var result = Success(entity).AddCollections<TestCollectionable, string>(new[] { "winter-2026", null! });

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Collectionable.Collection.Required");
        entity.Collections.Should().BeEmpty();
    }

    [Fact]
    public void AddCollections_SecondItemTooLong_ShouldFailWithoutPartialMutation()
    {
        // Arrange:
        var entity = new TestCollectionable();
        var longName = new string('x', CollectionableConstant.Constraints.Collection.MaxNameLength + 1);

        // Act:
        var result = Success(entity).AddCollections<TestCollectionable, string>(new[] { "ok", longName });

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Collectionable.Collection.TooLong");
        entity.Collections.Should().BeEmpty();
    }

    [Fact]
    public void AddCollections_ExceedingLimit_ShouldFailWithoutPartialMutation()
    {
        // Arrange:
        var entity = new TestCollectionable();
        for (var i = 0; i < CollectionableConstant.Constraints.Collection.MaxCount - 1; i++)
        {
            entity.Collections.Add($"collection-{i}");
        }

        // Act:
        var result = Success(entity).AddCollections<TestCollectionable, string>(new[] { "extra-a", "extra-b" });

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Collectionable.Collection.LimitExceeded");
        entity.Collections.Count.Should().Be(CollectionableConstant.Constraints.Collection.MaxCount - 1);
        entity.Collections.Should().NotContain("extra-a");
        entity.Collections.Should().NotContain("extra-b");
    }

    [Fact]
    public void SetCollections_Replace_ShouldReplaceAll()
    {
        // Arrange:
        var entity = new TestCollectionable();
        Success(entity).AddCollection("old");

        // Act:
        var result = Success(entity).SetCollections<TestCollectionable, string>(new[] { "a", "b" });

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Collections.Count.Should().Be(2);
        entity.Collections.Should().Contain("a");
        entity.Collections.Should().Contain("b");
        entity.Collections.Should().NotContain("old");
    }

    [Fact]
    public void SetCollections_Duplicates_ShouldDedup()
    {
        // Arrange:
        var entity = new TestCollectionable();

        // Act:
        var result = Success(entity).SetCollections<TestCollectionable, string>(new[] { "  a  ", "a", "b" });

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Collections.Count.Should().Be(2);
        entity.Collections.Should().Contain("a");
        entity.Collections.Should().Contain("b");
    }

    [Fact]
    public void SetCollections_ExceedingLimit_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestCollectionable();
        Success(entity).AddCollection("keep");
        var oversized = Enumerable.Range(0, CollectionableConstant.Constraints.Collection.MaxCount + 1)
            .Select(i => $"collection-{i}")
            .ToList();

        // Act:
        var result = Success(entity).SetCollections<TestCollectionable, string>(oversized);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Collectionable.Collection.LimitExceeded");
        entity.Collections.Should().ContainSingle().Which.Should().Be("keep");
    }

    [Fact]
    public void ClearCollections_Empty_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestCollectionable();

        // Act:
        var result = Success(entity).ClearCollections<TestCollectionable, string>();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Collections.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAddCollection_GuidValid_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestCollectionableGuid();

        // Act:
        var result = CollectionableValidator.ValidateAddCollection(entity, Guid.NewGuid());

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void AddCollection_GuidValid_ShouldAdd()
    {
        // Arrange:
        var entity = new TestCollectionableGuid();
        var id = Guid.NewGuid();

        // Act:
        var result = Result<TestCollectionableGuid>.Success(entity).AddCollection(id);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Collections.Should().ContainSingle().Which.Should().Be(id);
    }

    [Fact]
    public void FailureCodes_ShouldHaveExpectedCodes()
    {
        // Arrange:
        // Act:
        // Assert:
        CollectionableResult.Failure.EntityRequired.Code.Should().Be("Collectionable.Entity.Required");
        CollectionableResult.Failure.CollectionRequired.Code.Should().Be("Collectionable.Collection.Required");
        CollectionableResult.Failure.CollectionEmpty.Code.Should().Be("Collectionable.Collection.Empty");
        CollectionableResult.Failure.CollectionTooLong.Code.Should().Be("Collectionable.Collection.TooLong");
        CollectionableResult.Failure.CollectionLimitExceeded.Code.Should().Be("Collectionable.Collection.LimitExceeded");
        CollectionableResult.Failure.CollectionDuplicate.Code.Should().Be("Collectionable.Collection.Duplicate");
        CollectionableResult.Failure.CollectionNotFound.Code.Should().Be("Collectionable.Collection.NotFound");
    }

    #endregion

    #region Constant Specs

    [Fact]
    public void Constants_ShouldMatchNestedConstraints()
    {
        // Arrange:
        // Act:
        // Assert:
        CollectionableConstant.Constraints.Collection.MinNameLength.Should().Be(1);
        CollectionableConstant.Constraints.Collection.MaxNameLength.Should().Be(128);
        CollectionableConstant.Constraints.Collection.MaxCount.Should().Be(32);
        CollectionableConstant.Defaults.TrimValues.Should().BeTrue();
    }

    #endregion
}