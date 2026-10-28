using Application.Common.Security.Authentication.Contexts;

using Core.Common.Concerns.Foundation.Tenantable;

namespace Application.Unit.Tests.Common.Security.Authentication.Contexts;

[Trait("Category", "Unit")]
public sealed class TenantContextExtensionTest
{
    [Fact(DisplayName = "RequireTenant should succeed when TenantId is set")]
    public void RequireTenant_ShouldSucceed_WhenTenantIdIsSet()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var context = new Mock<ITenantContext>();
        context.Setup(c => c.TenantId).Returns(tenantId);

        // Act
        Result<Guid> result = context.Object.RequireTenant();

        // Assert
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(tenantId);
    }

    [Fact(DisplayName = "RequireTenant should fail when TenantId is empty")]
    public void RequireTenant_ShouldFail_WhenTenantIdIsEmpty()
    {
        // Arrange
        var context = new Mock<ITenantContext>();
        context.Setup(c => c.TenantId).Returns(Guid.Empty);

        // Act
        Result<Guid> result = context.Object.RequireTenant();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Should().Be(TenantableOutcome.Failure.TenantIdRequired);
    }

    [Fact(DisplayName = "RequireTenant should throw on null context")]
    public void RequireTenant_ShouldThrow_WhenContextIsNull()
    {
        // Arrange
        ITenantContext? context = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => context!.RequireTenant());
    }
}
