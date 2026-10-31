using System.Text.RegularExpressions;

using Application.BuildingBlocks.Security.Authorization.Domain;

namespace Application.Unit.Tests.BuildingBlocks.Security.Authorization.Domain;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authorization")]
public sealed class PermissionMetadataConstantTest
{
    [Fact(DisplayName = "Constants should expose the documented values")]
    public void Constants_ShouldExposeDocumentedValues()
    {
        PermissionMetadataConstant.ClaimType.Should().Be("permission");
        PermissionMetadataConstant.Constraints.MaxPartLength.Should().Be(50);
        PermissionMetadataConstant.Constraints.MaxIdentifierLength.Should().Be(255);
        PermissionMetadataConstant.Constraints.MaxNameLength.Should().Be(100);
        PermissionMetadataConstant.Constraints.MaxDescriptionLength.Should().Be(500);
        PermissionMetadataConstant.Constraints.AllowedPartChars.Should()
            .Be("abcdefghijklmnopqrstuvwxyz0123456789_-");
    }

    [Theory(DisplayName = "NormalizePart should trim and lowercase text")]
    [InlineData(" Catalog ", "catalog")]
    [InlineData("READ", "read")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void NormalizePart_ShouldNormalizeText(string? value, string expected)
    {
        PermissionMetadataConstant.NormalizePart(value).Should().Be(expected);
    }

    [Theory(DisplayName = "NormalizeIdentifier should trim and lowercase text")]
    [InlineData(" Catalog.Products.Orders.Read ", "catalog.products.orders.read")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void NormalizeIdentifier_ShouldNormalizeText(string? value, string expected)
    {
        PermissionMetadataConstant.NormalizeIdentifier(value).Should().Be(expected);
    }

    [Fact(DisplayName = "Compose should build the canonical identifier, name, and description")]
    public void Compose_ShouldBuildCanonicalValues()
    {
        PermissionMetadataConstant.Compose.Identifier("Catalog", "Products", "Orders", "Read")
            .Should().Be("catalog.products.orders.read");

        PermissionMetadataConstant.Compose.DefaultName("read", "orders")
            .Should().Be("read orders");

        PermissionMetadataConstant.Compose.DefaultDescription("catalog", "products", "orders", "read")
            .Should().Be("Allows read on orders in catalog/products.");
    }

    [Fact(DisplayName = "Part pattern should enforce the canonical part shape")]
    public void PartPattern_ShouldEnforceCanonicalShape()
    {
        string maximum = new('a', PermissionMetadataConstant.Constraints.MaxPartLength);

        Regex.IsMatch(maximum, PermissionMetadataConstant.Patterns.Part).Should().BeTrue();
        Regex.IsMatch("catalog-v2", PermissionMetadataConstant.Patterns.Part).Should().BeTrue();
        Regex.IsMatch("product_groups", PermissionMetadataConstant.Patterns.Part).Should().BeTrue();
        Regex.IsMatch(maximum + "a", PermissionMetadataConstant.Patterns.Part).Should().BeFalse();
        Regex.IsMatch("Catalog", PermissionMetadataConstant.Patterns.Part).Should().BeFalse();
        Regex.IsMatch("", PermissionMetadataConstant.Patterns.Part).Should().BeFalse();
    }

    [Fact(DisplayName = "Identifier pattern should enforce four canonical parts")]
    public void IdentifierPattern_ShouldEnforceFourCanonicalParts()
    {
        Regex.IsMatch(
            "catalog.products.orders.read",
            PermissionMetadataConstant.Patterns.Identifier).Should().BeTrue();
        Regex.IsMatch(
            "catalog.products.read",
            PermissionMetadataConstant.Patterns.Identifier).Should().BeFalse();
        Regex.IsMatch(
            "catalog.products.orders.read.extra",
            PermissionMetadataConstant.Patterns.Identifier).Should().BeFalse();
    }

    [Theory(DisplayName = "IsValidIdentifier should accept canonical identifiers")]
    [InlineData("catalog.products.orders.read")]
    [InlineData(" Catalog.Products.Orders.Read ")]
    [InlineData("catalog-v2.product_groups.orders.read")]
    public void IsValidIdentifier_ShouldAcceptCanonicalIdentifiers(string identifier)
    {
        PermissionMetadataConstant.IsValidIdentifier(identifier).Should().BeTrue();
    }

    [Theory(DisplayName = "IsValidIdentifier should reject malformed identifiers")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("catalog.products.read")]
    [InlineData("catalog.products.orders.read.extra")]
    [InlineData("catalog.products.orders.read!")]
    [InlineData("catalog..orders.read")]
    public void IsValidIdentifier_ShouldRejectMalformedIdentifiers(string? identifier)
    {
        PermissionMetadataConstant.IsValidIdentifier(identifier).Should().BeFalse();
    }

    [Fact(DisplayName = "IsValidIdentifier should reject an overlong part or identifier")]
    public void IsValidIdentifier_ShouldRejectOverlongValues()
    {
        string part = new('a', PermissionMetadataConstant.Constraints.MaxPartLength + 1);
        string identifier = new('a', PermissionMetadataConstant.Constraints.MaxIdentifierLength + 1);

        PermissionMetadataConstant.IsValidIdentifier($"catalog.products.orders.{part}")
            .Should().BeFalse();
        PermissionMetadataConstant.IsValidIdentifier(identifier)
            .Should().BeFalse();
    }

    [Fact(DisplayName = "The cached matcher should match the identifier pattern")]
    public void IdentifierPattern_ShouldBeCached()
    {
        PermissionMetadataConstant.IdentifierPattern.IsMatch("catalog.products.orders.read")
            .Should().BeTrue();
    }
}
