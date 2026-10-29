using Application.Common.Security.Authorization.Domain;

using FluentValidation;

namespace Application.Unit.Tests.Common.Security.Authorization.Domain;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authorization")]
public sealed class PermissionMetadataTest
{
    [Fact(DisplayName = "Create should normalize parts and generate display defaults")]
    public void Create_ShouldNormalizePartsAndGenerateDefaults()
    {
        PermissionMetadata permission = PermissionMetadata.Create(
            " Catalog ",
            "Products",
            "Orders",
            "Read");

        permission.Domain.Should().Be("catalog");
        permission.Category.Should().Be("products");
        permission.Resource.Should().Be("orders");
        permission.Action.Should().Be("read");
        permission.Identifier.Should().Be("catalog.products.orders.read");
        permission.Name.Should().Be("read orders");
        permission.Description.Should().Be("Allows read on orders in catalog/products.");
        permission.Example.Should().BeNull();
    }

    [Fact(DisplayName = "Create should preserve and trim explicit text and example")]
    public void Create_ShouldPreserveExplicitTextAndExample()
    {
        var example = new { Resource = "orders" };

        PermissionMetadata permission = PermissionMetadata.Create(
            "catalog",
            "products",
            "orders",
            "read",
            name: "  Read orders  ",
            description: "  Allows reading orders.  ",
            example: example);

        permission.Name.Should().Be("Read orders");
        permission.Description.Should().Be("Allows reading orders.");
        permission.Example.Should().BeSameAs(example);
    }

    [Fact(DisplayName = "Create should normalize an explicit identifier")]
    public void Create_ShouldNormalizeExplicitIdentifier()
    {
        PermissionMetadata permission = PermissionMetadata.Create(
            "catalog",
            "products",
            "orders",
            "read",
            identifier: " Catalog.Products.Orders.Read ");

        permission.Identifier.Should().Be("catalog.products.orders.read");
    }

    [Theory(DisplayName = "Create should throw when a part is invalid")]
    [InlineData("", "products", "orders", "read")]
    [InlineData("catalog", "products orders", "orders", "read")]
    [InlineData("catalog", "products", "orders!", "read")]
    public void Create_ShouldThrow_WhenPartIsInvalid(
        string domain,
        string category,
        string resource,
        string action)
    {
        Action act = () => PermissionMetadata.Create(domain, category, resource, action);

        act.Should().Throw<ValidationException>();
    }

    [Fact(DisplayName = "Create should throw when a part exceeds the maximum length")]
    public void Create_ShouldThrow_WhenPartIsTooLong()
    {
        string part = new('a', PermissionMetadataConstant.Constraints.MaxPartLength + 1);

        Action act = () => PermissionMetadata.Create(part, "products", "orders", "read");

        act.Should().Throw<ValidationException>();
    }

    [Fact(DisplayName = "Create should throw when the display name is too long")]
    public void Create_ShouldThrow_WhenNameIsTooLong()
    {
        string name = new('a', PermissionMetadataConstant.Constraints.MaxNameLength + 1);

        Action act = () => PermissionMetadata.Create(
            "catalog",
            "products",
            "orders",
            "read",
            name: name);

        act.Should().Throw<ValidationException>();
    }

    [Fact(DisplayName = "Create should throw when the description is too long")]
    public void Create_ShouldThrow_WhenDescriptionIsTooLong()
    {
        string description = new('a', PermissionMetadataConstant.Constraints.MaxDescriptionLength + 1);

        Action act = () => PermissionMetadata.Create(
            "catalog",
            "products",
            "orders",
            "read",
            description: description);

        act.Should().Throw<ValidationException>();
    }

    [Fact(DisplayName = "Create should throw when an explicit identifier is malformed")]
    public void Create_ShouldThrow_WhenIdentifierIsMalformed()
    {
        Action act = () => PermissionMetadata.Create(
            "catalog",
            "products",
            "orders",
            "read",
            identifier: "catalog.products.read");

        act.Should().Throw<ValidationException>();
    }

    [Fact(DisplayName = "TryCreate should return validated metadata")]
    public void TryCreate_ShouldReturnValidatedMetadata()
    {
        Result<PermissionMetadata> result = PermissionMetadata.TryCreate(
            "catalog",
            "products",
            "orders",
            "read");

        result.IsSuccess.Should().BeTrue();
        result.Value.Identifier.Should().Be("catalog.products.orders.read");
    }

    [Fact(DisplayName = "TryCreate should return every validation error")]
    public void TryCreate_ShouldReturnEveryValidationError()
    {
        string name = new('a', PermissionMetadataConstant.Constraints.MaxNameLength + 1);
        string description = new('b', PermissionMetadataConstant.Constraints.MaxDescriptionLength + 1);

        Result<PermissionMetadata> result = PermissionMetadata.TryCreate(
            "",
            "products orders",
            "orders",
            "read",
            name: name,
            description: description,
            identifier: "bad");

        result.IsFailure.Should().BeTrue();
        string[] codes = [.. result.Errors.Select(error => error.Code)];

        codes.Should().Contain("Permission.Domain.Required");
        codes.Should().Contain("Permission.Category.InvalidChars");
        codes.Should().Contain("Permission.Name.TooLong");
        codes.Should().Contain("Permission.Description.TooLong");
        codes.Should().Contain("Permission.Identifier.InvalidFormat");
    }

    [Fact(DisplayName = "ToString and the implicit conversion should return the identifier")]
    public void ToStringAndImplicitConversion_ShouldReturnIdentifier()
    {
        PermissionMetadata permission = PermissionMetadata.Create(
            "catalog",
            "products",
            "orders",
            "read");

        permission.ToString().Should().Be("catalog.products.orders.read");

        string identifier = permission;
        identifier.Should().Be("catalog.products.orders.read");
    }

    [Fact(DisplayName = "Record equality should compare the full metadata shape")]
    public void Equality_ShouldCompareFullMetadataShape()
    {
        PermissionMetadata left = PermissionMetadata.Create(
            "catalog",
            "products",
            "orders",
            "read");
        PermissionMetadata right = PermissionMetadata.Create(
            "catalog",
            "products",
            "orders",
            "read");

        left.Should().Be(right);
        left.GetHashCode().Should().Be(right.GetHashCode());
    }
}
