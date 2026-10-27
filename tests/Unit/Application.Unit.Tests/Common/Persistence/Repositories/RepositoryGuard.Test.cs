using Application.Common.Persistence.Repositories;

namespace Application.Unit.Tests.Common.Persistence.Repositories;

[Trait("Category", "Unit")]
public sealed class RepositoryGuardTest
{
    #region ValidatePageSize

    [Theory(DisplayName = "ValidatePageSize Should Accept Valid Page Sizes")]
    [InlineData(1)]    // MinPageSize
    [InlineData(20)]   // DefaultPageSize
    [InlineData(100)]
    [InlineData(200)]  // MaxPageSize
    public void ValidatePageSize_ShouldAcceptValidPageSizes(int pageSize)
    {
        // Act
        var result = RepositoryGuard.ValidatePageSize(pageSize);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidatePageSize Should Reject Page Sizes Below Minimum")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void ValidatePageSize_ShouldRejectPageSizesBelowMinimum(int pageSize)
    {
        // Act
        var result = RepositoryGuard.ValidatePageSize(pageSize);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.PageSizeTooSmall);
    }

    [Theory(DisplayName = "ValidatePageSize Should Reject Page Sizes Above Maximum")]
    [InlineData(201)] // MaxPageSize is 200
    [InlineData(500)]
    [InlineData(int.MaxValue)]
    public void ValidatePageSize_ShouldRejectPageSizesAboveMaximum(int pageSize)
    {
        // Act
        var result = RepositoryGuard.ValidatePageSize(pageSize);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.PageSizeTooLarge);
    }

    [Fact(DisplayName = "ValidatePageSize Should Use Custom Parameter Name")]
    public void ValidatePageSize_ShouldUseCustomParameterName()
    {
        // Act
        var result = RepositoryGuard.ValidatePageSize(0, "customPageSize");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customPageSize"));
    }

    #endregion

    #region ValidateBatchSize

    [Theory(DisplayName = "ValidateBatchSize Should Accept Valid Batch Sizes")]
    [InlineData(1)]     // MinBatchSize
    [InlineData(100)]
    [InlineData(1000)]  // DefaultBatchSize
    [InlineData(5000)]  // MaxBatchSize
    public void ValidateBatchSize_ShouldAcceptValidBatchSizes(int batchSize)
    {
        // Act
        var result = RepositoryGuard.ValidateBatchSize(batchSize);

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
        var result = RepositoryGuard.ValidateBatchSize(batchSize);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.BatchSizeTooSmall);
    }

    [Theory(DisplayName = "ValidateBatchSize Should Reject Batch Sizes Above Maximum")]
    [InlineData(5001)] // MaxBatchSize is 5000
    [InlineData(10000)]
    [InlineData(int.MaxValue)]
    public void ValidateBatchSize_ShouldRejectBatchSizesAboveMaximum(int batchSize)
    {
        // Act
        var result = RepositoryGuard.ValidateBatchSize(batchSize);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.BatchSizeTooLarge);
    }

    [Fact(DisplayName = "ValidateBatchSize Should Use Custom Parameter Name")]
    public void ValidateBatchSize_ShouldUseCustomParameterName()
    {
        // Act
        var result = RepositoryGuard.ValidateBatchSize(0, "customBatchSize");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customBatchSize"));
    }

    #endregion

    #region ValidateTableName

    [Theory(DisplayName = "ValidateTableName Should Accept Valid Lengths")]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("Users")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")] // 128 chars
    public void ValidateTableName_ShouldAcceptValidLengths(string tableName)
    {
        // Act
        var result = RepositoryGuard.ValidateTableName(tableName);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateTableName Should Accept Null")]
    public void ValidateTableName_ShouldAcceptNull()
    {
        // Act
        var result = RepositoryGuard.ValidateTableName(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateTableName Should Reject Exceeding Max Length")]
    [InlineData(129)] // MaxTableNameLength is 128
    [InlineData(200)]
    [InlineData(500)]
    public void ValidateTableName_ShouldRejectExceedingMaxLength(int length)
    {
        // Arrange
        var tableName = new string('a', length);

        // Act
        var result = RepositoryGuard.ValidateTableName(tableName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.TableNameTooLong);
    }

    [Fact(DisplayName = "ValidateTableName Should Use Custom Parameter Name")]
    public void ValidateTableName_ShouldUseCustomParameterName()
    {
        // Act
        var result = RepositoryGuard.ValidateTableName(new string('a', 129), "customTableName");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customTableName"));
    }

    #endregion

    #region ValidateColumnName

    [Theory(DisplayName = "ValidateColumnName Should Accept Valid Lengths")]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("Id")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")] // 128 chars
    public void ValidateColumnName_ShouldAcceptValidLengths(string columnName)
    {
        // Act
        var result = RepositoryGuard.ValidateColumnName(columnName);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateColumnName Should Accept Null")]
    public void ValidateColumnName_ShouldAcceptNull()
    {
        // Act
        var result = RepositoryGuard.ValidateColumnName(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateColumnName Should Reject Exceeding Max Length")]
    [InlineData(129)]
    [InlineData(200)]
    [InlineData(500)]
    public void ValidateColumnName_ShouldRejectExceedingMaxLength(int length)
    {
        // Arrange
        var columnName = new string('a', length);

        // Act
        var result = RepositoryGuard.ValidateColumnName(columnName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.ColumnNameTooLong);
    }

    [Fact(DisplayName = "ValidateColumnName Should Use Custom Parameter Name")]
    public void ValidateColumnName_ShouldUseCustomParameterName()
    {
        // Act
        var result = RepositoryGuard.ValidateColumnName(new string('a', 129), "customColumnName");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customColumnName"));
    }

    #endregion

    #region ValidateIndexName

    [Theory(DisplayName = "ValidateIndexName Should Accept Valid Lengths")]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("IX_Users_Email")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")] // 128 chars
    public void ValidateIndexName_ShouldAcceptValidLengths(string indexName)
    {
        // Act
        var result = RepositoryGuard.ValidateIndexName(indexName);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateIndexName Should Accept Null")]
    public void ValidateIndexName_ShouldAcceptNull()
    {
        // Act
        var result = RepositoryGuard.ValidateIndexName(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateIndexName Should Reject Exceeding Max Length")]
    [InlineData(129)]
    [InlineData(200)]
    [InlineData(500)]
    public void ValidateIndexName_ShouldRejectExceedingMaxLength(int length)
    {
        // Arrange
        var indexName = new string('a', length);

        // Act
        var result = RepositoryGuard.ValidateIndexName(indexName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.IndexNameTooLong);
    }

    [Fact(DisplayName = "ValidateIndexName Should Use Custom Parameter Name")]
    public void ValidateIndexName_ShouldUseCustomParameterName()
    {
        // Act
        var result = RepositoryGuard.ValidateIndexName(new string('a', 129), "customIndexName");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customIndexName"));
    }

    #endregion

    #region ValidateConstraintName

    [Theory(DisplayName = "ValidateConstraintName Should Accept Valid Lengths")]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("FK_Users_Email")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")] // 128 chars
    public void ValidateConstraintName_ShouldAcceptValidLengths(string constraintName)
    {
        // Act
        var result = RepositoryGuard.ValidateConstraintName(constraintName);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateConstraintName Should Accept Null")]
    public void ValidateConstraintName_ShouldAcceptNull()
    {
        // Act
        var result = RepositoryGuard.ValidateConstraintName(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateConstraintName Should Reject Exceeding Max Length")]
    [InlineData(129)]
    [InlineData(200)]
    [InlineData(500)]
    public void ValidateConstraintName_ShouldRejectExceedingMaxLength(int length)
    {
        // Arrange
        var constraintName = new string('a', length);

        // Act
        var result = RepositoryGuard.ValidateConstraintName(constraintName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.ConstraintNameTooLong);
    }

    [Fact(DisplayName = "ValidateConstraintName Should Use Custom Parameter Name")]
    public void ValidateConstraintName_ShouldUseCustomParameterName()
    {
        // Act
        var result = RepositoryGuard.ValidateConstraintName(new string('a', 129), "customConstraintName");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customConstraintName"));
    }

    #endregion

    #region ValidateParameterName

    [Theory(DisplayName = "ValidateParameterName Should Accept Valid Lengths")]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("@param")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")] // 128 chars
    public void ValidateParameterName_ShouldAcceptValidLengths(string parameterName)
    {
        // Act
        var result = RepositoryGuard.ValidateParameterName(parameterName);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateParameterName Should Accept Null")]
    public void ValidateParameterName_ShouldAcceptNull()
    {
        // Act
        var result = RepositoryGuard.ValidateParameterName(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateParameterName Should Reject Exceeding Max Length")]
    [InlineData(129)]
    [InlineData(200)]
    [InlineData(500)]
    public void ValidateParameterName_ShouldRejectExceedingMaxLength(int length)
    {
        // Arrange
        var parameterName = new string('a', length);

        // Act
        var result = RepositoryGuard.ValidateParameterName(parameterName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.ParameterNameTooLong);
    }

    [Fact(DisplayName = "ValidateParameterName Should Use Custom Parameter Name")]
    public void ValidateParameterName_ShouldUseCustomParameterName()
    {
        // Act
        var result = RepositoryGuard.ValidateParameterName(new string('a', 129), "customParamName");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customParamName"));
    }

    #endregion

    #region ValidateIdentifiers

    [Fact(DisplayName = "ValidateIdentifiers Should Succeed When All Valid")]
    public void ValidateIdentifiers_ShouldSucceedWhenAllValid()
    {
        // Act
        var result = RepositoryGuard.ValidateIdentifiers(
            "Users",
            "Email",
            "IX_Users_Email",
            "FK_Users_Email",
            "@param");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateIdentifiers Should Combine Multiple Errors")]
    public void ValidateIdentifiers_ShouldCombineMultipleErrors()
    {
        // Act
        var result = RepositoryGuard.ValidateIdentifiers(
            new string('a', 129), // tableName too long
            new string('a', 129), // columnName too long
            new string('a', 129), // indexName too long
            new string('a', 129), // constraintName too long
            new string('a', 129)); // parameterName too long

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(5);
    }

    [Fact(DisplayName = "ValidateIdentifiers Should Accept Null For All")]
    public void ValidateIdentifiers_ShouldAcceptNullForAll()
    {
        // Act
        var result = RepositoryGuard.ValidateIdentifiers(
            null, null, null, null, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region ValidateCommandTimeout

    [Theory(DisplayName = "ValidateCommandTimeout Should Delegate To TransactionGuard")]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(300)]
    public void ValidateCommandTimeout_ShouldDelegateToTransactionGuard(int timeoutSeconds)
    {
        // Act
        var result = RepositoryGuard.ValidateCommandTimeout(timeoutSeconds);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateCommandTimeout Should Return Failure For Invalid Timeout")]
    public void ValidateCommandTimeout_ShouldReturnFailureForInvalidTimeout()
    {
        // Act
        var result = RepositoryGuard.ValidateCommandTimeout(0);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Command Error Factories

    [Fact(DisplayName = "CommandTimeout Should Return Failed Result")]
    public void CommandTimeout_ShouldReturnFailedResult()
    {
        // Act
        var result = RepositoryGuard.CommandTimeout("myCommand");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.CommandTimeout);
    }

    [Fact(DisplayName = "CommandCancelled Should Return Failed Result")]
    public void CommandCancelled_ShouldReturnFailedResult()
    {
        // Act
        var result = RepositoryGuard.CommandCancelled("myCommand");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.CommandCancelled);
    }

    [Fact(DisplayName = "CommandInvalid Should Return Failed Result With Reason")]
    public void CommandInvalid_ShouldReturnFailedResultWithReason()
    {
        // Act
        var result = RepositoryGuard.CommandInvalid("myCommand", "Syntax error");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Code == RepositoryOutcome.Codes.CommandInvalid &&
            e.Message.Contains("Syntax error"));
    }

    [Fact(DisplayName = "CommandInvalid Should Return Failed Result Without Reason")]
    public void CommandInvalid_ShouldReturnFailedResultWithoutReason()
    {
        // Act
        var result = RepositoryGuard.CommandInvalid("myCommand");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.CommandInvalid);
    }

    #endregion

    #region Connection Error Factories

    [Fact(DisplayName = "ConnectionStringInvalid Should Return Failed Result")]
    public void ConnectionStringInvalid_ShouldReturnFailedResult()
    {
        // Act
        var result = RepositoryGuard.ConnectionStringInvalid("myConnectionString");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.ConnectionStringInvalid);
    }

    [Fact(DisplayName = "PoolExhausted Should Return Failed Result")]
    public void PoolExhausted_ShouldReturnFailedResult()
    {
        // Act
        var result = RepositoryGuard.PoolExhausted("myPool");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.PoolExhausted);
    }

    [Fact(DisplayName = "ConnectionBroken Should Return Failed Result")]
    public void ConnectionBroken_ShouldReturnFailedResult()
    {
        // Act
        var result = RepositoryGuard.ConnectionBroken("myConnection");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.ConnectionBroken);
    }

    #endregion

    #region Schema Error Factories

    [Fact(DisplayName = "SchemaMismatch Should Return Failed Result")]
    public void SchemaMismatch_ShouldReturnFailedResult()
    {
        // Act
        var result = RepositoryGuard.SchemaMismatch("mySchema");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.SchemaMismatch);
    }

    [Fact(DisplayName = "TableMissing Should Return Failed Result")]
    public void TableMissing_ShouldReturnFailedResult()
    {
        // Act
        var result = RepositoryGuard.TableMissing("myTable");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.TableMissing);
    }

    [Fact(DisplayName = "ColumnMissing Should Return Failed Result")]
    public void ColumnMissing_ShouldReturnFailedResult()
    {
        // Act
        var result = RepositoryGuard.ColumnMissing("myColumn");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.ColumnMissing);
    }

    [Fact(DisplayName = "IndexMissing Should Return Failed Result")]
    public void IndexMissing_ShouldReturnFailedResult()
    {
        // Act
        var result = RepositoryGuard.IndexMissing("myIndex");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.IndexMissing);
    }

    [Fact(DisplayName = "ConstraintViolation Should Return Failed Result With Constraint Name")]
    public void ConstraintViolation_ShouldReturnFailedResultWithConstraintName()
    {
        // Act
        var result = RepositoryGuard.ConstraintViolation("myConstraint", "FK_Users_Email");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Code == RepositoryOutcome.Codes.ConstraintViolation &&
            e.Message.Contains("FK_Users_Email"));
    }

    [Fact(DisplayName = "ConstraintViolation Should Return Failed Result Without Constraint Name")]
    public void ConstraintViolation_ShouldReturnFailedResultWithoutConstraintName()
    {
        // Act
        var result = RepositoryGuard.ConstraintViolation("myConstraint");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.ConstraintViolation);
    }

    #endregion

    #region Bulk Operations Error Factories

    [Fact(DisplayName = "BulkInsertFailed Should Return Failed Result With Reason")]
    public void BulkInsertFailed_ShouldReturnFailedResultWithReason()
    {
        // Act
        var result = RepositoryGuard.BulkInsertFailed("myInsert", "Duplicate key");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Code == RepositoryOutcome.Codes.BulkInsertFailed &&
            e.Message.Contains("Duplicate key"));
    }

    [Fact(DisplayName = "BulkUpdateFailed Should Return Failed Result With Reason")]
    public void BulkUpdateFailed_ShouldReturnFailedResultWithReason()
    {
        // Act
        var result = RepositoryGuard.BulkUpdateFailed("myUpdate", "Concurrency conflict");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Code == RepositoryOutcome.Codes.BulkUpdateFailed &&
            e.Message.Contains("Concurrency conflict"));
    }

    [Fact(DisplayName = "BulkDeleteFailed Should Return Failed Result With Reason")]
    public void BulkDeleteFailed_ShouldReturnFailedResultWithReason()
    {
        // Act
        var result = RepositoryGuard.BulkDeleteFailed("myDelete", "Foreign key violation");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Code == RepositoryOutcome.Codes.BulkDeleteFailed &&
            e.Message.Contains("Foreign key violation"));
    }

    [Fact(DisplayName = "BulkCancelled Should Return Failed Result")]
    public void BulkCancelled_ShouldReturnFailedResult()
    {
        // Act
        var result = RepositoryGuard.BulkCancelled("myBulk");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.BulkCancelled);
    }

    [Theory(DisplayName = "ValidateBatchSizeExceeded Should Accept Valid Sizes")]
    [InlineData(100, 1000)]
    [InlineData(1000, 1000)] // Equal is valid
    [InlineData(1, 5000)]
    public void ValidateBatchSizeExceeded_ShouldAcceptValidSizes(int requested, int max)
    {
        // Act
        var result = RepositoryGuard.ValidateBatchSizeExceeded(requested, max);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateBatchSizeExceeded Should Reject Exceeded Sizes")]
    [InlineData(1001, 1000)]
    [InlineData(5001, 5000)]
    [InlineData(10000, 5000)]
    public void ValidateBatchSizeExceeded_ShouldRejectExceededSizes(int requested, int max)
    {
        // Act
        var result = RepositoryGuard.ValidateBatchSizeExceeded(requested, max);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Code == RepositoryOutcome.Codes.BatchSizeExceeded &&
            e.Message.Contains(requested.ToString()) &&
            e.Message.Contains(max.ToString()));
    }

    [Fact(DisplayName = "ValidateBatchSizeExceeded Should Use Custom Parameter Name")]
    public void ValidateBatchSizeExceeded_ShouldUseCustomParameterName()
    {
        // Act
        var result = RepositoryGuard.ValidateBatchSizeExceeded(1001, 1000, "customBatchSize");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customBatchSize"));
    }

    #endregion

    #region Concurrency Error Factories

    [Fact(DisplayName = "ConcurrencyTokenMissing Should Return Failed Result")]
    public void ConcurrencyTokenMissing_ShouldReturnFailedResult()
    {
        // Act
        var result = RepositoryGuard.ConcurrencyTokenMissing("myToken");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.ConcurrencyTokenMissing);
    }

    [Fact(DisplayName = "ConcurrencyTokenInvalid Should Return Failed Result")]
    public void ConcurrencyTokenInvalid_ShouldReturnFailedResult()
    {
        // Act
        var result = RepositoryGuard.ConcurrencyTokenInvalid("myToken");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == RepositoryOutcome.Codes.ConcurrencyTokenInvalid);
    }

    #endregion

    #region Provider Error Factories

    [Fact(DisplayName = "ProviderNotRegistered Should Return Failed Result")]
    public void ProviderNotRegistered_ShouldReturnFailedResult()
    {
        // Act
        var result = RepositoryGuard.ProviderNotRegistered("MyProvider");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Code == RepositoryOutcome.Codes.ProviderNotRegistered &&
            e.Message.Contains("MyProvider"));
    }

    [Fact(DisplayName = "ProviderNotSupported Should Return Failed Result")]
    public void ProviderNotSupported_ShouldReturnFailedResult()
    {
        // Act
        var result = RepositoryGuard.ProviderNotSupported("MyProvider");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Code == RepositoryOutcome.Codes.ProviderNotSupported &&
            e.Message.Contains("MyProvider"));
    }

    [Fact(DisplayName = "ProviderFeatureUnavailable Should Return Failed Result")]
    public void ProviderFeatureUnavailable_ShouldReturnFailedResult()
    {
        // Act
        var result = RepositoryGuard.ProviderFeatureUnavailable("BulkInsert", "MyProvider");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Code == RepositoryOutcome.Codes.ProviderFeatureUnavailable &&
            e.Message.Contains("BulkInsert") &&
            e.Message.Contains("MyProvider"));
    }

    #endregion
}