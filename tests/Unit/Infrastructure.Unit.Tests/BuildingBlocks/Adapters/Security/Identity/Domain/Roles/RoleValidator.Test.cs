using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Identity.Domain.Roles;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Roles")]
public sealed class RoleValidatorTest
{
    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact(DisplayName = "Validate should reject a missing role")]
    public void Validate_ShouldRejectMissingRole()
    {
        ShouldFailWith(RoleValidator.Validate(null), "Role.Entity.Required");
    }

    [Fact(DisplayName = "Validate should accept a valid role")]
    public void Validate_ShouldAcceptValidRole()
    {
        Role role = Role.Create("Operator", "Operations").Value;

        RoleValidator.Validate(role).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateName should enforce required and length rules")]
    public void ValidateName_ShouldEnforceRules()
    {
        ShouldFailWith(RoleValidator.ValidateName(""), "Role.Name.Required");
        ShouldFailWith(
            RoleValidator.ValidateName(new string('a', RoleConstant.Constraints.Name.MaxLength + 1)),
            "Role.Name.TooLong");
        RoleValidator.ValidateName("Operator").IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateDescription should enforce the maximum length")]
    public void ValidateDescription_ShouldEnforceLength()
    {
        ShouldFailWith(
            RoleValidator.ValidateDescription(
                new string('a', RoleConstant.Constraints.Description.MaxLength + 1)),
            "Role.Description.TooLong");
        RoleValidator.ValidateDescription(null).IsSuccess.Should().BeTrue();
        RoleValidator.ValidateDescription("Operations").IsSuccess.Should().BeTrue();
    }
}
