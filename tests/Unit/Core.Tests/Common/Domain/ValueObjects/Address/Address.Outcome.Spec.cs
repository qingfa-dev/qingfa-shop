using Core.Common.Domain.ValueObjects.Address;

namespace Core.Tests.Common.Domain.ValueObjects.Address;

public class AddressOutcomeSpec
{
    [Fact]
    public void Line1Required_ShouldHaveExpectedCode()
    {
        // Arrange & Act:
        var error = AddressOutcome.Failure.Line1Required;

        // Assert:
        error.Code.Should().Be("Address.Line1.Required");
    }

    [Fact]
    public void Line1TooLong_ShouldHaveExpectedCode()
    {
        // Arrange & Act:
        var error = AddressOutcome.Failure.Line1TooLong;

        // Assert:
        error.Code.Should().Be("Address.Line1.TooLong");
    }

    [Fact]
    public void Line2TooLong_ShouldHaveExpectedCode()
    {
        // Arrange & Act:
        var error = AddressOutcome.Failure.Line2TooLong;

        // Assert:
        error.Code.Should().Be("Address.Line2.TooLong");
    }

    [Fact]
    public void CityRequired_ShouldHaveExpectedCode()
    {
        // Arrange & Act:
        var error = AddressOutcome.Failure.CityRequired;

        // Assert:
        error.Code.Should().Be("Address.City.Required");
    }

    [Fact]
    public void CityTooLong_ShouldHaveExpectedCode()
    {
        // Arrange & Act:
        var error = AddressOutcome.Failure.CityTooLong;

        // Assert:
        error.Code.Should().Be("Address.City.TooLong");
    }

    [Fact]
    public void RegionTooLong_ShouldHaveExpectedCode()
    {
        // Arrange & Act:
        var error = AddressOutcome.Failure.RegionTooLong;

        // Assert:
        error.Code.Should().Be("Address.Region.TooLong");
    }

    [Fact]
    public void PostalCodeInvalid_ShouldHaveExpectedCode()
    {
        // Arrange & Act:
        var error = AddressOutcome.Failure.PostalCodeInvalid;

        // Assert:
        error.Code.Should().Be("Address.PostalCode.Invalid");
    }

    [Fact]
    public void CountryCodeInvalid_ShouldHaveExpectedCode()
    {
        // Arrange & Act:
        var error = AddressOutcome.Failure.CountryCodeInvalid;

        // Assert:
        error.Code.Should().Be("Address.CountryCode.Invalid");
    }
}