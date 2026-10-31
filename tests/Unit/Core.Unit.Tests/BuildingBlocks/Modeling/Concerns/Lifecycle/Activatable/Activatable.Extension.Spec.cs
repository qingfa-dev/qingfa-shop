using Core.BuildingBlocks.Modeling.Concerns.Lifecycle;
using Core.BuildingBlocks.Modeling.Concerns.Lifecycle.Activatable;

namespace Core.Unit.Tests.BuildingBlocks.Modeling.Concerns.Lifecycle.Activatable;

/// <summary>Specification tests for <see cref="ActivatableExtensions"/>.</summary>
public class ActivatableExtensionSpec
{
    private static readonly DateTimeOffset NowUtc =
        new(2024, 5, 1, 12, 0, 0, TimeSpan.Zero);

    #region Helper Methods

    /// <summary>Creates a successful result wrapping the given entity.</summary>
    private static Result<TestActivatable> Success(TestActivatable entity)
    {
        return Result<TestActivatable>.Success(entity);
    }

    private sealed class TestActivatable : IActivatable
    {
        public bool IsActive { get; set; }
        public DateTimeOffset? ActivatedAtUtc { get; set; }
        public string? ActivatedBy { get; set; }
    }

    private static TestActivatable ActiveEntity(
        string? activatedBy = "bob",
        DateTimeOffset? activatedAtUtc = null)
    {
        return new TestActivatable
        {
            IsActive = true,
            ActivatedAtUtc = activatedAtUtc ?? NowUtc,
            ActivatedBy = activatedBy
        };
    }

    #endregion

    #region Test Cases

    #region Activate

    [Fact]
    public void Activate_InactiveEntity_ShouldSetActivationFields()
    {
        // Arrange:
        var entity = new TestActivatable();

        // Act:
        var result = Success(entity).Activate(NowUtc, "alice");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.IsActive.Should().BeTrue();
        entity.ActivatedAtUtc.Should().Be(NowUtc);
        entity.ActivatedBy.Should().Be("alice");
    }

    [Fact]
    public void Activate_OffsetTimestamp_ShouldStoreUtcInstant()
    {
        // Arrange:
        var entity = new TestActivatable();
        var offset = new DateTimeOffset(2024, 5, 1, 20, 0, 0, TimeSpan.FromHours(8));

        // Act:
        var result = Success(entity).Activate(offset, "alice");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.ActivatedAtUtc!.Value.Offset.Should().Be(TimeSpan.Zero);
        entity.ActivatedAtUtc.Should().Be(offset.ToUniversalTime());
    }

    [Fact]
    public void Activate_DefaultNowUtc_ShouldFailActivatedAtRequiredWithoutMutation()
    {
        // Arrange:
        var entity = new TestActivatable();

        // Act:
        var result = Success(entity).Activate(default, "alice");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.ActivatedAt.Required");
        entity.IsActive.Should().BeFalse();
        entity.ActivatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Activate_AlreadyActive_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = ActiveEntity();

        // Act:
        var result = Success(entity).Activate(NowUtc, "alice");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.AlreadyActive");
        entity.ActivatedAtUtc.Should().Be(NowUtc);
        entity.ActivatedBy.Should().Be("bob");
    }

