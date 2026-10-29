using Application.Common.Security.Authorization.Domain;

namespace Application.Unit.Tests.Common.Security.Authorization.Domain;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authorization")]
public sealed class PermissionMetadataParserTest
{
    [Fact(DisplayName = "Parse should return metadata for a canonical identifier")]
    public void Parse_ShouldReturnMetadata_ForCanonicalIdentifier()
    {
        PermissionMetadata permission = PermissionMetadata.Parse("catalog.products.orders.read");

        permission.Domain.Should().Be("catalog");
        permission.Category.Should().Be("products");
        permission.Resource.Should().Be("orders");
        permission.Action.Should().Be("read");
        permission.Identifier.Should().Be("catalog.products.orders.read");
    }

    [Fact(DisplayName = "Parse should normalize surrounding whitespace and casing")]
    public void Parse_ShouldNormalizeWhitespaceAndCasing()
    {
        PermissionMetadata permission = PermissionMetadata.Parse(" Catalog.Products.Orders.Read ");

        permission.Identifier.Should().Be("catalog.products.orders.read");
    }

    [Theory(DisplayName = "Parse should throw FormatException for a malformed identifier")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("catalog.products.read")]
    [InlineData("catalog.products.orders.read.extra")]
    [InlineData("catalog.products.orders.read!")]
    public void Parse_ShouldThrow_ForMalformedIdentifier(string identifier)
    {
        Action act = () => PermissionMetadata.Parse(identifier);

        act.Should().Throw<FormatException>()
            .WithMessage("*Invalid permission identifier format*");
    }

    [Fact(DisplayName = "Parse should throw FormatException when a part exceeds the maximum length")]
    public void Parse_ShouldThrow_WhenPartIsTooLong()
    {
        string part = new('a', PermissionMetadataConstant.Constraints.MaxPartLength + 1);
        string identifier = $"catalog.products.orders.{part}";

        Action act = () => PermissionMetadata.Parse(identifier);

        act.Should().Throw<FormatException>()
            .WithMessage("*Invalid permission identifier format*");
    }

    [Fact(DisplayName = "TryParse should return validated metadata")]
    public void TryParse_ShouldReturnValidatedMetadata()
    {
        Result<PermissionMetadata> result = PermissionMetadata.TryParse("catalog.products.orders.read");

        result.IsSuccess.Should().BeTrue();
        result.Value.Identifier.Should().Be("catalog.products.orders.read");
    }

    [Fact(DisplayName = "TryParse should return an identifier error for malformed input")]
    public void TryParse_ShouldReturnIdentifierError_ForMalformedInput()
    {
        Result<PermissionMetadata> result = PermissionMetadata.TryParse("catalog.products.read");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Permission.Identifier.InvalidFormat");
    }

    [Fact(DisplayName = "TryParse should return an identifier error for null input")]
    public void TryParse_ShouldReturnIdentifierError_ForNullInput()
    {
        string? identifier = null;

        Result<PermissionMetadata> result = PermissionMetadata.TryParse(identifier!);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Permission.Identifier.InvalidFormat");
    }

    [Fact(DisplayName = "TryParse out overload should return true and the metadata")]
    public void TryParseOut_ShouldReturnTrue_WhenValid()
    {
        bool parsed = PermissionMetadata.TryParse("catalog.products.orders.read", out PermissionMetadata? permission);

        parsed.Should().BeTrue();
        permission.Should().NotBeNull();
        permission!.Identifier.Should().Be("catalog.products.orders.read");
    }

    [Fact(DisplayName = "TryParse out overload should return false and null")]
    public void TryParseOut_ShouldReturnFalse_WhenInvalid()
    {
        bool parsed = PermissionMetadata.TryParse("catalog.products.read", out PermissionMetadata? permission);

        parsed.Should().BeFalse();
        permission.Should().BeNull();
    }
}
