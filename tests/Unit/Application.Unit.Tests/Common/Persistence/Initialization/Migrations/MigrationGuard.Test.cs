using Application.Common.Persistence.Initialization.Migrations;

namespace Application.Unit.Tests.Common.Persistence.Initialization.Migrations;

[Trait("Category", "Unit")]
public sealed class MigrationGuardTest
{
    #region ValidateName

    [Theory(DisplayName = "ValidateName Should Accept Valid Names")]
    [InlineData("Migration_20240101000000")]
    [InlineData("AddUsersTable")]
    [InlineData("Migration_1")]
    [InlineData("a")] // Minimum length 1
    public void ValidateName_ShouldAcceptValidNames(string name)
    {
        // Act
        var result = MigrationGuard.ValidateName(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateName Should Reject Null")]
    [InlineData(null)]
    public void ValidateName_ShouldRejectNull(string? name)
    {
        // Act
        var result = MigrationGuard.ValidateName(name);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == MigrationOutcome.Codes.NameNull);
    }

    [Theory(DisplayName = "ValidateName Should Reject Empty Or Whitespace")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void ValidateName_ShouldRejectEmptyOrWhitespace(string name)
    {
        // Act
        var result = MigrationGuard.ValidateName(name);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == MigrationOutcome.Codes.NameNullOrWhitespace);
    }

    [Theory(DisplayName = "ValidateName Should Reject Names Exceeding Max Length")]
    [InlineData(257)] // MaxNameLength is 256
    [InlineData(500)]
    [InlineData(1000)]
    public void ValidateName_ShouldRejectNamesExceedingMaxLength(int length)
    {
        // Arrange
        var name = new string('a', length);

        // Act
        var result = MigrationGuard.ValidateName(name);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == MigrationOutcome.Codes.NameExceedsMaxLength);
    }

    [Fact(DisplayName = "ValidateName Should Accept Max Length")]
    public void ValidateName_ShouldAcceptMaxLength()
    {
        // Arrange
        var name = new string('a', MigrationConstant.Limits.MaxNameLength);

        // Act
        var result = MigrationGuard.ValidateName(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateName Should Use Custom Parameter Name")]
    public void ValidateName_ShouldUseCustomParameterName()
    {
        // Act
        var result = MigrationGuard.ValidateName(null, "customName");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customName"));
    }

    #endregion

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
        var result = MigrationGuard.ValidateOrder(order);

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
        var result = MigrationGuard.ValidateOrder(order);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == MigrationOutcome.Codes.OrderNegative);
    }

    [Fact(DisplayName = "ValidateOrder Should Use Custom Parameter Name")]
    public void ValidateOrder_ShouldUseCustomParameterName()
    {
        // Act
        var result = MigrationGuard.ValidateOrder(-1, "customOrder");

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
    [InlineData(1000)] // MaxMigrations is 1000
    public void ValidateCount_ShouldAcceptValidCounts(int count)
    {
        // Act
        var result = MigrationGuard.ValidateCount(count);

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
        var result = MigrationGuard.ValidateCount(count);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == MigrationOutcome.Codes.CountNegative);
    }

