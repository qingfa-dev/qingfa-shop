using Application.BuildingBlocks.Persistence.Initialization.Seeders;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.Initialization.Seeders;

/// <summary>Unit tests for SeederGuard.</summary>
public sealed class SeederGuardTest
{
    #region ValidateOrder

    [Fact]
    public void ValidateOrder_WhenNegative_ShouldFail()
    {
        var result = SeederGuard.ValidateOrder(-1);
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(SeederOutcome.Codes.OrderNegative);
    }

    [Fact]
    public void ValidateOrder_WhenZero_ShouldSucceed()
    {
        var result = SeederGuard.ValidateOrder(0);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateOrder_WhenPositive_ShouldSucceed()
    {
        var result = SeederGuard.ValidateOrder(100);
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region ValidateCount

    [Fact]
    public void ValidateCount_WhenNegative_ShouldFail()
    {
        var result = SeederGuard.ValidateCount(-1);
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(SeederOutcome.Codes.CountNegative);
    }

    [Fact]
    public void ValidateCount_WhenExceedsMax_ShouldFail()
    {
        var result = SeederGuard.ValidateCount(SeederConstant.Limits.MaxSeeders + 1);
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(SeederOutcome.Codes.CountExceedsMax);
    }

    [Fact]
    public void ValidateCount_WhenValid_ShouldSucceed()
    {
        var result = SeederGuard.ValidateCount(100);
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region ValidateBatchSize

    [Fact]
    public void ValidateBatchSize_WhenTooSmall_ShouldFail()
    {
        var result = SeederGuard.ValidateBatchSize(0);
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(SeederOutcome.Codes.BatchSizeTooSmall);
    }

    [Fact]
    public void ValidateBatchSize_WhenTooLarge_ShouldFail()
    {
        var result = SeederGuard.ValidateBatchSize(SeederConstant.Limits.MaxBatchSize + 1);
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(SeederOutcome.Codes.BatchSizeTooLarge);
    }

    [Fact]
    public void ValidateBatchSize_WhenValid_ShouldSucceed()
    {
        var result = SeederGuard.ValidateBatchSize(100);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateBatchSize_WhenMin_ShouldSucceed()
    {
        var result = SeederGuard.ValidateBatchSize(SeederConstant.Limits.MinBatchSize);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateBatchSize_WhenMax_ShouldSucceed()
    {
        var result = SeederGuard.ValidateBatchSize(SeederConstant.Limits.MaxBatchSize);
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region ValidateSeeder

    [Fact]
    public void ValidateSeeder_WhenAllValid_ShouldSucceed()
    {
        var result = SeederGuard.ValidateSeeder(10, 100);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateSeeder_WhenOrderNegative_ShouldFail()
    {
        var result = SeederGuard.ValidateSeeder(-1, 100);
        result.IsFailure.Should().BeTrue();
        result.Errors.Any(e => e.Code == SeederOutcome.Codes.OrderNegative).Should().BeTrue();
    }

    [Fact]
    public void ValidateSeeder_WhenBatchSizeInvalid_ShouldFail()
    {
        var result = SeederGuard.ValidateSeeder(10, 0);
        result.IsFailure.Should().BeTrue();
        result.Errors.Any(e => e.Code == SeederOutcome.Codes.BatchSizeTooSmall).Should().BeTrue();
    }

    [Fact]
    public void ValidateSeeder_WhenMultipleInvalid_ShouldReturnMultipleErrors()
    {
        var result = SeederGuard.ValidateSeeder(-1, 0);
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCountGreaterThan(1);
    }

    #endregion
}
