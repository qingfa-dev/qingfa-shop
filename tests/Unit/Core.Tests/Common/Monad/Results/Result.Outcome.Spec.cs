namespace Core.Tests.Common.Monad.Results;

/// <summary>Tests for <see cref="ResultOutcome.Failure"/> factory methods.</summary>
[Trait("Category", "Unit")]
public class ResultOutcomeSpec
{
    #region Status

    /// <summary>Status error factories should identify their status kind.</summary>
    [Theory]
    [MemberData(nameof(StatusFactoryCases))]
    public void InvalidStatusErrorFactories_ShouldIdentifyStatusKind(Error error, string expectedCode)
    {
        // Arrange
        // Act
        // Assert
        error.Code.Should().Be(expectedCode);
    }

    public static TheoryData<Error, string> StatusFactoryCases()
    {
        var data = new TheoryData<Error, string>();
        data.Add(ResultOutcome.Failure.Status.InvalidStatusCode(99), ResultOutcome.Failure.Status.InvalidStatusCode(99).Code);
        data.Add(ResultOutcome.Failure.Status.InvalidSuccessStatus(404), ResultOutcome.Failure.Status.InvalidSuccessStatus(404).Code);
        data.Add(ResultOutcome.Failure.Status.InvalidFailureStatus(200), ResultOutcome.Failure.Status.InvalidFailureStatus(200).Code);
        return data;
    }

    #endregion

    #region Result and Errors

    /// <summary>Result and error validation factories should expose their codes.</summary>
    [Theory]
    [MemberData(nameof(ResultAndErrorsFactoryCases))]
    public void ResultAndErrorValidationFactories_ShouldExposeTheirCodes(Error error, string expectedCode)
    {
        // Arrange
        // Act
        // Assert
        error.Code.Should().Be(expectedCode);
    }

    public static TheoryData<Error, string> ResultAndErrorsFactoryCases()
    {
        var data = new TheoryData<Error, string>();
        data.Add(ResultOutcome.Failure.Errors.MixedStatusCodes, ResultOutcome.Failure.Errors.MixedStatusCodes.Code);
        data.Add(ResultOutcome.Failure.Errors.Empty, ResultOutcome.Failure.Errors.Empty.Code);
        data.Add(ResultOutcome.Failure.Errors.ExceedsMaxCount(50), ResultOutcome.Failure.Errors.ExceedsMaxCount(50).Code);
        return data;
    }

    #endregion

    #region Metadata and Value

    /// <summary>Metadata and value error factories should format arguments.</summary>
    [Theory]
    [MemberData(nameof(MetadataAndValueFactoryCases))]
    public void MetadataAndValueErrorFactories_ShouldFormatArguments(
        Error error,
        string expectedCode,
        string? expectedMessageFragment)
    {
        // Arrange
        // Act
        // Assert
        error.Code.Should().Be(expectedCode);

        if (expectedMessageFragment is not null)
        {
            error.Message.Should().Contain(expectedMessageFragment);
        }
    }

    public static TheoryData<Error, string, string?> MetadataAndValueFactoryCases()
    {
        var data = new TheoryData<Error, string, string?>();
        var nullValueError = ResultOutcome.Failure.Metadata.NullValue("trace.id");
        data.Add(
            nullValueError,
            nullValueError.Code,
            "trace.id");
        var emptyKeyError = ResultOutcome.Failure.Metadata.EmptyKey;
        data.Add(emptyKeyError, emptyKeyError.Code, null);
        var exceedsMaxEntriesError = ResultOutcome.Failure.Metadata.ExceedsMaxEntries(50);
        data.Add(exceedsMaxEntriesError, exceedsMaxEntriesError.Code, null);
        var requiredError = ResultOutcome.Failure.Value.Required;
        data.Add(requiredError, requiredError.Code, null);
        var notAllowedError = ResultOutcome.Failure.Value.NotAllowed;
        data.Add(notAllowedError, notAllowedError.Code, null);
        return data;
    }

    #endregion
}