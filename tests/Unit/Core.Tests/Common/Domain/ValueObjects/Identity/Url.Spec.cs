using Core.Common.Domain.ValueObjects.Identity;

namespace Core.Tests.Common.Domain.ValueObjects.Identity;

public class UrlSpec
{
    [Fact]
    public void Create_AbsoluteHttpUrl_ShouldTrimValue()
    {
        // Arrange & Act
        var result = Url.Create(" https://example.com ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("https://example.com");
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com")]
    public void Create_HttpOrHttps_ShouldSucceed(string value)
    {
        // Arrange & Act
        var result = Url.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("HTTPS://EXAMPLE.COM")]
    [InlineData("HTTP://Example.COM/Path")]
    public void Create_UppercaseSchemeAndHost_ShouldSucceed(string value)
    {
        // Arrange & Act
        var result = Url.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void Create_MissingValue_ShouldFailWithValueRequired()
    {
        // Arrange & Act
        var result = Url.Create(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Url.Value.Required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyValue_ShouldFailWithValueRequired(string? value)
    {
        // Arrange & Act
        var result = Url.Create(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Url.Value.Required");
    }

    [Fact]
    public void Create_OverLimit_ShouldFailWithValueTooLong()
    {
        // Arrange
        var value = "https://example.com/" + new string('a', 2049 - 20);

        // Act
        var result = Url.Create(value);

        // Assert
        value.Length.Should().Be(2049);
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Url.Value.TooLong");
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData("/relative/path")]
    [InlineData("ftp://example.com")]
    public void Create_InvalidSchemeOrRelative_ShouldFailWithValueInvalid(string value)
    {
        // Arrange & Act
        var result = Url.Create(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Url.Value.Invalid");
    }
}