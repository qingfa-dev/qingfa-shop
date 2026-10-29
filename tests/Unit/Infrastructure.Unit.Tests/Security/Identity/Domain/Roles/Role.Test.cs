using Infrastructure.Security.Identity.Domain.Roles;
using Infrastructure.Security.Identity.Domain.Users.Roles;

namespace Infrastructure.Unit.Tests.Security.Identity.Domain.Roles;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Roles")]
public sealed class RoleTest
{
    private static Role CreateRole() => Role.Create("Operator", "Operations role").Value;

    [Fact(DisplayName = "Create should initialize an auditable role and raise RoleCreated")]
    public void Create_ShouldInitializeRole()
    {
        Result<Role> result = Role.Create("Operator", "Operations role");

        result.IsSuccess.Should().BeTrue();
        Role role = result.Value;
        role.Name.Should().Be("Operator");
        role.Description.Should().Be("Operations role");
        role.IsSystem.Should().BeFalse();
        role.CreatedAtUtc.Should().NotBe(default);
        role.CreatedBy.Should().Be("System");
        role.Events.Should().ContainSingle().Which.Should().BeOfType<RoleCreated>();
    }

    [Theory(DisplayName = "Create should reject invalid role input")]
    [InlineData("", "Role.Name.Required")]
    [InlineData("   ", "Role.Name.Required")]
    public void Create_ShouldRejectMissingName(string name, string expectedCode)
    {
        Result<Role> result = Role.Create(name);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact(DisplayName = "Create should reject an overlong name")]
    public void Create_ShouldRejectLongName()
    {
        string name = new('a', RoleConstant.Constraints.Name.MaxLength + 1);

        Result<Role> result = Role.Create(name);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("Role.Name.TooLong");
    }

    [Fact(DisplayName = "Create should reject an overlong description")]
    public void Create_ShouldRejectLongDescription()
    {
        string description = new('a', RoleConstant.Constraints.Description.MaxLength + 1);

        Result<Role> result = Role.Create("Operator", description);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("Role.Description.TooLong");
    }

    [Fact(DisplayName = "Update should change a mutable role and raise RoleUpdated")]
    public void Update_ShouldChangeMutableRole()
    {
        Role role = CreateRole();

        Result<Role> result = Result<Role>.Success(role).Update("Supervisor", "Supervision role");

        result.IsSuccess.Should().BeTrue();
        role.Name.Should().Be("Supervisor");
        role.Description.Should().Be("Supervision role");
        role.ModifiedAtUtc.Should().NotBeNull();
        role.ModifiedBy.Should().Be("System");
        role.Events.OfType<RoleUpdated>().Should().ContainSingle();
    }

    [Fact(DisplayName = "Update should protect system roles")]
    public void Update_ShouldProtectSystemRole()
    {
        Role role = CreateRole();
        role.IsSystem = true;

        Result<Role> result = Result<Role>.Success(role).Update("Supervisor");

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("Role.System.Protected");
    }

    [Fact(DisplayName = "Delete should protect system roles and roles with users")]
    public void Delete_ShouldProtectRestrictedRoles()
    {
        Role role = CreateRole();
        role.IsSystem = true;
        Result<Role>.Success(role).Delete().IsFailure.Should().BeTrue();

        role.IsSystem = false;
        role.UserRoles.Add(new UserRole());
        Result<Role>.Success(role).Delete().Errors[0].Code
            .Should().Be("Role.CannotDeleteRoleWithUsers");

        role.UserRoles.Clear();
        Result<Role>.Success(role).Delete().IsSuccess.Should().BeTrue();
        role.Events.OfType<RoleDeleted>().Should().ContainSingle();
    }
}
