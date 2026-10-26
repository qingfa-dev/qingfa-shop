namespace Core.Unit.Tests.Common.Monad.Metadata;

/// <summary>
/// Tests for <see cref="MetadataConstant"/> keyword constants.
/// </summary>
/// <remarks>
/// Verifies that metadata keywords remain stable and preserve their declared wire keys.
/// </remarks>
[Trait("Category", "Unit")]
public class MetadataConstantSpec
{
    #region Keyword constants

    [Theory]
    [InlineData(MetadataConstant.Keyword.Timestamp, "Timestamp")]
    [InlineData(MetadataConstant.Keyword.CorrelationId, "CorrelationId")]
    [InlineData(MetadataConstant.Keyword.RequestId, "RequestId")]
    [InlineData(MetadataConstant.Keyword.CausationId, "CausationId")]
    [InlineData(MetadataConstant.Keyword.TenantId, "TenantId")]
    public void Keyword_ShouldPreserveStableWireKey(
        string keyword,
        string stableKey)
    {
        // Arrange

        // Act

        // Assert
        keyword.Should().Be(stableKey);
    }

    #endregion
}