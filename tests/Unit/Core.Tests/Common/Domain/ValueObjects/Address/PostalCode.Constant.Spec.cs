using Core.Common.Domain.ValueObjects.Address;

namespace Core.Tests.Domain.ValueObjects.Address;

public class PostalCodeConstantSpec
{
    [Fact]
    public void Constraints_MinLength_ShouldBe1()
    {
        // Arrange & Act:
        var minLength = PostalCodeConstant.Constraints.Code.MinLength;

        // Assert:
        minLength.Should().Be(1);
    }

    [Fact]
    public void Constraints_MaxLength_ShouldBe10()
    {
        // Arrange & Act:
        var maxLength = PostalCodeConstant.Constraints.Code.MaxLength;

        // Assert:
        maxLength.Should().Be(10);
    }

    [Fact]
    public void Defaults_ValueNormalization_ShouldBeUppercase()
    {
        // Arrange & Act:
        var normalization = PostalCodeConstant.Defaults.ValueNormalization;

        // Assert:
        normalization.Should().Be("Uppercase");
    }

    [Fact]
    public void Patterns_Code_ShouldHaveExpectedValue()
    {
        // Arrange & Act:
        var pattern = PostalCodeConstant.Patterns.Code;

        // Assert:
        pattern.Should().Be("^[A-Za-z0-9][A-Za-z0-9 -]{0,9}$");
    }

    [Theory]
    [InlineData("M5V 2T6")]
    [InlineData("90210")]
    [InlineData("A1B2C3D4E5")]
    public void Patterns_Code_ValidValues_ShouldMatch(string value)
    {
        // Arrange & Act:
        var matches = System.Text.RegularExpressions.Regex.IsMatch(value, PostalCodeConstant.Patterns.Code);

        // Assert:
        matches.Should().BeTrue();
    }

    [Theory]
    [InlineData("!")]
    [InlineData(" A123")]
    public void Patterns_Code_InvalidValues_ShouldNotMatch(string value)
    {
        // Arrange & Act:
        var matches = System.Text.RegularExpressions.Regex.IsMatch(value, PostalCodeConstant.Patterns.Code);

        // Assert:
        matches.Should().BeFalse();
    }
}