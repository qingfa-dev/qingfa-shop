using Core.Common.Domain.ValueObjects.Address;

namespace Core.Tests.Domain.ValueObjects.Address;

public class PostalCodeSpec
{
    [Fact]
    public void Create_ValidInput_ShouldTrimAndUppercaseValue()
    {
        // Arrange & Act:
        var result = PostalCode.Create(" a1b 2c3 ");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("A1B 2C3");
    }

    [Fact]
    public void Create_MissingValue_ShouldFailWithValueRequired()
    {
        // Arrange & Act:
        var result = PostalCode.Create(null);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("PostalCode.Value.Required");
    }

    [Fact]
    public void Create_LowercaseValue_ShouldNormalizeToUppercase()
    {
        // Arrange & Act:
        var result = PostalCode.Create("m5v 2t6");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("M5V 2T6");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankValue_ShouldFailWithValueRequired(string? value)
    {
        // Arrange & Act:
        var result = PostalCode.Create(value);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("PostalCode.Value.Required");
    }

    [Fact]
    public void Create_ValueExceedingMaxLength_ShouldFailWithValueTooLong()
    {
        // Arrange:
        var value = new string('A', PostalCodeConstant.Constraints.Code.MaxLength + 1);

        // Act:
        var result = PostalCode.Create(value);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("PostalCode.Value.TooLong");
    }

    [Fact]
    public void Create_ExactMaxLengthValue_ShouldSucceed()
    {
        // Arrange:
        var value = "A1B2C3D4E5";

        // Act:
        var result = PostalCode.Create(value);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void Create_InvalidCharset_ShouldFailWithValueInvalid()
    {
        // Arrange & Act:
        var result = PostalCode.Create("!");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("PostalCode.Value.Invalid");
    }

    [Fact]
    public void RecordEquality_SameValue_ShouldBeEqual()
    {
        // Arrange:
        var first = PostalCode.Create("M5V 2T6").Value;
        var second = PostalCode.Create("M5V 2T6").Value;

        // Act & Assert:
        first.Should().Be(second);
    }

    [Fact]
    public void RecordEquality_DifferentValues_ShouldNotBeEqual()
    {
        // Arrange:
        var first = PostalCode.Create("M5V 2T6").Value;
        var second = PostalCode.Create("90210").Value;

        // Act & Assert:
        first.Should().NotBe(second);
    }
}