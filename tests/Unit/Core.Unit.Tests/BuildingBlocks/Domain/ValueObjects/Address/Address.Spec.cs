using Core.BuildingBlocks.Domain.ValueObjects.Address;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Address;

public class AddressSpec
{
    [Fact]
    public void Create_ValidInput_ShouldTrimFieldsAndNormalizeCountryCode()
    {
        // Arrange:
        var postalCode = PostalCode.Create("M5V 2T6").Value;

        // Act:
        var result = global::Core.BuildingBlocks.Domain.ValueObjects.Address.Address.Create(
            " 12 Main St ",
            null,
            " Toronto ",
            " ON ",
            postalCode,
            "ca");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Line1.Should().Be("12 Main St");
        result.Value.City.Should().Be("Toronto");
        result.Value.Region.Should().Be("ON");
        result.Value.CountryCode.Should().Be("CA");
    }

    [Fact]
    public void Create_MissingLine1_ShouldFailWithLine1Required()
    {
        // Arrange:
        var postalCode = PostalCode.Create("M5V 2T6").Value;

        // Act:
        var result = global::Core.BuildingBlocks.Domain.ValueObjects.Address.Address.Create(
            null,
            null,
            "Toronto",
            null,
            postalCode,
            "CA");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Address.Line1.Required");
    }

    [Fact]
    public void RecordEquality_ShouldCompareAllAddressComponents()
    {
        // Arrange:
        var postalCode = PostalCode.Create("M5V 2T6").Value;

        // Act:
        var address = global::Core.BuildingBlocks.Domain.ValueObjects.Address.Address.Create("12 Main St", null, "Toronto", "ON", postalCode, "CA").Value;
        var equivalent = global::Core.BuildingBlocks.Domain.ValueObjects.Address.Address.Create("12 Main St", null, "Toronto", "ON", postalCode, "CA").Value;
        var changed = global::Core.BuildingBlocks.Domain.ValueObjects.Address.Address.Create("12 Main St", null, "Toronto", "ON", postalCode, "US").Value;

        // Assert:
        address.Should().Be(equivalent);
        address.Should().NotBe(changed);
    }

    [Fact]
    public void Create_NullLine2_ShouldSucceedWithNullLine2()
    {
        // Arrange:
        var postalCode = PostalCode.Create("M5V 2T6").Value;

        // Act:
        var result = global::Core.BuildingBlocks.Domain.ValueObjects.Address.Address.Create(
            "12 Main St",
            null,
            "Toronto",
            null,
            postalCode,
            "CA");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Line2.Should().BeNull();
    }

    [Fact]
    public void Create_ProvidedLine2_ShouldTrimLine2()
    {
        // Arrange:
        var postalCode = PostalCode.Create("M5V 2T6").Value;

        // Act:
        var result = global::Core.BuildingBlocks.Domain.ValueObjects.Address.Address.Create(
            "12 Main St",
            "  Apt 4B ",
            "Toronto",
            null,
            postalCode,
            "CA");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Line2.Should().Be("Apt 4B");
    }

    [Fact]
    public void Create_NullRegion_ShouldSucceedWithNullRegion()
    {
        // Arrange:
        var postalCode = PostalCode.Create("M5V 2T6").Value;

        // Act:
        var result = global::Core.BuildingBlocks.Domain.ValueObjects.Address.Address.Create(
            "12 Main St",
            null,
            "Toronto",
            null,
            postalCode,
            "CA");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Region.Should().BeNull();
    }

    [Fact]
    public void Create_ProvidedRegion_ShouldTrimRegion()
    {
        // Arrange:
        var postalCode = PostalCode.Create("M5V 2T6").Value;

        // Act:
        var result = global::Core.BuildingBlocks.Domain.ValueObjects.Address.Address.Create(
            "12 Main St",
            null,
            "Toronto",
            "  ON  ",
            postalCode,
            "CA");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Region.Should().Be("ON");
    }

    [Fact]
    public void Create_LowercaseCountryCode_ShouldNormalizeToUppercase()
    {
        // Arrange:
        var postalCode = PostalCode.Create("M5V 2T6").Value;

        // Act:
        var result = global::Core.BuildingBlocks.Domain.ValueObjects.Address.Address.Create(
            "12 Main St",
            null,
            "Toronto",
            null,
            postalCode,
            "us");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.CountryCode.Should().Be("US");
    }

    [Fact]
    public void Create_NullCity_ShouldFailWithCityRequired()
    {
        // Arrange:
        var postalCode = PostalCode.Create("M5V 2T6").Value;

        // Act:
        var result = global::Core.BuildingBlocks.Domain.ValueObjects.Address.Address.Create(
            "12 Main St",
            null,
            null,
            null,
            postalCode,
            "CA");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Address.City.Required");
    }

    [Fact]
    public void Create_NullCountryCode_ShouldFailWithCountryCodeInvalid()
    {
        // Arrange:
        var postalCode = PostalCode.Create("M5V 2T6").Value;

        // Act:
        var result = global::Core.BuildingBlocks.Domain.ValueObjects.Address.Address.Create(
            "12 Main St",
            null,
            "Toronto",
            null,
            postalCode,
            null);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Address.CountryCode.Invalid");
    }

    [Fact]
    public void Create_AlreadyUppercaseCountryCode_ShouldPreserveValue()
    {
        // Arrange:
        var postalCode = PostalCode.Create("M5V 2T6").Value;

        // Act:
        var result = global::Core.BuildingBlocks.Domain.ValueObjects.Address.Address.Create(
            "12 Main St",
            null,
            "Toronto",
            null,
            postalCode,
            "US");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.CountryCode.Should().Be("US");
    }
}