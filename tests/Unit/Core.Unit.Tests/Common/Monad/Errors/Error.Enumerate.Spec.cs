namespace Core.Unit.Tests.Common.Monad.Errors;

/// <summary>Tests for <see cref="ErrorSeverity"/> enum values.</summary>
/// <remarks>
/// Verifies that severity levels have stable wire values for serialization and comparison.
/// </remarks>
[Trait("Category", "Unit")]
public class ErrorSeveritySpec
{
    #region Wire values

    [Theory]
    [InlineData(ErrorSeverity.Info, 0)]
    [InlineData(ErrorSeverity.Warning, 1)]
    [InlineData(ErrorSeverity.Error, 2)]
    [InlineData(ErrorSeverity.Critical, 3)]
    public void ErrorSeverity_ShouldHaveStableWireValues(ErrorSeverity severity, int expected)
    {
        // Arrange
        // Act
        var actual = (int)severity;

        // Assert
        actual.Should().Be(expected);
    }

    #endregion
}