    [Fact]
    public void Activate_ActorTooLong_ShouldFailActivatedByTooLongWithoutMutation()
    {
        // Arrange:
        var entity = new TestActivatable();
        var actor = new string('a', LifecycleConstant.Constraints.MaxActorLength + 1);

        // Act:
        var result = Success(entity).Activate(NowUtc, actor);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.ActivatedBy.TooLong");
        entity.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_UntrimmedActor_ShouldFailActorInvalid()
    {
        // Arrange:
        var entity = new TestActivatable();

        // Act:
        var result = Success(entity).Activate(NowUtc, " alice ");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.Actor.Invalid");
        entity.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_EmptyActor_ShouldFailActorInvalid()
    {
        // Arrange:
        var entity = new TestActivatable();

        // Act:
        var result = Success(entity).Activate(NowUtc, string.Empty);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.Actor.Invalid");
    }

    #endregion

    #region Deactivate

    [Fact]
    public void Deactivate_ActiveEntity_ShouldClearActivationFields()
    {
        // Arrange:
        var entity = ActiveEntity();

        // Act:
        var result = Success(entity).Deactivate("carol");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.IsActive.Should().BeFalse();
        entity.ActivatedAtUtc.Should().BeNull();
        entity.ActivatedBy.Should().BeNull();
    }

    [Fact]
    public void Deactivate_NotActive_ShouldFailNotActiveWithoutMutation()
    {
        // Arrange:
        var entity = new TestActivatable();

        // Act:
        var result = Success(entity).Deactivate("carol");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.NotActive");
        entity.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_RequireActorWithoutActor_ShouldFailActivatedByRequiredWithoutMutation()
    {
        // Arrange:
        var entity = ActiveEntity();

        // Act:
        var result = Success(entity).Deactivate(
            actor: null,
            requireActor: true);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.ActivatedBy.Required");
        entity.IsActive.Should().BeTrue();
        entity.ActivatedAtUtc.Should().Be(NowUtc);
    }

    #endregion

    #region Validator Specs

    [Fact]
    public void ValidateActivation_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange + Act:
        var result = ActivatableValidator.ValidateActivation<TestActivatable>(
            null!,
            NowUtc,
            "alice");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.Entity.Required");
    }

    [Fact]
    public void ValidateActivation_DefaultTimestamp_ShouldFailActivatedAtRequired()
    {
        // Arrange:
        var entity = new TestActivatable();

        // Act:
        var result = ActivatableValidator.ValidateActivation(
            entity,
            default,
            "alice");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.ActivatedAt.Required");
    }

    [Fact]
    public void ValidateActivation_FutureTimestampBeyondSkew_ShouldFailActivatedAtInFuture()
    {
        // Arrange:
        var entity = new TestActivatable();
        var future = DateTimeOffset.UtcNow.AddSeconds(
            LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds + 60);

        // Act:
        var result = ActivatableValidator.ValidateActivation(
            entity,
            future,
            "alice");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.ActivatedAt.InFuture");
    }

    [Fact]
    public void ValidateActivation_AlreadyActive_ShouldFailAlreadyActive()
    {
        // Arrange:
        var entity = ActiveEntity();

        // Act:
        var result = ActivatableValidator.ValidateActivation(
            entity,
            NowUtc,
            "alice");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.AlreadyActive");
    }

    [Fact]
    public void ValidateActivation_ActorTooLong_ShouldFailActivatedByTooLong()
    {
        // Arrange:
        var entity = new TestActivatable();
        var actor = new string('a', LifecycleConstant.Constraints.Actor.MaxLength + 1);

        // Act:
        var result = ActivatableValidator.ValidateActivation(
            entity,
            NowUtc,
            actor);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.ActivatedBy.TooLong");
    }

    [Fact]
    public void ValidateActivation_RequireActorWithoutActor_ShouldFailActivatedByRequired()
    {
        // Arrange:
        var entity = new TestActivatable();

        // Act:
        var result = ActivatableValidator.ValidateActivation(
            entity,
            NowUtc,
            actor: null,
            requireActor: true);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.ActivatedBy.Required");
    }

    [Fact]
    public void ValidateActivation_UntrimmedActor_ShouldFailActorInvalid()
    {
        // Arrange:
        var entity = new TestActivatable();

        // Act:
        var result = ActivatableValidator.ValidateActivation(
            entity,
            NowUtc,
            " alice ");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.Actor.Invalid");
    }

    [Fact]
    public void ValidateActivation_ActorTooShort_ShouldFailActivatedByTooShort()
    {
        // Arrange:
        var entity = new TestActivatable();

        // Act:
        var result = ActivatableValidator.ValidateActivation(
            entity,
            NowUtc,
            "x");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.ActivatedBy.TooShort");
    }

    [Fact]
    public void ValidateActivation_ValidInput_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestActivatable();

        // Act:
        var result = ActivatableValidator.ValidateActivation(
            entity,
            NowUtc,
            "alice");

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateDeactivation_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange + Act:
        var result = ActivatableValidator.ValidateDeactivation<TestActivatable>(
            null!,
            "carol");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.Entity.Required");
    }

    [Fact]
    public void ValidateDeactivation_NotActive_ShouldFailNotActive()
    {
        // Arrange:
        var entity = new TestActivatable();

        // Act:
        var result = ActivatableValidator.ValidateDeactivation(
            entity,
            "carol");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.NotActive");
    }

    [Fact]
    public void ValidateDeactivation_ActorTooLong_ShouldFailActivatedByTooLong()
    {
        // Arrange:
        var entity = ActiveEntity();
        var actor = new string('a', LifecycleConstant.Constraints.Actor.MaxLength + 1);

        // Act:
        var result = ActivatableValidator.ValidateDeactivation(
            entity,
            actor);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.ActivatedBy.TooLong");
    }

    [Fact]
    public void ValidateDeactivation_RequireActorWithoutActor_ShouldFailActivatedByRequired()
    {
        // Arrange:
        var entity = ActiveEntity();

        // Act:
        var result = ActivatableValidator.ValidateDeactivation(
            entity,
            actor: null,
            requireActor: true);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.ActivatedBy.Required");
    }

    [Fact]
    public void ValidateDeactivation_UntrimmedActor_ShouldFailActorInvalid()
    {
        // Arrange:
        var entity = ActiveEntity();

        // Act:
        var result = ActivatableValidator.ValidateDeactivation(
            entity,
            "carol ");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.Actor.Invalid");
    }

    [Fact]
    public void ValidateDeactivation_ActorTooShort_ShouldFailActivatedByTooShort()
    {
        // Arrange:
        var entity = ActiveEntity();

        // Act:
        var result = ActivatableValidator.ValidateDeactivation(
            entity,
            "x");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.ActivatedBy.TooShort");
    }

    [Fact]
    public void ValidateDeactivation_ValidInput_ShouldSucceed()
    {
        // Arrange:
        var entity = ActiveEntity();

        // Act:
        var result = ActivatableValidator.ValidateDeactivation(
            entity,
            "carol");

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Branch Coverage

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Activate_RequireMatrix_ValidActor_ShouldSucceed(bool requireActor)
    {
        // Arrange:
        var entity = new TestActivatable();

        // Act:
        var result = Success(entity).Activate(NowUtc, "alice", requireActor);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.IsActive.Should().BeTrue();
        entity.ActivatedAtUtc.Should().Be(NowUtc);
        entity.ActivatedBy.Should().Be("alice");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Deactivate_RequireMatrix_ValidActor_ShouldSucceed(bool requireActor)
    {
        // Arrange:
        var entity = ActiveEntity();

        // Act:
        var result = Success(entity).Deactivate("carol", requireActor);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.IsActive.Should().BeFalse();
        entity.ActivatedAtUtc.Should().BeNull();
        entity.ActivatedBy.Should().BeNull();
    }

    [Fact]
    public void Activate_NullActorWithDefaultRequire_ShouldSucceedAndStoreNull()
    {
        // Arrange:
        var entity = new TestActivatable();

        // Act: omit requireActor to exercise LifecycleConstant.Defaults.RequireActor path.
        var result = Success(entity).Activate(NowUtc, actor: null);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.IsActive.Should().BeTrue();
        entity.ActivatedAtUtc.Should().Be(NowUtc);
        entity.ActivatedBy.Should().BeNull();
    }

    [Fact]
    public void Deactivate_NullActorWithDefaultRequire_ShouldClearAndSucceed()
    {
        // Arrange:
        var entity = ActiveEntity();

        // Act: omit requireActor to exercise LifecycleConstant.Defaults.RequireActor path.
        var result = Success(entity).Deactivate(actor: null);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.IsActive.Should().BeFalse();
        entity.ActivatedAtUtc.Should().BeNull();
        entity.ActivatedBy.Should().BeNull();
    }

    [Theory]
    [InlineData(false, "Activatable.Actor.Invalid")]
    [InlineData(true, "Activatable.ActivatedBy.Required")]
    public void Activate_WhitespaceActor_RequireMatrix_ShouldFailWithExpectedCode(
        bool requireActor,
        string expectedCode)
    {
        // Arrange:
        var entity = new TestActivatable();

        // Act:
        var result = Success(entity).Activate(NowUtc, " ", requireActor);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(expectedCode);
        entity.IsActive.Should().BeFalse();
        entity.ActivatedAtUtc.Should().BeNull();
    }

    [Theory]
    [InlineData(false, "Activatable.Actor.Invalid")]
    [InlineData(true, "Activatable.ActivatedBy.Required")]
    public void Deactivate_WhitespaceActor_RequireMatrix_ShouldFailWithExpectedCode(
        bool requireActor,
        string expectedCode)
    {
        // Arrange:
        var entity = ActiveEntity();

        // Act:
        var result = Success(entity).Deactivate(" ", requireActor);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(expectedCode);
        entity.IsActive.Should().BeTrue();
        entity.ActivatedAtUtc.Should().Be(NowUtc);
        entity.ActivatedBy.Should().Be("bob");
    }

    [Fact]
    public void Activate_ActorTooShort_ShouldFailActivatedByTooShortWithoutMutation()
    {
        // Arrange:
        var entity = new TestActivatable();

        // Act:
        var result = Success(entity).Activate(NowUtc, "x");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.ActivatedBy.TooShort");
        entity.IsActive.Should().BeFalse();
        entity.ActivatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Deactivate_ActorTooShort_ShouldFailActivatedByTooShortWithoutMutation()
    {
        // Arrange:
        var entity = ActiveEntity();

        // Act:
        var result = Success(entity).Deactivate("x");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.ActivatedBy.TooShort");
        entity.IsActive.Should().BeTrue();
        entity.ActivatedAtUtc.Should().Be(NowUtc);
        entity.ActivatedBy.Should().Be("bob");
    }

    [Fact]
    public void Activate_ActorAtMaxLength_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestActivatable();
        var actor = new string('a', LifecycleConstant.Constraints.Actor.MaxLength);

        // Act:
        var result = Success(entity).Activate(NowUtc, actor);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.ActivatedBy.Should().Be(actor);
    }

    [Fact]
    public void Activate_NegativeOffsetTimestamp_ShouldStoreUtcInstant()
    {
        // Arrange:
        var entity = new TestActivatable();
        var offset = new DateTimeOffset(2024, 5, 1, 7, 0, 0, TimeSpan.FromHours(-5));

        // Act:
        var result = Success(entity).Activate(offset, "alice");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.ActivatedAtUtc!.Value.Offset.Should().Be(TimeSpan.Zero);
        entity.ActivatedAtUtc.Should().Be(offset.ToUniversalTime());
    }

    [Fact]
    public void Activate_AlreadyUtcTimestamp_ShouldKeepZeroOffset()
    {
        // Arrange:
        var entity = new TestActivatable();

        // Act:
        var result = Success(entity).Activate(NowUtc, "alice");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.ActivatedAtUtc!.Value.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Activate_NearSkewBoundary_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestActivatable();
        var nearBoundary = DateTimeOffset.UtcNow.AddSeconds(
            LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds - 5);

        // Act:
        var result = Success(entity).Activate(nearBoundary, "alice");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.IsActive.Should().BeTrue();
        entity.ActivatedAtUtc.Should().Be(nearBoundary.ToUniversalTime());
    }

    [Fact]
    public void Activate_JustOverSkewBoundary_ShouldFailInFutureWithoutMutation()
    {
        // Arrange:
        var entity = new TestActivatable();
        var justOver = DateTimeOffset.UtcNow.AddSeconds(
            LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds + 5);

        // Act:
        var result = Success(entity).Activate(justOver, "alice");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.ActivatedAt.InFuture");
        entity.IsActive.Should().BeFalse();
        entity.ActivatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Activate_DoubleActivate_ShouldFailSecondAndKeepOriginal()
    {
        // Arrange:
        var entity = new TestActivatable();
        var first = Success(entity).Activate(NowUtc, "alice");
        first.IsSuccess.Should().BeTrue();

        // Act:
        var result = Success(entity).Activate(NowUtc.AddHours(1), "carol");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.AlreadyActive");
        entity.IsActive.Should().BeTrue();
        entity.ActivatedAtUtc.Should().Be(NowUtc);
        entity.ActivatedBy.Should().Be("alice");
    }

    [Fact]
    public void Deactivate_DoubleDeactivate_ShouldFailSecondAndKeepCleared()
    {
        // Arrange:
        var entity = ActiveEntity();
        var first = Success(entity).Deactivate("carol");
        first.IsSuccess.Should().BeTrue();

        // Act:
        var result = Success(entity).Deactivate("carol");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Activatable.NotActive");
        entity.IsActive.Should().BeFalse();
        entity.ActivatedAtUtc.Should().BeNull();
        entity.ActivatedBy.Should().BeNull();
    }

    [Fact]
    public void Activate_InputFailure_ShouldPropagateErrors()
    {
        // Arrange:
        var input = Result<TestActivatable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        var result = input.Activate(NowUtc, "alice");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Test.Fail");
    }

    [Fact]
    public void Deactivate_InputFailure_ShouldPropagateErrors()
    {
        // Arrange:
        var input = Result<TestActivatable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        var result = input.Deactivate("carol");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Test.Fail");
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