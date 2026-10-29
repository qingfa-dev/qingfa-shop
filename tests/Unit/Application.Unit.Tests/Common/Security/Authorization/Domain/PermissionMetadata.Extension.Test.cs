using Application.Common.Security.Authorization.Domain;

namespace Application.Unit.Tests.Common.Security.Authorization.Domain;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authorization")]
public sealed class PermissionMetadataExtensionTest
{
    private static PermissionMetadata CreatePermission() =>
        PermissionMetadata.Create("catalog", "products", "orders", "read");

    [Fact(DisplayName = "WithName should return a trimmed copy")]
    public void WithName_ShouldReturnTrimmedCopy()
    {
        PermissionMetadata original = CreatePermission();

        PermissionMetadata updated = original.WithName("  Read orders  ");

        updated.Name.Should().Be("Read orders");
        updated.Identifier.Should().Be(original.Identifier);
        original.Name.Should().Be("read orders");
    }

    [Fact(DisplayName = "WithDescription should return a trimmed copy")]
    public void WithDescription_ShouldReturnTrimmedCopy()
    {
        PermissionMetadata original = CreatePermission();

        PermissionMetadata updated = original.WithDescription("  Allows reading orders.  ");

        updated.Description.Should().Be("Allows reading orders.");
        original.Description.Should().Be("Allows read on orders in catalog/products.");
    }

    [Fact(DisplayName = "WithExample should set and clear the example")]
    public void WithExample_ShouldSetAndClearExample()
    {
        PermissionMetadata original = CreatePermission();
        var example = new { Id = 1 };

        PermissionMetadata updated = original.WithExample(example);

        updated.Example.Should().BeSameAs(example);
        updated.WithExample(null).Example.Should().BeNull();
        original.Example.Should().BeNull();
    }

    [Fact(DisplayName = "WithName should throw when the name is blank")]
    public void WithName_ShouldThrow_WhenBlank()
    {
        PermissionMetadata permission = CreatePermission();

        Action act = () => permission.WithName("   ");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact(DisplayName = "WithName should throw when the name is too long")]
    public void WithName_ShouldThrow_WhenTooLong()
    {
        PermissionMetadata permission = CreatePermission();
        string name = new('a', PermissionMetadataConstant.Constraints.MaxNameLength + 1);

        Action act = () => permission.WithName(name);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact(DisplayName = "WithDescription should throw when the description is blank")]
    public void WithDescription_ShouldThrow_WhenBlank()
    {
        PermissionMetadata permission = CreatePermission();

        Action act = () => permission.WithDescription("   ");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("description");
    }

    [Fact(DisplayName = "WithDescription should throw when the description is too long")]
    public void WithDescription_ShouldThrow_WhenTooLong()
    {
        PermissionMetadata permission = CreatePermission();
        string description = new('a', PermissionMetadataConstant.Constraints.MaxDescriptionLength + 1);

        Action act = () => permission.WithDescription(description);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("description");
    }

    [Fact(DisplayName = "TryWithName should return the updated metadata")]
    public void TryWithName_ShouldReturnUpdatedMetadata()
    {
        Result<PermissionMetadata> result = CreatePermission().TryWithName("  Read orders  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Read orders");
    }

    [Fact(DisplayName = "TryWithName should return a required error for blank input")]
    public void TryWithName_ShouldReturnRequiredError()
    {
        Result<PermissionMetadata> result = CreatePermission().TryWithName("   ");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Permission.Name.Required");
    }

    [Fact(DisplayName = "TryWithName should return a length error")]
    public void TryWithName_ShouldReturnLengthError()
    {
        string name = new('a', PermissionMetadataConstant.Constraints.MaxNameLength + 1);

        Result<PermissionMetadata> result = CreatePermission().TryWithName(name);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Permission.Name.TooLong");
    }

    [Fact(DisplayName = "TryWithDescription should return the updated metadata")]
    public void TryWithDescription_ShouldReturnUpdatedMetadata()
    {
        Result<PermissionMetadata> result = CreatePermission().TryWithDescription("  Allows reading orders.  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().Be("Allows reading orders.");
    }

    [Fact(DisplayName = "TryWithDescription should return a required error for blank input")]
    public void TryWithDescription_ShouldReturnRequiredError()
    {
        Result<PermissionMetadata> result = CreatePermission().TryWithDescription("   ");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Permission.Description.Required");
    }

    [Fact(DisplayName = "TryWithDescription should return a length error")]
    public void TryWithDescription_ShouldReturnLengthError()
    {
        string description = new('a', PermissionMetadataConstant.Constraints.MaxDescriptionLength + 1);

        Result<PermissionMetadata> result = CreatePermission().TryWithDescription(description);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Permission.Description.TooLong");
    }

    [Fact(DisplayName = "Customization helpers should reject a null metadata instance")]
    public void CustomizationHelpers_ShouldRejectNullMetadata()
    {
        PermissionMetadata? permission = null;

        Action withExample = () => permission!.WithExample(null);
        Action tryWithName = () => permission!.TryWithName("name");
        Action tryWithDescription = () => permission!.TryWithDescription("description");

        withExample.Should().Throw<ArgumentNullException>();
        tryWithName.Should().Throw<ArgumentNullException>();
        tryWithDescription.Should().Throw<ArgumentNullException>();
    }
}
