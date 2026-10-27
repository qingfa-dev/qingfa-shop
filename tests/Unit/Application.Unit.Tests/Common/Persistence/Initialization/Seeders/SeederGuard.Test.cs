using Application.Common.Persistence.Initialization.Seeders;

namespace Application.Tests.Common.Persistence.Initialization.Seeders;

[Trait("Category", "Unit")]
public sealed class SeederGuardTest
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
        var result = SeederGuard.ValidateOrder(order);

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
        var result = SeederGuard.ValidateOrder(order);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == SeederOutcome.Codes.OrderNegative);
    }

    [Fact(DisplayName = "ValidateOrder Should Use Custom Parameter Name")]
    public void ValidateOrder_ShouldUseCustomParameterName()
    {
        // Act
        var result = SeederGuard.ValidateOrder(-1, "customOrder");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customOrder"));
    }

    #endregion

    #region ValidateCount

    [Theory(DisplayName = "ValidateCount Should Accept Valid Counts")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(500)] // MaxSeeders is 500
    public void ValidateCount_ShouldAcceptValidCounts(int count)
    {
        // Act
        var result = SeederGuard.ValidateCount(count);

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
        var result = SeederGuard.ValidateCount(count);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == SeederOutcome.Codes.CountNegative);
    }

    [Theory(DisplayName = "ValidateCount Should Reject Counts Exceeding Max")]
    [InlineData(501)] // MaxSeeders is 500
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void ValidateCount_ShouldRejectCountsExceedingMax(int count)
    {
        // Act
        var result = SeederGuard.ValidateCount(count);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == SeederOutcome.Codes.CountExceedsMax);
    }

    [Fact(DisplayName = "ValidateCount Should Use Custom Parameter Name")]
    public void ValidateCount_ShouldUseCustomParameterName()
    {
        // Act
        var result = SeederGuard.ValidateCount(-1, "customCount");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customCount"));
    }

    #endregion

    #region ValidateBatchSize

    [Theory(DisplayName = "ValidateBatchSize Should Accept Valid Batch Sizes")]
    [InlineData(1)]    // MinBatchSize is 1
    [InlineData(100)]
    [InlineData(1000)] // DefaultBatchSize is 1000
    [InlineData(5000)] // MaxBatchSize is 5000
    public void ValidateBatchSize_ShouldAcceptValidBatchSizes(int batchSize)
    {
        // Act
        var result = SeederGuard.ValidateBatchSize(batchSize);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateBatchSize Should Reject Batch Sizes Below Minimum")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void ValidateBatchSize_ShouldRejectBatchSizesBelowMinimum(int batchSize)
    {
        // Act
        var result = SeederGuard.ValidateBatchSize(batchSize);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == SeederOutcome.Codes.BatchSizeTooSmall);
    }

    [Theory(DisplayName = "ValidateBatchSize Should Reject Batch Sizes Above Maximum")]
    [InlineData(5001)] // MaxBatchSize is 5000
    [InlineData(10000)]
    [InlineData(int.MaxValue)]
    public void ValidateBatchSize_ShouldRejectBatchSizesAboveMaximum(int batchSize)
    {
        // Act
        var result = SeederGuard.ValidateBatchSize(batchSize);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == SeederOutcome.Codes.BatchSizeTooLarge);
    }

    [Fact(DisplayName = "ValidateBatchSize Should Use Custom Parameter Name")]
    public void ValidateBatchSize_ShouldUseCustomParameterName()
    {
        // Act
        var result = SeederGuard.ValidateBatchSize(0, "customBatchSize");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customBatchSize"));
    }

    #endregion

    #region ValidateSeeder

    [Fact(DisplayName = "ValidateSeeder Should Succeed When All Valid")]
    public void ValidateSeeder_ShouldSucceedWhenAllValid()
    {
        // Act
        var result = SeederGuard.ValidateSeeder(100, 1000);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateSeeder Should Fail When Order Invalid")]
    public void ValidateSeeder_ShouldFailWhenOrderInvalid()
    {
        // Act
        var result = SeederGuard.ValidateSeeder(-1, 1000);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == SeederOutcome.Codes.OrderNegative);
    }

    [Fact(DisplayName = "ValidateSeeder Should Fail When Batch Size Invalid")]
    public void ValidateSeeder_ShouldFailWhenBatchSizeInvalid()
    {
        // Act
        var result = SeederGuard.ValidateSeeder(100, 0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == SeederOutcome.Codes.BatchSizeTooSmall);
    }

    [Fact(DisplayName = "ValidateSeeder Should Combine Multiple Errors")]
    public void ValidateSeeder_ShouldCombineMultipleErrors()
    {
        // Act
        var result = SeederGuard.ValidateSeeder(-1, 0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
    }

    #endregion
}