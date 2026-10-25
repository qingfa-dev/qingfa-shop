using Core.Common.Concerns.Foundation.Versionable;

namespace Core.Tests.Common.Concerns.Foundation.Versionable;

/// <summary>Specifications for <see cref="VersionableExtensions"/> and <see cref="VersionableValidator"/>.</summary>
public class VersionableExtensionSpec
{
    #region Helper Methods

    private sealed class TestVersionable : IVersionable
    {
        public long Version { get; set; }
    }

    /// <summary>Creates a successful result wrapping the given entity.</summary>
    /// <param name="entity">The entity to wrap.</param>
    /// <returns>A successful result containing the entity.</returns>
    private static Result<TestVersionable> Success(TestVersionable entity)
    {
        return Result<TestVersionable>.Success(entity);
    }

    #endregion

    #region Test Cases

    [Fact]
    public void BumpVersion_ZeroVersion_ShouldIncrementToOne()
    {
        // Arrange:
        var entity = new TestVersionable();

        // Act:
        var result = Success(entity).BumpVersion();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Version.Should().Be(1);
    }

    [Fact]
    public void BumpVersion_NegativeVersion_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestVersionable { Version = -1 };

        // Act:
        var result = Success(entity).BumpVersion();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Versionable.Version.Negative");
        entity.Version.Should().Be(-1);
    }

    [Fact]
    public void BumpVersion_MaxVersion_ShouldFailOverflowWithoutMutation()
    {
        // Arrange:
        var entity = new TestVersionable { Version = long.MaxValue };

        // Act:
        var result = Success(entity).BumpVersion();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Versionable.Version.Overflow");
        entity.Version.Should().Be(long.MaxValue);
    }

    [Fact]
    public void ValidateBump_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = VersionableValidator.ValidateBump(
            (TestVersionable)null!);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Versionable.Entity.Required");
    }

    #endregion

    #region Validator Specs

    [Fact]
    public void ValidateBump_NegativeVersion_ShouldFailNegative()
    {
        // Arrange:
        var entity = new TestVersionable { Version = -5 };

        // Act:
        var result = VersionableValidator.ValidateBump(entity);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Versionable.Version.Negative");
    }

    [Fact]
    public void ValidateBump_MaxVersion_ShouldFailOverflow()
    {
        // Arrange:
        var entity = new TestVersionable { Version = VersionableConstant.Constraints.Version.Max };

        // Act:
        var result = VersionableValidator.ValidateBump(entity);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Versionable.Version.Overflow");
    }

    [Fact]
    public void ValidateBump_ValidVersion_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestVersionable { Version = 0 };

        // Act:
        var result = VersionableValidator.ValidateBump(entity);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void ValidateVersion_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = VersionableValidator.ValidateVersion((TestVersionable)null!, 0);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Versionable.Entity.Required");
    }

    [Fact]
    public void ValidateVersion_NegativeVersion_ShouldFailOutOfRange()
    {
        // Arrange:
        var entity = new TestVersionable();

        // Act:
        var result = VersionableValidator.ValidateVersion(entity, -1);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Versionable.Version.OutOfRange");
    }

    [Fact]
    public void ValidateVersion_MinVersion_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestVersionable();

        // Act:
        var result = VersionableValidator.ValidateVersion(entity, VersionableConstant.Constraints.Version.Min);

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateVersion_MaxVersion_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestVersionable();

        // Act:
        var result = VersionableValidator.ValidateVersion(entity, VersionableConstant.Constraints.Version.Max);

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateConcurrency_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = VersionableValidator.ValidateConcurrency((TestVersionable)null!, 0);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Versionable.Entity.Required");
    }

    [Fact]
    public void ValidateConcurrency_Mismatch_ShouldFailConcurrencyConflict()
    {
        // Arrange:
        var entity = new TestVersionable { Version = 3 };

        // Act:
        var result = VersionableValidator.ValidateConcurrency(entity, 2);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Versionable.Version.ConcurrencyConflict");
    }

    [Fact]
    public void ValidateConcurrency_Match_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestVersionable { Version = 3 };

        // Act:
        var result = VersionableValidator.ValidateConcurrency(entity, 3);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void EnsureVersionMatch_Mismatch_ShouldFailConcurrencyConflict()
    {
        // Arrange:
        var entity = new TestVersionable { Version = 5 };

        // Act:
        var result = Success(entity).EnsureVersionMatch(4);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Versionable.Version.ConcurrencyConflict");
    }

    [Fact]
    public void EnsureVersionMatch_Match_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestVersionable { Version = 5 };

        // Act:
        var result = Success(entity).EnsureVersionMatch(5);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void InitializeVersion_ShouldResetToInitial()
    {
        // Arrange:
        var entity = new TestVersionable { Version = 42 };

        // Act:
        var result = Success(entity).InitializeVersion();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Version.Should().Be(VersionableConstant.Defaults.InitialVersion);
    }

    [Fact]
    public void EnsureVersioned_ValidVersion_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestVersionable { Version = 7 };

        // Act:
        var result = Success(entity).EnsureVersioned();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void EnsureVersioned_NegativeVersion_ShouldFailOutOfRange()
    {
        // Arrange:
        var entity = new TestVersionable { Version = -1 };

        // Act:
        var result = Success(entity).EnsureVersioned();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Versionable.Version.OutOfRange");
    }

    [Fact]
    public void BumpVersion_AtMaxMinusOne_ShouldSucceedToMax()
    {
        // Arrange:
        var entity = new TestVersionable { Version = VersionableConstant.Constraints.Version.Max - VersionableConstant.Defaults.Increment };

        // Act:
        var result = Success(entity).BumpVersion();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Version.Should().Be(VersionableConstant.Constraints.Version.Max);
    }

    [Fact]
    public void BumpVersion_InputFailure_ShouldPropagateWithoutMutation()
    {
        // Arrange:
        var entity = new TestVersionable { Version = 5 };
        var input = Result<TestVersionable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        var result = input.BumpVersion();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Test.Fail");
        entity.Version.Should().Be(5);
    }

    [Fact]
    public void EnsureVersioned_MaxVersion_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestVersionable { Version = VersionableConstant.Constraints.Version.Max };

        // Act:
        var result = Success(entity).EnsureVersioned();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void EnsureVersioned_MinVersion_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestVersionable { Version = VersionableConstant.Constraints.Version.Min };

        // Act:
        var result = Success(entity).EnsureVersioned();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void EnsureVersioned_InputFailure_ShouldPropagateErrors()
    {
        // Arrange:
        var input = Result<TestVersionable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        var result = input.EnsureVersioned();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Test.Fail");
    }

    [Fact]
    public void EnsureVersionMatch_InputFailure_ShouldPropagateErrors()
    {
        // Arrange:
        var input = Result<TestVersionable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        var result = input.EnsureVersionMatch(5);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Test.Fail");
    }

    [Fact]
    public void InitializeVersion_AlreadyInitial_ShouldStayInitial()
    {
        // Arrange:
        var entity = new TestVersionable { Version = VersionableConstant.Defaults.InitialVersion };

        // Act:
        var result = Success(entity).InitializeVersion();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Version.Should().Be(VersionableConstant.Defaults.InitialVersion);
    }

    [Fact]
    public void InitializeVersion_InputFailure_ShouldPropagateErrors()
    {
        // Arrange:
        var input = Result<TestVersionable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        var result = input.InitializeVersion();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Test.Fail");
    }

    [Fact]
    public void RequireVersioned_ValidVersion_ShouldReturnEntity()
    {
        // Arrange:
        var entity = new TestVersionable { Version = 7 };

        // Act:
        var value = Success(entity).RequireVersioned();

        // Assert:
        value.Should().BeSameAs(entity);
    }

    [Fact]
    public void RequireVersioned_NegativeVersion_ShouldThrow()
    {
        // Arrange:
        var entity = new TestVersionable { Version = -1 };

        // Act:
        // Assert:
        FluentActions.Invoking(() => Success(entity).RequireVersioned()).Should().Throw<ResultException>();
    }

    [Fact]
    public void RequireVersioned_InputFailure_ShouldThrow()
    {
        // Arrange:
        var input = Result<TestVersionable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        // Assert:
        FluentActions.Invoking(() => input.RequireVersioned()).Should().Throw<ResultException>();
    }

    [Fact]
    public void ValidateVersion_TooLarge_ShouldFailOutOfRange()
    {
        // Arrange:
        var entity = new TestVersionable();
        var tooLarge = VersionableConstant.Constraints.Version.Max;

        // Act:
        var validAtMax = VersionableValidator.ValidateVersion(entity, tooLarge);
        var invalidBelowMin = VersionableValidator.ValidateVersion(entity, VersionableConstant.Constraints.Version.Min - 1);

        // Assert:
        validAtMax.IsSuccess.Should().BeTrue();
        invalidBelowMin.IsFailure.Should().BeTrue();
        invalidBelowMin.Errors![0].Code.Should().Be("Versionable.Version.OutOfRange");
    }

    [Fact]
    public void ValidateBump_AtMaxMinusOne_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestVersionable { Version = VersionableConstant.Constraints.Version.Max - VersionableConstant.Defaults.Increment };

        // Act:
        var result = VersionableValidator.ValidateBump(entity);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void Constants_ShouldMatchNestedDefinitions()
    {
        // Arrange:
        // Act:
        // Assert:
        VersionableConstant.Constraints.Version.Min.Should().Be(0L);
        VersionableConstant.Constraints.Version.Max.Should().Be(long.MaxValue);
        VersionableConstant.Constraints.MinVersion.Should().Be(VersionableConstant.Constraints.Version.Min);
        VersionableConstant.Constraints.MaxVersion.Should().Be(VersionableConstant.Constraints.Version.Max);
        VersionableConstant.Defaults.InitialVersion.Should().Be(0L);
        VersionableConstant.Defaults.Increment.Should().Be(1L);
    }

    #endregion
}