using Infrastructure.Persistence.EntityFramework.Converters;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Converters;

/// <summary>Unit tests for <see cref="DecimalValueConverter"/>.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class DecimalConverterTest
{
    [Fact(DisplayName = "The default converter should round to the default scale")]
    public void Default_ShouldRoundToDefaultScale()
    {
        // Act
        var rounded = (decimal)DecimalValueConverter.Instance.ConvertToProvider(1.23456789m)!;

        // Assert
        rounded.Should().Be(1.234568m);
    }

    [Fact(DisplayName = "A custom converter should expose and honour its precision and scale")]
    public void Constructor_WhenCustomPrecisionAndScale_ShouldUseTheScale()
    {
        // Act
        var converter = new DecimalValueConverter(10, 2);
        var rounded = (decimal)converter.ConvertToProvider(1.239m)!;

        // Assert
        converter.Precision.Should().Be(10);
        converter.Scale.Should().Be(2);
        rounded.Should().Be(1.24m);
    }

    [Theory(DisplayName = "The converter should reject an invalid precision or scale")]
    [InlineData(0, 2)]
    [InlineData(-1, 2)]
    [InlineData(4, 5)]
    [InlineData(4, -1)]
    public void Constructor_WhenPrecisionOrScaleIsInvalid_ShouldThrow(int precision, int scale)
    {
        // Act
        Action act = () => _ = new DecimalValueConverter(precision, scale);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
