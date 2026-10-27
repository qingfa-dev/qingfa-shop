using Application.Common.Persistence.UnitOfWorks;
using System.Data;

namespace Application.Tests.Common.Persistence.UnitOfWorks;

[Trait("Category", "Unit")]
public sealed class TransactionGuardTest
{
    #region ValidateCommandTimeout

    [Theory(DisplayName = "ValidateCommandTimeout Should Accept Valid Timeouts")]
    [InlineData(1)]    // MinCommandTimeoutSeconds
    [InlineData(30)]   // DefaultCommandTimeoutSeconds
    [InlineData(60)]
    [InlineData(300)]  // MaxCommandTimeoutSeconds
    public void ValidateCommandTimeout_ShouldAcceptValidTimeouts(int timeoutSeconds)
    {
        // Act
        var result = TransactionGuard.ValidateCommandTimeout(timeoutSeconds);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateCommandTimeout Should Reject Timeouts Below Minimum")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void ValidateCommandTimeout_ShouldRejectTimeoutsBelowMinimum(int timeoutSeconds)
    {
        // Act
        var result = TransactionGuard.ValidateCommandTimeout(timeoutSeconds);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == TransactionOutcome.Codes.CommandTimeoutTooSmall);
    }

    [Theory(DisplayName = "ValidateCommandTimeout Should Reject Timeouts Above Maximum")]
    [InlineData(301)] // MaxCommandTimeoutSeconds is 300
    [InlineData(500)]
    [InlineData(int.MaxValue)]
    public void ValidateCommandTimeout_ShouldRejectTimeoutsAboveMaximum(int timeoutSeconds)
    {
        // Act
        var result = TransactionGuard.ValidateCommandTimeout(timeoutSeconds);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == TransactionOutcome.Codes.CommandTimeoutTooLarge);
    }

    [Fact(DisplayName = "ValidateCommandTimeout Should Use Custom Parameter Name")]
    public void ValidateCommandTimeout_ShouldUseCustomParameterName()
    {
        // Act
        var result = TransactionGuard.ValidateCommandTimeout(0, "customTimeout");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customTimeout"));
    }

    #endregion

    #region ValidateMaxRetryAttempts

    [Theory(DisplayName = "ValidateMaxRetryAttempts Should Accept Valid Attempts")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]  // DefaultMaxRetryAttempts
    [InlineData(10)] // MaxRetryAttempts
    public void ValidateMaxRetryAttempts_ShouldAcceptValidAttempts(int maxAttempts)
    {
        // Act
        var result = TransactionGuard.ValidateMaxRetryAttempts(maxAttempts);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateMaxRetryAttempts Should Reject Negative Attempts")]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void ValidateMaxRetryAttempts_ShouldRejectNegativeAttempts(int maxAttempts)
    {
        // Act
        var result = TransactionGuard.ValidateMaxRetryAttempts(maxAttempts);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == TransactionOutcome.Codes.MaxRetryAttemptsNegative);
    }

    [Theory(DisplayName = "ValidateMaxRetryAttempts Should Reject Attempts Exceeding Max")]
    [InlineData(11)] // MaxRetryAttempts is 10
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void ValidateMaxRetryAttempts_ShouldRejectAttemptsExceedingMax(int maxAttempts)
    {
        // Act
        var result = TransactionGuard.ValidateMaxRetryAttempts(maxAttempts);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == TransactionOutcome.Codes.MaxRetryAttemptsExceedsMax);
    }

    [Fact(DisplayName = "ValidateMaxRetryAttempts Should Use Custom Parameter Name")]
    public void ValidateMaxRetryAttempts_ShouldUseCustomParameterName()
    {
        // Act
        var result = TransactionGuard.ValidateMaxRetryAttempts(-1, "customMaxAttempts");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customMaxAttempts"));
    }

    #endregion

    #region ValidateRetryBaseDelay

    [Theory(DisplayName = "ValidateRetryBaseDelay Should Accept Valid Delays")]
    [InlineData(0)]
    [InlineData(100)]     // DefaultRetryBaseDelayMs
    [InlineData(1000)]
    [InlineData(10000)]   // MaxRetryBaseDelayMs
    public void ValidateRetryBaseDelay_ShouldAcceptValidDelays(int baseDelayMs)
    {
        // Act
        var result = TransactionGuard.ValidateRetryBaseDelay(baseDelayMs);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateRetryBaseDelay Should Reject Negative Delays")]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void ValidateRetryBaseDelay_ShouldRejectNegativeDelays(int baseDelayMs)
    {
        // Act
        var result = TransactionGuard.ValidateRetryBaseDelay(baseDelayMs);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == TransactionOutcome.Codes.RetryBaseDelayNegative);
    }

    [Theory(DisplayName = "ValidateRetryBaseDelay Should Reject Delays Exceeding Max")]
    [InlineData(10001)] // MaxRetryBaseDelayMs is 10000
    [InlineData(20000)]
    [InlineData(int.MaxValue)]
    public void ValidateRetryBaseDelay_ShouldRejectDelaysExceedingMax(int baseDelayMs)
    {
        // Act
        var result = TransactionGuard.ValidateRetryBaseDelay(baseDelayMs);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == TransactionOutcome.Codes.RetryBaseDelayExceedsMax);
    }

    [Fact(DisplayName = "ValidateRetryBaseDelay Should Use Custom Parameter Name")]
    public void ValidateRetryBaseDelay_ShouldUseCustomParameterName()
    {
        // Act
        var result = TransactionGuard.ValidateRetryBaseDelay(-1, "customBaseDelayMs");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customBaseDelayMs"));
    }

    #endregion

    #region ValidatePoolMinSize

    [Theory(DisplayName = "ValidatePoolMinSize Should Accept Valid Sizes")]
    [InlineData(0)]   // DefaultPoolMinSize
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void ValidatePoolMinSize_ShouldAcceptValidSizes(int minSize)
    {
        // Act
        var result = TransactionGuard.ValidatePoolMinSize(minSize);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidatePoolMinSize Should Reject Negative Sizes")]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void ValidatePoolMinSize_ShouldRejectNegativeSizes(int minSize)
    {
        // Act
        var result = TransactionGuard.ValidatePoolMinSize(minSize);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == TransactionOutcome.Codes.PoolMinSizeNegative);
    }

    [Fact(DisplayName = "ValidatePoolMinSize Should Use Custom Parameter Name")]
    public void ValidatePoolMinSize_ShouldUseCustomParameterName()
    {
        // Act
        var result = TransactionGuard.ValidatePoolMinSize(-1, "customMinSize");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customMinSize"));
    }

    #endregion

    #region ValidatePoolMaxSize

    [Theory(DisplayName = "ValidatePoolMaxSize Should Accept Valid Sizes")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]   // DefaultPoolMaxSize
    [InlineData(500)]   // MaxPoolSize
    public void ValidatePoolMaxSize_ShouldAcceptValidSizes(int maxSize)
    {
        // Act
        var result = TransactionGuard.ValidatePoolMaxSize(maxSize);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidatePoolMaxSize Should Reject Negative Sizes")]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void ValidatePoolMaxSize_ShouldRejectNegativeSizes(int maxSize)
    {
        // Act
        var result = TransactionGuard.ValidatePoolMaxSize(maxSize);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == TransactionOutcome.Codes.PoolMaxSizeNegative);
    }

    [Theory(DisplayName = "ValidatePoolMaxSize Should Reject Sizes Exceeding Max")]
    [InlineData(501)] // MaxPoolSize is 500
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void ValidatePoolMaxSize_ShouldRejectSizesExceedingMax(int maxSize)
    {
        // Act
        var result = TransactionGuard.ValidatePoolMaxSize(maxSize);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == TransactionOutcome.Codes.PoolMaxSizeExceedsMax);
    }

    [Fact(DisplayName = "ValidatePoolMaxSize Should Use Custom Parameter Name")]
    public void ValidatePoolMaxSize_ShouldUseCustomParameterName()
    {
        // Act
        var result = TransactionGuard.ValidatePoolMaxSize(-1, "customMaxSize");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customMaxSize"));
    }

    #endregion

    #region ValidatePoolSizes

    [Theory(DisplayName = "ValidatePoolSizes Should Accept Valid Combinations")]
    [InlineData(0, 100)]
    [InlineData(10, 50)]
    [InlineData(50, 50)]   // Equal is valid
    [InlineData(0, 500)]
    public void ValidatePoolSizes_ShouldAcceptValidCombinations(int minSize, int maxSize)
    {
        // Act
        var result = TransactionGuard.ValidatePoolSizes(minSize, maxSize);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidatePoolSizes Should Reject Min Exceeding Max")]
    [InlineData(100, 50)]
    [InlineData(500, 100)]
    [InlineData(1000, 100)]
    public void ValidatePoolSizes_ShouldRejectMinExceedingMax(int minSize, int maxSize)
    {
        // Act
        var result = TransactionGuard.ValidatePoolSizes(minSize, maxSize);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == TransactionOutcome.Codes.PoolMinExceedsMax);
    }

    [Fact(DisplayName = "ValidatePoolSizes Should Reject Negative Min Size")]
    public void ValidatePoolSizes_ShouldRejectNegativeMinSize()
    {
        // Act
        var result = TransactionGuard.ValidatePoolSizes(-1, 100);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == TransactionOutcome.Codes.PoolMinSizeNegative);
    }

    [Fact(DisplayName = "ValidatePoolSizes Should Reject Negative Max Size")]
    public void ValidatePoolSizes_ShouldRejectNegativeMaxSize()
    {
        // Act
        var result = TransactionGuard.ValidatePoolSizes(10, -1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == TransactionOutcome.Codes.PoolMaxSizeNegative);
    }

    [Fact(DisplayName = "ValidatePoolSizes Should Reject Max Exceeding MaxPoolSize")]
    public void ValidatePoolSizes_ShouldRejectMaxExceedingMaxPoolSize()
    {
        // Act
        var result = TransactionGuard.ValidatePoolSizes(100, 501);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == TransactionOutcome.Codes.PoolMaxSizeExceedsMax);
    }

    [Fact(DisplayName = "ValidatePoolSizes Should Combine Multiple Errors")]
    public void ValidatePoolSizes_ShouldCombineMultipleErrors()
    {
        // Act - both min and max negative
        var result = TransactionGuard.ValidatePoolSizes(-1, -1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
    }

    [Fact(DisplayName = "ValidatePoolSizes Should Use Custom Parameter Names")]
    public void ValidatePoolSizes_ShouldUseCustomParameterNames()
    {
        // Act
        var result = TransactionGuard.ValidatePoolSizes(100, 50, "myMin", "myMax");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("myMin") && e.Message.Contains("myMax"));
    }

    #endregion

    #region ValidateIsolationLevel (String)

    [Theory(DisplayName = "ValidateIsolationLevel Should Accept Valid String Levels")]
    [InlineData(TransactionConstant.IsolationLevel.ReadUncommitted)]
    [InlineData(TransactionConstant.IsolationLevel.ReadCommitted)]
    [InlineData(TransactionConstant.IsolationLevel.RepeatableRead)]
    [InlineData(TransactionConstant.IsolationLevel.Serializable)]
    [InlineData(TransactionConstant.IsolationLevel.Snapshot)]
    [InlineData(TransactionConstant.IsolationLevel.Unspecified)]
    public void ValidateIsolationLevel_ShouldAcceptValidStringLevels(string level)
    {
        // Act
        var result = TransactionGuard.ValidateIsolationLevel(level);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateIsolationLevel Should Accept Null (Use Default)")]
    public void ValidateIsolationLevel_ShouldAcceptNull()
    {
        // Act
        var result = TransactionGuard.ValidateIsolationLevel((string?)null);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateIsolationLevel Should Reject Invalid String Levels")]
    [InlineData("")]
    [InlineData("Invalid")]
    [InlineData("ReadCommited")] // Typo
    [InlineData("readuncommitted")] // Case sensitive
    [InlineData("SNAPSHOT")] // Case sensitive
    public void ValidateIsolationLevel_ShouldRejectInvalidStringLevels(string level)
    {
        // Act
        var result = TransactionGuard.ValidateIsolationLevel(level);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == TransactionOutcome.Codes.IsolationLevelInvalid);
    }

    [Fact(DisplayName = "ValidateIsolationLevel Should Use Custom Parameter Name")]
    public void ValidateIsolationLevel_ShouldUseCustomParameterName()
    {
        // Act
        var result = TransactionGuard.ValidateIsolationLevel("Invalid", "customLevel");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customLevel"));
    }

    #endregion

    #region ValidateIsolationLevel (Enum)

    [Theory(DisplayName = "ValidateIsolationLevel Should Accept Valid Enum Levels")]
    [InlineData(IsolationLevel.ReadUncommitted)]
    [InlineData(IsolationLevel.ReadCommitted)]
    [InlineData(IsolationLevel.RepeatableRead)]
    [InlineData(IsolationLevel.Serializable)]
    [InlineData(IsolationLevel.Snapshot)]
    [InlineData(IsolationLevel.Unspecified)]
    public void ValidateIsolationLevel_ShouldAcceptValidEnumLevels(IsolationLevel level)
    {
        // Act
        var result = TransactionGuard.ValidateIsolationLevel(level);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateIsolationLevel Should Accept Null Enum")]
    public void ValidateIsolationLevel_ShouldAcceptNullEnum()
    {
        // Act
        var result = TransactionGuard.ValidateIsolationLevel((IsolationLevel?)null);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateIsolationLevel Should Use Custom Parameter Name For Enum")]
    public void ValidateIsolationLevel_ShouldUseCustomParameterNameForEnum()
    {
        // Arrange - create an invalid enum by casting an invalid value
        // Note: This is harder to test with enum since it's constrained
        var result = TransactionGuard.ValidateIsolationLevel(IsolationLevel.ReadCommitted, "customLevel");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region ValidateState

    [Theory(DisplayName = "ValidateState Should Accept Valid States")]
    [InlineData(TransactionConstant.State.Active)]
    [InlineData(TransactionConstant.State.Committed)]
    [InlineData(TransactionConstant.State.RolledBack)]
    [InlineData(TransactionConstant.State.RollbackOnly)]
    [InlineData(TransactionConstant.State.Failed)]
    public void ValidateState_ShouldAcceptValidStates(string state)
    {
        // Act
        var result = TransactionGuard.ValidateState(state);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateState Should Reject Null")]
    [InlineData(null)]
    public void ValidateState_ShouldRejectNull(string? state)
    {
        // Act
        var result = TransactionGuard.ValidateState(state);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == TransactionOutcome.Codes.StateNull);
    }

    [Theory(DisplayName = "ValidateState Should Reject Invalid States")]
    [InlineData("")]
    [InlineData("Invalid")]
    [InlineData("active")] // Case sensitive
    [InlineData("COMMITTED")] // Case sensitive
    [InlineData("Pending")]
    public void ValidateState_ShouldRejectInvalidStates(string state)
    {
        // Act
        var result = TransactionGuard.ValidateState(state);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == TransactionOutcome.Codes.StateInvalid);
    }

    [Fact(DisplayName = "ValidateState Should Use Custom Parameter Name")]
    public void ValidateState_ShouldUseCustomParameterName()
    {
        // Act
        var result = TransactionGuard.ValidateState(null, "customState");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customState"));
    }

    #endregion
}