    [Theory(DisplayName = "ValidateCount Should Reject Counts Exceeding Max")]
    [InlineData(1001)] // MaxMigrations is 1000
    [InlineData(2000)]
    [InlineData(int.MaxValue)]
    public void ValidateCount_ShouldRejectCountsExceedingMax(int count)
    {
        // Act
        var result = MigrationGuard.ValidateCount(count);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == MigrationOutcome.Codes.CountExceedsMax);
    }

    [Fact(DisplayName = "ValidateCount Should Use Custom Parameter Name")]
    public void ValidateCount_ShouldUseCustomParameterName()
    {
        // Act
        var result = MigrationGuard.ValidateCount(-1, "customCount");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customCount"));
    }

    #endregion

    #region ValidateVersion

    [Theory(DisplayName = "ValidateVersion Should Accept Valid Versions")]
    [InlineData("20240101000000")]
    [InlineData("20241231235959")]
    [InlineData("20240229120000")] // Leap year
    public void ValidateVersion_ShouldAcceptValidVersions(string version)
    {
        // Act
        var result = MigrationGuard.ValidateVersion(version);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateVersion Should Reject Null")]
    [InlineData(null)]
    public void ValidateVersion_ShouldRejectNull(string? version)
    {
        // Act
        var result = MigrationGuard.ValidateVersion(version);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == MigrationOutcome.Codes.VersionNull);
    }

    [Theory(DisplayName = "ValidateVersion Should Reject Empty Or Whitespace")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void ValidateVersion_ShouldRejectEmptyOrWhitespace(string version)
    {
        // Act
        var result = MigrationGuard.ValidateVersion(version);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == MigrationOutcome.Codes.VersionNullOrWhitespace);
    }

    [Theory(DisplayName = "ValidateVersion Should Reject Invalid Formats")]
    [InlineData("2024-01-01")]       // Wrong format
    [InlineData("20240101")]         // Too short
    [InlineData("2024010100000")]    // 13 digits
    [InlineData("202401010000000")]  // 15 digits
    [InlineData("abcd")]             // Non-numeric
    [InlineData("2024010100000x")]   // Contains letter
    public void ValidateVersion_ShouldRejectInvalidFormats(string version)
    {
        // Act
        var result = MigrationGuard.ValidateVersion(version);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == MigrationOutcome.Codes.VersionInvalidFormat);
    }

    [Fact(DisplayName = "ValidateVersion Should Use Custom Parameter Name")]
    public void ValidateVersion_ShouldUseCustomParameterName()
    {
        // Act
        var result = MigrationGuard.ValidateVersion("invalid", "customVersion");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customVersion"));
    }

    #endregion

    #region ValidateChecksum

    [Theory(DisplayName = "ValidateChecksum Should Accept Valid SHA-256 Checksums")]
    [InlineData("a" + "000000000000000000000000000000000000000000000000000000000000000")] // 64 hex chars
    [InlineData("f" + "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF")] // All uppercase (63 Fs)
    [InlineData("1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef")] // Mixed case
    public void ValidateChecksum_ShouldAcceptValidChecksums(string checksum)
    {
        // Act
        var result = MigrationGuard.ValidateChecksum(checksum);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateChecksum Should Reject Null")]
    [InlineData(null)]
    public void ValidateChecksum_ShouldRejectNull(string? checksum)
    {
        // Act
        var result = MigrationGuard.ValidateChecksum(checksum);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == MigrationOutcome.Codes.ChecksumNull);
    }

    [Theory(DisplayName = "ValidateChecksum Should Reject Empty Or Whitespace")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void ValidateChecksum_ShouldRejectEmptyOrWhitespace(string checksum)
    {
        // Act
        var result = MigrationGuard.ValidateChecksum(checksum);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == MigrationOutcome.Codes.ChecksumNullOrWhitespace);
    }

    [Theory(DisplayName = "ValidateChecksum Should Reject Invalid Formats")]
    [InlineData("abc")] // Too short
    [InlineData("g" + "000000000000000000000000000000000000000000000000000000000000000")] // Invalid hex char
    [InlineData("a" + "00000000000000000000000000000000000000000000000000000000000000")] // 63 chars
    [InlineData("a" + "0000000000000000000000000000000000000000000000000000000000000000")] // 65 chars
    [InlineData("ABCDEFGH")] // Non-hex
    public void ValidateChecksum_ShouldRejectInvalidFormats(string checksum)
    {
        // Act
        var result = MigrationGuard.ValidateChecksum(checksum);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == MigrationOutcome.Codes.ChecksumInvalidFormat);
    }

    [Fact(DisplayName = "ValidateChecksum Should Use Custom Parameter Name")]
    public void ValidateChecksum_ShouldUseCustomParameterName()
    {
        // Act
        var result = MigrationGuard.ValidateChecksum("invalid", "customChecksum");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customChecksum"));
    }

    #endregion

    #region ValidateMigration

    [Fact(DisplayName = "ValidateMigration Should Succeed When All Valid")]
    public void ValidateMigration_ShouldSucceedWhenAllValid()
    {
        // Act
        var result = MigrationGuard.ValidateMigration(
            "Migration_20240101000000",
            100,
            "20240101000000",
            "a" + new string('0', 63));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateMigration Should Fail When Name Invalid")]
    public void ValidateMigration_ShouldFailWhenNameInvalid()
    {
        // Act
        var result = MigrationGuard.ValidateMigration(
            "", // Invalid name
            100,
            "20240101000000",
            "a" + new string('0', 63));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == MigrationOutcome.Codes.NameNullOrWhitespace);
    }

    [Fact(DisplayName = "ValidateMigration Should Fail When Order Invalid")]
    public void ValidateMigration_ShouldFailWhenOrderInvalid()
    {
        // Act
        var result = MigrationGuard.ValidateMigration(
            "Migration_20240101000000",
            -1, // Invalid order
            "20240101000000",
            "a" + new string('0', 63));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == MigrationOutcome.Codes.OrderNegative);
    }

    [Fact(DisplayName = "ValidateMigration Should Fail When Version Invalid")]
    public void ValidateMigration_ShouldFailWhenVersionInvalid()
    {
        // Act
        var result = MigrationGuard.ValidateMigration(
            "Migration_20240101000000",
            100,
            "invalid", // Invalid version
            "a" + new string('0', 63));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == MigrationOutcome.Codes.VersionInvalidFormat);
    }

    [Fact(DisplayName = "ValidateMigration Should Fail When Checksum Invalid")]
    public void ValidateMigration_ShouldFailWhenChecksumInvalid()
    {
        // Act
        var result = MigrationGuard.ValidateMigration(
            "Migration_20240101000000",
            100,
            "20240101000000",
            "invalid"); // Invalid checksum

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == MigrationOutcome.Codes.ChecksumInvalidFormat);
    }

    [Fact(DisplayName = "ValidateMigration Should Combine Multiple Errors")]
    public void ValidateMigration_ShouldCombineMultipleErrors()
    {
        // Act
        var result = MigrationGuard.ValidateMigration(
            "", // Invalid name
            -1, // Invalid order
            "invalid", // Invalid version
            "invalid"); // Invalid checksum

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(4);
    }

    [Fact(DisplayName = "ValidateMigration Should Skip Checksum Validation When Null")]
    public void ValidateMigration_ShouldSkipChecksumValidationWhenNull()
    {
        // Act
        var result = MigrationGuard.ValidateMigration(
            "Migration_20240101000000",
            100,
            "20240101000000",
            null); // Null checksum - should be skipped

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion
}