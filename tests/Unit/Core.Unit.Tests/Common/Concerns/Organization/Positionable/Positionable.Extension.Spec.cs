using Core.Common.Concerns.Organization.Positionable;

namespace Core.Unit.Tests.Common.Concerns.Organization.Positionable;

/// <summary>Specification for <see cref="PositionableExtensions"/>.</summary>
public class PositionableExtensionSpec
{
    #region Helper Methods

    private sealed class TestPositionable : IPositionable
    {
        public int Position { get; set; }
    }

    /// <summary>Creates a successful result for the test entity.</summary>
    private static Result<TestPositionable> Success(
        TestPositionable entity)
    {
        return Result<TestPositionable>.Success(entity);
    }

    #endregion

    #region Test Cases

    [Fact]
    public void SetPosition_PositivePosition_ShouldSetPosition()
    {
        // Arrange:
        var entity = new TestPositionable();

        // Act:
        var result = Success(entity).SetPosition(5);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Position.Should().Be(5);
    }

    [Fact]
    public void SetPosition_ZeroPosition_ShouldSetPosition()
    {
        // Arrange:
        var entity = new TestPositionable { Position = 7 };

        // Act:
        var result = Success(entity).SetPosition(0);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Position.Should().Be(0);
    }

    [Fact]
    public void SetPosition_NegativePosition_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestPositionable { Position = 3 };

