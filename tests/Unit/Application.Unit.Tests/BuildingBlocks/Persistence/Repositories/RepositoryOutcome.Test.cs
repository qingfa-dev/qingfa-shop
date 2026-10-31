using Application.BuildingBlocks.Persistence.Repositories;

namespace Application.Unit.Tests.BuildingBlocks.Persistence.Repositories;

[Trait("Category", "Unit")]
public sealed class RepositoryOutcomeTest
{
    #region Failure Properties

    public static TheoryData<string, string, object[]> FailureTestCases()
    {
        var data = new TheoryData<string, string, object[]>
        {
            { "PageSizeTooSmall", RepositoryOutcome.Codes.PageSizeTooSmall, new object[] { "testParam" } },
            { "PageSizeTooLarge", RepositoryOutcome.Codes.PageSizeTooLarge, new object[] { "testParam" } },
            { "BatchSizeTooSmall", RepositoryOutcome.Codes.BatchSizeTooSmall, new object[] { "testParam" } },
            { "BatchSizeTooLarge", RepositoryOutcome.Codes.BatchSizeTooLarge, new object[] { "testParam" } },
            { "TableNameTooLong", RepositoryOutcome.Codes.TableNameTooLong, new object[] { "testParam" } },
            { "ColumnNameTooLong", RepositoryOutcome.Codes.ColumnNameTooLong, new object[] { "testParam" } },
            { "IndexNameTooLong", RepositoryOutcome.Codes.IndexNameTooLong, new object[] { "testParam" } },
            { "ConstraintNameTooLong", RepositoryOutcome.Codes.ConstraintNameTooLong, new object[] { "testParam" } },
            { "ParameterNameTooLong", RepositoryOutcome.Codes.ParameterNameTooLong, new object[] { "testParam" } },
            { "CommandTimeout", RepositoryOutcome.Codes.CommandTimeout, new object[] { "testParam" } },
            { "CommandCancelled", RepositoryOutcome.Codes.CommandCancelled, new object[] { "testParam" } },
            { "CommandInvalid", RepositoryOutcome.Codes.CommandInvalid, new object[] { "testParam", "testReason" } },
            { "ConnectionStringInvalid", RepositoryOutcome.Codes.ConnectionStringInvalid, new object[] { "testParam" } },
            { "PoolExhausted", RepositoryOutcome.Codes.PoolExhausted, new object[] { "testParam" } },
            { "ConnectionBroken", RepositoryOutcome.Codes.ConnectionBroken, new object[] { "testParam" } },
            { "SchemaMismatch", RepositoryOutcome.Codes.SchemaMismatch, new object[] { "testParam" } },
            { "TableMissing", RepositoryOutcome.Codes.TableMissing, new object[] { "testParam" } },
            { "ColumnMissing", RepositoryOutcome.Codes.ColumnMissing, new object[] { "testParam" } },
            { "IndexMissing", RepositoryOutcome.Codes.IndexMissing, new object[] { "testParam" } },
            { "ConstraintViolation", RepositoryOutcome.Codes.ConstraintViolation, new object[] { "testConstraint", "FK_Test" } },
            { "BulkInsertFailed", RepositoryOutcome.Codes.BulkInsertFailed, new object[] { "testParam", "testReason" } },
            { "BulkUpdateFailed", RepositoryOutcome.Codes.BulkUpdateFailed, new object[] { "testParam", "testReason" } },
            { "BulkDeleteFailed", RepositoryOutcome.Codes.BulkDeleteFailed, new object[] { "testParam", "testReason" } },
            { "BulkCancelled", RepositoryOutcome.Codes.BulkCancelled, new object[] { "testParam" } },
            { "BatchSizeExceeded", RepositoryOutcome.Codes.BatchSizeExceeded, new object[] { 1001, 1000, "testBatchSize" } },
            { "ConcurrencyTokenMissing", RepositoryOutcome.Codes.ConcurrencyTokenMissing, new object[] { "testParam" } },
            { "ConcurrencyTokenInvalid", RepositoryOutcome.Codes.ConcurrencyTokenInvalid, new object[] { "testParam" } },
            { "ProviderNotRegistered", RepositoryOutcome.Codes.ProviderNotRegistered, new object[] { "testProvider" } },
            { "ProviderNotSupported", RepositoryOutcome.Codes.ProviderNotSupported, new object[] { "testProvider" } },
            { "ProviderFeatureUnavailable", RepositoryOutcome.Codes.ProviderFeatureUnavailable, new object[] { "testFeature", "testProvider" } },
        };
        return data;
    }

