using Core.Common.Concerns.Lifecycle;
using Core.Common.Concerns.Lifecycle.Creatable;
using Core.Common.Concerns.Lifecycle.Modifiable;

namespace Core.Tests.Common.Concerns.Lifecycle.Modifiable;

/// <summary>Specification tests for <see cref="ModifiableExtensions"/>.</summary>
public class ModifiableExtensionSpec
{
    private static readonly DateTimeOffset NowUtc =
        new(2024, 5, 1, 12, 0, 0, TimeSpan.Zero);

    #region Helper Methods

    /// <summary>Creates a successful result wrapping the given entity.</summary>
    private static Result<TestModifiable> Success(TestModifiable entity)
    {
        return Result<TestModifiable>.Success(entity);
    }

    private sealed class TestModifiable : ICreatable, IModifiable
    {
        public DateTimeOffset CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAtUtc { get; set; }
        public string? ModifiedBy { get; set; }
    }

    private static TestModifiable InitializedEntity(
        string? createdBy = "alice",
        DateTimeOffset? createdAtUtc = null)
    {
        return new TestModifiable
        {
            CreatedAtUtc = createdAtUtc ?? NowUtc,
            CreatedBy = createdBy
        };
    }

    #endregion

    #region Test Cases

    #region MarkModified

    [Fact]
    public void MarkModified_InitializedEntity_ShouldSetModifiedFields()
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act:
        var result = Success(entity).MarkModified(NowUtc.AddHours(1), "carol");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.ModifiedAtUtc.Should().Be(NowUtc.AddHours(1));
        entity.ModifiedBy.Should().Be("carol");
        entity.CreatedAtUtc.Should().Be(NowUtc);
        entity.CreatedBy.Should().Be("alice");
    }

    [Fact]
    public void MarkModified_OffsetTimestamp_ShouldStoreUtcInstant()
    {
        // Arrange:
        var entity = InitializedEntity();
        var offset = new DateTimeOffset(2024, 5, 1, 20, 0, 0, TimeSpan.FromHours(8));

        // Act:
        var result = Success(entity).MarkModified(offset, "carol");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.ModifiedAtUtc!.Value.Offset.Should().Be(TimeSpan.Zero);
        entity.ModifiedAtUtc.Should().Be(offset.ToUniversalTime());
    }

    [Fact]
    public void MarkModified_NullActor_ShouldClearModifiedBy()
    {
        // Arrange:
        var entity = InitializedEntity();
        entity.ModifiedBy = "stale";

        // Act:
        var result = Success(entity).MarkModified(NowUtc.AddHours(1));

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.ModifiedBy.Should().BeNull();
    }

    [Fact]
    public void MarkModified_DefaultNowUtc_ShouldFailModifiedAtRequiredWithoutMutation()
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act:
        var result = Success(entity).MarkModified(default, "carol");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Modifiable.ModifiedAt.Required");
        entity.ModifiedAtUtc.Should().BeNull();
    }

    [Fact]
    public void MarkModified_UninitializedEntity_ShouldFailNotInitializedWithoutMutation()
    {
        // Arrange:
        var entity = new TestModifiable();

        // Act:
        var result = Success(entity).MarkModified(NowUtc, "carol");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Modifiable.NotInitialized");
        entity.ModifiedAtUtc.Should().BeNull();
    }

    [Fact]
    public void MarkModified_BeforeCreatedAt_ShouldFailModifiedAtBeforeCreatedAt()
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act:
        var result = Success(entity).MarkModified(NowUtc.AddMinutes(-1), "carol");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Modifiable.ModifiedAt.InvalidRange");
        entity.ModifiedAtUtc.Should().BeNull();
    }

    [Fact]
    public void MarkModified_RequireModifiedByWithoutActor_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act:
        var result = Success(entity).MarkModified(
            NowUtc.AddHours(1),
            actor: null,
            requireModifiedBy: true);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Modifiable.ModifiedBy.Required");
        entity.ModifiedAtUtc.Should().BeNull();
    }

    [Fact]
    public void MarkModified_ActorTooLong_ShouldFailModifiedByTooLongWithoutMutation()
    {
        // Arrange:
        var entity = InitializedEntity();
        var actor = new string('a', LifecycleConstant.Constraints.MaxActorLength + 1);

        // Act:
        var result = Success(entity).MarkModified(NowUtc.AddHours(1), actor);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Modifiable.ModifiedBy.TooLong");
        entity.ModifiedAtUtc.Should().BeNull();
    }

    [Fact]
    public void MarkModified_InputFailure_ShouldPropagateErrors()
    {
        // Arrange:
        var input = Result<TestModifiable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        var result = input.MarkModified(NowUtc, "carol");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Test.Fail");
    }

    #endregion

    #region Validator Specs

    [Fact]
    public void ValidateModification_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange + Act:
        var result = ModifiableValidator.ValidateModification<TestModifiable>(
            null!,
            NowUtc,
            "carol");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Modifiable.Entity.Required");
    }

    [Fact]
    public void ValidateModification_DefaultTimestamp_ShouldFailModifiedAtRequired()
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act:
        var result = ModifiableValidator.ValidateModification(
            entity,
            default,
            "carol");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Modifiable.ModifiedAt.Required");
    }

    [Fact]
    public void ValidateModification_FutureTimestampBeyondSkew_ShouldFailModifiedAtInFuture()
    {
        // Arrange:
        var entity = InitializedEntity(
            createdAtUtc: DateTimeOffset.UtcNow);
        var future = DateTimeOffset.UtcNow.AddSeconds(
            LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds + 60);

        // Act:
        var result = ModifiableValidator.ValidateModification(
            entity,
            future,
            "carol");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Modifiable.ModifiedAt.InFuture");
    }

    [Fact]
    public void ValidateModification_UninitializedEntity_ShouldFailNotInitialized()
    {
        // Arrange:
        var entity = new TestModifiable();

        // Act:
        var result = ModifiableValidator.ValidateModification(
            entity,
            NowUtc,
            "carol");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Modifiable.NotInitialized");
    }

    [Fact]
    public void ValidateModification_BeforeCreatedAt_ShouldFailModifiedAtInvalidRange()
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act:
        var result = ModifiableValidator.ValidateModification(
            entity,
            NowUtc.AddMinutes(-1),
            "carol");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Modifiable.ModifiedAt.InvalidRange");
    }

    [Fact]
    public void ValidateModification_ActorTooLong_ShouldFailModifiedByTooLong()
    {
        // Arrange:
        var entity = InitializedEntity();
        var actor = new string('a', LifecycleConstant.Constraints.Actor.MaxLength + 1);

        // Act:
        var result = ModifiableValidator.ValidateModification(
            entity,
            NowUtc.AddHours(1),
            actor);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Modifiable.ModifiedBy.TooLong");
    }

    [Fact]
    public void ValidateModification_RequireModifiedByWithoutActor_ShouldFailModifiedByRequired()
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act:
        var result = ModifiableValidator.ValidateModification(
            entity,
            NowUtc.AddHours(1),
            actor: null,
            requireModifiedBy: true);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Modifiable.ModifiedBy.Required");
    }

    [Fact]
    public void ValidateModification_UntrimmedActor_ShouldFailActorInvalid()
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act:
        var result = ModifiableValidator.ValidateModification(
            entity,
            NowUtc.AddHours(1),
            " carol ");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Modifiable.Actor.Invalid");
    }

    [Fact]
    public void ValidateModification_ActorTooShort_ShouldFailModifiedByTooShort()
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act:
        var result = ModifiableValidator.ValidateModification(
            entity,
            NowUtc.AddHours(1),
            "x");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Modifiable.ModifiedBy.TooShort");
    }

    [Fact]
    public void ValidateModification_ValidInput_ShouldSucceed()
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act:
        var result = ModifiableValidator.ValidateModification(
            entity,
            NowUtc.AddHours(1),
            "carol");

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Branch Coverage

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MarkModified_RequireMatrix_ValidActor_ShouldSucceed(bool requireModifiedBy)
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act:
        var result = Success(entity).MarkModified(NowUtc.AddHours(1), "carol", requireModifiedBy);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.ModifiedAtUtc.Should().Be(NowUtc.AddHours(1));
        entity.ModifiedBy.Should().Be("carol");
    }

    [Theory]
    [InlineData(false, "Modifiable.Actor.Invalid")]
    [InlineData(true, "Modifiable.ModifiedBy.Required")]
    public void MarkModified_WhitespaceActor_RequireMatrix_ShouldFailWithExpectedCode(
        bool requireModifiedBy,
        string expectedCode)
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act:
        var result = Success(entity).MarkModified(NowUtc.AddHours(1), " ", requireModifiedBy);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(expectedCode);
        entity.ModifiedAtUtc.Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MarkModified_NullActor_RequireMatrix_ShouldMatchExpectation(bool requireModifiedBy)
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act:
        var result = Success(entity).MarkModified(
            NowUtc.AddHours(1),
            actor: null,
            requireModifiedBy: requireModifiedBy);

        // Assert:
        if (requireModifiedBy)
        {
            result.IsFailure.Should().BeTrue();
            result.Errors![0].Code.Should().Be("Modifiable.ModifiedBy.Required");
            entity.ModifiedAtUtc.Should().BeNull();
        }
        else
        {
            result.IsSuccess.Should().BeTrue();
            entity.ModifiedAtUtc.Should().Be(NowUtc.AddHours(1));
            entity.ModifiedBy.Should().BeNull();
        }
    }

    [Fact]
    public void MarkModified_EqualToCreatedAt_ShouldSucceed()
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act: nowUtc exactly equal to CreatedAtUtc satisfies nowUtc >= CreatedAtUtc.
        var result = Success(entity).MarkModified(NowUtc, "carol");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.ModifiedAtUtc.Should().Be(NowUtc);
        entity.ModifiedBy.Should().Be("carol");
    }

    [Fact]
    public void MarkModified_OneTickBeforeCreatedAt_ShouldFailInvalidRangeWithoutMutation()
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act:
        var result = Success(entity).MarkModified(NowUtc.AddTicks(-1), "carol");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Modifiable.ModifiedAt.InvalidRange");
        entity.ModifiedAtUtc.Should().BeNull();
        entity.ModifiedBy.Should().BeNull();
    }

    [Fact]
    public void MarkModified_ActorTooShort_ShouldFailModifiedByTooShortWithoutMutation()
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act:
        var result = Success(entity).MarkModified(NowUtc.AddHours(1), "x");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Modifiable.ModifiedBy.TooShort");
        entity.ModifiedAtUtc.Should().BeNull();
    }

    [Fact]
    public void MarkModified_UntrimmedActor_ShouldFailActorInvalidWithoutMutation()
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act:
        var result = Success(entity).MarkModified(NowUtc.AddHours(1), " carol ");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Modifiable.Actor.Invalid");
        entity.ModifiedAtUtc.Should().BeNull();
    }

    [Fact]
    public void MarkModified_ActorAtMaxLength_ShouldSucceed()
    {
        // Arrange:
        var entity = InitializedEntity();
        var actor = new string('a', LifecycleConstant.Constraints.Actor.MaxLength);

        // Act:
        var result = Success(entity).MarkModified(NowUtc.AddHours(1), actor);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.ModifiedBy.Should().Be(actor);
    }

    [Fact]
    public void MarkModified_NegativeOffsetTimestamp_ShouldStoreUtcInstant()
    {
        // Arrange:
        var entity = InitializedEntity(
            createdAtUtc: new DateTimeOffset(2024, 5, 1, 7, 0, 0, TimeSpan.FromHours(-5)));
        var offset = new DateTimeOffset(2024, 5, 1, 8, 0, 0, TimeSpan.FromHours(-5));

        // Act:
        var result = Success(entity).MarkModified(offset, "carol");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.ModifiedAtUtc!.Value.Offset.Should().Be(TimeSpan.Zero);
        entity.ModifiedAtUtc.Should().Be(offset.ToUniversalTime());
    }

    [Fact]
    public void MarkModified_AlreadyUtcTimestamp_ShouldKeepZeroOffset()
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act:
        var result = Success(entity).MarkModified(NowUtc.AddHours(1), "carol");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.ModifiedAtUtc!.Value.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void MarkModified_NearSkewBoundary_ShouldSucceed()
    {
        // Arrange:
        var now = DateTimeOffset.UtcNow;
        var entity = InitializedEntity(createdAtUtc: now);
        var nearBoundary = now.AddSeconds(
            LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds - 5);

        // Act:
        var result = Success(entity).MarkModified(nearBoundary, "carol");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.ModifiedAtUtc.Should().Be(nearBoundary.ToUniversalTime());
    }

    [Fact]
    public void MarkModified_JustOverSkewBoundary_ShouldFailInFutureWithoutMutation()
    {
        // Arrange:
        var now = DateTimeOffset.UtcNow;
        var entity = InitializedEntity(createdAtUtc: now);
        var justOver = now.AddSeconds(
            LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds + 5);

        // Act:
        var result = Success(entity).MarkModified(justOver, "carol");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Modifiable.ModifiedAt.InFuture");
        entity.ModifiedAtUtc.Should().BeNull();
    }

    [Fact]
    public void ValidateModification_NullActorWithDefaultRequire_ShouldSucceed()
    {
        // Arrange:
        var entity = InitializedEntity();

        // Act:
        var result = ModifiableValidator.ValidateModification(
            entity,
            NowUtc.AddHours(1),
            actor: null);

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Constants

    [Fact]
    public void LifecycleConstant_ShouldExposeExpectedValues()
    {
        // Assert:
        LifecycleConstant.Constraints.Actor.MinLength.Should().Be(2);
        LifecycleConstant.Constraints.Actor.MaxLength.Should().Be(256);
        LifecycleConstant.Constraints.MaxActorLength.Should().Be(256);
        LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds.Should().Be(300);
        LifecycleConstant.Defaults.RequireActor.Should().BeFalse();
        LifecycleConstant.Patterns.Actor.Should().Be(@"^\S(?:.*\S)?$");
    }

    #endregion

    #endregion
}