using Application.Common.Persistence.Initialization.Resetters;

namespace Application.Tests.Common.Persistence.Initialization.Resetters;

[Trait("Category", "Unit")]
public sealed class ResetterGuardTest
{
    #region ValidateOrder

    [Theory(DisplayName = "ValidateOrder Should Accept Valid Orders")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void ValidateOrder_ShouldAcceptValidOrders(int order)
    {
        // Act
        var result = ResetterGuard.ValidateOrder(order);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateOrder Should Reject Negative Orders")]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void ValidateOrder_ShouldRejectNegativeOrders(int order)
    {
        // Act
        var result = ResetterGuard.ValidateOrder(order);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == ResetterOutcome.Codes.OrderNegative);
    }

    [Fact(DisplayName = "ValidateOrder Should Use Custom Parameter Name")]
    public void ValidateOrder_ShouldUseCustomParameterName()
    {
        // Act
        var result = ResetterGuard.ValidateOrder(-1, "customOrder");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customOrder"));
    }

    #endregion

    #region ValidateCount

    [Theory(DisplayName = "ValidateCount Should Accept Valid Counts")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)] // MaxResetters is 100
    public void ValidateCount_ShouldAcceptValidCounts(int count)
    {
        // Act
        var result = ResetterGuard.ValidateCount(count);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateCount Should Reject Negative Counts")]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void ValidateCount_ShouldRejectNegativeCounts(int count)
    {
        // Act
        var result = ResetterGuard.ValidateCount(count);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == ResetterOutcome.Codes.CountNegative);
    }

    [Theory(DisplayName = "ValidateCount Should Reject Counts Exceeding Max")]
    [InlineData(101)] // MaxResetters is 100
    [InlineData(500)]
    [InlineData(int.MaxValue)]
    public void ValidateCount_ShouldRejectCountsExceedingMax(int count)
    {
        // Act
        var result = ResetterGuard.ValidateCount(count);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == ResetterOutcome.Codes.CountExceedsMax);
    }

    [Fact(DisplayName = "ValidateCount Should Use Custom Parameter Name")]
    public void ValidateCount_ShouldUseCustomParameterName()
    {
        // Act
        var result = ResetterGuard.ValidateCount(-1, "customCount");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customCount"));
    }

    #endregion

    #region ValidateScope

    [Theory(DisplayName = "ValidateScope Should Accept Valid Scopes")]
    [InlineData(ResetterConstant.Scope.Data)]
    [InlineData(ResetterConstant.Scope.Schema)]
    [InlineData(ResetterConstant.Scope.All)]
    public void ValidateScope_ShouldAcceptValidScopes(string scope)
    {
        // Act
        var result = ResetterGuard.ValidateScope(scope);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateScope Should Reject Null")]
    [InlineData(null)]
    public void ValidateScope_ShouldRejectNull(string? scope)
    {
        // Act
        var result = ResetterGuard.ValidateScope(scope);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == ResetterOutcome.Codes.ScopeNull);
    }

    [Theory(DisplayName = "ValidateScope Should Reject Invalid Scopes")]
    [InlineData("")]
    [InlineData("Invalid")]
    [InlineData("data")] // Case sensitive
    [InlineData("SCHEMA")] // Case sensitive
    [InlineData("ALL")] // Case sensitive
    public void ValidateScope_ShouldRejectInvalidScopes(string scope)
    {
        // Act
        var result = ResetterGuard.ValidateScope(scope);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == ResetterOutcome.Codes.ScopeInvalid);
    }

    [Fact(DisplayName = "ValidateScope Should Use Custom Parameter Name")]
    public void ValidateScope_ShouldUseCustomParameterName()
    {
        // Act
        var result = ResetterGuard.ValidateScope(null, "customScope");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customScope"));
    }

    #endregion

    #region ValidateResetter

    [Fact(DisplayName = "ValidateResetter Should Succeed When All Valid")]
    public void ValidateResetter_ShouldSucceedWhenAllValid()
    {
        // Act
        var result = ResetterGuard.ValidateResetter(100, ResetterConstant.Scope.Data);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateResetter Should Fail When Order Invalid")]
    public void ValidateResetter_ShouldFailWhenOrderInvalid()
    {
        // Act
        var result = ResetterGuard.ValidateResetter(-1, ResetterConstant.Scope.Data);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == ResetterOutcome.Codes.OrderNegative);
    }

    [Fact(DisplayName = "ValidateResetter Should Fail When Scope Invalid")]
    public void ValidateResetter_ShouldFailWhenScopeInvalid()
    {
        // Act
        var result = ResetterGuard.ValidateResetter(100, "Invalid");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == ResetterOutcome.Codes.ScopeInvalid);
    }

    [Fact(DisplayName = "ValidateResetter Should Combine Multiple Errors")]
    public void ValidateResetter_ShouldCombineMultipleErrors()
    {
        // Act
        var result = ResetterGuard.ValidateResetter(-1, "Invalid");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
    }

    #endregion
}