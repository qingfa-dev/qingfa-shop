namespace Core.Unit.Tests.Common.Monad.Results;

/// <summary>Tests for <see cref="ResultExtension"/> metadata and status extensions.</summary>
[Trait("Category", "Unit")]
public class ResultExtensionSpec
{
    #region Metadata and Status

    /// <summary>Non-generic metadata and status extensions should return copies.</summary>
    [Theory]
    [InlineData("original", "updated")]
    [InlineData("first", "second")]
    public void NonGenericMetadataAndStatusExtensions_ShouldReturnCopies(
        string originalValue,
        string updatedValue)
    {
        // Arrange
        var original = Result.Ok()
            .WithMetadata("source", originalValue);

        // Act
        var copy = original
            .WithMetadata("source", updatedValue)
            .WithStatus(ResultConstant.StatusCode.Accepted);

        // Assert
        copy.Should().NotBeSameAs(original);
        copy.Metadata["source"].Should().Be(updatedValue);
        copy.StatusCode.Should().Be(ResultConstant.StatusCode.Accepted);
        original.Metadata["source"].Should().Be(originalValue);
        original.StatusCode.Should().Be(ResultConstant.StatusCode.Ok);
    }

    /// <summary>Generic metadata and status extensions should return copies.</summary>
    [Theory]
    [InlineData(5)]
    [InlineData(0)]
    [InlineData(-1)]
    public void GenericMetadataAndStatusExtensions_ShouldReturnCopies(int value)
    {
        // Arrange
        var original = Result<int>.Ok(value)
            .WithMetadata("source", "original");

        // Act
        var copy = original
            .WithMetadata("source", "updated")
            .WithStatus(ResultConstant.StatusCode.Accepted);

        // Assert
        copy.Should().NotBeSameAs(original);
        copy.Metadata["source"].Should().Be("updated");
        copy.StatusCode.Should().Be(ResultConstant.StatusCode.Accepted);
        copy.Value.Should().Be(value);
        original.Metadata["source"].Should().Be("original");
        original.StatusCode.Should().Be(ResultConstant.StatusCode.Ok);
    }

    #endregion

    #region Metadata Dictionary

    /// <summary>Dictionary metadata overloads should merge into copies.</summary>
    [Fact]
    public void MetadataDictionaryOverloads_ShouldMergeIntoCopies()
    {
        // Arrange
        var seed = new DefaultMetadata { ["seed"] = "s" };
        var genericSeed = new DefaultMetadata { ["seed"] = "s" };

        // Act
        var nonGeneric = Result.Ok().WithMetadata(seed);
        var generic = Result<int>.Ok(5).WithMetadata(genericSeed);

        // Assert
        nonGeneric.Metadata["seed"].Should().Be("s");
        generic.Metadata["seed"].Should().Be("s");
        generic.Value.Should().Be(5);
        seed.Keys.Should().ContainSingle().Which.Should().Be("seed");
        genericSeed.Keys.Should().ContainSingle().Which.Should().Be("seed");
    }

    #endregion

    #region Standard Metadata

    /// <summary>Standard metadata helpers should set their keys on both shapes.</summary>
    [Fact]
    public void StandardMetadataHelpers_ShouldSetTheirKeys()
    {
        // Arrange
        var nonGeneric = Result.Ok();
        var generic = Result<int>.Ok(7);

        // Act
        var stamped = nonGeneric
            .WithTraceId("trace-1")
            .WithTimestamp("ts-1")
            .WithResource("resource-1")
            .WithField("field-1")
            .WithAttempt(1);
        var genericStamped = generic
            .WithTraceId("trace-2")
            .WithTimestamp("ts-2")
            .WithResource("resource-2")
            .WithField("field-2")
            .WithAttempt(2);

        // Assert
        stamped.Metadata[ErrorConstant.Metadata.TraceId].Should().Be("trace-1");
        stamped.Metadata[ErrorConstant.Metadata.Timestamp].Should().Be("ts-1");
        stamped.Metadata[ErrorConstant.Metadata.Resource].Should().Be("resource-1");
        stamped.Metadata[ErrorConstant.Metadata.Field].Should().Be("field-1");
        stamped.Metadata[ErrorConstant.Metadata.Attempt].Should().Be(1);
        stamped.IsSuccess.Should().BeTrue();

        genericStamped.Metadata[ErrorConstant.Metadata.TraceId].Should().Be("trace-2");
        genericStamped.Metadata[ErrorConstant.Metadata.Timestamp].Should().Be("ts-2");
        genericStamped.Metadata[ErrorConstant.Metadata.Resource].Should().Be("resource-2");
        genericStamped.Metadata[ErrorConstant.Metadata.Field].Should().Be("field-2");
        genericStamped.Metadata[ErrorConstant.Metadata.Attempt].Should().Be(2);
        genericStamped.Value.Should().Be(7);
    }

    #endregion
}