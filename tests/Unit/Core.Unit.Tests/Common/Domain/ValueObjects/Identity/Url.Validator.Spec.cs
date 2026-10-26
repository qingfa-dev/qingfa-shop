using Core.Common.Domain.ValueObjects.Identity;

namespace Core.Unit.Tests.Common.Domain.ValueObjects.Identity;

public class UrlValidatorSpec
{
    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com")]
    public void Validate_HttpOrHttps_ShouldSucceed(string value)
    {
        // Arrange
        var candidate = new Url(value);

        // Act
        var result = UrlValidator.Validate(candidate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingValue_ShouldFailWithValueRequired(string? value)
    {
        // Arrange
        var candidate = new Url(value);

        // Act
        var result = UrlValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Url.Value.Required");
    }

    [Fact]
    public void Validate_OverLimit_ShouldFailWithValueTooLong()
    {
        // Arrange
        var candidate = new Url("https://example.com/" + new string('a', 2049 - 20));

        // Act
        var result = UrlValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Url.Value.TooLong");
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("example.com")]
    [InlineData("/relative/path")]
    public void Validate_InvalidSchemeOrRelative_ShouldFailWithValueInvalid(string value)
    {
        // Arrange
        var candidate = new Url(value);

        // Act
        var result = UrlValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Url.Value.Invalid");
    }
}