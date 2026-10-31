using Application.BuildingBlocks.Persistence.Initialization.Resetters;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.Initialization.Resetters;

/// <summary>Unit tests for ResetterGuard.</summary>
public sealed class ResetterGuardTest
{
    #region ValidateOrder

    [Fact]
    public void ValidateOrder_WhenNegative_ShouldFail()
    {
        var result = ResetterGuard.ValidateOrder(-1);
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(ResetterOutcome.Codes.OrderNegative);
    }

    [Fact]
    public void ValidateOrder_WhenZero_ShouldSucceed()
    {
        var result = ResetterGuard.ValidateOrder(0);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateOrder_WhenPositive_ShouldSucceed()
    {
        var result = ResetterGuard.ValidateOrder(100);
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region ValidateCount

    [Fact]
    public void ValidateCount_WhenNegative_ShouldFail()
    {
        var result = ResetterGuard.ValidateCount(-1);
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(ResetterOutcome.Codes.CountNegative);
    }

    [Fact]
    public void ValidateCount_WhenExceedsMax_ShouldFail()
    {
        var result = ResetterGuard.ValidateCount(ResetterConstant.Limits.MaxResetters + 1);
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(ResetterOutcome.Codes.CountExceedsMax);
    }

    [Fact]
    public void ValidateCount_WhenValid_ShouldSucceed()
    {
        var result = ResetterGuard.ValidateCount(10);
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region ValidateScope

    [Fact]
    public void ValidateScope_WhenNull_ShouldFail()
    {
        var result = ResetterGuard.ValidateScope(null);
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(ResetterOutcome.Codes.ScopeNull);
    }

    [Fact]
    public void ValidateScope_WhenInvalid_ShouldFail()
    {
        var result = ResetterGuard.ValidateScope("invalid");
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(ResetterOutcome.Codes.ScopeInvalid);
    }

    [Theory]
    [InlineData("Data")]
    [InlineData("Schema")]
    [InlineData("All")]
    public void ValidateScope_WhenValid_ShouldSucceed(string scope)
    {
        var result = ResetterGuard.ValidateScope(scope);
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region ValidateResetter

    [Fact]
    public void ValidateResetter_WhenAllValid_ShouldSucceed()
    {
        var result = ResetterGuard.ValidateResetter(10, "Data");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateResetter_WhenOrderNegative_ShouldFail()
    {
        var result = ResetterGuard.ValidateResetter(-1, "Data");
        result.IsFailure.Should().BeTrue();
        result.Errors.Any(e => e.Code == ResetterOutcome.Codes.OrderNegative).Should().BeTrue();
    }

    [Fact]
    public void ValidateResetter_WhenScopeNull_ShouldFail()
    {
        var result = ResetterGuard.ValidateResetter(10, null);
        result.IsFailure.Should().BeTrue();
        result.Errors.Any(e => e.Code == ResetterOutcome.Codes.ScopeNull).Should().BeTrue();
    }

    [Fact]
    public void ValidateResetter_WhenMultipleInvalid_ShouldReturnMultipleErrors()
    {
        var result = ResetterGuard.ValidateResetter(-1, null);
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCountGreaterThan(1);
    }

    #endregion
}
