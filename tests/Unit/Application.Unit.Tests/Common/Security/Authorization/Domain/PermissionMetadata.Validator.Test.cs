using Application.Common.Security.Authorization.Domain;

using FluentValidation.Results;

namespace Application.Unit.Tests.Common.Security.Authorization.Domain;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authorization")]
public sealed class PermissionMetadataValidatorTest
{
    private readonly PermissionMetadataValidator _validator = new();

    [Fact(DisplayName = "Validator should accept canonical metadata")]
    public void Validate_ShouldAcceptCanonicalMetadata()
    {
        var metadata = new PermissionMetadata("catalog", "products", "orders", "read");

        ValidationResult result = _validator.Validate(metadata);

        result.IsValid.Should().BeTrue();
    }

    [Theory(DisplayName = "Validator should report a required part")]
    [InlineData("", "products", "orders", "read", "Permission.Domain.Required")]
    [InlineData("catalog", "", "orders", "read", "Permission.Category.Required")]
    [InlineData("catalog", "products", "", "read", "Permission.Resource.Required")]
    [InlineData("catalog", "products", "orders", "", "Permission.Action.Required")]
    public void Validate_ShouldReportRequiredPart(
        string domain,
        string category,
        string resource,
        string action,
        string expectedCode)
    {
        var metadata = new PermissionMetadata(domain, category, resource, action);

        ValidationResult result = _validator.Validate(metadata);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.ErrorCode == expectedCode);
    }

    [Theory(DisplayName = "Validator should report invalid characters in a part")]
    [InlineData("bad domain", "products", "orders", "read", "Permission.Domain.InvalidChars")]
    [InlineData("catalog", "products.orders", "orders", "read", "Permission.Category.InvalidChars")]
    [InlineData("catalog", "products", "orders!", "read", "Permission.Resource.InvalidChars")]
    [InlineData("catalog", "products", "orders", "read/write", "Permission.Action.InvalidChars")]
    public void Validate_ShouldReportInvalidCharacters(
        string domain,
        string category,
        string resource,
        string action,
        string expectedCode)
    {
        var metadata = new PermissionMetadata(domain, category, resource, action);

        ValidationResult result = _validator.Validate(metadata);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.ErrorCode == expectedCode);
    }

    [Fact(DisplayName = "Validator should accept parts at the maximum length")]
    public void Validate_ShouldAcceptPartAtMaximumLength()
    {
        string part = new('a', PermissionMetadataConstant.Constraints.MaxPartLength);
        var metadata = new PermissionMetadata(part, "products", "orders", "read");

        ValidationResult result = _validator.Validate(metadata);

        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "Validator should report a part beyond the maximum length")]
    public void Validate_ShouldReportPartTooLong()
    {
        string part = new('a', PermissionMetadataConstant.Constraints.MaxPartLength + 1);
        var metadata = new PermissionMetadata(part, "products", "orders", "read");

        ValidationResult result = _validator.Validate(metadata);

        result.Errors.Should().Contain(failure =>
            failure.ErrorCode == "Permission.Domain.TooLong");
        result.Errors.Should().Contain(failure =>
            failure.ErrorMessage == PermissionMetadataResult.Failure.PartTooLong("Domain", part.Length).Message);
    }

    [Fact(DisplayName = "Validator should accept hyphen and underscore inside parts")]
    public void Validate_ShouldAcceptHyphenAndUnderscore()
    {
        var metadata = new PermissionMetadata("catalog-v2", "product_groups", "orders", "read");

        ValidationResult result = _validator.Validate(metadata);

        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "Validator should accept names and descriptions at their limits")]
    public void Validate_ShouldAcceptDisplayTextAtLimits()
    {
        var metadata = new PermissionMetadata(
            "catalog",
            "products",
            "orders",
            "read",
            name: new string('a', PermissionMetadataConstant.Constraints.MaxNameLength),
            description: new string('b', PermissionMetadataConstant.Constraints.MaxDescriptionLength));

        ValidationResult result = _validator.Validate(metadata);

        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "Validator should report display text beyond its limits")]
    public void Validate_ShouldReportDisplayTextTooLong()
    {
        var metadata = new PermissionMetadata(
            "catalog",
            "products",
            "orders",
            "read",
            name: new string('a', PermissionMetadataConstant.Constraints.MaxNameLength + 1),
            description: new string('b', PermissionMetadataConstant.Constraints.MaxDescriptionLength + 1));

        ValidationResult result = _validator.Validate(metadata);

        result.Errors.Should().Contain(failure =>
            failure.ErrorCode == "Permission.Name.TooLong");
        result.Errors.Should().Contain(failure =>
            failure.ErrorCode == "Permission.Description.TooLong");
    }

    [Fact(DisplayName = "Validator should report a malformed explicit identifier")]
    public void Validate_ShouldReportMalformedIdentifier()
    {
        var metadata = new PermissionMetadata(
            "catalog",
            "products",
            "orders",
            "read",
            identifier: "catalog.products.read");

        ValidationResult result = _validator.Validate(metadata);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure =>
            failure.ErrorCode == "Permission.Identifier.InvalidFormat");
    }

    [Fact(DisplayName = "Validator should report an identifier beyond the maximum length")]
    public void Validate_ShouldReportIdentifierTooLong()
    {
        var metadata = new PermissionMetadata(
            "catalog",
            "products",
            "orders",
            "read",
            identifier: new string('a', PermissionMetadataConstant.Constraints.MaxIdentifierLength + 1));

        ValidationResult result = _validator.Validate(metadata);

        result.Errors.Should().Contain(failure =>
            failure.ErrorCode == "Permission.Identifier.TooLong");
    }

    [Fact(DisplayName = "Validator should skip display text length rules when the text is empty")]
    public void Validate_ShouldSkipDisplayTextRules_WhenTextIsEmpty()
    {
        var metadata = new PermissionMetadata("catalog", "products", "orders", "read") with
        {
            Name = string.Empty,
            Description = string.Empty
        };

        ValidationResult result = _validator.Validate(metadata);

        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "Validator messages should match the shared failure results")]
    public void Validate_ShouldReuseFailureMessages()
    {
        var metadata = new PermissionMetadata("", "products", "orders", "read");

        ValidationResult result = _validator.Validate(metadata);

        result.Errors.Should().Contain(failure =>
            failure.ErrorCode == "Permission.Domain.Required" &&
            failure.ErrorMessage == PermissionMetadataResult.Failure.PartRequired("Domain").Message);
    }
}
