using Application.Common.Security.Authorization;
using Application.Common.Security.Authorization.Policies;

namespace Application.Unit.Tests.Common.Security.Authorization.Policies;

[Trait("Category", "Unit")]
[Trait("Module", "Authorization")]
[Trait("Feature", "PermissionPolicyParser")]
public sealed class PermissionPolicyParserTests
{
    [Fact(DisplayName = "Compose: a single permission keeps its canonical identifier")]
    public void Compose_WithSinglePermission_ShouldReturnIdentifier()
    {
        string policy = PermissionPolicyParser.Compose(
            ["catalog.products.product.publish"],
            PermissionMatchMode.Any);

        policy.Should().Be("catalog.products.product.publish");
    }

    [Fact(DisplayName = "Compose: multiple permissions should encode the mode and identifiers")]
    public void Compose_WithMultiplePermissions_ShouldEncode()
    {
        PermissionPolicyParser.Compose(
                ["catalog.products.product.publish", "catalog.products.product.retire"],
                PermissionMatchMode.All)
            .Should().Be("permission:all:catalog.products.product.publish|catalog.products.product.retire");

        PermissionPolicyParser.Compose(
                ["catalog.products.product.publish", "catalog.products.product.retire"],
                PermissionMatchMode.Any)
            .Should().Be("permission:any:catalog.products.product.publish|catalog.products.product.retire");
    }

    [Fact(DisplayName = "Compose: should normalize, de-duplicate, and drop blanks")]
    public void Compose_ShouldNormalizeAndDistinct()
    {
        string policy = PermissionPolicyParser.Compose(
            [" Catalog.Products.Product.Publish ", "catalog.products.product.publish", "   "],
            PermissionMatchMode.All);

        policy.Should().Be("catalog.products.product.publish");
    }

    [Fact(DisplayName = "Compose: no permissions should throw")]
    public void Compose_WithNoPermissions_ShouldThrow()
    {
        Action empty = () => PermissionPolicyParser.Compose([], PermissionMatchMode.All);
        Action blanks = () => PermissionPolicyParser.Compose(["  "], PermissionMatchMode.All);
        Action missing = () => PermissionPolicyParser.Compose(null!, PermissionMatchMode.All);

        empty.Should().Throw<ArgumentException>();
        blanks.Should().Throw<ArgumentException>();
        missing.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Compose: malformed identifiers should throw")]
    public void Compose_WithMalformedIdentifier_ShouldThrow()
    {
        Action action = () => PermissionPolicyParser.Compose(
            ["not-a-permission"],
            PermissionMatchMode.All);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*not-a-permission*");
    }

    [Fact(DisplayName = "TryCompose: should return a stable failure for no permissions")]
    public void TryCompose_WithNoPermissions_ShouldFail()
    {
        Result<string> result = PermissionPolicyParser.TryCompose([], PermissionMatchMode.All);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(PermissionPolicyOutcome.Codes.NoPermissions);
    }

    [Fact(DisplayName = "TryCompose: should return a stable failure for a malformed identifier")]
    public void TryCompose_WithMalformedIdentifier_ShouldFail()
    {
        Result<string> result = PermissionPolicyParser.TryCompose(
            ["not-a-permission"],
            PermissionMatchMode.All);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(PermissionPolicyOutcome.Codes.InvalidIdentifier);
    }

    [Fact(DisplayName = "TryCompose: should encode multiple permissions without throwing")]
    public void TryCompose_WithMultiplePermissions_ShouldEncode()
    {
        Result<string> result = PermissionPolicyParser.TryCompose(
            ["catalog.products.product.publish", "catalog.products.product.retire"],
            PermissionMatchMode.Any);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().StartWith("permission:any:");
    }

    [Fact(DisplayName = "TryParse: should parse an ALL policy")]
    public void TryParse_All_ShouldSucceed()
    {
        bool parsed = PermissionPolicyParser.TryParse(
            "permission:all:catalog.products.product.publish|catalog.products.product.retire",
            out PermissionPolicyDefinition? definition);

        parsed.Should().BeTrue();
        definition!.MatchMode.Should().Be(PermissionMatchMode.All);
        definition.Permissions.Should().BeEquivalentTo(
            ["catalog.products.product.publish", "catalog.products.product.retire"]);
    }

    [Fact(DisplayName = "TryParse: should parse an ANY policy")]
    public void TryParse_Any_ShouldSucceed()
    {
        bool parsed = PermissionPolicyParser.TryParse(
            "permission:any:Catalog.Products.Product.Publish|catalog.products.product.retire",
            out PermissionPolicyDefinition? definition);

        parsed.Should().BeTrue();
        definition!.MatchMode.Should().Be(PermissionMatchMode.Any);
        definition.Permissions.Should().Contain("catalog.products.product.publish");
    }

    [Theory(DisplayName = "TryParse: malformed policies should fail")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("catalog.products.product.publish")]
    [InlineData("permission")]
    [InlineData("permission:all")]
    [InlineData("permission:all:")]
    [InlineData("permission:some:catalog.products.product.publish")]
    [InlineData("permission:all:not-a-permission")]
    [InlineData("permission:all:catalog.products.product.publish|bad")]
    public void TryParse_Malformed_ShouldFail(string? policyName)
    {
        bool parsed = PermissionPolicyParser.TryParse(policyName, out PermissionPolicyDefinition? definition);

        parsed.Should().BeFalse();
        definition.Should().BeNull();
    }
}
