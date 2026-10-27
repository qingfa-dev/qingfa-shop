using Application.Common.Persistence.Initialization.Migrations;

namespace Infrastructure.Unit.Tests.Persistence.Initialization.Migrations;

/// <summary>Unit tests for MigrationGuard.</summary>
public sealed class MigrationGuardTest
{
    #region ValidateName

    [Fact]
    public void ValidateName_WhenNull_ShouldFail()
    {
        var result = MigrationGuard.ValidateName(null);
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(MigrationOutcome.Codes.NameNull);
    }

    [Fact]
    public void ValidateName_WhenEmpty_ShouldFail()
    {
        var result = MigrationGuard.ValidateName("");
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(MigrationOutcome.Codes.NameNullOrWhitespace);
    }

    [Fact]
    public void ValidateName_WhenWhitespace_ShouldFail()
    {
        var result = MigrationGuard.ValidateName("   ");
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(MigrationOutcome.Codes.NameNullOrWhitespace);
    }

    [Fact]
    public void ValidateName_WhenTooLong_ShouldFail()
    {
        var name = new string('a', MigrationConstant.Limits.MaxNameLength + 1);
        var result = MigrationGuard.ValidateName(name);
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(MigrationOutcome.Codes.NameExceedsMaxLength);
    }

    [Fact]
    public void ValidateName_WhenValid_ShouldSucceed()
    {
        var result = MigrationGuard.ValidateName("InitialCreate");
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region ValidateOrder

    [Fact]
    public void ValidateOrder_WhenNegative_ShouldFail()
    {
        var result = MigrationGuard.ValidateOrder(-1);
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(MigrationOutcome.Codes.OrderNegative);
    }

    [Fact]
    public void ValidateOrder_WhenZero_ShouldSucceed()
    {
        var result = MigrationGuard.ValidateOrder(0);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateOrder_WhenPositive_ShouldSucceed()
    {
        var result = MigrationGuard.ValidateOrder(100);
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region ValidateCount

    [Fact]
    public void ValidateCount_WhenNegative_ShouldFail()
    {
        var result = MigrationGuard.ValidateCount(-1);
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(MigrationOutcome.Codes.CountNegative);
    }

    [Fact]
    public void ValidateCount_WhenExceedsMax_ShouldFail()
    {
        var result = MigrationGuard.ValidateCount(MigrationConstant.Limits.MaxMigrations + 1);
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(MigrationOutcome.Codes.CountExceedsMax);
    }

    [Fact]
    public void ValidateCount_WhenValid_ShouldSucceed()
    {
        var result = MigrationGuard.ValidateCount(50);
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region ValidateVersion

    [Fact]
    public void ValidateVersion_WhenNull_ShouldFail()
    {
        var result = MigrationGuard.ValidateVersion(null);
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(MigrationOutcome.Codes.VersionNull);
    }

    [Fact]
    public void ValidateVersion_WhenEmpty_ShouldFail()
    {
        var result = MigrationGuard.ValidateVersion("");
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(MigrationOutcome.Codes.VersionNullOrWhitespace);
    }

    [Fact]
    public void ValidateVersion_WhenInvalidFormat_ShouldFail()
    {
        var result = MigrationGuard.ValidateVersion("2024-01-01");
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(MigrationOutcome.Codes.VersionInvalidFormat);
    }

    [Fact]
    public void ValidateVersion_WhenValid_ShouldSucceed()
    {
        var result = MigrationGuard.ValidateVersion("20261027082700");
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region ValidateChecksum

    [Fact]
    public void ValidateChecksum_WhenNull_ShouldFail()
    {
        var result = MigrationGuard.ValidateChecksum(null);
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(MigrationOutcome.Codes.ChecksumNull);
    }

    [Fact]
    public void ValidateChecksum_WhenEmpty_ShouldFail()
    {
        var result = MigrationGuard.ValidateChecksum("");
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(MigrationOutcome.Codes.ChecksumNullOrWhitespace);
    }

    [Fact]
    public void ValidateChecksum_WhenInvalidFormat_ShouldFail()
    {
        var result = MigrationGuard.ValidateChecksum("abc123");
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be(MigrationOutcome.Codes.ChecksumInvalidFormat);
    }

    [Fact]
    public void ValidateChecksum_WhenValid_ShouldSucceed()
    {
        var checksum = new string('a', 64);
        var result = MigrationGuard.ValidateChecksum(checksum);
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region ValidateMigration

    [Fact]
    public void ValidateMigration_WhenAllValid_ShouldSucceed()
    {
        var result = MigrationGuard.ValidateMigration(
            "InitialCreate", 100, "20261027082700", new string('a', 64));
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateMigration_WhenNameNull_ShouldFail()
    {
        var result = MigrationGuard.ValidateMigration(
            null, 100, "20261027082700", new string('a', 64));
        result.IsFailure.Should().BeTrue();
        result.Errors.Any(e => e.Code == MigrationOutcome.Codes.NameNull).Should().BeTrue();
    }

    [Fact]
    public void ValidateMigration_WhenVersionInvalid_ShouldFail()
    {
        var result = MigrationGuard.ValidateMigration(
            "InitialCreate", 100, "invalid", new string('a', 64));
        result.IsFailure.Should().BeTrue();
        result.Errors.Any(e => e.Code == MigrationOutcome.Codes.VersionInvalidFormat).Should().BeTrue();
    }

    [Fact]
    public void ValidateMigration_WhenMultipleInvalid_ShouldReturnMultipleErrors()
    {
        var result = MigrationGuard.ValidateMigration(null, -1, "bad", "short");
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCountGreaterThan(1);
    }

    #endregion
}
