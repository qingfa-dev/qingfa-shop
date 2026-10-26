using Core.Common.Domain.ValueObjects.Address;

namespace Core.Unit.Tests.Common.Domain.ValueObjects.Address;

public class AddressConstantSpec
{
    [Fact]
    public void Line1_MaxLength_ShouldBe256()
    {
        // Arrange & Act:
        var maxLength = AddressConstant.Constraints.Line1.MaxLength;

        // Assert:
        maxLength.Should().Be(256);
    }

    [Fact]
    public void Line2_MaxLength_ShouldBe256()
    {
        // Arrange & Act:
        var maxLength = AddressConstant.Constraints.Line2.MaxLength;

        // Assert:
        maxLength.Should().Be(256);
    }

    [Fact]
    public void City_MaxLength_ShouldBe128()
    {
        // Arrange & Act:
        var maxLength = AddressConstant.Constraints.City.MaxLength;

        // Assert:
        maxLength.Should().Be(128);
    }

    [Fact]
    public void Region_MaxLength_ShouldBe128()
    {
        // Arrange & Act:
        var maxLength = AddressConstant.Constraints.Region.MaxLength;

        // Assert:
        maxLength.Should().Be(128);
    }

    [Fact]
    public void CountryCode_MaxLength_ShouldBe2()
    {
        // Arrange & Act:
        var maxLength = AddressConstant.Constraints.CountryCode.MaxLength;

        // Assert:
        maxLength.Should().Be(2);
    }

    [Fact]
    public void Defaults_CountryCodeNormalization_ShouldBeUppercase()
    {
        // Arrange & Act:
        var normalization = AddressConstant.Defaults.CountryCodeNormalization;

        // Assert:
        normalization.Should().Be("Uppercase");
    }

    [Fact]
    public void Patterns_CountryCode_ShouldRequireTwoUppercaseLetters()
    {
        // Arrange & Act:
        var pattern = AddressConstant.Patterns.CountryCode;

        // Assert:
        pattern.Should().Be("^[A-Z]{2}$");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("CA")]
    [InlineData("GB")]
    public void Patterns_CountryCode_ValidCodes_ShouldMatch(string code)
    {
        // Arrange & Act:
        var matches = System.Text.RegularExpressions.Regex.IsMatch(code, AddressConstant.Patterns.CountryCode);

        // Assert:
        matches.Should().BeTrue();
    }

    [Theory]
    [InlineData("us")]
    [InlineData("USA")]
    [InlineData("U")]
    [InlineData("1A")]
    [InlineData("")]
    public void Patterns_CountryCode_InvalidCodes_ShouldNotMatch(string code)
    {
        // Arrange & Act:
        var matches = System.Text.RegularExpressions.Regex.IsMatch(code, AddressConstant.Patterns.CountryCode);

        // Assert:
        matches.Should().BeFalse();
    }
}