        // Act:
        var result = Success(entity).SetPosition(-1);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(
            "Positionable.Position.Negative");
        entity.Position.Should().Be(3);
    }

    [Fact]
    public void ValidatePosition_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = PositionableValidator.ValidatePosition(
            (TestPositionable)null!,
            1);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Positionable.Entity.Required");
    }

    #endregion

    #region Validator Specs

    [Fact]
    public void ValidatePosition_Negative_ShouldFailPositionNegative()
    {
        // Arrange:
        var entity = new TestPositionable();

        // Act:
        var result = PositionableValidator.ValidatePosition(
            entity,
            PositionableConstant.Constraints.Position.Min - 1);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Positionable.Position.Negative");
    }

    [Fact]
    public void ValidatePosition_Min_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestPositionable();

        // Act:
        var result = PositionableValidator.ValidatePosition(
            entity,
            PositionableConstant.Constraints.Position.Min);

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidatePosition_Max_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestPositionable();

        // Act:
        var result = PositionableValidator.ValidatePosition(
            entity,
            PositionableConstant.Constraints.Position.Max);

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void MoveBy_PositiveOffset_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestPositionable { Position = 5 };

        // Act:
        var result = Success(entity).MoveBy<TestPositionable>(3);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Position.Should().Be(8);
    }

    [Fact]
    public void MoveBy_NegativeOverflow_ShouldFailPositionNegative()
    {
        // Arrange:
        var entity = new TestPositionable { Position = 0 };

        // Act:
        var result = Success(entity).MoveBy<TestPositionable>(-1);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Positionable.Position.Negative");
        entity.Position.Should().Be(0);
    }

    [Fact]
    public void MoveBy_PositiveOverflow_ShouldFailPositionTooLarge()
    {
        // Arrange:
        var entity = new TestPositionable
        {
            Position = PositionableConstant.Constraints.Position.Max
        };

        // Act:
        var result = Success(entity).MoveBy<TestPositionable>(1);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Positionable.Position.TooLarge");
        entity.Position.Should().Be(PositionableConstant.Constraints.Position.Max);
    }

    [Fact]
    public void MoveNext_AtMax_ShouldFailPositionTooLarge()
    {
        // Arrange:
        var entity = new TestPositionable
        {
            Position = PositionableConstant.Constraints.Position.Max
        };

        // Act:
        var result = Success(entity).MoveNext<TestPositionable>();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Positionable.Position.TooLarge");
        entity.Position.Should().Be(PositionableConstant.Constraints.Position.Max);
    }

    [Fact]
    public void MoveNext_Valid_ShouldIncrementByStep()
    {
        // Arrange:
        var entity = new TestPositionable { Position = 5 };

        // Act:
        var result = Success(entity).MoveNext<TestPositionable>();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Position.Should().Be(5 + PositionableConstant.Constraints.Position.Step);
    }

    [Fact]
    public void MovePrevious_Valid_ShouldDecrementByStep()
    {
        // Arrange:
        var entity = new TestPositionable { Position = 5 };

        // Act:
        var result = Success(entity).MovePrevious<TestPositionable>();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Position.Should().Be(5 - PositionableConstant.Constraints.Position.Step);
    }

    [Fact]
    public void MovePrevious_AtMin_ShouldFailPositionNegative()
    {
        // Arrange:
        var entity = new TestPositionable
        {
            Position = PositionableConstant.Constraints.Position.Min
        };

        // Act:
        var result = Success(entity).MovePrevious<TestPositionable>();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Positionable.Position.Negative");
        entity.Position.Should().Be(PositionableConstant.Constraints.Position.Min);
    }

    [Fact]
    public void ResetPosition_AssignedPosition_ShouldRestoreDefault()
    {
        // Arrange:
        var entity = new TestPositionable { Position = 7 };

        // Act:
        var result = Success(entity).ResetPosition<TestPositionable>();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Position.Should().Be(PositionableConstant.Defaults.DefaultPosition);
    }

    [Fact]
    public void SetPosition_Max_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestPositionable();

        // Act:
        var result = Success(entity).SetPosition(PositionableConstant.Constraints.Position.Max);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Position.Should().Be(PositionableConstant.Constraints.Position.Max);
    }

    [Fact]
    public void MoveBy_ZeroOffset_ShouldSucceedWithoutChange()
    {
        // Arrange:
        var entity = new TestPositionable { Position = 5 };

        // Act:
        var result = Success(entity).MoveBy<TestPositionable>(0);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Position.Should().Be(5);
    }

    [Fact]
    public void MoveBy_AtMaxZeroOffset_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestPositionable
        {
            Position = PositionableConstant.Constraints.Position.Max
        };

        // Act:
        var result = Success(entity).MoveBy<TestPositionable>(0);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Position.Should().Be(PositionableConstant.Constraints.Position.Max);
    }

    [Theory]
    [InlineData(5, 3, 8)]
    [InlineData(0, 0, 0)]
    [InlineData(int.MaxValue, 0, int.MaxValue)]
    [InlineData(int.MaxValue, -int.MaxValue, 0)]
    public void MoveBy_SuccessBoundaries_ShouldSetExpectedPosition(int start, int offset, int expected)
    {
        // Arrange:
        var entity = new TestPositionable { Position = start };

        // Act:
        var result = Success(entity).MoveBy<TestPositionable>(offset);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Position.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, -1, "Positionable.Position.Negative")]
    [InlineData(1, int.MinValue, "Positionable.Position.Negative")]
    [InlineData(0, int.MinValue, "Positionable.Position.Negative")]
    [InlineData(int.MaxValue, 1, "Positionable.Position.TooLarge")]
    [InlineData(int.MaxValue - 1, 2, "Positionable.Position.TooLarge")]
    [InlineData(int.MaxValue, int.MaxValue, "Positionable.Position.TooLarge")]
    public void MoveBy_Overflow_ShouldFailWithoutMutation(int start, int offset, string expectedCode)
    {
        // Arrange:
        var entity = new TestPositionable { Position = start };

        // Act:
        var result = Success(entity).MoveBy<TestPositionable>(offset);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(expectedCode);
        entity.Position.Should().Be(start);
    }

    [Fact]
    public void ResetPosition_AlreadyDefault_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestPositionable
        {
            Position = PositionableConstant.Defaults.DefaultPosition
        };

        // Act:
        var result = Success(entity).ResetPosition<TestPositionable>();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Position.Should().Be(PositionableConstant.Defaults.DefaultPosition);
    }

    [Fact]
    public void FailureCodes_ShouldHaveExpectedCodes()
    {
        // Arrange:
        // Act:
        // Assert:
        PositionableResult.Failure.EntityRequired.Code.Should().Be("Positionable.Entity.Required");
        PositionableResult.Failure.PositionNegative.Code.Should().Be("Positionable.Position.Negative");
        PositionableResult.Failure.PositionTooLarge.Code.Should().Be("Positionable.Position.TooLarge");
    }

    #endregion

    #region Constant Specs

    [Fact]
    public void Constants_ShouldMatchNestedConstraints()
    {
        // Arrange:
        // Act:
        // Assert:
        PositionableConstant.Constraints.Position.Min.Should().Be(0);
        PositionableConstant.Constraints.Position.Max.Should().Be(int.MaxValue);
        PositionableConstant.Constraints.Position.Step.Should().Be(1);
        PositionableConstant.Defaults.DefaultPosition.Should().Be(0);
    }

    #endregion
}