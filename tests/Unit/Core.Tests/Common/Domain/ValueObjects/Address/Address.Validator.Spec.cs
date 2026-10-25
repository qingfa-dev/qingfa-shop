using Core.Common.Domain.ValueObjects.Address;

namespace Core.Tests.Domain.ValueObjects.Address;

public class AddressValidatorSpec
{
    private static PostalCode ValidPostalCode()
    {
        // Arrange & Act helper: returns an already-validated postal code.
        return PostalCode.Create("M5V 2T6").Value;
    }

    private static global::Core.Common.Domain.ValueObjects.Address.Address ValidAddress(
        string? line1 = "12 Main St",
        string? line2 = null,
        string? city = "Toronto",
        string? region = "ON",
        string? countryCode = "CA")
    {
        // Arrange helper: builds an already-normalized address around the given overrides.
        return new global::Core.Common.Domain.ValueObjects.Address.Address(
            line1,
            line2,
            city,
            region,
            ValidPostalCode(),
            countryCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingLine1_ShouldFailWithLine1Required(string? line1)
    {
        // Arrange:
        var address = ValidAddress(line1: line1);

        // Act:
        var result = AddressValidator.Validate(address);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Address.Line1.Required");
    }

    [Fact]
    public void Validate_Line1TooLong_ShouldFailWithLine1TooLong()
    {
        // Arrange:
        var address = ValidAddress(line1: new string('A', AddressConstant.Constraints.Line1.MaxLength + 1));

        // Act:
        var result = AddressValidator.Validate(address);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Address.Line1.TooLong");
    }

    [Fact]
    public void Validate_Line1AtMaxLength_ShouldSucceed()
    {
        // Arrange:
        var address = ValidAddress(line1: new string('A', AddressConstant.Constraints.Line1.MaxLength));

        // Act:
        var result = AddressValidator.Validate(address);

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullLine2_ShouldSucceed()
    {
        // Arrange:
        var address = ValidAddress(line2: null);

        // Act:
        var result = AddressValidator.Validate(address);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(address);
    }

    [Fact]
    public void Validate_Line2TooLong_ShouldFailWithLine2TooLong()
    {
        // Arrange:
        var address = ValidAddress(line2: new string('A', AddressConstant.Constraints.Line2.MaxLength + 1));

        // Act:
        var result = AddressValidator.Validate(address);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Address.Line2.TooLong");
    }

    [Fact]
    public void Validate_Line2AtMaxLength_ShouldSucceed()
    {
        // Arrange:
        var address = ValidAddress(line2: new string('A', AddressConstant.Constraints.Line2.MaxLength));

        // Act:
        var result = AddressValidator.Validate(address);

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingCity_ShouldFailWithCityRequired(string? city)
    {
        // Arrange:
        var address = ValidAddress(city: city);

        // Act:
        var result = AddressValidator.Validate(address);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Address.City.Required");
    }

    [Fact]
    public void Validate_CityTooLong_ShouldFailWithCityTooLong()
    {
        // Arrange:
        var address = ValidAddress(city: new string('A', AddressConstant.Constraints.City.MaxLength + 1));

        // Act:
        var result = AddressValidator.Validate(address);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Address.City.TooLong");
    }

    [Fact]
    public void Validate_NullRegion_ShouldSucceed()
    {
        // Arrange:
        var address = ValidAddress(region: null);

        // Act:
        var result = AddressValidator.Validate(address);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(address);
    }

    [Fact]
    public void Validate_RegionTooLong_ShouldFailWithRegionTooLong()
    {
        // Arrange:
        var address = ValidAddress(region: new string('A', AddressConstant.Constraints.Region.MaxLength + 1));

        // Act:
        var result = AddressValidator.Validate(address);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Address.Region.TooLong");
    }

    [Fact]
    public void Validate_RegionAtMaxLength_ShouldSucceed()
    {
        // Arrange:
        var address = ValidAddress(region: new string('A', AddressConstant.Constraints.Region.MaxLength));

        // Act:
        var result = AddressValidator.Validate(address);

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!")]
    public void Validate_InvalidPostalCode_ShouldFailWithPostalCodeInvalid(string? postalValue)
    {
        // Arrange:
        var address = new global::Core.Common.Domain.ValueObjects.Address.Address(
            "12 Main St",
            null,
            "Toronto",
            null,
            new PostalCode(postalValue),
            "CA");

        // Act:
        var result = AddressValidator.Validate(address);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Address.PostalCode.Invalid");
    }

    [Theory]
    [InlineData("M5V 2T6")]
    [InlineData("90210")]
    public void Validate_ValidPostalCode_ShouldSucceed(string postalValue)
    {
        // Arrange:
        var address = new global::Core.Common.Domain.ValueObjects.Address.Address(
            "12 Main St",
            null,
            "Toronto",
            null,
            PostalCode.Create(postalValue).Value,
            "CA");

        // Act:
        var result = AddressValidator.Validate(address);

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("C")]
    [InlineData("USA")]
    [InlineData("ca")]
    [InlineData("1A")]
    public void Validate_InvalidCountryCode_ShouldFailWithCountryCodeInvalid(string? countryCode)
    {
        // Arrange:
        var address = ValidAddress(countryCode: countryCode);

        // Act:
        var result = AddressValidator.Validate(address);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Address.CountryCode.Invalid");
    }

    [Theory]
    [InlineData("CA")]
    [InlineData("US")]
    public void Validate_ValidCountryCode_ShouldSucceed(string countryCode)
    {
        // Arrange:
        var address = ValidAddress(countryCode: countryCode);

        // Act:
        var result = AddressValidator.Validate(address);

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidAddress_ShouldSucceedWithoutMutation()
    {
        // Arrange:
        var address = ValidAddress(
            line1: "12 Main St",
            line2: "Apt 4B",
            city: "Toronto",
            region: "ON",
            countryCode: "CA");

        // Act:
        var result = AddressValidator.Validate(address);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(address);
    }
}