    [Theory(DisplayName = "Failure Properties Should Return Errors With Correct Codes")]
    [MemberData(nameof(FailureTestCases))]
    public void Failure_ShouldReturnErrorWithCorrectCode(string propertyName, string expectedCode, object[] parameters)
    {
        // Arrange
        var method = typeof(RepositoryOutcome.Failure)
            .GetMethod(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        method.Should().NotBeNull($"Failure.{propertyName} should exist");

        // Act
        object error = method!.Invoke(null, parameters)!;

        // Assert
        ((Error)error).Code.Should().Be(expectedCode);
        foreach (var param in parameters)
        {
            if (param is string s && s != "testReason")
            {
                ((Error)error).Message.Should().Contain(s);
            }
        }
    }

    [Fact(DisplayName = "Failure Properties Should Return Validation Errors")]
    public void Failure_ShouldReturnValidationErrors()
    {
        // Act
        var error = RepositoryOutcome.Failure.PageSizeTooSmall("test");

        // Assert
        error.Severity.Should().Be(ErrorSeverity.Warning);
        error.Status.Should().Be(422); // Validation status
    }

    [Fact(DisplayName = "CommandInvalid Should Include Reason When Provided")]
    public void CommandInvalid_ShouldIncludeReasonWhenProvided()
    {
        // Act
        var error = RepositoryOutcome.Failure.CommandInvalid("testCommand", "Syntax error");

        // Assert
        error.Message.Should().Contain("Syntax error");
    }

    [Fact(DisplayName = "CommandInvalid Should Work Without Reason")]
    public void CommandInvalid_ShouldWorkWithoutReason()
    {
        // Act
        var error = RepositoryOutcome.Failure.CommandInvalid("testCommand");

        // Assert
        error.Code.Should().Be(RepositoryOutcome.Codes.CommandInvalid);
        error.Message.Should().Contain("testCommand");
    }

    [Fact(DisplayName = "ConstraintViolation Should Include Constraint Name When Provided")]
    public void ConstraintViolation_ShouldIncludeConstraintNameWhenProvided()
    {
        // Act
        var error = RepositoryOutcome.Failure.ConstraintViolation("testConstraint", "FK_Test");

        // Assert
        error.Message.Should().Contain("FK_Test");
    }

    [Fact(DisplayName = "ConstraintViolation Should Work Without Constraint Name")]
    public void ConstraintViolation_ShouldWorkWithoutConstraintName()
    {
        // Act
        var error = RepositoryOutcome.Failure.ConstraintViolation("testConstraint");

        // Assert
        error.Code.Should().Be(RepositoryOutcome.Codes.ConstraintViolation);
        error.Message.Should().Contain("testConstraint");
    }

    [Fact(DisplayName = "BatchSizeExceeded Should Include Requested And Max In Message")]
    public void BatchSizeExceeded_ShouldIncludeRequestedAndMaxInMessage()
    {
        // Act
        var error = RepositoryOutcome.Failure.BatchSizeExceeded(1001, 1000, "testBatchSize");

        // Assert
        error.Message.Should().Contain("1001");
        error.Message.Should().Contain("1000");
        error.Message.Should().Contain("testBatchSize");
    }

    [Fact(DisplayName = "ProviderFeatureUnavailable Should Include Feature And Provider In Message")]
    public void ProviderFeatureUnavailable_ShouldIncludeFeatureAndProviderInMessage()
    {
        // Act
        var error = RepositoryOutcome.Failure.ProviderFeatureUnavailable("BulkInsert", "MyProvider");

        // Assert
        error.Message.Should().Contain("BulkInsert");
        error.Message.Should().Contain("MyProvider");
    }

    #endregion

    #region Codes

    [Fact(DisplayName = "Codes Should Be Unique")]
    public void Codes_ShouldBeUnique()
    {
        // Arrange
        var codes = typeof(RepositoryOutcome.Codes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(c => c != null)
            .ToList();

        // Assert
        codes.Should().OnlyHaveUniqueItems();
    }

    [Fact(DisplayName = "Codes Should Follow Naming Convention")]
    public void Codes_ShouldFollowNamingConvention()
    {
        // Arrange
        var codes = typeof(RepositoryOutcome.Codes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(c => c != null)
            .ToList();

        // Assert
        codes.Should().AllSatisfy(c => c.Should().StartWith("Repository."));
    }

    #endregion
}